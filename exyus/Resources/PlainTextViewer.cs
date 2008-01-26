using System;
using System.Web;
using Exyus.Web;

namespace Exyus.Web
{
    [MediaTypes("text/plain")]
    public class PlainTextViewer : StaticResource
    {
        Utility util = new Utility();
        Caching.Cache ch = new Exyus.Caching.Cache();
        private string[] mediaTypes = null;
        public System.Collections.Hashtable Files = new System.Collections.Hashtable();

        public PlainTextViewer()
        {
            this.ContentType = "text/plain";
            mediaTypes = ((MediaTypes)this.GetType().GetCustomAttributes(typeof(MediaTypes), false)[0]).Types;
        }

        public override void Get()
        {
            string results = string.Empty;

            // handle args
            string f = (this.Context.Request["f"] != null ? this.Context.Request["f"] : string.Empty);
            if (f == string.Empty)
                throw new HttpException(400, "Missing argument [f]");

            // validate media type (may throw 416 error)
            util.LookUpFileType(util.SetMediaType(this, mediaTypes));

            // check cache first
            if (ch.CachedResourceIsValid((HTTPResource)this))
                return;

            if (Files.ContainsKey(f))
            {
                results = Helper.ReadFile(Files[f].ToString());
            }
            else
            {
                throw new HttpException(400, "Invalid argument [f]");
            }

            // post results cache
            ch.CacheResource((HTTPResource)this, results);

            // return results
            this.Response = results;
        }
    }
}
