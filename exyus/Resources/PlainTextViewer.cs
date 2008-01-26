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
            // validate media type (may throw 416 error)
            util.SetMediaType(this, mediaTypes);

            // handle args
            string f = (this.Context.Request["f"] != null ? this.Context.Request["f"] : string.Empty);

            if (f == string.Empty)
                throw new HttpException(400, "Missing argument [f]");

            // check cache first
            if (ch.CachedResourceIsValid((HTTPResource)this))
            {
                return;
            }

            // make sure it's a valid item
            if (!Files.ContainsKey(f))
            {
                throw new HttpException(400, "Invalid argument [f]");
            }

            // do the work
            string results = Helper.ReadFile((string)Files[f]);
            ch.CacheResource((HTTPResource)this, results);
            this.Response = results;
        }
    }
}
