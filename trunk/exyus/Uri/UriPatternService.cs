using System;
using System.Collections;
using System.Reflection;
using System.Security.Permissions;
using System.Web;
using System.Web.UI;
using System.Text.RegularExpressions;

namespace Exyus.Web
{
    public class UriHandlerFactory : IHttpHandlerFactory
    {
        private bool _init_complete = false;
        private object _ilock = new Object();
        private static string _vpath;
        private Hashtable _uriPatterns = new Hashtable();

        public IHttpHandler GetHandler(HttpContext context, string requestType, string url, string pathTranslated)
        {
            if (!_init_complete)
                Initialize();

            // Strip the virtual directory from the front of the url
            restart:
            string relativeUrl = context.Request.Url.LocalPath.Substring(VPath.Length);

            // try to get a match
            ConstructorInfo ci = MatchUriPattern(relativeUrl);

            // nothing? then check for redir, rewrite, or asp.net
            if (ci == null)
            {
                ExyusRedirect er = new ExyusRedirect();

                // check for redirection
                string r_url = er.RedirLookUp(relativeUrl);
                if (r_url != string.Empty)
                {
                    er.MovedPermanently(r_url);
                    return null;
                }

                // check for internal rewrite
                r_url = er.RewriteLookUp(relativeUrl);
                if (r_url != string.Empty)
                {
                    er.Rewrite(r_url);
                    goto restart;
                }

                // try asp.net as a last resort
                return PageParser.GetCompiledPageInstance(url, pathTranslated, context);
            }
            else
            {
                // get instance
                object o = ci.Invoke(new object[] { });
                
                if (o == null)
                    throw new HttpException(500, "Unable to invoke handler for [" + ci.Name + "]");

                // try to pass to factory
                IHttpHandlerFactory factory = o as IHttpHandlerFactory;
                if (factory != null)
                    return factory.GetHandler(context, requestType, url, pathTranslated);

                // try to pass to handler
                IHttpHandler handler = o as IHttpHandler;
                if (handler != null)
                    return handler;

                // boom!
                throw new HttpException(500, "Error matching uri pattern to handler ");
            }
        }

        public void ReleaseHandler(IHttpHandler handler)
        {
            IDisposable d = handler as IDisposable;

            if (d != null)
                d.Dispose();
        }

        private void Initialize()
        {
            lock (_ilock)
            {
                if (_init_complete)
                    return;

                Hashtable cacheUri = new Hashtable();
                string[] mtypes = null;

                // scan all loaded assemblies
                foreach (Assembly assm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    foreach (Type type in assm.GetTypes())
                    {
                        try
                        {
                            // Look for our attribute on the class
                            object[] attrs = type.GetCustomAttributes(typeof(UriPattern), false);
                            if (attrs == null || attrs.Length <= 0)
                                continue;

                            // Grab the default constructor
                            ConstructorInfo ci = type.GetConstructor(Type.EmptyTypes);
                            if (ci == null)
                                continue;

                            // Add a mapping for all the URLs to the given constructor
                            UriPattern attr = (UriPattern)attrs[0];
                            foreach (string url in attr.Patterns)
                            {
                                _uriPatterns.Add(url.ToLower(), ci);

                                object[] media = type.GetCustomAttributes(typeof(MediaTypes), false);
                                if (media != null && media.Length != 0)
                                {
                                    MediaTypes mt = (MediaTypes)media[0];
                                    mtypes = mt.Types;
                                }
                                else
                                {
                                    WebResource wr = (WebResource)ci.Invoke(new object[] { });
                                    mtypes = new string[] { wr.ContentType };
                                    wr = null;
                                }

                                cacheUri.Add(url.ToLower(), mtypes);
                            }
                        }
                        catch (Exception) { }

                    }
                }

                Utility util = new Utility();
                util.SaveUriCache(cacheUri);
                
                _init_complete = true;
            }
        }

        private ConstructorInfo MatchUriPattern(string relativeUrl)
        {
            object rtn = null;
            if (_uriPatterns.Count != 0)
            {
                IDictionaryEnumerator Enumerator = _uriPatterns.GetEnumerator();
                while (Enumerator.MoveNext())
                {
                    string pattern = Enumerator.Key.ToString();
                    //relativeUrl = Regex.Replace(relativeUrl, @"/([^.?]*)(?:\.xcs)?(\?.*)?", "/$1$2", RegexOptions.IgnoreCase);
                    if (new Regex(pattern, RegexOptions.IgnoreCase).IsMatch(relativeUrl))
                    {
                        rtn = Enumerator.Value;
                        goto exit;
                    }
                }
            }

            exit:
            return rtn as ConstructorInfo;
        }

        protected string VPath
        {
            get
            {
                if (_vpath == null)
                {
                    _vpath = HttpRuntime.AppDomainAppVirtualPath;

                    if (_vpath[_vpath.Length - 1] == '/')
                        _vpath = _vpath.TrimEnd('/');
                }
                return _vpath;
            }
        }

    }
}
