using System.Configuration;

namespace Xrm.Mvc.ConfigHandlers
{
    [ConfigurationCollection(typeof(ModelBindingElement))]
    // ReSharper disable once ClassNeverInstantiated.Global
    public class ModelBindingCollection : ConfigurationElementCollection
    {
        protected override ConfigurationElement CreateNewElement()
        {
            return new ModelBindingElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((ModelBindingElement)element).Name;
        }
    }
}
