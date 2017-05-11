using System.Web.Mvc;

namespace Xrm.Mvc.ModelBinder
{
    public class DynamicJsonAttribute : CustomModelBinderAttribute
    {
        public bool MatchName { get; set; }

        public override IModelBinder GetBinder()
        {
            return new DynamicJsonBinder(MatchName);
        }
    }
}
