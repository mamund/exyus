using System;
using System.Collections.Generic;
using System.Text;

using System.IO;
using System.Web;
using System.Web.Caching;

using Exyus.Web;
using Exyus.Caching;

namespace Exyus
{
    public class Helper
    {
        public static string LoadFromUrl(string url)
        {
            return LoadUrl(url, "text/html");
        }
        public static string LoadUrl(string url, string contenttype)
        {
            string rtn = string.Empty;
            WebClient wc = new WebClient();
            rtn = wc.Execute(url, "get", contenttype);
            wc = null;

            return rtn;
        }

        public static string ReadFile(string fullpath)
        {
            return ReadFile(fullpath, "text/html");
        }
        public static string ReadFile(string fullpath, string ctype)
        {
            Utility util = new Utility();
            string content = null;
            string etag = string.Empty;
            string filepath = (fullpath.IndexOf("/") != -1 ? HttpContext.Current.Server.MapPath(fullpath) : fullpath);

            // assemble cache key
            string url = HttpContext.Current.Request.Url.AbsoluteUri;
            string key = url + ((char)250).ToString() + ctype;
            
            // look in memory for the object
            content = (string)HttpContext.Current.Cache.Get(key);

            // then get it from the disk
            if (content == null)
            {
                // load it
                using (StreamReader sr = new StreamReader(filepath))
                {
                    content = sr.ReadToEnd();
                    sr.Close();
                }

                // now put it in memory w/ callback to reload on change
                HttpContext.Current.Cache.Add(
                    key,
                    content,
                    new CacheDependency(filepath),
                    System.Web.Caching.Cache.NoAbsoluteExpiration,
                    System.Web.Caching.Cache.NoSlidingExpiration,
                    System.Web.Caching.CacheItemPriority.Normal,
                    new CacheItemRemovedCallback(ReloadCacheFile)
                    );
            }
            return content;
        }

        private static void ReloadCacheFile(string key, object data, CacheItemRemovedReason reason)
        {
            WebClient wc = new WebClient();
            Utility util = new Utility();

            string etag = string.Empty;
            string url = string.Empty;
            string ctype = string.Empty;
            string[] cache_key;
            
            // parse cache key
            cache_key = key.Split((char)250);
            if (cache_key.Length > 1)
            {
                url = cache_key[0];
                ctype = (cache_key[1]!=string.Empty?cache_key[1]:string.Empty);
            }
            else
            {
                url = key;
                ctype = string.Empty;
            }

            // make head request
            wc.Credentials = util.GetSystemCredentials();
            wc.RequestHeaders.Add(Constants.hdr_cache_control, "no-cache");
            wc.Execute(url, "head",ctype);
        }
    }
}
