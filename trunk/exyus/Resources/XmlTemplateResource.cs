using System;
using System.Web;
using System.Xml;
using System.Net;
using System.Text.RegularExpressions;

using Exyus.Xml;
using Exyus.Security;
using Exyus.Caching;

namespace Exyus.Web
{
    [UriPattern("/templates/(.*)")]
    [MediaTypes("text/xml")]
    class XmlTemplateResource : HTTPResource
    {        
        Utility util = new Utility();
        Cache ch = new Cache();

        string file = string.Empty;
        string url = string.Empty;


        private string template_dir = "/";
        private string rex_url = "/templates/(.*)";
        
        public XmlTemplateResource()
        {
            this.ContentType = Constants.cType_Xml;
            this.MaxAge = 600;
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

            // settle content negotitation
            string mtype = util.SetMediaType(this);
            string ftype = util.LookUpFileType(mtype);

            try
            {
                // return cached copy, if you can
                if (ch.CachedResourceIsValid((HTTPResource)this))
                    return;

                // generate a new version
                // pull the template from the url
                string template = Regex.Match(this.Context.Request.RawUrl, rex_url).Groups[1].Value;
                url = this.Context.Request.Url.ToString();

                // get the document
                file = Context.Server.MapPath(string.Format("{0}{1}{2}", util.GetConfigSectionItem(Constants.cfg_exyusSettings, Constants.cfg_templatefolder), template_dir, template));

                // resolve the xml document and any includes
                xmlout = xfr.GetXmlFile(file, url);
                out_text = util.FixEncoding(xmlout.OuterXml);

                // handle caching of this resource
                ch.CacheResource((HTTPResource)this, out_text);
            }
            catch (HttpException hex)
            {
                this.StatusCode = (HttpStatusCode)hex.GetHttpCode();
                out_text = string.Format(Constants.fmt_xml_error_inc, hex.Message, this.Context.Request.Url, file);
            }
            catch (Exception ex)
            {
                this.StatusCode = HttpStatusCode.InternalServerError;
                out_text = string.Format(Constants.fmt_xml_error_inc, ex.Message);
            }

            // return the results
            this.Response = out_text;

            xmlout = null;
        }
    }
}
