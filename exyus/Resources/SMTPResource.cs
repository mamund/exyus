using System;
using System.Web;
using System.Xml;
using System.Collections.Specialized;
using System.Collections;
using System.Net;
using System.IO;
using System.Text.RegularExpressions;
using System.Net.Mail;

using Exyus.Xml;
using Exyus.Security;
using Exyus.Caching;

namespace Exyus.Web
{
    [MediaTypes("text/xml")]
    public class SMTPResource : HTTPResource
    {
        // public properties        
        public string FileExtension = ".xml";
        public string PostLocationUri = string.Empty;
        public string UrlPattern = string.Empty;
        public string DocumentsFolder = string.Empty;
        public string[] XHtmlNodes = null;
        public string[] UpdateMediaTypes = null;
        public string Title = string.Empty;
        public bool RedirectOnPost = false;
        public string SMTPHost = string.Empty;

        // internal vars
        private string[] mediaTypes = null;
        private Utility util = new Utility();
        private string s_ext = string.Empty;
        private string absoluteUri = string.Empty;
        Cache ch = new Cache();

        public SMTPResource()
        {
            if(this.ContentType==null || this.ContentType==string.Empty)
                this.ContentType = Constants.cType_Html;

            // set system extension
            s_ext = util.GetConfigSectionItem(Constants.cfg_exyusSettings,Constants.cfg_fileExtension, Constants.msc_sys_file_ext);

            //get first pattern (if none set already)
            if (this.UrlPattern == null || this.UrlPattern == string.Empty)
                this.UrlPattern = ((UriPattern)this.GetType().GetCustomAttributes(typeof(UriPattern), false)[0]).Patterns[0];

            if (this.Title == string.Empty)
                this.Title = this.GetType().ToString();

            mediaTypes = ((MediaTypes)this.GetType().GetCustomAttributes(typeof(MediaTypes), false)[0]).Types;
        }

        public override void Post()
        {
            XmlDocument xmlout = new XmlDocument();
            XmlDocument xmlin = new XmlDocument();
            XmlDocument xmlargs = new XmlDocument();
            Hashtable arg_list = new Hashtable();
            string stor_folder = string.Empty;
            string id = string.Empty;
            string xsl_file = string.Empty;
            string xsd_file = string.Empty;
            string original_contentType = this.ContentType;

            absoluteUri = this.Context.Request.RawUrl;

            // settle on media type for the method
            string mtype = util.SetMediaType(this,this.UpdateMediaTypes);
            string ftype = util.LookUpFileType(mtype);

            string XslArgs = this.Context.Server.MapPath(this.DocumentsFolder + "args.xsl");
            string XslPostArgs = this.Context.Server.MapPath(this.DocumentsFolder + "post_args.xsl");
            string XsdFile = this.Context.Server.MapPath(this.DocumentsFolder + "post.xsd");
            string XsdFileMtype = this.Context.Server.MapPath(this.DocumentsFolder + (mtype == string.Empty ? "post.xsd" : string.Format("post_{0}.xsd", ftype)));
            string XslPostRequest = this.Context.Server.MapPath(this.DocumentsFolder + "post_request.xsl");
            string XslPostRequestMtype = this.Context.Server.MapPath(this.DocumentsFolder + (mtype == string.Empty ? "post_request.xsl" : string.Format("post_request_{0}.xsl", ftype)));

            try
            {
                // make sure we can do this
                if (this.AllowPost == false)
                    throw new HttpException((int)HttpStatusCode.MethodNotAllowed, "Cannot POST this resource.");

                // use regexp pattern to covert url into xml document
                arg_list = util.ParseUrlPattern(absoluteUri, this.UrlPattern);

                // validate args
                xsl_file = (File.Exists(XslPostArgs) ? XslPostArgs : XslArgs);
                if (File.Exists(xsl_file))
                {
                    xmlargs.LoadXml("<root />");
                    XslTransformer xslt = new XslTransformer();
                    id = xslt.ExecuteText(xmlargs, xsl_file, arg_list);
                }
                // transform *must not* return doc id!
                if (id != string.Empty)
                    throw new HttpException(400, "Cannot POST using resource id");

                // get the xmldoc from the entity
                this.Context.Request.InputStream.Position = 0;
                switch (mtype.ToLower())
                {
                    case Constants.cType_FormUrlEncoded:
                        xmlin = util.ProcessFormVars(this.Context.Request.Form);
                        break;
                    case Constants.cType_Json:
                        xmlin = util.ProcessJSON(this.Context.Request.InputStream);
                        break;
                    default:
                        xmlin.Load(this.Context.Request.InputStream);
                        break;
                }

                // validate the doc
                xsd_file = (File.Exists(XsdFileMtype) ? XsdFileMtype : XsdFile);
                if (File.Exists(xsd_file))
                {
                    SchemaValidator sv = new SchemaValidator();
                    string sch_error = sv.Execute(xmlin, xsd_file);
                    if (sch_error != string.Empty)
                        throw new HttpException(422, sch_error);
                }

                // validate html
                util.ValidateXHtmlNodes(ref xmlin,this.XHtmlNodes);

                // transform xmldoc into final form (if needed)
                xsl_file = (File.Exists(XslPostRequestMtype)?XslPostRequestMtype:XslPostRequest);
                if (File.Exists(xsl_file))
                {
                    XslTransformer xslt = new XslTransformer();
                    string out_text = xslt.ExecuteText(xmlin, xsl_file, arg_list);
                    xmlout.LoadXml(out_text);
                }
                else
                    xmlout = xmlin;

                // now build up the email and send it
                string from = xmlout.SelectSingleNode("//from").InnerText;
                string to = xmlout.SelectSingleNode("//to").InnerText;
                string subject = xmlout.SelectSingleNode("//subject").InnerText;
                string body = xmlout.SelectSingleNode("//body").InnerXml;

                SmtpClient smtp = new SmtpClient();
                if (this.SMTPHost != string.Empty)
                    smtp.Host = this.SMTPHost;
                smtp.Send(from, to, subject, body);

                // redirect to created item
                this.StatusCode = (this.RedirectOnPost?HttpStatusCode.Redirect:HttpStatusCode.Created);
                this.Location = util.GetConfigSectionItem(Constants.cfg_exyusSettings, Constants.cfg_rootfolder) + util.ReplaceArgs(this.PostLocationUri.Replace("{id}",id), arg_list);

                // if we were using form-posting, reset to preferred content type (text/html, most likely)
                if (this.ContentType == Constants.cType_FormUrlEncoded)
                    this.ContentType = original_contentType;

                xmlout = null;
            }
            catch (HttpException hex)
            {
                this.StatusCode = (HttpStatusCode)hex.GetHttpCode();
                this.StatusDescription = hex.Message;
                xmlout.LoadXml(string.Format(Constants.fmt_xml_error, util.XmlEncodeData(hex.Message)));
            }
            catch (Exception ex)
            {
                this.StatusCode = HttpStatusCode.InternalServerError;
                this.StatusDescription = ex.Message;
                xmlout.LoadXml(string.Format(Constants.fmt_xml_error, util.XmlEncodeData(ex.Message)));
            }

            if (xmlout != null)
                this.Response = util.FixEncoding(xmlout.OuterXml);
            else
                this.Response = null;

            xmlin = null;
            xmlout = null;
        }
    }
}