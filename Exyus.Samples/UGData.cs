using System;
using Exyus.Web;

namespace Exyus.Samples
{
    [UriPattern(@"/ugdata/(?<id>.*)\.xcs")]
    [MediaTypes("text/xml")]
    class UGData : XmlSqlResource
    {
        public UGData()
        {
            this.ContentType = "text/xml";
            this.ConnectionString = "exyus_samples";
            this.LocalMaxAge = 600;

            this.AllowPost = true;
            this.AllowDelete = true;
            this.DocumentsFolder = "~/documents/ugdata/";
            this.RedirectOnPost = true;
            this.PostLocationUri = "/ugdata/{id}";

            // set cache invalidation rules
            this.ImmediateCacheUriTemplates = new string[]
                {
                    "/ugdata/.xcs",
                    "/ugdata/{id}.xcs"
                };

        }
    }
}
