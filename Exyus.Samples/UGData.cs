using System;
using System.IO;
using System.Net;
using System.Web;
using System.Xml;
using System.Collections;

using Exyus.Web;
using Exyus.Security;

namespace Exyus.Samples
{
    // user group data example
    [UriPattern(@"/ugdata/(?<id>[0-9]*)\.xcs")]
    [MediaTypes("text/html","text/xml","application/json")]
    class UGData : XmlSqlResource
    {
        public UGData()
        {
            this.ContentType = "text/html";
            this.ConnectionString = "exyus_samples";
            this.LocalMaxAge = 600;

            this.AllowPost = true;
            this.AllowDelete = true;
            this.DocumentsFolder = "~/documents/ugdata/";
            this.RedirectOnPost = true;
            this.PostLocationUri = "/ugdata/{id}";
            this.UpdateMediaTypes = new string[]
            {
                "text/xml",
                "application/x-www-form-urlencoded",
                "application/json"
            };

            // set cache invalidation rules
            this.ImmediateCacheUriTemplates = new string[]
            {
                "/ugdata/.xcs",
                "/ugdata/{id}.xcs"
            };

        }
    }

    // handle form-posting for updates
    [UriPattern(@"/ugdata/(?<id>[0-9]*);update\.xcs")]
    [MediaTypes("application/x-www-form-urlencoded")]
    class UGDataUpdate : HTTPResource
    {
        Utility util = new Utility();
        private string[] mediaTypes = null;
        private string UrlPattern;

        public UGDataUpdate()
        {
            this.ContentType = "application/x-www-form-urlencoded";

            //get first pattern (if none set already)
            if (this.UrlPattern == null || this.UrlPattern == string.Empty)
            {
                this.UrlPattern = util.GetDefaultUriPattern(this);
            }

            // copy media types to make things easier
            mediaTypes = util.GetMediaTypes(this);
            
        }

        public override void Get()
        {
            Hashtable arg_list = util.ParseUrlPattern(this.Context.Request.RawUrl, this.UrlPattern);
            if (!arg_list.Contains("id"))
            {
                throw new HttpException(400, "Missing document id");
            }
            string id = arg_list["id"].ToString().Replace(";update", "");
            this.Context.Response.Redirect("/xcs/ugdata/" + id);
        }

        public override void Post()
        {
            // validate media type
            string mtype = util.SetMediaType(this, mediaTypes);

            // validate argument
            Hashtable arg_list = util.ParseUrlPattern(this.Context.Request.RawUrl, this.UrlPattern);
            if (!arg_list.Contains("id"))
            {
                throw new HttpException(400, "Missing document id");
            }

            // get POSTed body
            string data = string.Empty;
            using (StreamReader sr = new StreamReader(Context.Request.InputStream))
            {
                data = sr.ReadToEnd();
                sr.Close();
            }

            // compose tgarget URL
            string url = string.Format("{0}://{1}{2}{3}",
                    this.Context.Request.Url.Scheme,
                    this.Context.Request.Url.DnsSafeHost,
                    "/xcs/ugdata/",
                    arg_list["id"]);

            // build up execution client w/ credentials
            HTTPClient c = new HTTPClient();
            c.Credentials = util.GetCurrentCredentials(this);

            // validate record already exists
            string rtn = c.Execute(url, "head", "text/html");
            string etag = c.ResponseHeaders["etag"];

            // execute PUT to target
            c.RequestHeaders.Add("if-match", etag);
            c.Execute(url, "put", this.ContentType, data);
            c = null;

            // redirect to list
            this.Context.Response.Redirect("/xcs/ugdata/");
        }
    }

    // handle form-posting for deletes
    [UriPattern(@"/ugdata/(?<id>[0-9]*);delete\.xcs")]
    [MediaTypes("application/x-www-form-urlencoded")]
    class UGDataDelete : HTTPResource
    {
        Utility util = new Utility();
        private string[] mediaTypes = null;
        private string UrlPattern;

        public UGDataDelete()
        {
            this.ContentType = "application/x-www-form-urlencoded";

            //get first pattern (if none set already)
            if (this.UrlPattern == null || this.UrlPattern == string.Empty)
            {
                this.UrlPattern = util.GetDefaultUriPattern(this);
            }

            // copy media types to make things easier
            mediaTypes = util.GetMediaTypes(this);

        }

        public override void Get()
        {
            Hashtable arg_list = util.ParseUrlPattern(this.Context.Request.RawUrl, this.UrlPattern);
            if (!arg_list.Contains("id"))
            {
                throw new HttpException(400, "Missing document id");
            }
            string id = arg_list["id"].ToString().Replace(";delete", "");
            this.Context.Response.Redirect("/xcs/ugdata/"+id);
        }

        public override void Post()
        {
            // validate content type
            string mtype = util.SetMediaType(this, mediaTypes);

            // validate arg
            Hashtable arg_list = util.ParseUrlPattern(this.Context.Request.RawUrl, this.UrlPattern);
            if (!arg_list.Contains("id"))
            {
                throw new HttpException(400, "Missing document id");
            }

            // get POSTed body
            string data = string.Empty;
            using (StreamReader sr = new StreamReader(Context.Request.InputStream))
            {
                data = sr.ReadToEnd();
                sr.Close();
            }

            // compose target URL
            string url = string.Format("{0}://{1}{2}{3}",
                    this.Context.Request.Url.Scheme,
                    this.Context.Request.Url.DnsSafeHost,
                    "/xcs/ugdata/",
                    arg_list["id"]);

            // execute DELETE w/ credentials
            HTTPClient c = new HTTPClient();
            c.Credentials = util.GetCurrentCredentials(this);
            c.Execute(url, "delete", "text/html");
            c = null;

            //redirect to list
            this.Context.Response.Redirect("/xcs/ugdata/");
        }
    }

    // return JSON-enabled HTML page
    [UriPattern(@"/ugdata/json\.xcs")]
    [MediaTypes("text/html")]
    class UGDataJSON : StaticResource
    {
        public UGDataJSON()
        {
            this.Content = Helper.ReadFile("/xcs/files/ugdata/json.html");
        }
    }
}
