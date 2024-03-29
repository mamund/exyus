/*
 * tasklist.cs
 * 2008-01-24 (mca)
 * 2008-02-02 (mca) : added application/json
 * 2008-02-03 (mca) : added application/atom+xml
 */
using System;
using Exyus.Web;

/*********************************************************************
 * TASKLIST.CS
 * 
 * Define an HTTP resource that supports:
 * - HTML and XML content-types
 * - GET(item/list), POST, PUT(update-only), DELETE
 * 
 *********************************************************************/ 
namespace Exyus.Samples
{
    // set uri pattern matching and supported media types
    [UriPattern(@"/tasklist/(?<taskid>[^/?]*)?(?:\.xcs)(?:.*)?")]
    [MediaTypes("text/html","text/xml","application/json","application/atom+xml")]
    class TaskList : XmlFileResource
    {
        public TaskList()
        {
            // set default type and caching
            this.ContentType = "text/html";
            this.LocalMaxAge = 600;

            // set rules on interactions
            this.AllowPost = true;
            this.RedirectOnPost = true;
            this.AllowCreateOnPut = false;

            // set client-side post target
            this.PostLocationUri = "/tasklist/";

            // set server-side locations
            this.DocumentsFolder = "~/documents/tasklist/";
            this.StorageFolder = "~/storage/tasklist/";
            
            // set xhtml validation nodes
            this.XHtmlNodes = new string[] { "//name" };
            
            // set supported post/put types
            this.UpdateMediaTypes = new string[] 
                {
                    "text/xml",
                    "application/json",
                    "application/atom+xml",
                    "application/x-www-form-urlencoded"
                };
            
            // set cache invalidation rules
            this.ImmediateCacheUriTemplates = new string[]
                {
                    "/tasklist/.xcs",
                    "/tasklist/{taskid}.xcs"
                };
        }
    }
}
