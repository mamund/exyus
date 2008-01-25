using System;
using Exyus.Web;

namespace Exyus.Samples.Postable
{
    class Postable
    {
        // simple direct get handler
        [UriPattern(@"/postable/(?<docid>[^/?]*)?(?:\.xcs)(?:[?])?(?:.*)?")]
        [MediaTypes("text/html")]
        public class postPages : XmlFileResource
        {
            public postPages()
            {
                this.ContentType = "text/html";
                this.AllowPost = true;
                this.RedirectOnPost = true;
                this.AllowCreateOnPut = false;
                this.PostLocationUri = "/postable/";
                this.DocumentsFolder = "~/documents/postable/";
                this.StorageFolder = "~/storage/postable/";
                this.XHtmlNodes = new string[] { "//body" };
                this.LocalMaxAge = 600;
                this.UpdateMediaTypes = new string[]
                {
                    "application/x-www-form-urlencoded"
                };
                this.ImmediateCacheUriTemplates = new string[]
                {
                    "/postable/.xcs",
                    "/postable/{docid}.xcs"
                };
            }
        }
    }
}
