using System;
using System.Collections;
using System.Collections.Specialized;
using System.Text;

using System.Text.RegularExpressions;
using System.Web;
using System.Xml;
using System.Net;

using Exyus;
using Exyus.Web;
using Exyus.Security;
using System.Diagnostics;

namespace Exyus.Web
{
    // TODO - implement db version of auth/auth
    public class Module : IHttpModule
    {
        private HttpApplication app;
        private Exyus.Utility util = new Exyus.Utility();
        private string sid = string.Empty;
        private int timeout = 20; // default session/cache timeout
        private SortedList authUrls = new SortedList();
        private Authentication auth = new Authentication();
        private ListDictionary dlist = new ListDictionary();

        #region IHttpModule Members

        public void Dispose()
        {
            //throw new Exception("The method or operation is not implemented.");
        }

        public void Init(HttpApplication context)
        {
            this.app = context;
            context.BeginRequest += new System.EventHandler(this.BeginRequest);
            context.EndRequest += new System.EventHandler(this.EndRequest);
            context.AuthenticateRequest += new System.EventHandler(this.AuthenticateRequest);
            context.AuthorizeRequest += new System.EventHandler(this.AuthorizeRequest);

            timeout = Convert.ToInt32((util.GetConfigSectionItem(Constants.cfg_exyusSecurity,Constants.cfg_authTimeout) != string.Empty ? util.GetConfigSectionItem(Constants.cfg_exyusSecurity,Constants.cfg_authTimeout) : "20"));
        }

        #endregion

