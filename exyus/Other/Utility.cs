using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

using System.Web;
using System.Configuration;
using System.Collections.Specialized;
using System.Security.Cryptography;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;

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
    public struct PutHeaders
    {
        public string IfMatch;
        public string IfNoneMatch;
        public string IfUnmodifiedSince;
        public string IfModifiedSince;
        public string LastModified;

        public PutHeaders(HttpContext ctx)
        {
            Utility util = new Utility();
            this.IfMatch = util.GetHttpHeader(Constants.hdr_if_match, ctx.Request.Headers);
            this.IfUnmodifiedSince = util.GetHttpHeader(Constants.hdr_if_unmodified_since, ctx.Request.Headers);
            this.IfNoneMatch = util.GetHttpHeader(Constants.hdr_if_none_match, ctx.Request.Headers);
            this.IfModifiedSince = util.GetHttpHeader(Constants.hdr_if_modified_since, ctx.Request.Headers);
            this.LastModified = util.GetHttpHeader(Constants.hdr_last_modified, ctx.Request.Headers);
        }
    }

    public class Utility
    {
        public XmlDocument ProcessFormVars(NameValueCollection formVars)
        {
            XmlDocument doc = new XmlDocument();
            XmlNode root = doc.CreateElement("form");
            for (int i = 0; i < formVars.Count; i++)
            {
                XmlNode node = doc.CreateElement(formVars.Keys[i]);
                node.AppendChild(doc.CreateTextNode(formVars[i]));
                root.AppendChild(node);
            }
            doc.AppendChild(root);
            return doc;
        }

        public void SafeAdd(ref Hashtable list, string key, string value)
        {
            if(list.Contains(key))
            {
                list.Remove(key);
            }
            list.Add(key,value);
        }

        public string MapUrl(string filepath)
        {
            return MapUrl(filepath, string.Empty);
        }
        public string MapUrl(string filepath, string prefix)
        {
           return string.Format("{0}", filepath.Replace(HttpContext.Current.Server.MapPath("~"), "").Replace(@"\", "/").Replace(prefix,""));
        }

        public void SaveUriCache(Hashtable cacheUri)
        {
            IFormatter formatter = new BinaryFormatter();
            using (Stream fs = new FileStream(HttpContext.Current.Server.MapPath("~/storage/uri-cache.bin"), FileMode.Create, FileAccess.Write, FileShare.None))
            {
                formatter.Serialize(fs, cacheUri);
                fs.Close();
            }

            HttpContext.Current.Cache.Add(
                "_cacheUri",
                cacheUri,
                null,
                System.Web.Caching.Cache.NoAbsoluteExpiration,
                System.Web.Caching.Cache.NoSlidingExpiration,
                System.Web.Caching.CacheItemPriority.High,
                null);
        }

        public Hashtable LoadUriCache()
        {
            Hashtable cacheUri = new Hashtable();

            cacheUri = (Hashtable)HttpContext.Current.Cache.Get("_cacheUri");
            if (cacheUri == null)
            {
                cacheUri = new Hashtable();

                using (FileStream fs = new FileStream(HttpContext.Current.Server.MapPath("~/storage/uri-cache.bin"), FileMode.Open))
                {
                    BinaryFormatter formatter = new BinaryFormatter();
                    cacheUri = (Hashtable)formatter.Deserialize(fs);
                    fs.Close();
                }
            }
            return cacheUri;
        }

        public Stream LoadAndTokenize(string filePath, Hashtable parameters)
        {
            string contents = string.Empty;
            contents = (string)HttpContext.Current.Cache.Get(filePath);
            if (contents == null)
            {
                // load file into string pile
                using (StreamReader reader = File.OpenText(filePath))
                {
                    contents = reader.ReadToEnd();
                    reader.Close();
                }

                HttpContext.Current.Cache.Add(
                    filePath,
                    contents,
                    new System.Web.Caching.CacheDependency(filePath),
                    System.Web.Caching.Cache.NoAbsoluteExpiration,
                    System.Web.Caching.Cache.NoSlidingExpiration,
                    System.Web.Caching.CacheItemPriority.Default,
                    null);
            }

            return LoadAndTokenizeString(contents, parameters);
        }

        public Stream LoadAndTokenizeString(string contents, Hashtable parameters)
        {
            // if we have args, scan and replace as needed
            if (parameters.Count > 0)
            {
                // get params to check
                string[] keys = new string[parameters.Count];
                parameters.Keys.CopyTo(keys, 0);

                // loop for each param
                foreach (string key in keys)
                    contents = contents.Replace(string.Format("${0}$", key), parameters[key].ToString());
            }

            // now go remove any other tokens that were not resolved
            contents = Regex.Replace(contents, @"(\$([^$]+)\$)", string.Empty, RegexOptions.IgnoreCase);

            // return results as memory stream
            return new MemoryStream(System.Text.Encoding.UTF8.GetBytes(contents));
        }

        public string LookUpFileType(string contenttype)
        {
            SortedList filetypes;
            string rtn = string.Empty;
            XmlNodeList mediaNodes = null;
            XmlDocument xmldoc = new XmlDocument();
            string fullpath = string.Empty;
            HttpContext ctx = HttpContext.Current;

            fullpath = ctx.Server.MapPath(GetConfigSectionItem(Constants.cfg_exyusSettings, Constants.cfg_mediaTypes));

            filetypes = (SortedList)ctx.Cache.Get(fullpath);
            if (filetypes == null)
            {
                filetypes = new SortedList();
                using (XmlTextReader xtr = new XmlTextReader(fullpath))
                {
                    xmldoc.Load(xtr);
                    xtr.Close();
                }

                mediaNodes = xmldoc.SelectNodes("/media-types/media");
                int numNodes = mediaNodes.Count;
                if (numNodes > 0)
                {
                    for (int i = 0; i < numNodes; i++)
                        filetypes.Add(mediaNodes[i].Attributes["content-type"].Value, mediaNodes[i].Attributes["file-type"].Value);
                }

                // add to cache w/ file dependency
                ctx.Cache.Add(
                    fullpath,
                    filetypes,
                    new System.Web.Caching.CacheDependency(fullpath),
                    System.Web.Caching.Cache.NoAbsoluteExpiration,
                    System.Web.Caching.Cache.NoSlidingExpiration,
                    System.Web.Caching.CacheItemPriority.High,
                    null);

            }

            return (filetypes[contenttype] != null ? filetypes[contenttype].ToString() : string.Empty);
        }

        public string SetMediaType(Exyus.Web.WebResource wr)
        {
            return SetMediaType(wr, null);
        }
        public string SetMediaType(Exyus.Web.WebResource wr, string[] mtypes)
        {
            string accept = string.Empty;
            string[] mediaTypes;

            // override?
            accept = (wr.Context.Request.QueryString["_accept"] != null ? wr.Context.Request.QueryString["_accept"] : null);

            // check header based on http method
            if (accept == null)
            {
                switch (wr.Context.Request.HttpMethod.ToLower())
                {
                    case "head":
                    case "get":
                    case "options":
                        accept = wr.Context.Request.Headers["accept"];
                        break;
                    case "put":
                    case "post":
                    case "delete":
                        accept = wr.Context.Request.Headers["content-type"];
                        break;
                }
            }

            // if not supplied, get this resource's media type(s)
            if (mtypes == null)
            {
                try
                {
                    mediaTypes = ((MediaTypes)wr.GetType().GetCustomAttributes(typeof(MediaTypes), false)[0]).Types;
                }
                catch (Exception ex)
                {
                    mediaTypes = new string[] { wr.ContentType };  // assume this is the only one
                }
            }
            else
            {
                mediaTypes = mtypes;
            }

            // now determine mediatype for this request
            MimeParser mp = new MimeParser(accept);
            string mtype = mp.GetBestFit(mediaTypes, wr.ContentType);
            if (mtype == string.Empty)
                throw new HttpException((int)HttpStatusCode.NotAcceptable, HttpStatusCode.NotAcceptable.ToString());
            else
            {
                // force vary header, conn-neg results in new mime-type
                if (mtype != wr.ContentType)
                    wr.Context.Response.AppendHeader("vary", "accept");
                
                // set the mime-type for this request
                wr.ContentType = mtype;
            }

            // return to caller
            return mtype;
        }

        public void CheckPutCreateCondition(PutHeaders ph, bool allowcreate, string etag, ref string put_error, ref bool save_item)
        {
            if (allowcreate == false)
            {
                put_error = "Cannot create using PUT.";
                save_item = false;
            }
            else if
                (
                    (ph.IfNoneMatch == "*" && etag == string.Empty)  // anything
                    ||
                    (ph.IfNoneMatch == string.Empty)   // none passed
                    ||
                    (ph.IfNoneMatch != etag) // passed, but not equal
                )
                save_item = true;
            else
            {
                put_error = "Header mismatch (If-None-Match). Unable to create.";
                save_item = false;
            }
        }

        public void CheckPutUpdateCondition(PutHeaders ph, string etag, string last_mod, ref string put_error, ref bool save_item)
        {
            // compare resource headers to client headers
            if (ph.IfMatch == string.Empty && ph.IfUnmodifiedSince == string.Empty && ph.LastModified == string.Empty)
            {
                // gotta gimme one of these
                put_error = "Missing If-Match, If-Unmodified-Since or Last-Modified headers. Unable to update.";
                save_item = false;
            }
            else if (ph.IfMatch != string.Empty && etag != ph.IfMatch)
            {
                // etags must match
                put_error = "Header mismatch (If-Match). Unable to update.";
                save_item = false;
            }
            else if (ph.IfUnmodifiedSince != string.Empty && last_mod != ph.IfUnmodifiedSince)
            {
                // last mod date must match
                put_error = "Header mismatch (If-Unmodified-Since). Unable to update.";
                save_item = false;
            }
            else if (ph.LastModified != string.Empty && (DateTime.Parse(last_mod) >  DateTime.Parse(ph.LastModified)))
            {
                // last mod date must match
                put_error = "Header mismatch (Last-Modified). Unable to update.";
                save_item = false;
            }
            else
            {
                save_item = true;
            }
        }

        public string FixUpHttpError(int code, string msg)
        {
            // trap for 'unsupported' error codes
            switch (code)
            {
                case 422:
                    msg = "Cannot Process Entity";
                    break;
                case 423:
                    msg = "Locked";
                    break;
                case 424:
                    msg = "Failed Dependency";
                    break;
                case 507:
                    msg = "Insuffcient Storage";
                    break;
            }

            return msg;
        }
        public string ReplaceArgs(string data, Hashtable arg_list)
        {
            IDictionaryEnumerator denum = arg_list.GetEnumerator();
            while (denum.MoveNext())
            {
                data = data.Replace("{" + denum.Key.ToString() + "}", denum.Value.ToString());
            }

            return data;
        }

        public Hashtable ParseUrlPattern(string url, string pattern)
        {
            string name = string.Empty;
            Hashtable args = new Hashtable();

            Regex r = new Regex(pattern);
            GroupCollection gcoll = r.Match(url).Groups;

            // collect important uri parts (based on regex)
            for (int i = 0; i < gcoll.Count; i++)
            {
                name = r.GroupNameFromNumber(i);
                // fix numeric group name
                if (Regex.IsMatch(name, "^[0-9]+$"))
                    name = "arg" + name;
                args.Add(name, gcoll[i].Value);
            }

            return args;
        }
        public string AuthFormUrl(string uri)
        {
            HttpApplication app = HttpContext.Current.ApplicationInstance;
            int timeout = Convert.ToInt32((GetConfigSectionItem(Constants.cfg_exyusSecurity, Constants.cfg_authTimeout) != string.Empty ? GetConfigSectionItem(Constants.cfg_exyusSecurity, Constants.cfg_authTimeout) : "20"));
            string rtn = string.Empty;
            SortedList formUrls = null;
            string form_url = string.Empty;
            string formUrl = GetConfigSectionItem(Constants.cfg_exyusSecurity, Constants.cfg_authFormUrl, string.Empty);

            if (formUrl == string.Empty)
                return string.Empty;

            // see if we have this already
            form_url = (string)app.Context.Cache.Get(uri + "_formurl");
            if (form_url != null)
                rtn = (form_url == "true" ? formUrl : string.Empty);
            else
            {
                // we need this for cache dependency
                string authFile = GetConfigSectionItem(Constants.cfg_exyusSecurity, Constants.cfg_authUrls);
                string fullpath = app.Request.MapPath(authFile);

                // go get url collection
                formUrls = LoadFormUrls();
                // find the one we want
                for (int i = formUrls.Count - 1; i > -1; i--)
                {
                    if (new Regex(formUrls.GetKey(i).ToString(), RegexOptions.IgnoreCase).IsMatch(uri))
                    {
                        if (formUrls.GetByIndex(i).ToString().Split('|')[0] == "true")
                            rtn = formUrl;
                        else
                            rtn = string.Empty;

                        // save this for later
                        app.Context.Cache.Add(
                            uri + "_formurl",
                            formUrls.GetByIndex(i).ToString(),
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

        private SortedList LoadFormUrls()
        {
            SortedList formUrls = null;
            HttpApplication app = HttpContext.Current.ApplicationInstance;
            int timeout = Convert.ToInt32((GetConfigSectionItem(Constants.cfg_exyusSecurity, Constants.cfg_authTimeout) != string.Empty ? GetConfigSectionItem(Constants.cfg_exyusSecurity, Constants.cfg_authTimeout) : "20"));
            string formFile = GetConfigSectionItem(Constants.cfg_exyusSecurity, Constants.cfg_authUrls);
            string fullpath = app.Request.MapPath(formFile);
            XmlNodeList formNodes = null;
            XmlDocument xmldoc = new XmlDocument();

            // load list of secured urls from cache or xml
            formUrls = (SortedList)app.Context.Cache.Get("formUrls");
            if (formUrls == null)
            {
                formUrls = new SortedList();
                using (XmlTextReader xtr = new XmlTextReader(fullpath))
                {
                    xmldoc.Load(xtr);
                    xtr.Close();
                }

                formNodes = xmldoc.SelectNodes("/urls/url");
                int numUrls = formNodes.Count;
                if (numUrls > 0)
                {
                    for (int i = 0; i < numUrls; i++)
                        formUrls.Add(formNodes[i].Attributes["path"].Value, (formNodes[i].Attributes["form"]) != null ? formNodes[i].Attributes["form"].Value : "");
                }

                // add to cache w/ file dependency
                app.Context.Cache.Add(
                    "formUrls",
                    formUrls,
                    new System.Web.Caching.CacheDependency(fullpath),
                    System.Web.Caching.Cache.NoAbsoluteExpiration,
                    new TimeSpan(0, timeout, 0),
                    System.Web.Caching.CacheItemPriority.High,
                    null);
            }

            return formUrls;
        }

        public bool MaxTries()
        {
            bool rtn = false;
            int max = 5;
            int tries = 0;
            object count = null;
            string cachekey = MD5(HttpContext.Current.Request.UserAgent + HttpContext.Current.Request.UserHostAddress);

            count = HttpContext.Current.Cache.Get(cachekey);
            if (count != null)
                tries = (int)count;
            tries++;

            if (tries > max)
                rtn = true;
            else
            {
                HttpContext.Current.Cache.Remove(cachekey);

                HttpContext.Current.Cache.Add(
                    cachekey,
                    tries,
                    null,
                    System.Web.Caching.Cache.NoAbsoluteExpiration,
                    new TimeSpan(0, 0, 1),
                    System.Web.Caching.CacheItemPriority.High,
                    null);
                rtn = false;
            }
            return rtn;
        }

        public string MD5BinHex(string val)
        {
            Encoding encoding = new ASCIIEncoding();
            byte[] bs = new MD5CryptoServiceProvider().ComputeHash(encoding.GetBytes(val));
            string hash = "";

            for (int i = 0; i < 16; i++)
                hash = String.Concat(hash, String.Format("{0:x02}", bs[i]));

            return hash;
        }

        public string FixEncoding(string data)
        {
            // force the encoding to read utf-8
            Regex rx = new Regex("^<\\?xml version=\"1\\.0\" encoding=\"utf-16\"\\?>(.*)", RegexOptions.IgnoreCase);
            Match mx = rx.Match(data);
            if (mx.Success)
                data = rx.Replace(data, "<?xml version=\"1.0\" encoding=\"utf-8\"?>$1");
            return data;
        }
        public string UID()
        {
            return string.Format("x{0:x}", DateTime.UtcNow.Ticks);
        }

        // valdiate posted (x)html fragments
        public void ValidateXHtmlNodes(ref XmlDocument xmlin, string[] XHtmlNodes)
        {
            if (XHtmlNodes != null)
            {
                string fragment = string.Empty;
                string html_errors = string.Empty;

                for (int i = 0; i < XHtmlNodes.Length; i++)
                {
                    if (XHtmlNodes[i] != string.Empty)
                    {
                        if (xmlin.SelectSingleNode(XHtmlNodes[i] + "/text()") != null)
                        {
                            fragment = xmlin.SelectSingleNode(XHtmlNodes[i] + "/text()").Value;
                            html_errors = ValidateFragment(ref fragment);
                            if (html_errors != string.Empty)
                                throw new HttpException(400, html_errors);
                            else
                                xmlin.SelectSingleNode(XHtmlNodes[i]).InnerText = fragment;
                        }
                    }
                }
            }
        }

        public string GetRequestArg(string key)
        {
            if (this.ctx.Request[key] != null)
                return this.ctx.Request[key];
            else
                return string.Empty;
        }

        public virtual bool ValidateUser(string user, string pass, out string[] roles, out SortedList permissions)
        {
            int timeout = 20; // default session/cache timeout
            HttpContext ctx = HttpContext.Current;
            bool isUserCached = true;
            string cache_key = MD5(user + pass);
            string xpath = String.Format("/users/user[@name='{0}'][@password='{1}']", user, pass);
            string userFile = GetConfigSectionItem(Constants.cfg_exyusSecurity, Constants.cfg_authUsers);
            string fullpath = ctx.Request.MapPath(userFile);
            XmlNode userNode = null;
            XmlDocument xmldoc = new XmlDocument();

            // get the user document (from cache or xml)
            xmldoc = (XmlDocument)ctx.Cache.Get(fullpath);
            if (xmldoc == null)
            {
                xmldoc = new XmlDocument();
                using (XmlTextReader xtr = new XmlTextReader(fullpath))
                {
                    xmldoc.Load(xtr);
                    xtr.Close();
                }

                // cache document using file dependency
                ctx.Cache.Add(
                    fullpath,
                    xmldoc,
                    new System.Web.Caching.CacheDependency(fullpath),
                    System.Web.Caching.Cache.NoAbsoluteExpiration,
                    new TimeSpan(0, timeout, 0),
                    System.Web.Caching.CacheItemPriority.High,
                    null);

            }
            // get user (from cache or file)
            userNode = (XmlNode)ctx.Cache.Get(cache_key);
            if (userNode == null)
            {
                isUserCached = false;
                userNode = xmldoc.SelectSingleNode(xpath);
            }

            // if we have a valid user, get roles and permissions
            if (userNode != null)
            {
                // get roles (from cache or from xml)
                roles = (string[])ctx.Cache.Get(cache_key + "_roles");
                if (roles == null)
                {
                    XmlNodeList roleNodes = userNode.SelectNodes("role");
                    int numRoles = roleNodes.Count;
                    roles = new string[roleNodes.Count];

                    if (numRoles > 0)
                    {
                        for (int i = 0; i < numRoles; i++)
                        {
                            XmlNode roleNode = roleNodes[i];
                            roles[i] = roleNode.Attributes["name"].Value;
                        }
                    }
                }

                // get permissions (from cache or xml)
                permissions = (SortedList)ctx.Cache.Get(cache_key + "_permissions");
                if (permissions == null)
                {
                    permissions = new SortedList();
                    XmlNodeList permNodes = userNode.SelectNodes("permission");
                    int numPerms = permNodes.Count;

                    if (numPerms > 0)
                    {
                        for (int i = 0; i < numPerms; i++)
                        {
                            XmlNode permNode = permNodes[i];
                            permissions.Add(permNode.Attributes["path"].Value, permNode.Attributes["methods"].Value);
                        }
                    }
                }

                // cache the data, if needed
                if (!isUserCached)
                {
                    ctx.Cache.Add(
                        cache_key,
                        userNode,
                        new System.Web.Caching.CacheDependency(fullpath),
                        System.Web.Caching.Cache.NoAbsoluteExpiration,
                        new TimeSpan(0, timeout, 0),
                        System.Web.Caching.CacheItemPriority.AboveNormal,
                        null);

                    ctx.Cache.Add(
                        cache_key + "_roles",
                        roles,
                        new System.Web.Caching.CacheDependency(fullpath),
                        System.Web.Caching.Cache.NoAbsoluteExpiration,
                        new TimeSpan(0, timeout, 0),
                        System.Web.Caching.CacheItemPriority.AboveNormal,
                        null);

                    ctx.Cache.Add(
                        cache_key + "_permissions",
                        permissions,
                        new System.Web.Caching.CacheDependency(fullpath),
                        System.Web.Caching.Cache.NoAbsoluteExpiration,
                        new TimeSpan(0, timeout, 0),
                        System.Web.Caching.CacheItemPriority.AboveNormal,
                        null);
                }
                return true;
            }
            else
            {
                roles = new string[0];
                permissions = new SortedList();
                return false;
            }
        }

        // check for inline xsl
        public string GetInlineXslFile(string xmlfile)
        {
            string xslfile = string.Empty;
            string stype = string.Empty;

            XPathDocument xpdoc = null;
            XPathNavigator xpn = null;

            xpdoc = new XPathDocument(xmlfile);

            // get the xml-stylehseet pi from the xmldoc
            // NOTE: gets only the first xml-stylesheet pi and expects it to be type="text/xsl"
            xpn = xpdoc.CreateNavigator().SelectSingleNode(Constants.xsl_pi);
            if (xpn == null)
                return xslfile; // no inline, just return as is

            // parse to make sure this is an xsl ref
            stype = Regex.Match(xpn.Value, Constants.xsl_type).Groups[1].Value;
            if (stype != "text/xsl")
                return xslfile; // not xsl

            // parse to find the href in in the pi
            xslfile = Regex.Match(xpn.Value, Constants.xsl_href).Groups[1].Value;

            return xslfile;
        }

        public string GetHttpHeader(string name)
        {
            if (HttpContext.Current.Request.Headers[name] != null)
                return HttpContext.Current.Request.Headers[name];
            else
                return string.Empty;
        }
        public string GetHttpHeader(string name, NameValueCollection headers)
        {
            if (headers[name] != null)
                return headers[name];
            else
                return string.Empty;
        }

        public XsltArgumentList GetXsltArgs()
        {
            return GetXsltArgs(new Hashtable());
        }
        public XsltArgumentList GetXsltArgs(Hashtable htargs)
        {
            XsltArgumentList xargs = new XsltArgumentList();
            Hashtable args = GetArgs();

            // handle built-ins
            IDictionaryEnumerator en = args.GetEnumerator();
            while (en.MoveNext())
            {
                xargs.AddParam(en.Key.ToString(), "", en.Value.ToString());
            }

            // handle pass-ins
            en = htargs.GetEnumerator();
            while (en.MoveNext())
            {
                xargs.AddParam(en.Key.ToString(), "", en.Value.ToString());
            }

            return xargs;
        }

        public Hashtable GetArgs()
        {
            Hashtable args = new Hashtable();
            HttpContext ctx = HttpContext.Current;

            // get document args
            string doc = GetDocId(ctx.Request.Url.AbsoluteUri);
            string docname = GetDocIdWithoutTail(ctx.Request.Url.AbsoluteUri);
            string fragment = this.ctx.Request.Url.Fragment;
            string user = this.ctx.User.Identity.Name;
            string scheme = this.ctx.Request.Url.Scheme;
            string authority = this.ctx.Request.Url.Authority;
            string path = this.ctx.Request.Path;
            string method = this.ctx.Request.HttpMethod.ToLower();
            string port = this.ctx.Request.Url.Port.ToString();
            string absolutepath = this.ctx.Request.Url.AbsolutePath;
            string absoluteuri = this.ctx.Request.Url.AbsoluteUri;
            string dnssafehost = this.ctx.Request.Url.DnsSafeHost;
            string host = this.ctx.Request.Url.Host;
            string originalstring = this.ctx.Request.Url.OriginalString;
            string pathandquery = this.ctx.Request.Url.PathAndQuery;
            string query = this.ctx.Request.Url.Query;

            if (user != string.Empty)
                args.Add("_user", user);
            if (method != string.Empty)
                args.Add("_method", method);
            if (fragment != string.Empty)
                args.Add("_fragment", fragment);
            if (doc != string.Empty)
                args.Add("_doc", doc);
            if (docname != string.Empty)
                args.Add("_docname", docname);
            if (scheme != string.Empty)
                args.Add("_scheme", scheme);
            if (authority != string.Empty)
                args.Add("_authority", authority);
            if (path != string.Empty)
                args.Add("_path", path.Replace(".xcs",""));
            if (port != string.Empty)
                args.Add("_port", port);
            if (absolutepath != string.Empty)
                args.Add("_absolute-path", absolutepath.Replace(".xcs", ""));
            if (absoluteuri != string.Empty)
                args.Add("_absolute-uri", absoluteuri.Replace(".xcs", ""));
            if (dnssafehost != string.Empty)
                args.Add("_dns-safe-host", dnssafehost);
            if (host != string.Empty)
                args.Add("_host", host);
            if (originalstring != string.Empty)
                args.Add("_original-string", originalstring.Replace(".xcs", ""));
            if (pathandquery != string.Empty)
                args.Add("_path-and-query", pathandquery.Replace(".xcs", ""));
            if (query != string.Empty)
                args.Add("_query", query);

            // handle query string contents
            if (ctx.Request.Url.Query.Length != 0)
            {
                string qs = ctx.Request.Url.Query.Remove(0, 1);

                // add whole querystring
                args.Add("_qs", "?" + ctx.Server.HtmlEncode(qs));

                // now walk through the list
                string[] arg_set = Regex.Split(ctx.Server.UrlDecode(qs), "[&,;]", RegexOptions.IgnoreCase);
                for (int i = 0; i < arg_set.Length; i++)
                {
                    string[] pair = arg_set[i].Split('=');
                    switch (pair.Length)
                    {
                        case 2:
                            args = safeAdd(args, arg_set[i].Split('=')[0].Trim(), arg_set[i].Split('=')[1]);
                            break;
                        case 1:
                            args = safeAdd(args, arg_set[i].Split('=')[0].Trim(), arg_set[i].Split('=')[0].Trim());
                            break;
                    }
                }
            }

            // handle cookies
            if (ctx.Request.Cookies.Count > 0)
            {
                for (int i = 0; i < ctx.Request.Cookies.Count; i++)
                {
                    args = safeAdd(args,"ck-" + ctx.Request.Cookies[i].Name, ctx.Request.Cookies[i].Value);
                }
            }
            return args;
        }

        private Hashtable safeAdd(Hashtable arg_set,string key, string value)
        {
            if (key.Length != 0)
            {
                if (Regex.IsMatch(key[0].ToString(), "[0-9]"))
                    key = "_" + key;
                if (arg_set.ContainsKey(key))
                {
                    arg_set[key] = arg_set[key] + ";" + value;
                }
                else
                {
                    arg_set.Add(key, value);
                }
            }
            return arg_set;
        }

        // validate any user-input for proper xml/xhtml
        public string ValidateFragment(ref string fragment)
        {
            string rtn = string.Empty;
            string work = fragment;

            // replace entities
            fragment = fragment.Replace("&nbsp;", "&#160;");

            work = string.Format("<fragment>{0}</fragment>", fragment);

            XHtmlValidator val = new XHtmlValidator(work);
            Collection<ValidationRecord> errors = val.Validate();
            if (errors.Count != 0)
            {
                foreach (ValidationRecord rec in errors)
                    rtn += string.Format("{0}\n", rec);
            }

            return rtn;
        }

        public System.Net.NetworkCredential GetUserCredentials(System.Security.Principal.IPrincipal p)
        {
            Exyus.Security.ExyusPrincipal ep = (Exyus.Security.ExyusPrincipal)p;// ctx.User;
            return new NetworkCredential(((Exyus.Security.ExyusIdentity)ep.Identity).Name, ((Exyus.Security.ExyusIdentity)ep.Identity).Password);

        }
        public System.Net.NetworkCredential GetSystemCredentials()
        {
            // get system user from config file
            string sysuser = GetConfigSectionItem(Constants.cfg_exyusSecurity, Constants.cfg_systemUser);
            if (sysuser == string.Empty)
                sysuser = "Guest;";
            string[] up = sysuser.Split(';');

            // create credentails with the system user & pass
            NetworkCredential nc = new NetworkCredential(up[0], up[1]);
            return nc;
        }

        HttpContext ctx = HttpContext.Current;

        Random objRandom = new Random(Convert.ToInt32(System.DateTime.UtcNow.Ticks % Int32.MaxValue));

        public int GetRandomNumber()
        {
            return GetRandomNumber(1, 10000);
        }
        public int GetRandomNumber(int low, int high)
        {
            return objRandom.Next(low, high + 1);
        }

        // get item from selected section in config file
        public string GetConfigSectionItem(string section, string key)
        {
            return GetConfigSectionItem(section, key, "");
        }
        public string GetConfigSectionItem(string section, string key, string defaultValue)
        {
            if (ConfigurationManager.GetSection(section) != null)
            {
                NameValueCollection coll = (NameValueCollection)ConfigurationManager.GetSection(section);
                if (coll[key] != null)
                    return coll[key].ToString();
                else
                    return defaultValue;
            }
            else
                return defaultValue;
        }

        // get item from config file
        public string GetConfigItem(string key)
        {
            return GetConfigItem(key, "");
        }
        public string GetConfigItem(string key, string defaultValue)
        {
            if (ConfigurationManager.AppSettings[key] != null)
                return ConfigurationManager.AppSettings[key].ToString();
            else
                return defaultValue;
        }

        // cookie routines
        public string CookieRead(string key)
        {
            if (ctx.Request.Cookies[key] != null)
                return ctx.Request.Cookies[key].Value;
            else
                return string.Empty;
        }

        public void CookieClear(string key)
        {
            if (ctx.Request.Cookies[key] != null)
            {
                HttpCookie aCookie = new HttpCookie(key);
                aCookie.Expires = DateTime.UtcNow.AddDays(-1);
                ctx.Response.Cookies.Add(aCookie);
            }
        }

        public void CookieWrite(string key, string value)
        {
            CookieWrite(key, value, 0, "");
        }
        public void CookieWrite(string key, string value, string path)
        {
            CookieWrite(key, value, 0, path);
        }
        public void CookieWrite(string key, string value, double days, string path)
        {
            HttpCookie aCookie = new HttpCookie(key);
            aCookie.Value = value;
            if (days > 0)
                aCookie.Expires = DateTime.UtcNow.AddDays(days);
            if (path.Length != 0)
                aCookie.Path = path;

            ctx.Response.Cookies.Add(aCookie);
        }

        public string Base64Encode(string data)
        {
            try
            {
                byte[] encData_byte = new byte[data.Length];
                encData_byte = System.Text.Encoding.UTF8.GetBytes(data);
                string encodedData = Convert.ToBase64String(encData_byte);
                return encodedData;
            }
            catch (Exception e)
            {
                throw new Exception("Error in base64Encode" + e.Message);
            }
        }

        public string Base64Decode(string data)
        {
            try
            {
                System.Text.UTF8Encoding encoder = new System.Text.UTF8Encoding();
                System.Text.Decoder utf8Decode = encoder.GetDecoder();

                byte[] todecode_byte = Convert.FromBase64String(data);
                int charCount = utf8Decode.GetCharCount(todecode_byte, 0, todecode_byte.Length);
                char[] decoded_char = new char[charCount];
                utf8Decode.GetChars(todecode_byte, 0, todecode_byte.Length, decoded_char, 0);
                string result = new String(decoded_char);
                return result;
            }
            catch (Exception e)
            {
                throw new Exception("Error in base64Decode" + e.Message);
            }
        }

        public string MD5(string data)
        {
            return MD5(data, false);
        }
        public string MD5(string data, bool removeTail)
        {
            string rtn = Convert.ToBase64String(new System.Security.Cryptography.MD5CryptoServiceProvider().ComputeHash(System.Text.Encoding.Default.GetBytes(data)));
            if (removeTail)
                return rtn.Replace("=", "");
            else
                return rtn;
        }

        private string SHA1(string data)
        {
            string rtn = string.Empty;
            SHA1 md = new SHA1CryptoServiceProvider();

            byte[] digest = md.ComputeHash(Encoding.Default.GetBytes(data));
            foreach (byte i in digest)
            {
                rtn += i.ToString("x2");
            }
            return rtn;

        }

        private string GetDocId(string url)
        {
            string path = string.Empty;
            string doc = string.Empty;

            path = (url.IndexOf("?") != -1 ? url.Substring(0, url.IndexOf("?")) : url);
            doc = (path.IndexOf("/") != -1 ? path.Substring(path.LastIndexOf("/") + 1) : path);

            return doc;
        }

        private string GetDocIdWithoutTail(string url)
        {
            string doc = GetDocId(url);
            string name = (doc.IndexOf(".") > 0 ? doc.Substring(0, doc.IndexOf(".")) : string.Empty);

            return name;
        }

        public string[] GetResourceMediaTypes(string url, string defaultType, Hashtable uriCache)
        {
            //url = Regex.Replace(url, @"/([^.?]*)(?:\.xcs)?(\?.*)?", "/$1.xcs$2", RegexOptions.IgnoreCase);
            Uri thisUri = new Uri(url);
            string relUri = thisUri.PathAndQuery;
            string[] mtypes = MatchUriPattern(relUri, uriCache);

            if (mtypes == null || mtypes.Length == 0)
                mtypes = new string[] { defaultType };

            return mtypes;
        }

        private string[] MatchUriPattern(string relativeUrl, Hashtable uriPatterns)
        {
            object rtn = null;
            if (uriPatterns.Count != 0)
            {
                IDictionaryEnumerator Enumerator = uriPatterns.GetEnumerator();
                while (Enumerator.MoveNext())
                {
                    string pattern = Enumerator.Key.ToString();
                    if (new Regex(pattern, RegexOptions.IgnoreCase).IsMatch(relativeUrl))
                    {
                        rtn = Enumerator.Value;
                        goto exit;
                    }
                }
            }

        exit:
            return rtn as string[];
        }
    }
}
