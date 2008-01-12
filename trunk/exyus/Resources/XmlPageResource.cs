using System;
using System.Web;
using System.Xml;
using System.Net;

using Exyus.Xml;
using Exyus.Caching;

namespace Exyus.Web
{
    [MediaTypes("text/html")]
    public class XmlPageResource : ExyusResource
    {        
        Utility util = new Utility();
        Cache ch = new Cache();

        string file = string.Empty;
        string url = string.Empty;

        public string TemplateXml;
        public string TemplateXsl;

        public XmlPageResource()
        {
            this.ContentType = Constants.cType_Html;
        }
        public XmlPageResource(string xml)
        {
            this.TemplateXml = xml;
            this.ContentType = Constants.cType_Html;
        }
        public XmlPageResource(string xml, string xsl)
        {
            this.TemplateXml = xml;
            this.TemplateXsl = xsl;
            this.ContentType = Constants.cType_Html;
        }
        public XmlPageResource(string xml, string xsl, int age)
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

            string mtype = util.SetMediaType(this);
            string ftype = util.LookUpFileType(mtype);

            try
            {
                // return cached copy, if you can
                if (ch.CachedResourceIsValid((ExyusResource)this))
                    return;

                // get the document or make one
                if (this.TemplateXml != string.Empty)
                {
                    // resolve the xml document and any includes
                    file = Context.Server.MapPath(util.GetConfigSectionItem(Constants.cfg_exyusSettings, Constants.cfg_templatefolder) + this.TemplateXml);
                    url = this.Context.Request.Url.ToString();
                    xmlout = xfr.GetXmlFile(file, url);
                }
                else
                    xmlout.LoadXml("<root />");

                // see if we need to look in the xmlfile for xsldoc
                if (this.TemplateXsl == null || this.TemplateXsl == string.Empty)
                    this.TemplateXsl = util.GetInlineXslFile(file);

                // transform results for final output
                if (this.TemplateXsl != string.Empty)
                {
                    XslTransformer xslt = new XslTransformer();
                    out_text = xslt.ExecuteText(xmlout, this.Context.Server.MapPath(util.GetConfigSectionItem(Constants.cfg_exyusSettings, Constants.cfg_templatefolder) + this.TemplateXsl));
                }

                // handle caching of this resource
                ch.CacheResource((ExyusResource)this, out_text);
            }
            catch (HttpException hex)
            {
                this.StatusCode = (HttpStatusCode)hex.GetHttpCode();
                this.StatusDescription = hex.GetHtmlErrorMessage();
                xmlout.LoadXml(string.Format(Constants.fmt_xml_error_inc, hex.Message, this.Context.Request.Url, file));
            }
            catch (Exception ex)
            {
                this.StatusCode = HttpStatusCode.InternalServerError;
                this.StatusDescription = ex.Message;
                out_text = string.Format(Constants.fmt_html_error, ex.Message);
            }

            // return the results
            this.Response = util.FixEncoding(out_text);

            xmlout = null;
        }
    }
}
