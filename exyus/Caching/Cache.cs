using System;
using System.Collections.Generic;
using System.Text;

using System.Web;
using System.Xml;
using System.Net;
using System.IO;
using System.Xml.Serialization;
using System.Collections;

using Exyus.Web;

namespace Exyus.Caching
{
    public class Cache
    {
        Utility util = new Utility();

        public Cache() {}

        public void CacheResource(Exyus.Web.HTTPResource rs, string content)
        {
            // see if we should write a cached version
            if (rs.MaxAge > 0 || rs.LocalMaxAge > 0)
            {
                CacheObject co = null;
                co = PutCacheItem(rs.Context, content, rs.MaxAge, rs.LocalMaxAge, rs.ContentType);

                // mark for caching
                if (co != null)
                {
                    // use expiration caching
                    if (co.Expires != DateTime.MinValue)
                        rs.Expires = co.Expires;

                    // use validation caching
                    if (rs.UseValidationCaching == true)
                    {
                        rs.EntityTag = string.Format("\"{0}\"", co.Etag);
                        rs.LastModified = co.LastModified;
                    }
                }
            }

            return;
        }

        // return 304 *or* cache copy if possible
        public bool CachedResourceIsValid(Exyus.Web.HTTPResource rs)
        {
            CacheObject co = null;
           
            // see if it exists in cache
            co = GetCacheItem(rs.Context,rs.ContentType);

            // check for not-modified
            if (co != null && Return304(rs.Context,co))
            {
                rs.EntityTag = string.Format("\"{0}\"",co.Etag);
                rs.Expires = co.Expires;
                rs.LastModified = co.LastModified;
                rs.StatusCode = HttpStatusCode.NotModified;
                rs.ContentType = co.ContentType;
                rs.Response = null;

                return true;
            }

            // try cache object, if current
            if (co != null)
            {
                // use expiration caching
                if (co.Expires != DateTime.MinValue)
                    rs.Expires = co.Expires;

                // use validation caching
                if (rs.UseValidationCaching == true)
                {
                    rs.EntityTag = string.Format("\"{0}\"",co.Etag);
                    rs.LastModified = co.LastModified;
                }

                rs.Response = util.FixEncoding(co.Payload);
                rs.ContentType = co.ContentType;
                rs.Context.Response.AppendHeader(Constants.hdr_ExyusCache, co.Etag);

                return true;
            }

            // nope, nothing available in cache
            return false;
        }

        // force cache invalidation
        public void ClearCache(string[] immediateUriTemplates, string[] backgroundUriTemplates)
        {
            ClearCache(immediateUriTemplates, backgroundUriTemplates, string.Empty, null);
        }
        public void ClearCache(string[] immediateUriTemplates, string[] backgroundUriTemplates, string id, Hashtable uriCache)
        {
            ClearCache(immediateUriTemplates, backgroundUriTemplates, id, null, uriCache);
        }
        public void ClearCache(string[] immediateUriTemplates, string[] backgroundUriTemplates, Hashtable arg_list)
        {
            ClearCache(immediateUriTemplates, backgroundUriTemplates, string.Empty, arg_list, new Hashtable());
        }
        public void ClearCache(string[] immediateUriTemplates, string[] backgroundUriTemplates, string id, Hashtable arg_list, Hashtable uriCache)
        {
            string domain_root = string.Format(
                "{0}://{1}{2}",
                HttpContext.Current.Request.Url.Scheme,
                HttpContext.Current.Request.Url.DnsSafeHost,
                util.GetConfigSectionItem(Constants.cfg_exyusSettings,Constants.cfg_rootfolder)
                );

            // do the immiedate cache invalidations on the current thread
            // NOTE: long list will slow the response of the app
            if (immediateUriTemplates != null)
            {
                // build list of absolute uri
                string[] uri = new string[immediateUriTemplates.Length];
                for (int i = 0; i < uri.Length; i++)
                {
                    // handle special case (deprecate?)
                    uri[i] = domain_root + immediateUriTemplates[i].Replace("{@id}", id);
                    // handle arg collection
                    if (arg_list != null)
                        uri[i] = util.ReplaceArgs(uri[i], arg_list);
                }

                // initialize object and start on a new thread
                CollectionRequestor creq = new CollectionRequestor();
                creq.Uri = uri;
                creq.defaultType = "text/xml";
                creq.UriTypeMap = uriCache;
                creq.Execute();
            }

            // do the background invalidations on a diff thread
            // good for updating the cache of archive and other non-essential resources
            // NOTE: in a heavy trffic mode, can this starve the thread pool?
            if (backgroundUriTemplates != null)
            {
                // build list of absolute uri
                string[] uri = new string[backgroundUriTemplates.Length];
                for (int i = 0; i < uri.Length; i++)
                {
                    // special case (deprecate?)
                    uri[i] = domain_root + backgroundUriTemplates[i].Replace("{@id}", id);
                    // arg collection
                    if (arg_list != null)
                        uri[i] = util.ReplaceArgs(uri[i], arg_list);
                }

                // initialize object and start on a new thread
                CollectionRequestor creq = new CollectionRequestor();
                creq.Uri = uri;
                creq.defaultType = "text/xml";
                creq.UriTypeMap = uriCache;
                System.Threading.Thread th = new System.Threading.Thread(creq.Execute);
                th.Start();

                // wait .1 second before returning 
                System.Threading.Thread.Sleep(100);
            }
        }

