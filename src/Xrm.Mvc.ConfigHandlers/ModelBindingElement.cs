using System.Configuration;

namespace Xrm.Mvc.ConfigHandlers
{
    public class ModelBindingElement : ConfigurationElement
    {
        [ConfigurationProperty("name", IsKey = true, IsRequired = true)]
        public string Name
        {
            get
            {
                return (string)this["name"];
            }

            set
            {
                this["name"] = value;
            }
        }

        [ConfigurationProperty("type", IsRequired = true)]
        public string Type
        {
            get
            {
                return (string)this["type"];
            }

            set
            {
                this["type"] = value;
            }
        }

        [ConfigurationProperty("enumerable", IsRequired = false, DefaultValue = false)]
        public bool Enumerable
        {
            get
            {
                return (bool)this["enumerable"];
            }

            set
            {
                this["enumerable"] = value;
            }
        }
    }
}
