using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;

using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Web;
using System.Threading;
using System.Text.RegularExpressions;

namespace Exyus.Web
{
    public class Requestor
    {
        // public properties
        public WebHeaderCollection RequestHeaders = new WebHeaderCollection();
        public WebHeaderCollection ResponseHeaders = new WebHeaderCollection();
        public CookieContainer CookieCollection = new CookieContainer();
        public HttpStatusCode ResponseStatusCode = HttpStatusCode.OK;
        public string ResponseDescription = string.Empty;
        public DateTime ResponseLastModified = System.DateTime.MaxValue;
        public NetworkCredential Credentials = new NetworkCredential();
        public long ResponseLength = 0;

        public Requestor() { }

        // method that makes the call
        public string Execute(string url)
        {
            return Execute(url, Constants.HttpGet, Constants.cType_Xml, string.Empty);
        }
        public string Execute(string url, string method)
        {
            return Execute(url, method, Constants.cType_Xml, string.Empty);
        }
        public string Execute(string url, string method, string contentType)
        {
            return Execute(url, method, contentType, string.Empty);
        }
        public string Execute(string url, string method, string contentType, string body)
        {
            HttpWebRequest req = null;
            HttpWebResponse resp= null;
            string rtnBody = string.Empty;

            try
            {
                // build request object
                req = (HttpWebRequest)WebRequest.Create(url);
                req.UserAgent = Constants.msc_exyus_agent;
                req.Method = method;
                req.ContentType = contentType;
                req.Accept = contentType;
                req.ContentLength = body.Length;
                req.PreAuthenticate = true;
                if (this.Credentials.UserName != string.Empty)
                    req.Credentials = this.Credentials;

                // set headers
                if (this.RequestHeaders != null)
                {
                    for (int i = 0; i < this.RequestHeaders.Count; i++)
                    {
                        string key = this.RequestHeaders.GetKey(i);
                        string value = this.RequestHeaders[i];
                        switch (key.ToLower())
                        {
                            case "if-modified-since":
                                req.IfModifiedSince = DateTime.Parse(value);
                                break;
                            case "accept":
                                req.Accept = value;
                                break;
                            default:
                                req.Headers.Set(key, value);
                                break;
                        }
                    }
                }

                // set cookies
                if (this.CookieCollection != null)
                    req.CookieContainer = this.CookieCollection;
                if (HttpContext.Current != null &&
                    HttpContext.Current.Request != null &&
                    HttpContext.Current.Request.Headers != null &&
                    HttpContext.Current.Request.Headers[Constants.hdr_cookie] != null
                    )
                    req.CookieContainer.SetCookies(new Uri(url), HttpContext.Current.Request.Headers[Constants.hdr_cookie]);

                // set body
                if (body != null && body.Trim() != string.Empty)
                {
                    using (StreamWriter sw = new StreamWriter(req.GetRequestStream()))
                    {
                        sw.Write(body);
                        sw.Close();
                    }
                }

                // now use request obj to populate response obj
                resp = (HttpWebResponse)req.GetResponse();

                // get properties
                this.ResponseLength = resp.ContentLength;
                this.ResponseStatusCode = resp.StatusCode;
                this.ResponseDescription = resp.StatusDescription;
                this.ResponseLastModified = resp.LastModified;

                // get headers
                this.ResponseHeaders = resp.Headers;

                // get cookies
                this.CookieCollection = new CookieContainer();
                foreach (Cookie ck in resp.Cookies)
                    this.CookieCollection.Add(ck);

                // get body
                if (resp.ContentLength != 0)
                {
                    using (StreamReader sr = new StreamReader(resp.GetResponseStream()))
                    {
                        rtnBody = sr.ReadToEnd();
                        sr.Close();
                    }
                }

                // clean up
                if (resp != null)
                    resp.Close();

                resp = null;
                req = null;

                // return the results (if any)
                return rtnBody;
            }
            catch (HttpException hex)
            {
                throw new HttpException(hex.GetHttpCode(), hex.Message);
            }
            catch (WebException wex)
            {
                // typical http error
                if (wex.Status == WebExceptionStatus.ProtocolError)
                {
                    Utility util = new Utility();
                    HttpWebResponse wrsp = (HttpWebResponse)wex.Response;
                    throw new HttpException((int)wrsp.StatusCode, wrsp.StatusDescription);
                }
                else
                {
                    throw new HttpException(500, wex.Message);
                }
            }
            catch (Exception ex)
            {
                throw new HttpException(500, ex.Message);
            }
        }
    }

    // used to invalidate a collection of URIs
    public class CollectionRequestor
    {
        public string[] Uri;
        public CookieContainer CookieCollection = new CookieContainer();
        public string defaultType;
        public Hashtable UriTypeMap;

        public void Execute()
        {
            Utility util = new Utility();
            Requestor req = new Requestor();
            req.RequestHeaders.Set(Constants.hdr_cache_control, "no-cache");
            req.Credentials = util.GetSystemCredentials();
            req.CookieCollection = this.CookieCollection;

            for (int i = 0; i < Uri.Length; i++)
            {
                string[] media = util.GetResourceMediaTypes(Uri[i], this.defaultType, this.UriTypeMap);
                if (media != null && media.Length != 0)
                {
                    for (int j = 0; j < media.Length; j++)
                    {
                        req.RequestHeaders.Set("Accept", media[j]);
                        try {req.Execute(this.Uri[i], "head", media[j]);}
                        catch (Exception ex) { }
                    }
                }
                else
                {
                    try {req.Execute(this.Uri[i], "head");}
                    catch (Exception ex) { }
                }
            }
        }
    }

}
