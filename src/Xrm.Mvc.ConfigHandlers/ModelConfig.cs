using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.Web.Compilation;
using System.Web.Mvc;

using Xrm.Mvc.ModelBinder;

namespace Xrm.Mvc.ConfigHandlers
{
    public static class ModelConfig
    {
        private static readonly List<Assembly> ModelAsemblies;

        private static readonly object Locker = new object();

        static ModelConfig()
        {
            Config = ConfigurationManager.GetSection("modelBinder") as ModelBinderSection;
            ModelAsemblies = Config == null ? Enumerable.Empty<Assembly>().ToList() : BuildManager.GetReferencedAssemblies().Cast<Assembly>().Where(a => a.FullName.StartsWith(Config.ModelNamespace)).ToList();
        }

        private static ModelBinderSection Config { get; }

        public static void RegisterXrmModels(ModelBinderDictionary binders)
        {
            var customEntityModelBinder = new EntityModelBinder();
            if (Config != null)
            {
                foreach (ModelBindingElement modelBinding in Config.ModelBindings)
                {
                    var type = GetType($"{Config.ModelNamespace}.{modelBinding.Type}");
                    if (type != null)
                    {
                        lock (Locker)
                        {
                            if (!binders.ContainsKey(type))
                            {
                                binders.Add(type, customEntityModelBinder);
                            }
                        }

                        if (modelBinding.Enumerable)
                        {
                            type = typeof(IEnumerable<>).MakeGenericType(type);
                            lock (Locker)
                            {
                                if (!binders.ContainsKey(type))
                                {
                                    binders.Add(type, customEntityModelBinder);
                                }
                            }
                        }
                    }
                }
            }
        }

        private static Type GetType(string typeName)
        {
            var type = Type.GetType(typeName);
            if (type != null)
            {
                return type;
            }

            foreach (var a in ModelAsemblies)
            {
                type = a.GetType(typeName);
                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }
    }
}