        public CacheObject GetCacheItem(HttpContext ctx, string ctype)
        {
            CacheObject co = null;
            string out_text = string.Empty;
            string cachename = GetCacheMemoryName(ctx,ctype);
            bool nocache = false;

            // is caching config'ed as off?
            if (util.GetConfigSectionItem(Constants.cfg_exyusSettings, Constants.cfg_caching) == "false")
                return null;

            // did requestor specify no-cache?
            if (util.GetHttpHeader(Constants.hdr_cache_control).IndexOf(Constants.hdr_no_cache) != -1)
                nocache = true;

            // see if valid cache in memory is available
            co = (CacheObject)ctx.Cache.Get(cachename); 

            // check for nocache flag from caller
            if (co != null)
            {
                if (nocache == true)
                {
                    ctx.Cache.Remove(cachename);
                    co = null;
                }
            }

            // check for local refresh
            // this 'rolls' the internal expiration forward
            if (co != null)
            {
                if (co.LocalMaxAge != 0)
                    co = PutCacheItem(ctx, co);
            }

            // return whatever we got
            return co;
        }

        public CacheObject PutCacheItem(HttpContext ctx, CacheObject co)
        {
            return PutCacheItem(ctx, co.Payload, co.MaxAge, co.LocalMaxAge, co.LastModified, co.ContentType);
        }
        public CacheObject PutCacheItem(HttpContext ctx, string data)
        {
            return PutCacheItem(ctx, data, 0, 0, DateTime.UtcNow, string.Empty);
        }
        public CacheObject PutCacheItem(HttpContext ctx, string data, int maxage)
        {
            return PutCacheItem(ctx, data, maxage, 0, DateTime.UtcNow, string.Empty);
        }
        public CacheObject PutCacheItem(HttpContext ctx, string data, int maxage, int localage)
        {
            return PutCacheItem(ctx, data, maxage, localage, DateTime.UtcNow, string.Empty);
        }
        public CacheObject PutCacheItem(HttpContext ctx, string data, int maxage, int localage, string contenttype)
        {
            return PutCacheItem(ctx, data, maxage, localage, DateTime.UtcNow, contenttype);
        }
        public CacheObject PutCacheItem(HttpContext ctx, string data, int maxage, int localage, DateTime dt, string contenttype)
        {
            DateTime localdt = System.DateTime.Now;
            DateTime gmdt = dt;

            if (util.GetConfigSectionItem(Constants.cfg_exyusSettings, Constants.cfg_caching) == "false")
                return null;

            string cachename = GetCacheMemoryName(ctx,contenttype);

            // create & populate object
            CacheObject co = new CacheObject(gmdt, util.MD5(data,true), data);
            co.LocalMaxAge = localage;
            co.MaxAge = maxage;
            co.ContentType = contenttype;
            
            // set last-mod, if new item
            if (co.LastModified == DateTime.MinValue)
                co.LastModified = gmdt;
            
            // set public cache age, if passed
            if(maxage!=0)
                co.Expires = gmdt.AddSeconds(maxage);
    
            // log as sliding or absolute
            if (co.LocalMaxAge!=0)
            {
                // log as sliding expiration
                ctx.Cache.Add(
                    cachename,
                    co,
                    null,
                    System.Web.Caching.Cache.NoAbsoluteExpiration,
                    new TimeSpan(0,0,localage),
                    System.Web.Caching.CacheItemPriority.Normal,
                    null);
            }
            else
            {
                // log as absolute expiration
                ctx.Cache.Add(
                    cachename,
                    co,
                    null,
                    localdt.AddSeconds(maxage),
                    System.Web.Caching.Cache.NoSlidingExpiration,
                    System.Web.Caching.CacheItemPriority.Normal,
                    null);
            }
            return co;
        }

        // see if caller already has a good copy
        public bool Return304(HttpContext ctx, CacheObject co)
        {
            //no object in cache?
            if (co == null)
                return false;

            // did requestor specify no-cache?
            if (util.GetHttpHeader(Constants.hdr_cache_control).IndexOf(Constants.hdr_no_cache) != -1)
                return false;

            // old http 1.0 option
            if (util.GetHttpHeader("Pragma").IndexOf(Constants.hdr_no_cache) != -1)
                return false;

            // check for etag = if-match
            if (
                ctx.Request.Headers[Constants.hdr_if_none_match] != null 
                && 
                string.Format("\"{0}\"",co.Etag) == ctx.Request.Headers[Constants.hdr_if_none_match]
                )
                return true;

            // check for etag != if-match
            if (
                ctx.Request.Headers[Constants.hdr_if_none_match] != null 
                &&
                string.Format("\"{0}\"", co.Etag) != ctx.Request.Headers[Constants.hdr_if_none_match]
                )
                return false;

            // check for last-modified (if-modified-since)
            string fdate = string.Format(Constants.fmt_gmtdate, co.LastModified);
            DateTime mdate = (ctx.Request.Headers[Constants.hdr_if_modified_since]!=null?DateTime.Parse(ctx.Request.Headers[Constants.hdr_if_modified_since]).AddHours(4):DateTime.UtcNow);
            if (
                ctx.Request.Headers[Constants.hdr_if_modified_since] != null 
                && 
                    (ctx.Request.Headers[Constants.hdr_if_modified_since] == fdate
                    ||
                    mdate > co.LastModified
                    )
                )
                return true;

            return false;
        }

        // generate a proper memory name for the resource
        private string GetCacheMemoryName(HttpContext ctx, string ctype)
        {
            return util.Base64Encode((ctx.Request.RawUrl+ctype));
        }
    }
}
