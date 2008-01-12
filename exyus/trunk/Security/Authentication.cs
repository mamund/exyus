using System;
using System.Collections;
using System.Collections.Specialized;
using System.Text;

using System.Text.RegularExpressions;
using System.Web;
using System.Xml;
using System.Net;
using System.Security.Cryptography;

using Exyus;
using Exyus.Web;
using Exyus.Security;

// provides basic auth services
namespace Exyus.Security
{
    public class Authentication
    {
        private Utility util = new Utility();
        private SortedList authUrls = new SortedList();

        public void ProduceAuthHeader()
        {
            HttpApplication app = HttpContext.Current.ApplicationInstance;
            string authtype = util.GetConfigSectionItem(Constants.cfg_exyusSecurity, "authType");
            string[] authlist = authtype.Split(',');

            for (int i = 0; i < authlist.Length; i++)
            {
                // send out basic auth header
                if (authlist[i].ToLower() == "basic")
                {
                    app.Response.AppendHeader("WWW-Authenticate", String.Format("Basic Realm=\"{0}\"", util.GetConfigSectionItem(Constants.cfg_exyusSecurity, "AuthRealm")));
                }

                // send out digest auth header
                if (authlist[i].ToLower() == "digest")
                {
                    string authrealm = util.GetConfigSectionItem(Constants.cfg_exyusSecurity, "AuthRealm");
                    string nonce = GetCurrentNonce();
                    string opaque = "0000000000000000";

                    // check for stale nonce
                    bool flag = false;
                    object local = app.Context.Items["staleNonce"];
                    if (local != null)
                        flag = (bool)local;

                    StringBuilder sb = new StringBuilder("Digest");
                    sb.AppendFormat(" realm=\"{0}\",", authrealm);
                    sb.AppendFormat(" nonce=\"{0}\",", nonce);
                    sb.AppendFormat(" opaque=\"{0}\",", opaque);
                    sb.AppendFormat(" stale={0},", flag);
                    sb.AppendFormat(" algorithm={0},", "MD5");
                    sb.AppendFormat(" qop=\"{0}\"", "auth");

                    app.Response.AppendHeader("WWW-Authenticate", sb.ToString());
                }
            }
        }

        public bool ProcessAuthHeader(string authHeader, ref string user, ref string pass, ref ListDictionary dlist)
        {
            HttpApplication app = HttpContext.Current.ApplicationInstance;
            bool rtn = false;
            string authtype = util.GetConfigSectionItem(Constants.cfg_exyusSecurity, "authType");

            // if client sent back basic auth
            if (authHeader.Substring(0, 5).ToLower() == "basic")
            {
                string[] credentials = util.Base64Decode(authHeader.Substring(6)).Split(':');

                user = credentials[0];
                pass = credentials[1];
                rtn = true;
            }

            // if client sent back digest auth
            if (authHeader.Substring(0, 6).ToLower() == "digest")
            {
                // drop the digest word in the string
                authHeader = authHeader.Substring(7);

                string key = String.Empty;
                dlist = new ListDictionary();

                Regex rx = new Regex(@"([^=,""'\s]+\s*=\s*""[^""]*""|[^=,""'\s]+\s*=\s*'[^']*'|[^=,""'\s]+\s*=\s*[^=,""'\s]*)", RegexOptions.IgnoreCase);
                Match mr = rx.Match(authHeader);
                while (mr.Success)
                {
                    string[] pair = mr.Value.Split(new char[] { '=' }, 2);
                    key = pair[0].Trim(new char[] { ' ', '\"' });
                    string value = pair[1].Trim(new char[] { ' ', '\"' });
                    dlist.Add(key, value);

                    mr = mr.NextMatch();
                } 
                user = dlist["username"].ToString();
                pass = GetUserPassword(user);
                rtn = true;
            }

            // flag on whether we got a valid header back
            return rtn;
        }

