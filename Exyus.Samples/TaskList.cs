/*
 * tasklist.cs
 * 2008-01-24 (mca)
 */
using System;
using Exyus.Web;

namespace Exyus.Samples
{
    [UriPattern(@"/tasklist/(?<taskid>[^/?]*)?(?:\.xcs)(?:.*)?")]
    [MediaTypes("text/html","text/xml")]
    class TaskList : XmlFileResource
    {
        public TaskList()
        {
            this.ContentType = "text/html";
            this.AllowPost = true;
            this.RedirectOnPost = true;
            this.AllowCreateOnPut = false;
            this.PostLocationUri = "/tasklist/";
            this.DocumentsFolder = "~/documents/tasklist/";
            this.StorageFolder = "~/storage/tasklist/";
            this.XHtmlNodes = new string[] { "//name" };
            this.LocalMaxAge = 600;
            this.UpdateMediaTypes = new string[] 
                {
                    "application/x-www-form-urlencoded",
                    "text/xml" 
                };
            this.ImmediateCacheUriTemplates = new string[]
                {
                    "/tasklist/.xcs",
                    "/tasklist/{taskid}.xcs"
                };
        }
    }
}
