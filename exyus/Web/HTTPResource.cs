using System;
using System.Web;
using System.Xml;
using System.Xml.Serialization;
using System.Collections;
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
        private bool _allowPut = true;
        private bool _allowPost = true;
        private bool _allowDelete = true;
        private bool _allowGet = true;
        private bool _allowHead = true;
        private bool _allowOptions = true;
        private string _contenttype = "text/xml";
        private HttpStatusCode _statuscode = HttpStatusCode.OK;
        private string _statusdescription = string.Empty;
        private string _location = string.Empty;
        private string _title = string.Empty;
        private string _allowedMethods = string.Empty;
        private string _allowedMediaTypes = string.Empty;
        #endregion

        #region protected variables
        protected Hashtable ArgumentList = new Hashtable();
        protected string AbsoluteUri = string.Empty;
        protected string UrlPattern = string.Empty;
        protected string[] MediaTypes = null;
        protected string[] UpdateMediaTypes = null;
        protected string CurrentMediaType = string.Empty;
        protected string CurrentFileType = string.Empty;
        protected string Method = string.Empty;
        #endregion

        #region public fields
        public string[] ImmediateCacheUriTemplates = null;
        public string[] BackgroundCacheUriTemplates = null;
        #endregion

        #region public properties
        public string AllowedMediaTypes
        {
            get { return _allowedMediaTypes; }
            set { _allowedMediaTypes = value; }
        }
        public string AllowedMethods
        {
            get { return _allowedMethods; }
            set { _allowedMethods = value; }
        }
        public string Title
        {
            get { return _title; }
            set { _title = value; }
        }
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
        public bool AllowPut
        {
            get { return _allowPut; }
            set { _allowPut = value; }
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
        public bool AllowGet
        {
            get { return _allowGet; }
            set { _allowGet = value; }
        }
        public bool AllowHead
        {
            get { return _allowHead; }
            set { _allowHead = value; }
        }
        public bool AllowOptions
        {
            get { return _allowOptions; }
            set { _allowOptions = value; }
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
            this.Context = ctx;
            this.AbsoluteUri = ctx.Request.RawUrl;
            this.Method = ctx.Request.HttpMethod.ToLower();

            //get first pattern (if none set already)
            if (this.UrlPattern == null || this.UrlPattern == string.Empty)
                this.UrlPattern = ((UriPattern)this.GetType().GetCustomAttributes(typeof(UriPattern), false)[0]).Patterns[0];

            if (this.Title == string.Empty)
                this.Title = this.GetType().ToString();

            MakeAllowHeader();
            SetExyusHeader();

            this.SetMediaType();
            this.ParseUrlPattern();
            util.SafeAdd(ref ArgumentList, "_title", this.Title);
            util.SafeAdd(ref ArgumentList, "utc-datetime", string.Format("{0:s}Z", DateTime.UtcNow));
            util.SafeAdd(ref ArgumentList, "_media-type", CurrentMediaType);

            // add content-type to log file
            ctx.Response.AppendToLog(string.Format(" [exyus-mtype={0}]", this.ContentType));

            switch(this.Method)
            {
                case Constants.HttpGet:
                    if (this.AllowGet == false)
                        ThrowNotAllowed("GET Not Allowed.");
                    else 
                        Get();
                    break;
                case Constants.HttpPost:
                    if (this.AllowPost == false)
                        ThrowNotAllowed("POST Not Allowed.");
                    else 
                        Post();
                    break;
                case Constants.HttpPut:
                    if (this.AllowPut == false)
                        ThrowNotAllowed("PUT Not Allowed.");
                    else 
                        Put();
                    break;
                case Constants.HttpDelete:
                    if (this.AllowDelete == false)
                        ThrowNotAllowed("DELETE Not Allowed.");
                    else 
                        Delete();
                    break;
                case Constants.HttpHead:
                    if (this.AllowHead == false)
                        ThrowNotAllowed("HEAD Not Allowed.");
                    else
                        Head();
                    break;
                case Constants.HttpOptions:
                    if (this.AllowOptions == false)
                        ThrowNotAllowed("OPTIONS Not Allowed.");
                    else
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

            // handle caching headers
            if (this.Method == Constants.HttpGet || this.Method == Constants.HttpHead)
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
        // in case child class does not implement these methods
        public virtual void Get()
        {
            ThrowNotAllowed();
        }
        public virtual void Post()
        {
            ThrowNotAllowed();
        }
        public virtual void Put()
        {
            ThrowNotAllowed();
        }
        public virtual void Delete()
        {
            ThrowNotAllowed();
        }
        public virtual void Head()
        {
            ThrowNotAllowed();
        }
        public virtual void Options()
        {
            this.Context.Response.AppendHeader("Allow", this.AllowedMethods);
            // build accept types
            this.Context.Response.AppendHeader("X-Accept-Types", util.GetAllowedMediaTypes(this.MediaTypes));
            // build content types
            this.Context.Response.AppendHeader("X-Content-Types", util.GetAllowedMediaTypes((this.UpdateMediaTypes!=null?this.UpdateMediaTypes:this.MediaTypes)));
        }
        public virtual void Unknown()
        {
            ThrowNotAllowed();
        }
        #endregion

        #region protected methods
        // call parser to handle incoming URL
        protected void ParseUrlPattern()
        {
            util.ParseUrlPattern(ref this.ArgumentList, this.AbsoluteUri, this.UrlPattern, true);
        }
        protected void ParseUrlPattern(bool replace)
        {
            util.ParseUrlPattern(ref this.ArgumentList, this.AbsoluteUri, this.UrlPattern, replace);
        }
        protected void ParseUrlPattern(ref Hashtable ArgumentList, string url, string pattern)
        {
            util.ParseUrlPattern(ref ArgumentList,url,pattern,true);
        }
        protected void ParseUrlPattern(ref Hashtable ArgumentList, string url, string pattern, bool replace)
        {
            util.ParseUrlPattern(ref ArgumentList,url,pattern,replace);
        }

        // handle mediatypes/updatemediatypes, conneg, etc.
        protected void SetMediaType()
        {
            // make sure we have a mediatype collection
            if(this.GetType().GetCustomAttributes(typeof(MediaTypes), false).Length==0)
                this.MediaTypes = new string[] {this.ContentType};
            else 
                this.MediaTypes = ((MediaTypes)this.GetType().GetCustomAttributes(typeof(MediaTypes), false)[0]).Types;

            // handle conneg or throw error
            switch (this.Method.ToLower())
            {
                case "put":
                case "post":
                case "delete":
                    this.AllowedMediaTypes = util.GetAllowedMediaTypes((this.UpdateMediaTypes != null ? this.UpdateMediaTypes : this.MediaTypes));
                    this.CurrentMediaType = util.SetMediaType(this, (this.UpdateMediaTypes != null ? this.UpdateMediaTypes : this.MediaTypes));
                    break;
                case "get":
                case "head":
                case "options":
                default:
                    this.AllowedMediaTypes = util.GetAllowedMediaTypes(this.MediaTypes);
                    this.CurrentMediaType = util.SetMediaType(this, this.MediaTypes);
                    break;
            }

            // set filetype sctring for template lookups
            this.CurrentFileType = util.LookUpFileType(this.CurrentMediaType);
        }

        protected void ThrowNotAllowed()
        {
            ThrowNotAllowed("Method Not Allowed");
        }
        protected void ThrowNotAllowed(string description)
        {
            this.Context.Response.AppendHeader("Allow", this.AllowedMethods);
            this.Context.Response.AppendHeader("X-Acceptable", this.AllowedMediaTypes);
            this.Context.Response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
            this.Context.Response.StatusDescription = description;
            throw new HttpException((int)HttpStatusCode.MethodNotAllowed, description);
        }
        #endregion

        #region private methods
        // set up allow header
        private void MakeAllowHeader()
        {
            string allow = string.Empty;
            if (this.AllowedMethods != null && this.AllowedMethods != string.Empty)
                allow = this.AllowedMethods;
            else
            {
                // build up default allow header
                allow = string.Format("{0},{1},{2},{3},{4},{5}",
                    (this.AllowDelete?"DELETE":String.Empty),
                    (this.AllowGet?"GET":String.Empty),
                    (this.AllowHead?"HEAD":String.Empty),
                    (this.AllowOptions?"OPTIONS":String.Empty),
                    (this.AllowPost?"POST":String.Empty),
                    (this.AllowPut ? "PUT" : String.Empty)
                );
                // clean up
                allow = allow.Replace(",,", ",");
                allow = (allow.IndexOf(',') == 0 ? allow.Substring(1) : allow);
                allow = (allow.LastIndexOf(',') == allow.Length-1 ? allow.Substring(0, allow.Length - 1) : allow);
            }
            this.AllowedMethods = allow;
        }

        // NOTE: this is only called for GET and HEAD
        private void SetCachingHeaders()
        {
            string cache_control = string.Empty;

            // check for proper http status (200,203,206,300,301,410)
            if (IsCacheableStatus() == false)
                return;

            // settle cookie/auth issue
            if (this.Context.Request.IsAuthenticated)
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
