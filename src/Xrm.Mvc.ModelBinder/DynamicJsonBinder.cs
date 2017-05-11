using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Helpers;
using System.Web.Mvc;

namespace Xrm.Mvc.ModelBinder
{
    public class DynamicJsonBinder : IModelBinder
    {
        private readonly bool matchName;

        public DynamicJsonBinder(bool matchName)
        {
            this.matchName = matchName;
        }

        public object BindModel(ControllerContext controllerContext, ModelBindingContext bindingContext)
        {
            var contentType = controllerContext.HttpContext.Request.ContentType;
            if (!contentType.StartsWith("application/json", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            string bodyText;
            using (var stream = controllerContext.HttpContext.Request.InputStream)
            {
                stream.Seek(0, SeekOrigin.Begin);
                using (var reader = new StreamReader(stream))
                {
                    bodyText = reader.ReadToEnd();
                }
            }

            if (string.IsNullOrEmpty(bodyText))
            {
                return null;
            }

            var decoded = Json.Decode(bodyText);
            if (!matchName)
            {
                return decoded;
            }

            var members = decoded.GetDynamicMemberNames() as IEnumerable<string>;
            return members == null || members.Contains(bindingContext.ModelName)
                       ? decoded[bindingContext.ModelName]
                       : null;
        }
    }
}
