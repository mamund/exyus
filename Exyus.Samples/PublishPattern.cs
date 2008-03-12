using System;
using Exyus.Web;

namespace Exyus.Samples
{
    [UriPattern(@"/articles/(?<id>[0-9]*)\.xcs")]
    [MediaTypes("text/xml")]
    class Articles : XmlSqlResource
    {
        public Articles()
        {
            this.AllowCreateOnPut = false;
            this.AllowDelete = true;
            this.AllowPost = true;
            this.ConnectionString = "exyus_samples";
            this.ContentType="text/xml";
            this.DocumentsFolder = "~/documents/articles/";
            this.LocalMaxAge = 600;
            this.PostLocationUri = "/articles/";
            this.RedirectOnPost = true;
            this.RedirectOnPut = true;
            this.ImmediateCacheUriTemplates = new string[]
            {
                "/articles/.xcs",
                "/articles/{id}.xcs"
            };
        }
    }

    [UriPattern(@"/articles/(?<id>[0-9]*)/status\.xcs")]
    [MediaTypes("text/xml")]
    class ArticleStatus : XmlSqlResource
    {
        public ArticleStatus()
        {
            this.AllowCreateOnPut = false;
            this.AllowDelete = false;
            this.AllowPost = false;
            this.ConnectionString = "exyus_samples";
            this.ContentType = "text/xml";
            this.DocumentsFolder = "~/documents/articles/status/";
            this.LocalMaxAge = 600;
            this.RedirectOnPut = true;
            this.ImmediateCacheUriTemplates = new string[]
            {
                "/articles/{id}/status.xcs",
                "/articles/.xcs",
                "/articles/{id}.xcs"
            };
        }

        public override void Delete()
        {
            throw new System.Web.HttpException(405, "Unable to DELETE resource");
            //base.Delete();
        }

        public override void Post()
        {
            throw new System.Web.HttpException(405, "Unable to POST resource");
            //base.Post();
        }
    }
}
