using System;
using System.Web;
using System.Xml;
using System.Xml.Serialization;
using System.Collections.Specialized;

using System.Net;
using System.Reflection;
using System.Diagnostics;

namespace Exyus.Web
{
    public class HTTPResource : IHttpHandler
    {
        #region private variables
        Utility util = new Utility();
        private object _response = null;
        public HttpContext Context;
        private string _entityTag = string.Empty;
        private System.DateTime _lastModified = System.DateTime.MinValue;
        private int _maxAge = 0;
        private bool _useValidationCaching = true;
        private int _localMaxAge = 600;
        private DateTime _expires = DateTime.MinValue;
        private bool _allowCreateOnPut = false;
        private bool _allowPost = true;
        private bool _allowDelete = true;
        private string _contenttype = "text/xml";
        private HttpStatusCode _statuscode = HttpStatusCode.OK;
        private string _statusdescription = string.Empty;
        private string _location = string.Empty;
        #endregion

        #region public fields
        public string[] ImmediateCacheUriTemplates = null;
        public string[] BackgroundCacheUriTemplates = null;
        #endregion

        #region public properties
        public string ContentType
        {
            get { return _contenttype; }
            set { _contenttype = value; }
        }
        public bool AllowCreateOnPut
        {
            get { return _allowCreateOnPut; }
            set { _allowCreateOnPut = value; }
        }
        public bool AllowPost
        {
            get { return _allowPost; }
            set { _allowPost = value; }
        }
        public bool AllowDelete
        {
            get { return _allowDelete; }
            set { _allowDelete = value; }
        }
        public DateTime Expires
        {
            get { return _expires; }
            set { _expires = value; }
        }
        public int LocalMaxAge
        {
            get { return _localMaxAge; }
            set { _localMaxAge = value; }
        }
        public bool UseValidationCaching
        {
            get { return _useValidationCaching; }
            set { _useValidationCaching = value; }
        }
        public string EntityTag
        {
            get { return _entityTag; }
            set { _entityTag = value; }
        }
        public System.DateTime LastModified
        {
            get { return _lastModified; }
            set { _lastModified = value; }
        }
        public int MaxAge
        {
            get { return _maxAge; }
            set { _maxAge = value; }
        }

        public object Response
        {
            get {return _response;}
            set {_response = value;}
        }

        public HttpStatusCode StatusCode
        {
            get { return _statuscode; }
            set { _statuscode = value; }
        }
        public string StatusDescription
        {
            get { return _statusdescription; }
            set { _statusdescription = value; }
        }
        public string Location
        {
            get { return _location; }
            set { _location = value; }
        }
        #endregion

        #region base class implementation
        public HTTPResource() { }

        public bool IsReusable
        {
            get { return false; }
        }

        public void ProcessRequest(HttpContext ctx)
        {
            string method = ctx.Request.HttpMethod.ToLower();
            this.Context = ctx;

            switch(method)
            {
                case Constants.HttpGet:
                    Get();
                    break;
                case Constants.HttpPost:
                    Post();
                    break;
                case Constants.HttpPut:
                    Put();
                    break;
                case Constants.HttpDelete:
                    Delete();
                    break;
                case Constants.HttpHead:
                    Head();
                    break;
                case Constants.HttpOptions:
                    Options();
                    break;
                default:
                    Unknown();
                    break;
            }
            
            // handle basic response properties
            ctx.Response.ContentType = this.ContentType;
            ctx.Response.StatusCode = (int)this.StatusCode;
            ctx.Response.StatusDescription = (this.StatusDescription != string.Empty ? this.StatusDescription : this.StatusCode.ToString());
            if (this.Location.Length != 0)
                ctx.Response.RedirectLocation = this.Location;

            SetExyusHeader();

            // add content-type to log file
            ctx.Response.AppendToLog(string.Format(" [exyus-mtype={0}]", this.ContentType));

            // handle caching headers
            if (method == Constants.HttpGet || method == Constants.HttpHead)
                SetCachingHeaders();

            // serialize the response and send to client
            if (this.Response != null)
            {
                // try an xml doc
                if (this.Response.GetType() == typeof(XmlDocument))
                {
                    ctx.Response.Write(util.FixEncoding(((XmlDocument)this.Response).OuterXml));
                    goto end_response;
                }
                // try a string pile
                if (this.Response.GetType() == typeof(string))
                {
                    ctx.Response.Write((string)this.Response);
                    goto end_response;
                }

                // serialize the object directly
                XmlSerializer xs = new XmlSerializer(this.Response.GetType());
                xs.Serialize(ctx.Response.Output, this.Response);
            }
            else
            {
                ctx.Response.Write("");
            }

            // end this response
        end_response:
            ctx.Response.End();
            return;
        }
        #endregion

