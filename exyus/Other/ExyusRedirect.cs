using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

using System.Web;
using System.Configuration;
using System.Collections.Specialized;
using System.Security.Cryptography;

using System.Net;
using System.IO;
using System.Xml.Xsl;

using Exyus.Web;
using Exyus.Xml;
using System.Collections.ObjectModel;

using System.Xml;
using System.Xml.XPath;
using System.Text.RegularExpressions;


namespace Exyus
{
    public class ExyusRedirect
    {
        Utility util = new Utility();

        public SortedList LoadRedirUriList()
        {
            SortedList redirUriList = new SortedList();
            HttpApplication app = HttpContext.Current.ApplicationInstance;
            int timeout = Convert.ToInt32(util.GetConfigSectionItem(Constants.cfg_exyusSecurity, Constants.cfg_authTimeout, "20"));
            string redirFile = util.GetConfigSectionItem("exyusSettings", "redirectUrls");
            string fullpath = app.Request.MapPath(redirFile);
            XmlNodeList redirNodes = null;
            XmlDocument xmldoc = new XmlDocument();

            // load list of secured urls from cache or xml
            redirUriList = (SortedList)app.Context.Cache.Get("redirUriList");
            if (redirUriList == null)
            {
                redirUriList = new SortedList();
                using (XmlTextReader xtr = new XmlTextReader(fullpath))
                {
                    xmldoc.Load(xtr);
                    xtr.Close();
                }

                redirNodes = xmldoc.SelectNodes("/urls/url");
                int numUrls = redirNodes.Count;
                if (numUrls > 0)
                {
                    for (int i = 0; i < numUrls; i++)
                        redirUriList.Add(redirNodes[i].Attributes["path"].Value, (redirNodes[i].Attributes["newpath"]) != null ? redirNodes[i].Attributes["newpath"].Value : "");
                }

                // add to cache w/ file dependency
                app.Context.Cache.Add(
                    "redirUriList",
                    redirUriList,
                    new System.Web.Caching.CacheDependency(fullpath),
                    System.Web.Caching.Cache.NoAbsoluteExpiration,
                    new TimeSpan(0, timeout, 0),
                    System.Web.Caching.CacheItemPriority.High,
                    null);
            }

            return redirUriList;
        }

        public string RedirLookUp(string uri)
        {
            HttpApplication app = HttpContext.Current.ApplicationInstance;
            int timeout = Convert.ToInt32(util.GetConfigSectionItem(Constants.cfg_exyusSecurity, Constants.cfg_authTimeout,"20"));
            string rtn = string.Empty;
            SortedList redirUriList = null;
            string redir_uri = string.Empty;
            Regex rx;
            Match mx;

            // see if we have this already
            redir_uri = (string)app.Context.Cache.Get(uri + "_redirUri");
            if (redir_uri != null)
                rtn = redir_uri;
            else
            {
                // we need this for cache dependency
                string redirFile = util.GetConfigSectionItem("exyusSettings", "redirectUrls");
                string fullpath = app.Request.MapPath(redirFile);

                // go get url collection and search
                redirUriList = LoadRedirUriList();
                for (int i = redirUriList.Count - 1; i > -1; i--)
                {
                    rx = new Regex(redirUriList.GetKey(i).ToString(), RegexOptions.IgnoreCase);
                    mx = rx.Match(uri);

                    if (mx.Success)
                    {
                        // build up redirect
                        rtn = util.GetConfigSectionItem("exyusSettings", "rootfolder") + rx.Replace(uri, redirUriList.GetByIndex(i).ToString());
                        rtn = rtn.Replace(".xcs", "");

                        // save this for later
                        app.Context.Cache.Add(
                            uri + "_redirUri",
                            rtn,
                            new System.Web.Caching.CacheDependency(fullpath),
                            System.Web.Caching.Cache.NoAbsoluteExpiration,
                            new TimeSpan(0, timeout, 0),
                            System.Web.Caching.CacheItemPriority.AboveNormal,
                            null);

                        // all done!
                        goto exit;
                    }
                }
            }

        exit:
            return rtn;
        }

