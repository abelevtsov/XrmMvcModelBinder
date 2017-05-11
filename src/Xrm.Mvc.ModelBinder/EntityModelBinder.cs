using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Web.Mvc;

using Microsoft.Xrm.Sdk;
using Newtonsoft.Json;

namespace Xrm.Mvc.ModelBinder
{
    public class EntityModelBinder : IModelBinder
    {
        private const string Clear = "clear";

        private static readonly CultureInfo RuCulture = new CultureInfo("ru-RU");

        public object BindModel(ControllerContext controllerContext, ModelBindingContext bindingContext)
        {
            ViewProviderResultExtensions.Values.Clear();
            var provider = bindingContext.ValueProvider;
            var modelStateDictionary = bindingContext.ModelState;
            var type = bindingContext.ModelType;
            if (type.IsGenericType)
            {
                var typeArgs = type.GetGenericArguments();
                if (typeArgs.Length > 1)
                {
                    throw new NotSupportedException("Binder supports only generic types with single type argument.");
                }

                var typeArg = typeArgs.First();
                var listType = typeof(List<>); // override IEnumerable, ICollection and so on.. - use only List
                var genericType = listType.MakeGenericType(typeArgs);
                var entities = Activator.CreateInstance(genericType) as IList;
                if (entities != null)
                {
                    var typeName = typeArg.Name;

                    // get that property array, because all views contains this property (must have!)
                    // to determine count of entities in list
                    var propName = CreatePropertyModelName(typeName, "Id.0"); // special naming new format
                    var count = 0;
                    while (provider.GetValue(propName) != null)
                    {
                        count++;
                        propName = CreatePropertyModelName(typeName, $"Id.{count}");
                    }

                    var idPropertyName = CreatePropertyModelName(typeName, "Id");
                    var entitiesCount = provider.GetValue(idPropertyName).ToStringArray(idPropertyName).Length;
                    for (var i = 0; i < entitiesCount; i++)
                    {
                        entities.Add(GetFilledEntity(typeArg, provider, ref modelStateDictionary, i, entitiesCount));
                    }
                }

                return entities;
            }

            return GetFilledEntity(type, provider, ref modelStateDictionary);
        }

        private static string CreatePropertyModelName(string prefix, string propertyName)
        {
            if (string.IsNullOrEmpty(prefix))
            {
                return propertyName ?? string.Empty;
            }

            return string.IsNullOrEmpty(propertyName) ? prefix : $"{prefix}.{propertyName}";
        }

        private static object GetFilledEntity(Type type, IValueProvider provider, ref ModelStateDictionary modelState, int index = 0, int? count = null)
        {
            var typeName = type.Name;
            var properties = type.GetProperties();
            var entity = Activator.CreateInstance(type);

            // ((IEntity)entity).ChangedProperties.Clear();
            foreach (var pi in properties)
            {
                object checkedValue;
                if (CheckValue(pi, typeName, provider, index, modelState, out checkedValue, count))
                {
                    pi.SetValue(entity, checkedValue, null);
                }
            }

            return entity;
        }

        private static bool CheckValue(PropertyInfo pi, string typeName, IValueProvider provider, int index, ModelStateDictionary modelState, out object resolvedValue, int? count = null)
        {
            if (!pi.CanWrite)
            {
                resolvedValue = null;
                return false;
            }

            var propertyName = CreatePropertyModelName(typeName, pi.Name);
            var values = provider.GetValue(propertyName).ToStringArray(propertyName);
            string value;
            if (values.Length == 0)
            {
                resolvedValue = null;
                return false;
            }

            try
            {
                if (count.HasValue && values.Length > count)
                {
                    value = GetBoolValue(values, index);
                }
                else
                {
                    value = values[index];
                }
            }
            catch
            {
                resolvedValue = null;
                return false;
            }

            var valueType = pi.PropertyType;
            var customAttributes = Attribute.GetCustomAttributes(pi);
            if (valueType == typeof(string))
            {
                var range = customAttributes.OfType<RangeAttribute>()
                                            .Select(a => new { Minimum = (int)a.Minimum, Maximum = (int)a.Maximum })
                                            .FirstOrDefault();
                if (range != null &&
                    !string.IsNullOrEmpty(value) &&
                    (value.Length < range.Minimum || value.Length > range.Maximum))
                {
                    AddRangeError(provider, modelState, propertyName, pi);
                }
            }
            else if (valueType == typeof(int?))
            {
                var range = customAttributes.OfType<RangeAttribute>()
                                            .Select(a => new { Minimum = (int)a.Minimum, Maximum = (int)a.Maximum })
                                            .FirstOrDefault();
                if (range != null && !string.IsNullOrEmpty(value))
                {
                    int intValue;
                    if (int.TryParse(value, NumberStyles.Any, RuCulture, out intValue))
                    {
                        if (intValue < range.Minimum || intValue > range.Maximum)
                        {
                            AddRangeError(provider, modelState, propertyName, pi);
                        }
                    }
                    else
                    {
                        AddParseError(provider, modelState, propertyName, pi, true);
                    }
                }
            }
            else if (valueType == typeof(decimal?))
            {
                var range = customAttributes.OfType<RangeAttribute>()
                                            .Select(a => new { Minimum = decimal.Parse(a.Minimum.ToString()), Maximum = decimal.Parse(a.Maximum.ToString()) })
                                            .FirstOrDefault();
                if (range != null && !string.IsNullOrEmpty(value))
                {
                    decimal decValue;
                    if (decimal.TryParse(value.ReplaceSpecial(), NumberStyles.Any, RuCulture, out decValue))
                    {
                        if (decValue < range.Minimum || decValue > range.Maximum)
                        {
                            AddRangeError(provider, modelState, propertyName, pi);
                        }
                    }
                    else
                    {
                        AddParseError(provider, modelState, propertyName, pi, true);
                    }
                }
            }
            else if (valueType == typeof(double?))
            {
                var range = customAttributes.OfType<RangeAttribute>()
                                            .Select(a => new { Minimum = (double)a.Minimum, Maximum = (double)a.Maximum })
                                            .FirstOrDefault();
                if (range != null && !string.IsNullOrEmpty(value))
                {
                    double doubleValue;
                    if (double.TryParse(value.ReplaceSpecial(), NumberStyles.Any, RuCulture, out doubleValue))
                    {
                        if (doubleValue < range.Minimum || doubleValue > range.Maximum)
                        {
                            AddRangeError(provider, modelState, propertyName, pi);
                        }
                    }
                    else
                    {
                        AddParseError(provider, modelState, propertyName, pi, true);
                    }
                }
            }

            try
            {
                resolvedValue = ResolveValue(pi, value);
            }
            catch (Exception)
            {
                AddParseError(provider, modelState, propertyName, pi);
                resolvedValue = null;
                return false;
            }

            return true;
        }

