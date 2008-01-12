using System;
using Exyus.Web;

namespace Exyus.Editable
{
    // simple direct get handler
    [UriPattern(@"/editable/(?<docid>[^/?]*)?(?:\.xcs)(?:[?])?(?:.*)?")]
    [MediaTypes("text/html")]
    public class editPages : XmlFileResource
    {
        public editPages()
        {
            this.ContentType = "text/html";
            this.UpdateMediaTypes = new string[] { "text/html" };
            this.AllowPost = false;
            this.AllowCreateOnPut = true;
            this.DocumentsFolder = "~/documents/editable/";
            this.StorageFolder = "~/storage/editable/";
            this.XHtmlNodes = new string[] { "//body" };
            this.LocalMaxAge = 600;
            this.ImmediateCacheUriTemplates = new string[]
                {
                    "/editable/.xcs",
                    "/editable/{docid}.xcs"
                };
        }
    }

}
