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
            this.AllowCreateOnPut = false;
            this.AllowPut = false;
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

            // trap for fall-through all w/ no content associated
            if (this.Content == string.Empty)
                throw new HttpException(404, "Document Not Found");

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
                out_text = util.RenderError("http error", hex.Message, CurrentMediaType);
            }
            catch (Exception ex)
            {
                this.StatusCode = HttpStatusCode.InternalServerError;
                this.StatusDescription = ex.Message;
                out_text = util.RenderError("unknown error", ex.Message, CurrentMediaType);
            }

            // return the results
            this.Response = util.FixEncoding(out_text);
        }
    }
}
