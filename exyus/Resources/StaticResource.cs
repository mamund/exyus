using System;
using System.Web;
using System.Net;
using System.IO;
using System.Collections;

using Exyus.Caching;

namespace Exyus.Web
{
    [MediaTypes("text/html")]
    public class StaticResource : HTTPResource
    {        
        Utility util = new Utility();
        Cache ch = new Cache();

        public string UrlPattern = string.Empty;
        public string Content = string.Empty;
        
        public StaticResource()
        {
            this.ContentType = Constants.cType_Html;
            Init();
        }

        public StaticResource(string content)
        {
            this.Content = content;
            Init();
        }

        public StaticResource(string content, string _mediatype)
        {
            this.Content = content;
            this.ContentType = _mediatype;
            Init();
        }

        private void Init()
        {
            //get first pattern (if none set already)
            if (this.UrlPattern == null || this.UrlPattern == string.Empty)
                this.UrlPattern = ((UriPattern)this.GetType().GetCustomAttributes(typeof(UriPattern), false)[0]).Patterns[0];

            this.AllowCreateOnPut = false;
            this.AllowDelete = false;
            this.AllowPost = false;
            this.MaxAge = 600;
        }
        public override void Head()
        {
            this.Get();
            this.Response = null;
        }

        public override void Get()
        {
            string out_text = string.Empty;
            Hashtable arg_list = new Hashtable();

            string absoluteUri = this.Context.Request.RawUrl;
            string mtype = util.SetMediaType(this);
            string ftype = util.LookUpFileType(mtype);

            // trap for fall-through all w/ no content associated
            if (this.Content == string.Empty)
                throw new HttpException(404, "Document Not Found");

            arg_list = util.ParseUrlPattern(absoluteUri, this.UrlPattern);
            util.SafeAdd(ref arg_list, "_media-type", mtype);
            if (shared_args != null)
            {
                foreach (string key in shared_args.Keys)
                {
                    util.SafeAdd(ref arg_list, key, shared_args[key].ToString());
                }
            }

            try
            {
                // return cached copy, if you can
                if (ch.CachedResourceIsValid((HTTPResource)this))
                    return;

                // load and parse the content
                Stream ms = util.LoadAndTokenizeString(this.Content, util.GetArgs());
                using (StreamReader sr = new StreamReader(ms))
                {
                    out_text = sr.ReadToEnd();
                    sr.Close();
                }

                // cache the resource, if appropriate
                ch.CacheResource((HTTPResource)this, out_text);
            }
            catch (HttpException hex)
            {
                this.StatusCode = (HttpStatusCode)hex.GetHttpCode();
                this.StatusDescription = hex.Message;
                out_text = util.RenderError("http error", hex.Message, mtype);
            }
            catch (Exception ex)
            {
                this.StatusCode = HttpStatusCode.InternalServerError;
                this.StatusDescription = ex.Message;
                out_text = util.RenderError("unknown error", ex.Message, mtype);
            }

            // return the results
            this.Response = util.FixEncoding(out_text);
        }
    }
}