        public bool ValidateHeader(ListDictionary dlist, string pword)
        {
            HttpApplication app = HttpContext.Current.ApplicationInstance;
            string authtype = util.GetConfigSectionItem(Constants.cfg_exyusSecurity, "authType");
            bool rtn = false;

            // if it's digest, validate it 
            if (dlist.Count > 0)
            {
                string response = string.Empty;
                string a1 = string.Format("{0}:{1}:{2}", dlist["username"], util.GetConfigSectionItem(Constants.cfg_exyusSecurity, "AuthRealm"), pword);
                string a2 = string.Format("{0}:{1}", app.Request.HttpMethod, dlist["uri"]);

                if (dlist["qop"] != null)
                    response = util.MD5BinHex(string.Format("{0}:{1}:{2}:{3}:{4}:{5}", util.MD5BinHex(a1), dlist["nonce"], dlist["nc"], dlist["cnonce"], dlist["qop"], util.MD5BinHex(a2)));
                else
                    response = util.MD5BinHex(string.Format("{0}:{1}:{2}", util.MD5BinHex(a1), dlist["nonce"], util.MD5BinHex(a2)));

                bool stale = IsValidNonce((string)dlist["nonce"]) == false;
                app.Context.Items["staleNonce"] = stale;

                if ((string)dlist["response"] == response && !stale)
                    rtn = true;
                else
                {
                    // trap for clients that create endless auth loops
                    if (util.MaxTries() == true)
                        throw new HttpException((int)HttpStatusCode.Unauthorized, "Digest Authentication Failed.");

                    rtn = false;
                }
            }
            else
                rtn = true; // not digest - no need for further review

            return rtn;
        }