        public void BeginRequest(object sender, EventArgs args)
        {
            // only create session cookies if config sez it's ok
            if (util.GetConfigSectionItem(Constants.cfg_exyusSettings, Constants.cfg_sendSessionCookies) == "true")
            {
                string sk = string.Empty;

                if (app.Request.Cookies["session-id"] == null)
                {
                    CreateSession();
                }
                else
                {
                    // validate session
                    sk = app.Request.Cookies["session-id"].Value;

                    // has session died yet?
                    if (app.Context.Cache.Get(sk) == null)
                    {
                        // refresh session, make then re-auth, too
                        CreateSession(util.Base64Decode(sk));
                        if (auth.AuthRequired(app.Request.Url.AbsolutePath))
                        {
                            app.Context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                            app.Response.Cookies.Remove("session-user");
                            app.CompleteRequest();
                        }
                    }
                    else
                    {
                        // inspect session to prevent replay from another station
                        string[] ck = util.Base64Decode(sk).Split((char)255);

                        try
                        {
                            // is this the same agent/browser?
                            if (ck[1] != app.Request.UserAgent + app.Request.UserHostAddress)
                            {
                                CreateSession();
                                if (auth.AuthRequired(app.Request.Url.AbsolutePath))
                                {
                                    app.Context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                                    app.Response.Cookies.Remove("session-user");
                                    app.CompleteRequest();
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            throw new HttpException(500, "BeginRequest failed " + ex.Message);
                        }
                    }
                }
            }
        }

        private void CreateSession(string sk)
        {
            app.Response.Cookies.Add(new HttpCookie("session-id", util.Base64Encode(sk)));

            app.Context.Cache.Add(
                util.Base64Encode(sk),
                util.Base64Encode(sk),
                null,
                System.Web.Caching.Cache.NoAbsoluteExpiration,
                new TimeSpan(0, timeout, 0),
                System.Web.Caching.CacheItemPriority.High,
                null
                );
        }
        private void CreateSession()
        {
            sid = System.Guid.NewGuid().ToString();
            string sk = string.Format(
                "{0}" + (char)255 + "{1}",
                sid,
                app.Request.UserAgent + app.Request.UserHostAddress
                );
            CreateSession(sk);
        }

        public void AuthenticateRequest(object sender, EventArgs args)
        {
            ExyusIdentity ei = null;
            ExyusPrincipal ep = null;
            string[] roles = null;
            string user = string.Empty;
            string pass = string.Empty;
            string authHeader = string.Empty;
            SortedList permissions = new SortedList();
            dlist = new ListDictionary();

            authHeader = app.Request.Headers[Constants.hdr_authorization];

            // no auth header at all
            if (authHeader == null || authHeader.Length == 0)
                return; //anon request

            // exyus can't handle this type of auth
            if (!auth.ProcessAuthHeader(authHeader, ref user, ref pass, ref dlist))
                return;

            // attempt to auth the user and create security objects
            if (auth.ValidateUser(user, pass, out roles, out permissions))
            {
                // handle digest final details
                if (auth.ValidateHeader(dlist, pass))
                {
                    // write special cookie for this auth'ed user
                    if (util.GetConfigSectionItem(Constants.cfg_exyusSettings, Constants.cfg_sendUserCookies) == "true")
                    {
                        if (app.Request.Cookies.Get("session-user") != null)
                        {
                            if (app.Request.Cookies.Get("session-user").Value != user)
                                app.Request.Cookies["session-user"].Value = user;
                        }
                        else
                            app.Response.Cookies.Add(new HttpCookie("session-user", user));
                    }

                    // create local user for later
                    ei = new ExyusIdentity(user, pass);
                    ep = new ExyusPrincipal(ei, roles, permissions);

                    System.Threading.Thread.CurrentPrincipal = ep;
                    app.Context.User = ep;

                    if (util.GetConfigSectionItem(Constants.cfg_exyusSecurity, Constants.cfg_logExyusUser) != "false")
                    {
                        app.Context.Response.AppendToLog(string.Format(" [exyus-user={0}]", user));
                    }
                }
                else
                {
                    DenyAccess();
                }
            }
            else
            {
                DenyAccess();
            }
        }

        public void AuthorizeRequest(object sender, EventArgs args)
        {
            // if auth is off, validate as guest account
            if (!auth.AuthRequired(app.Request.Url.AbsolutePath))
            {
                ExyusIdentity ei = null;
                ExyusPrincipal ep = null;

                string user = "guest";
                string pass = "";
                string[] roles = null;
                SortedList permissions = new SortedList();

                if (auth.ValidateUser(user, pass, out roles, out permissions))
                {
                    ei = new ExyusIdentity(user,pass);
                    ep = new ExyusPrincipal(ei, roles, permissions);

                    System.Threading.Thread.CurrentPrincipal = ep;
                    app.Context.User = ep;
                }
            }

            // check authorization for this url
            try
            {
                ExyusPrincipal ep = (ExyusPrincipal)app.Context.User;
                if (!ep.HasPermission(app.Request.Url.AbsolutePath, app.Request.HttpMethod))
                {
                    app.Response.StatusCode = (int)HttpStatusCode.Forbidden; ;
                    app.CompleteRequest();
                }
            }
            catch (Exception ex)
            {
                app.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                app.CompleteRequest();
            }
        }

        // NOTE: try to figure out how to support forms-auth redirection properly
        public void EndRequest(object sender, EventArgs eventArgs)
        {
            if (app.Response.StatusCode == (int)HttpStatusCode.Unauthorized)
            {
                // marked for form auth?
                string url = util.AuthFormUrl(app.Request.Url.AbsolutePath.ToLower());
                string refer = (app.Request.UrlReferrer != null ? app.Request.UrlReferrer.ToString() : string.Empty);

                // if we have form url and that's not the current url
                if (url != string.Empty && refer.ToLower().IndexOf(url.ToLower())==-1)
                    app.Response.Redirect(url+"#"+app.Server.UrlEncode(app.Request.RawUrl), true);
                else
                    auth.ProduceAuthHeader();
            }
        }

        // return 401 unless they tried too many times
        private void DenyAccess()
        {
            // keep track of attempts
            if (util.MaxTries() == false)
            {
                // ask again
                app.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                app.Response.StatusDescription = "Access Denied";
                app.Response.Write(string.Format(Constants.fmt_html_error, "401 Access Denied"));
                app.CompleteRequest();
            }
            else
            {
                // cut 'em off!
                throw new HttpException(403, "Authentication Failed.");
            }
        }
    }
}
