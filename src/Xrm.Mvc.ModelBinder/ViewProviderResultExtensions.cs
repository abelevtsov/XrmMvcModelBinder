using System.Collections.Generic;
using System.Web.Mvc;

namespace Xrm.Mvc.ModelBinder
{
    public static class ViewProviderResultExtensions
    {
        static ViewProviderResultExtensions()
        {
            Values = new Dictionary<string, string[]>();
        }

        public static Dictionary<string, string[]> Values { get; }

        public static string[] ToStringArray(this ValueProviderResult result, string propertyName)
        {
            if (Values.ContainsKey(propertyName))
            {
                return Values[propertyName];
            }

            var propertyValues = result == null ? new string[] { } : (string[])result.RawValue;
            Values.Add(propertyName, propertyValues);

            return propertyValues;
        }
    }
}
