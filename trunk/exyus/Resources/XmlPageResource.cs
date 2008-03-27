using System;
using System.Web;
using System.Xml;
using System.Net;
using System.Collections;

using Exyus.Xml;
using Exyus.Caching;

namespace Exyus.Web
{
    [MediaTypes("text/html")]
    public class XmlPageResource : HTTPResource
    {        
        Utility util = new Utility();
        Cache ch = new Cache();

        string file = string.Empty;
        string url = string.Empty;
        private string[] mediaTypes = null;

        public string TemplateXml = string.Empty;
        public string TemplateXsl = string.Empty;
        public string UrlPattern = string.Empty;
        public string Title = string.Empty;

        public XmlPageResource()
        {
            this.Init();
        }
        public XmlPageResource(string xml)
        {
            this.TemplateXml = xml;
            this.Init();
        }
        public XmlPageResource(string xml, string xsl)
        {
            this.TemplateXml = xml;
            this.TemplateXsl = xsl;
            this.Init();
        }
        public XmlPageResource(string xml, string xsl, int age)
        {
            this.TemplateXml = xml;
            this.TemplateXsl = xsl;
            this.MaxAge = age;
            this.Init();
        }

        private void Init()
        {
            if (this.ContentType == null || this.ContentType == string.Empty)
            {
                this.ContentType = Constants.cType_Html;
            }

            //get first pattern (if none set already)
            if (this.UrlPattern == null || this.UrlPattern == string.Empty)
                this.UrlPattern = ((UriPattern)this.GetType().GetCustomAttributes(typeof(UriPattern), false)[0]).Patterns[0];

            if (this.Title == string.Empty)
                this.Title = this.GetType().ToString();

            mediaTypes = ((MediaTypes)this.GetType().GetCustomAttributes(typeof(MediaTypes), false)[0]).Types;
        }

        public override void Head()
        {
            this.Get();
            this.Response = null;
        }
        public override void Get()
        {
            XmlFileReader xfr = new XmlFileReader();
            XmlDocument xmlout = new XmlDocument();
            string out_text = string.Empty;
            Hashtable arg_list = new Hashtable();
            string absoluteUri = this.Context.Request.RawUrl;

            string mtype = util.SetMediaType(this);
            string ftype = util.LookUpFileType(mtype);

            try
            {
                // return cached copy, if you can
                if (ch.CachedResourceIsValid((HTTPResource)this))
                    return;

                arg_list = util.ParseUrlPattern(absoluteUri, this.UrlPattern);
                util.SafeAdd(ref arg_list, "_title", this.Title);
                util.SafeAdd(ref arg_list, "_last-modified", string.Format("{0:s}Z", DateTime.UtcNow));
                util.SafeAdd(ref arg_list, "_media-type", mtype);
                if (shared_args != null)
                {
                    foreach (string key in shared_args.Keys)
                    {
                        util.SafeAdd(ref arg_list, key, shared_args[key].ToString());
                    }
                }

                // get the document or make one
                if (this.TemplateXml != string.Empty)
                {
                    // resolve the xml document and any includes
                    if (this.TemplateXml.IndexOf("~") != -1)
                    {
                        file = Context.Server.MapPath(this.TemplateXml);
                    }
                    else
                    {
                        file = Context.Server.MapPath(util.GetConfigSectionItem(Constants.cfg_exyusSettings, Constants.cfg_templatefolder) + this.TemplateXml);
                    }
                    file = file.Replace("{ftype}",ftype);

                    url = this.Context.Request.Url.ToString();
                    xmlout = xfr.GetXmlFile(file, url);
                }
                else
                    xmlout.LoadXml("<root />");

                // see if we need to look in the xmlfile for xsldoc
                if (this.TemplateXsl == null || this.TemplateXsl == string.Empty && file != string.Empty)
                {
                    this.TemplateXsl = util.GetInlineXslFile(file);
                }

                // transform results for final output
                if (this.TemplateXsl != string.Empty)
                {
                    if (this.TemplateXsl.IndexOf("~") != -1)
                    {
                        file = this.Context.Server.MapPath(this.TemplateXsl);
                    }
                    else
                    {
                        file = this.Context.Server.MapPath(util.GetConfigSectionItem(Constants.cfg_exyusSettings, Constants.cfg_templatefolder) + this.TemplateXsl);
                    }
                    file = file.Replace("{ftype}", ftype);

                    XslTransformer xslt = new XslTransformer();
                    out_text = xslt.ExecuteText(xmlout, file,arg_list);
                }

                // handle caching of this resource
                ch.CacheResource((HTTPResource)this, out_text);
            }
            catch (HttpException hex)
            {
                this.StatusCode = (HttpStatusCode)hex.GetHttpCode();
                this.StatusDescription = hex.GetHtmlErrorMessage();
                out_text = util.RenderError("http error", hex.GetHtmlErrorMessage(), mtype);
                //xmlout.LoadXml(string.Format(Constants.fmt_xml_error_inc, hex.Message, this.Context.Request.Url, file));
            }
            catch (Exception ex)
            {
                this.StatusCode = HttpStatusCode.InternalServerError;
                this.StatusDescription = ex.Message;
                out_text = util.RenderError("unknown error", ex.Message, mtype);
            }

            // return the results
            this.Response = util.FixEncoding(out_text);

            xmlout = null;
        }
    }
}