        public bool ValidateUser(string user, string pass, out string[] roles, out SortedList permissions)
        {
            HttpApplication app = HttpContext.Current.ApplicationInstance;
            int timeout = Convert.ToInt32((util.GetConfigSectionItem(Constants.cfg_exyusSecurity, Constants.cfg_authTimeout) != string.Empty ? util.GetConfigSectionItem(Constants.cfg_exyusSecurity, Constants.cfg_authTimeout) : "20"));
            bool isUserCached = true;
            bool isPermCached = true;
            bool isRoleCached = true;
            string cache_key = util.MD5(user);
            string xpath = String.Format("/users/user[@name='{0}'][@password='{1}']", user, pass);
            string userFile = util.GetConfigSectionItem(Constants.cfg_exyusSecurity, "authUsers");
            string fullpath = app.Request.MapPath(userFile);
            XmlNode userNode = null;
            XmlDocument xmldoc = new XmlDocument();

            // get the user document (from cache or xml)
            xmldoc = (XmlDocument)app.Context.Cache.Get(fullpath);
            if (xmldoc == null)
            {
                xmldoc = new XmlDocument();
                using (XmlTextReader xtr = new XmlTextReader(fullpath))
                {
                    xmldoc.Load(xtr);
                    xtr.Close();
                }

                // cache document using file dependency
                app.Context.Cache.Add(
                    fullpath,
                    xmldoc,
                    new System.Web.Caching.CacheDependency(fullpath),
                    System.Web.Caching.Cache.NoAbsoluteExpiration,
                    new TimeSpan(0, timeout, 0),
                    System.Web.Caching.CacheItemPriority.High,
                    null);

            }
            // get user (from cache or file)
            userNode = (XmlNode)app.Context.Cache.Get(cache_key);
            if (userNode == null)
            {
                isUserCached = false;
                userNode = xmldoc.SelectSingleNode(xpath);
            }

            // if we have a valid user, get roles and permissions
            if (userNode != null)
            {
                // get roles (from cache or from xml)
                roles = (string[])app.Context.Cache.Get(cache_key + "_roles");
                if (roles == null)
                {
                    isRoleCached = false;
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
                permissions = (SortedList)app.Context.Cache.Get(cache_key + "_permissions");
                if (permissions == null)
                {
                    isPermCached = false;
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
                if (!isUserCached && userNode != null)
                {
                    app.Context.Cache.Add(
                        cache_key,
                        userNode,
                        new System.Web.Caching.CacheDependency(fullpath),
                        System.Web.Caching.Cache.NoAbsoluteExpiration,
                        new TimeSpan(0, timeout, 0),
                        System.Web.Caching.CacheItemPriority.AboveNormal,
                        null);
                }

                if (!isRoleCached && roles != null)
                {
                    app.Context.Cache.Add(
                        cache_key + "_roles",
                        roles,
                        new System.Web.Caching.CacheDependency(fullpath),
                        System.Web.Caching.Cache.NoAbsoluteExpiration,
                        new TimeSpan(0, timeout, 0),
                        System.Web.Caching.CacheItemPriority.AboveNormal,
                        null);
                }

                if (!isPermCached && permissions != null)
                {
                    app.Context.Cache.Add(
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

        public bool AuthRequired(string uri)
        {
            HttpApplication app = HttpContext.Current.ApplicationInstance;
            int timeout = Convert.ToInt32((util.GetConfigSectionItem(Constants.cfg_exyusSecurity, Constants.cfg_authTimeout) != string.Empty ? util.GetConfigSectionItem(Constants.cfg_exyusSecurity, Constants.cfg_authTimeout) : "20"));
            bool rtn = true;
            string auth_url = string.Empty;

            // see if we need to check at all
            if (util.GetConfigSectionItem(Constants.cfg_exyusSecurity, "authType").ToLower() == "none")
            {
                rtn = false;
            }
            else
            {
                // see if we have this already
                auth_url = (string)app.Context.Cache.Get(uri + "_authurl");
                if (auth_url != null)
                    rtn = (auth_url == "true" ? true : false);
                else
                {
                    // we need this for cache dependency
                    string authFile = util.GetConfigSectionItem(Constants.cfg_exyusSecurity, "authUrls");
                    string fullpath = app.Request.MapPath(authFile);

                    // go get url collection
                    LoadAuthUrls();
                    // find the one we want
                    for (int i = authUrls.Count - 1; i > -1; i--)
                    {
                        if (new Regex(authUrls.GetKey(i).ToString(), RegexOptions.IgnoreCase).IsMatch(uri))
                        {
                            if (authUrls.GetByIndex(i).ToString().Split('|')[0] == "true")
                                rtn = true;
                            else
                                rtn = false;

                            // save this for later
                            app.Context.Cache.Add(
                                uri + "_authurl",
                                authUrls.GetByIndex(i).ToString(),
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
            }

        exit:
            return rtn;
        }

        // get list of secure urls for this app
        private void LoadAuthUrls()
        {
            HttpApplication app = HttpContext.Current.ApplicationInstance;
            int timeout = Convert.ToInt32((util.GetConfigSectionItem(Constants.cfg_exyusSecurity, Constants.cfg_authTimeout) != string.Empty ? util.GetConfigSectionItem(Constants.cfg_exyusSecurity, Constants.cfg_authTimeout) : "20"));
            string authFile = util.GetConfigSectionItem(Constants.cfg_exyusSecurity, "authUrls");
            string fullpath = app.Request.MapPath(authFile);
            XmlNodeList authNodes = null;
            XmlDocument xmldoc = new XmlDocument();

            // load list of secured urls from cache or xml
            authUrls = (SortedList)app.Context.Cache.Get("authUrls");
            if (authUrls == null)
            {
                authUrls = new SortedList();
                using (XmlTextReader xtr = new XmlTextReader(fullpath))
                {
                    xmldoc.Load(xtr);
                    xtr.Close();
                }

                authNodes = xmldoc.SelectNodes("/urls/url");
                int numUrls = authNodes.Count;
                if (numUrls > 0)
                {
                    for (int i = 0; i < numUrls; i++)
                        authUrls.Add(authNodes[i].Attributes["path"].Value, authNodes[i].Attributes["auth"].Value);
                }

                // add to cache w/ file dependency
                app.Context.Cache.Add(
                    "authUrls",
                    authUrls,
                    new System.Web.Caching.CacheDependency(fullpath),
                    System.Web.Caching.Cache.NoAbsoluteExpiration,
                    new TimeSpan(0, timeout, 0),
                    System.Web.Caching.CacheItemPriority.High,
                    null);
            }
        }

        private string GetUserPassword(string user)
        {
            HttpApplication app = HttpContext.Current.ApplicationInstance;
            int timeout = Convert.ToInt32((util.GetConfigSectionItem(Constants.cfg_exyusSecurity, Constants.cfg_authTimeout) != string.Empty ? util.GetConfigSectionItem(Constants.cfg_exyusSecurity, Constants.cfg_authTimeout) : "20"));
            string cache_key = util.MD5(user);
            bool isUserCached = true;
            string xpath = String.Format("/users/user[@name='{0}']", user);
            string userFile = util.GetConfigSectionItem(Constants.cfg_exyusSecurity, "authUsers");
            string fullpath = app.Request.MapPath(userFile);
            XmlNode userNode = null;
            XmlDocument xmldoc = new XmlDocument();
            string pass = "";

            // get the user document (from cache or xml)
            xmldoc = (XmlDocument)app.Context.Cache.Get(fullpath);
            if (xmldoc == null)
            {
                xmldoc = new XmlDocument();
                //xmldoc.Load(fullpath);
                using (XmlTextReader xtr = new XmlTextReader(fullpath))
                {
                    xmldoc.Load(xtr);
                    xtr.Close();
                }

                // cache document using file dependency
                app.Context.Cache.Add(
                    fullpath,
                    xmldoc,
                    new System.Web.Caching.CacheDependency(fullpath),
                    System.Web.Caching.Cache.NoAbsoluteExpiration,
                    new TimeSpan(0, timeout, 0),
                    System.Web.Caching.CacheItemPriority.High,
                    null);

            }
            // get user (from cache or file)
            userNode = (XmlNode)app.Context.Cache.Get(cache_key);
            if (userNode == null)
            {
                isUserCached = false;
                userNode = xmldoc.SelectSingleNode(xpath);
            }

            if (userNode != null)
            {
                if (userNode.Attributes["password"] != null)
                    pass = userNode.Attributes["password"].Value;
            }

            if (!isUserCached && userNode != null)
            {
                app.Context.Cache.Add(
                    cache_key,
                    userNode,
                    new System.Web.Caching.CacheDependency(fullpath),
                    System.Web.Caching.Cache.NoAbsoluteExpiration,
                    new TimeSpan(0, timeout, 0),
                    System.Web.Caching.CacheItemPriority.AboveNormal,
                    null);
            }

            return pass;
        }

        private string GetCurrentNonce()
        {
            DateTime nonceTime = DateTime.Now + TimeSpan.FromMinutes(1);
            string expireStr = nonceTime.ToString("G");

            Encoding enc = new ASCIIEncoding();
            byte[] expireBytes = enc.GetBytes(expireStr);
            string nonce = Convert.ToBase64String(expireBytes);

            // nonce can't end in '=', so trim them from the end
            nonce = nonce.TrimEnd(new Char[] { '=' });
            return nonce;
        }

        private bool IsValidNonce(string nonce)
        {
            DateTime dateTime;
            int i = nonce.Length % 4;
            bool flag = false;

            if (i > 0)
                i = 4 - i;

            string authStr = nonce.PadRight(nonce.Length + i, '=');
            try
            {
                byte[] bs = Convert.FromBase64String(authStr);
                dateTime = DateTime.Parse(new ASCIIEncoding().GetString(bs));
            }
            catch (FormatException)
            {
                return flag;
            }

            flag = DateTime.Now <= dateTime;

            return flag;
        }
    }
}