        private static object ResolveValue(PropertyInfo pi, string value)
        {
            var type = pi.PropertyType;
            if (value.IsNull() || value == Clear)
            {
                value = null;
            }

            if (type == typeof(string))
            {
                return value;
            }

            if (type == typeof(DateTime?))
            {
                return value == null ? (DateTime?)null : DateTime.ParseExact(value, "dd.MM.yyyy", RuCulture).ToLocalTime();
            }

            if (type == typeof(OptionSetValue))
            {
                return value.IsNull() ? null : new OptionSetValue(int.Parse(value, NumberStyles.Any, RuCulture));
            }

            if (type == typeof(Guid?))
            {
                return value.IsNull() ? Guid.Empty : Guid.Parse(value);
            }

            if (type == typeof(int?))
            {
                return value.IsNull() ? (int?)null : int.Parse(value, NumberStyles.Any, RuCulture);
            }

            if (type == typeof(double?))
            {
                return value.IsNull() ? (double?)null : double.Parse(value.ReplaceSpecial(), NumberStyles.Any, RuCulture);
            }

            if (type == typeof(bool?))
            {
                return value.IsNull() ? (bool?)null : value.ToLowerInvariant() != bool.FalseString.ToLowerInvariant();
            }

            if (type == typeof(EntityReference))
            {
                var customAttributes = Attribute.GetCustomAttributes(pi);
                foreach (var targetEntityName in customAttributes.OfType<JsonConverterAttribute>().SelectMany(a => a.ConverterParameters).Cast<string>())
                {
                    return value.IsNull() ? null : new EntityReference(targetEntityName, Guid.Parse(value));
                }
            }

            if (type == typeof(Money))
            {
                return value.IsNull() ? new Money() : new Money(decimal.Parse(value.ReplaceSpecial(), NumberStyles.Any, RuCulture));
            }

            if (type == typeof(decimal?))
            {
                return value.IsNull() ? (decimal?)null : decimal.Parse(value.ReplaceSpecial(), NumberStyles.Any, RuCulture);
            }

            return null;
        }

        private static string GetBoolValue(IList<string> values, int index)
        {
            var result = new List<string>();
            var counter = 0;
            var count = values.Count;
            while (counter < count)
            {
                if (values[counter] == bool.FalseString.ToLowerInvariant())
                {
                    result.Add(values[counter]);
                    counter += 1;
                }
                else
                {
                    result.Add(values[counter]);
                    counter += 2;
                }
            }

            return result[index];
        }

        private static void AddParseError(IValueProvider provider, ModelStateDictionary modelState, string propertyName, PropertyInfo pi, bool isOverflow = false)
        {
            var name = GetHumanPropertyName(pi);
            name = string.IsNullOrEmpty(name) ? propertyName.Substring(propertyName.IndexOf('.') + 1) : name;
            var errorText = isOverflow
                ? $"{name}: введенное значение выходит за пределы своего типа данных."
                : $"{name}: введенное значение имеет недопустимый тип.";
            modelState.AddModelError(propertyName, errorText);
            modelState.SetModelValue(propertyName, provider.GetValue(propertyName));
        }

        private static void AddRangeError(IValueProvider provider, ModelStateDictionary modelState, string propertyName, PropertyInfo pi = null)
        {
            var name = GetHumanPropertyName(pi);
            name = string.IsNullOrEmpty(name) ? propertyName.Substring(propertyName.IndexOf('.') + 1) : name;
            modelState.AddModelError(propertyName, $"{name}: введенное значение не входит в допустимый диапазон.");
            modelState.SetModelValue(propertyName, provider.GetValue(propertyName));
        }

        private static string GetHumanPropertyName(PropertyInfo pi)
        {
            if (pi == null)
            {
                return string.Empty;
            }

            var customAttributes = Attribute.GetCustomAttributes(pi);
            return customAttributes.OfType<DisplayAttribute>().Select(a => a.Name).FirstOrDefault();
        }
    }
}