        public SortedList LoadRewriteUriList()
        {
            SortedList rewriteUriList = new SortedList();
            HttpApplication app = HttpContext.Current.ApplicationInstance;
            int timeout = Convert.ToInt32(util.GetConfigSectionItem(Constants.cfg_exyusSecurity, Constants.cfg_authTimeout, "20"));
            string rewriteFile = util.GetConfigSectionItem("exyusSettings", "rewriteUrls");
            string fullpath = app.Request.MapPath(rewriteFile);
            XmlNodeList rewriteNodes = null;
            XmlDocument xmldoc = new XmlDocument();

            // load list of secured urls from cache or xml
            rewriteUriList = (SortedList)app.Context.Cache.Get("rewriteUriList");
            if (rewriteUriList == null)
            {
                rewriteUriList = new SortedList();
                using (XmlTextReader xtr = new XmlTextReader(fullpath))
                {
                    xmldoc.Load(xtr);
                    xtr.Close();
                }
                rewriteNodes = xmldoc.SelectNodes("/urls/url");
                int numUrls = rewriteNodes.Count;
                if (numUrls > 0)
                {
                    for (int i = 0; i < numUrls; i++)
                        rewriteUriList.Add(rewriteNodes[i].Attributes["path"].Value, (rewriteNodes[i].Attributes["newpath"]) != null ? rewriteNodes[i].Attributes["newpath"].Value : "");
                }

                // add to cache w/ file dependency
                app.Context.Cache.Add(
                    "rewriteUriList",
                    rewriteUriList,
                    new System.Web.Caching.CacheDependency(fullpath),
                    System.Web.Caching.Cache.NoAbsoluteExpiration,
                    new TimeSpan(0, timeout, 0),
                    System.Web.Caching.CacheItemPriority.High,
                    null);
            }

            return rewriteUriList;
        }

        public string RewriteLookUp(string uri)
        {
            HttpApplication app = HttpContext.Current.ApplicationInstance;
            int timeout = Convert.ToInt32(util.GetConfigSectionItem(Constants.cfg_exyusSecurity, Constants.cfg_authTimeout, "20"));
            string rtn = string.Empty;
            SortedList rewriteUriList = null;
            string rewrite_uri = string.Empty;
            Regex rx;
            Match mx;

            // see if we have this already
            rewrite_uri = (string)app.Context.Cache.Get(uri + "_rewriteUri");
            if (rewrite_uri != null)
                rtn = rewrite_uri;
            else
            {
                // we need this for cache dependency
                string rewriteFile = util.GetConfigSectionItem("exyusSettings", "rewriteUrls");
                string fullpath = app.Request.MapPath(rewriteFile);

                // go get url collection and search
                rewriteUriList = LoadRewriteUriList();
                for (int i = rewriteUriList.Count - 1; i > -1; i--)
                {
                    rx = new Regex(rewriteUriList.GetKey(i).ToString(), RegexOptions.IgnoreCase);
                    mx = rx.Match(uri);

                    if(mx.Success)
                    {
                        // build up redirect
                        rtn = util.GetConfigSectionItem("exyusSettings","rootfolder")+rx.Replace(uri, rewriteUriList.GetByIndex(i).ToString());

                        // save this for later
                        app.Context.Cache.Add(
                            uri + "_rewriteUri",
                            rtn,
                            new System.Web.Caching.CacheDependency(fullpath),
                            System.Web.Caching.Cache.NoAbsoluteExpiration,
                            new TimeSpan(0, timeout, 0),
                            System.Web.Caching.CacheItemPriority.AboveNormal,
                            null);

                        // all done!
                        goto exit;
                    }
                }
            }

        exit:
            return rtn;
        }

        // echo 301 w/ new location
        public void MovedPermanently(string uri)
        {
            HttpContext ctx = HttpContext.Current;
            ctx.Response.StatusCode = (int)HttpStatusCode.MovedPermanently;
            ctx.Response.StatusDescription = "301 Moved Permanently";
            ctx.Response.RedirectLocation = uri;
            ctx.Response.End();
        }

        // echo 302 w/ new location
        public void Moved(string uri)
        {
            HttpContext ctx = HttpContext.Current;
            ctx.Response.StatusCode = (int)HttpStatusCode.Moved;
            ctx.Response.StatusDescription = "302 Moved";
            ctx.Response.RedirectLocation = uri;
            ctx.Response.End();
        }

        // handle it internally
        public void Rewrite(string uri)
        {
            HttpContext ctx = HttpContext.Current;
            ctx.RewritePath(uri);
        }
    }
}
