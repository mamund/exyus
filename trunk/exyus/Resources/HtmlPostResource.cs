/*
 * NOTICE: not working yet
 */
using System;
using System.Collections.Generic;
using System.Text;

using Exyus;
using Exyus.Web;
using Exyus.Xml;
using Exyus.Security;

using System.Web;
using System.Xml;
using System.Net;
using System.IO;
using System.Text.RegularExpressions;

using Exyus.Caching;

namespace Exyus.Web
{
    [MediaTypes("text/html")]
    public class HtmlPostResource : ExyusResource
    {        
        Utility util = new Utility();
        Cache ch = new Cache();

        string file = string.Empty;
        string url = string.Empty;

        public string TemplateXml = null;
        public string TemplateXsl = null;
        public string PostRoot = "root";
        public string PostXsl = string.Empty;
        public string[] FormPostValues = null;
        public string DataPostURI = string.Empty;

        public HtmlPostResource()
        {
            this.ContentType = Constants.cType_Html;
        }
        public HtmlPostResource(string xml)
        {
            this.TemplateXml = xml;
            this.ContentType = Constants.cType_Html;
        }
        public HtmlPostResource(string xml, string xsl)
        {
            this.TemplateXml = xml;
            this.TemplateXsl = xsl;
            this.ContentType = Constants.cType_Html;
        }
        public HtmlPostResource(string xml, string xsl, int age)
        {
            this.TemplateXml = xml;
            this.TemplateXsl = xsl;
            this.MaxAge = age;
            this.ContentType = Constants.cType_Html;
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

            try
            {
                if (ch.CachedResourceIsValid((ExyusResource)this))
                    return;

                // generate a new version
                // get the document
                file = Context.Server.MapPath(util.GetConfigSectionItem(Constants.cfg_exyusSettings, Constants.cfg_templatefolder) + this.TemplateXml);
                url = this.Context.Request.Url.ToString();

                // resolve the xml document and any includes
                xmlout = xfr.GetXmlFile(file, url);

                // see if we need to look in the xmlfile for xsldoc
                if (this.TemplateXsl == null || this.TemplateXsl == string.Empty)
                    this.TemplateXsl = util.GetInlineXslFile(file);

                if (this.TemplateXsl == string.Empty)
                {
                    this.ContentType = Constants.cType_Xml;
                }
                else
                {
                    this.ContentType = Constants.cType_Html;

                    // transform results for final output
                    XslTransformer xslt = new XslTransformer();
                    out_text = xslt.ExecuteText(xmlout, this.Context.Server.MapPath(util.GetConfigSectionItem(Constants.cfg_exyusSettings,Constants.cfg_templatefolder) + this.TemplateXsl));
                }

                // handle caching the resource
                ch.CacheResource((ExyusResource)this, out_text);
            }
            catch (HttpException hex)
            {
                this.StatusCode = (HttpStatusCode)hex.GetHttpCode();
                out_text = string.Format(Constants.fmt_html_error, hex.Message, this.Context.Request.Url, file);
            }
            catch (Exception ex)
            {
                this.StatusCode = HttpStatusCode.InternalServerError;
                out_text = string.Format(Constants.fmt_html_error, ex.Message);
            }

            // return the results
            this.Response = util.FixEncoding(out_text);

            xmlout = null;
        }

        public override void Post()
        {
            string out_text = string.Empty;
            XmlDocument xmlin = new XmlDocument();
            XmlDocument xmlout = new XmlDocument();

            // create xmldoc from form inputs
            xmlin.CreateXmlDeclaration("1.0",null,null);
            XmlElement root = xmlin.CreateElement(this.PostRoot);
            for (int i = 0; i < this.FormPostValues.Length; i++)
            {
                XmlElement item = xmlin.CreateElement(this.FormPostValues[i]);
                XmlText text = xmlin.CreateTextNode(util.GetRequestArg(this.FormPostValues[i]));
                item.AppendChild(text);
                root.AppendChild(item);
            }
            xmlin.AppendChild(root);

            // post to underlying data uri using current credentials
            // assumes posting of xml data!
            try
            {
                // set up call
                Requestor req = new Requestor();
                ExyusPrincipal ep = (ExyusPrincipal)this.Context.User;
                req.Credentials = new NetworkCredential(((ExyusIdentity)ep.Identity).Name, ((ExyusIdentity)ep.Identity).Password);

                // execute http call
                out_text = req.Execute(
                    string.Format("{0}://{1}{2}",
                        this.Context.Request.Url.Scheme,
                        this.Context.Request.Url.DnsSafeHost,
                        this.DataPostURI),
                    "post", "text/xml", util.FixEncoding(xmlin.OuterXml)
                );

                // transform results for final output
                xmlout.LoadXml(out_text);
                XslTransformer xslt = new XslTransformer();
                out_text = xslt.ExecuteText(xmlout, this.Context.Server.MapPath(util.GetConfigSectionItem(Constants.cfg_exyusSettings, Constants.cfg_templatefolder) + this.PostXsl));
            }
            catch (HttpException hex)
            {
                this.StatusCode = (HttpStatusCode)hex.GetHttpCode();
                out_text = string.Format(Constants.fmt_html_error, hex.Message, this.Context.Request.Url);
            }
            catch (Exception ex)
            {
                this.StatusCode = HttpStatusCode.InternalServerError;
                out_text = string.Format(Constants.fmt_html_error, ex.Message,this.Context.Request.Url );
            }

            // return the results
            this.Response = util.FixEncoding(out_text);

            xmlin = null;
            xmlout = null;
        }
    }
}
