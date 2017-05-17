using System.Configuration;

namespace Xrm.Mvc.ConfigHandlers
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class ModelBinderSection : ConfigurationSection
    {
        [ConfigurationProperty("modelNamespace", IsKey = true, IsRequired = true)]
        public string ModelNamespace
        {
            get
            {
                return (string)this["modelNamespace"];
            }

            set
            {
                this["modelNamespace"] = value;
            }
        }

        [ConfigurationProperty("modelBindings", IsDefaultCollection = true)]
        public ModelBindingCollection ModelBindings
        {
            get
            {
                return (ModelBindingCollection)this["modelBindings"];
            }

            set
            {
                this["modelBindings"] = value;
            }
        }
    }
}
