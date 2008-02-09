using System;
using System.Collections;

using System.IO;
using System.Net;
using System.Web.Caching;

using Exyus.Web;

namespace Exyus.Samples
{
    [UriPattern(@"/zipcheck/(?<zipid>[^/?]*)?(?:\.xcs)")]
    [MediaTypes("image/png","text/plain","text/html","text/xml","application/json")]
    class ZipCheck : HTTPResource
    {
        ArrayList list = null;
        Utility util = new Utility();
        string folder = "~/files/zipcheck/";

        public ZipCheck()
        {
            this.ContentType = "image/png";
            this.Expires = DateTime.UtcNow.AddDays(30);
        }

        public override void Get()
        {
            // get computed type (or throw 406)
            string mtype = util.SetMediaType(this);

            // parse url
            Hashtable arglist = util.ParseUrlPattern(this.Context.Request.Url.AbsoluteUri, util.GetUriPatterns(this)[0]);

            // load list check inputs
            list = LoadZipCodes();
            string lookup = (arglist["zipid"] != null ? arglist["zipid"].ToString() : string.Empty);
            bool valid = list.Contains(lookup);
            string rtn = (valid == true ? "OK" : "Invalid");

            // render requested representation
            switch (mtype.ToLower())
            {
                case "text/plain":
                    Response = rtn;
                    break;
                case "text/html":
                    Response = string.Format("<p class=\"{0}\">{0}</p>", rtn);
                    break;
                case "text/xml":
                    Response = string.Format("<zipcheck>{0}</zipcheck>", rtn);
                    break;
                case "application/json":
                    Response = string.Format("{{\"zipcheck\" : \"{0}\"}}", rtn);
                    break;
                case "image/png":
                default:
                    this.Context.Response.TransmitFile(this.Context.Server.MapPath(string.Format(folder+"{0}.png", valid)));
                    break;

            }

            // set return code
            this.StatusCode = (valid == true ? HttpStatusCode.OK : HttpStatusCode.NotFound);
        }

        public override void Head()
        {
            this.Get();
            this.Response = null;
        }

        private ArrayList LoadZipCodes()
        {
            // get from cache
            string datafile = this.Context.Server.MapPath(folder + "zip-codes.txt");
            ArrayList list = (ArrayList)this.Context.Cache.Get(datafile);

            // if missing, get from disk
            if (list == null)
            {
                list = new ArrayList();
                using (TextReader tr = new StreamReader(datafile))
                {
                    while (tr.Peek() != -1)
                    {
                        list.Add(tr.ReadLine());
                    }
                }

                // now add to cache for next time
                this.Context.Cache.Add(
                    datafile,
                    list,
                    new CacheDependency(datafile),
                    Cache.NoAbsoluteExpiration,
                    Cache.NoSlidingExpiration,
                    CacheItemPriority.Normal,
                    null);
            }

            return list;
        }
    }
}