        #region public methods
        public virtual void Get()
        {
            this.StatusCode =  HttpStatusCode.MethodNotAllowed;
            this.Response = new object();
        }
        public virtual void Post()
        {
            this.StatusCode = HttpStatusCode.MethodNotAllowed;
            this.Response = new object();
        }
        public virtual void Put()
        {
            this.StatusCode = HttpStatusCode.MethodNotAllowed;
            this.Response = new object();
        }
        public virtual void Delete()
        {
            this.StatusCode = HttpStatusCode.MethodNotAllowed;
            this.Response = new object();
        }
        public virtual void Head()
        {
            this.StatusCode = HttpStatusCode.MethodNotAllowed;
            this.Response = new object();
        }
        public virtual void Options()
        {
            this.StatusCode = HttpStatusCode.MethodNotAllowed;
            this.Response = new object();
        }
        public virtual void Unknown()
        {
            this.StatusCode = HttpStatusCode.MethodNotAllowed;
            this.Response = new object();
        }
        #endregion

        #region private methods
        // NOTE: this is only called for GET and HEAD
        private void SetCachingHeaders()
        {
            string cache_control = string.Empty;

            // check for proper http status (200,203,206,300,301,410)
            if (IsCacheableStatus() == false)
                return;

            // settle cookie/auth issue
            if (
                //(this.Context.Response.Cookies != null && this.Context.Request.Cookies.Count!=0)
                //|| 
                this.Context.Request.IsAuthenticated
                )
            {
                this.Expires = DateTime.MinValue.AddHours(1);
                this.MaxAge = -1;   // trick for private/auth'ed responses
                cache_control = "private,must-revalidate,nocache=\"set-cookie\"";
            }
            else
            {
                if (this.MaxAge == 0 && this.UseValidationCaching == false)
                {
                    cache_control = "no-cache";
                }
                else
                {
                    cache_control = "public,must-revalidate";
                }
            }

            // check for expiration caching
            if (this.Expires != DateTime.MinValue)
                this.Context.Response.AppendHeader(Constants.hdr_expires, string.Format(Constants.fmt_gmtdate, this.Expires));

            if (this.MaxAge != 0)
                cache_control += ","+string.Format(Constants.fmt_max_age, (this.MaxAge==-1?0:this.MaxAge));

            if (this.UseValidationCaching)
            {
                if (this.LastModified != System.DateTime.MinValue)
                    this.Context.Response.AppendHeader(Constants.hdr_last_modified, string.Format(Constants.fmt_gmtdate, this.LastModified));

                if (this.EntityTag != string.Empty)
                    this.Context.Response.AppendHeader(Constants.hdr_etag, this.EntityTag);
            }

            // HACK: force msie (only) to ignore it's local cache for GET requests
            cache_control += ",post-check=1,pre-check=2";

            // output cache-control header, if needed
            if (cache_control != string.Empty)
            {
                if (cache_control.Substring(0, 1) == ",")
                    cache_control = cache_control.Substring(1);

                this.Context.Response.AppendHeader(Constants.hdr_cache_control, cache_control);
            }
        }

        private void SetExyusHeader()
        {
            string exyus_header = string.Empty;

            exyus_header = (string)this.Context.Cache.Get(Constants.hdr_exyus);
            if (exyus_header == null || exyus_header==string.Empty)
            {
                System.Reflection.Assembly asm = System.Reflection.Assembly.GetExecutingAssembly();
                FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(asm.Location);
                exyus_header = string.Format(Constants.fmt_exyus, asm.GetName().Version, fvi.Comments);

                this.Context.Cache.Add(
                    Constants.hdr_exyus,
                    exyus_header,
                    null,
                    System.Web.Caching.Cache.NoAbsoluteExpiration,
                    System.Web.Caching.Cache.NoSlidingExpiration,
                    System.Web.Caching.CacheItemPriority.Low,
                    null);
            }
            this.Context.Response.AppendHeader(Constants.hdr_exyus,exyus_header);
        }

        private bool IsCacheableStatus()
        {
            bool rtn = false;

            switch (this.StatusCode)
            {
                case HttpStatusCode.OK: // 200
                case HttpStatusCode.NonAuthoritativeInformation: // 203
                case HttpStatusCode.PartialContent: // 206
                case HttpStatusCode.MultipleChoices: // 300
                case HttpStatusCode.Moved: // 301
                case HttpStatusCode.Gone: // 410
                    rtn = true;
                    break;
                default:
                    rtn = false;
                    break;
            }

            return rtn;
        }
        #endregion
    }
}
