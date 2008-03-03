using System;
using System.Web;
using System.Xml;
using System.Net;
using System.IO;
using Microsoft.Data.SqlXml;
using System.Text.RegularExpressions;
using System.Collections;

using Exyus.Xml;
using Exyus.Caching;

namespace Exyus.Web
{
    [MediaTypes("text/xml")]
    public class XmlSqlResource: HTTPResource
    {
        // public properties
        public string ConnectionString = string.Empty;
        public string UrlPattern = string.Empty;
        public string PostLocationUri = string.Empty;
        public string DocumentsFolder = string.Empty;
        public string[] XHtmlNodes = null;
        public string[] UpdateMediaTypes = null;
        public string[] ImmediateCacheUri = null;
        public string[] BackgroundCacheUri = null;
        public string Title = string.Empty;
        public bool RedirectOnPost = false;
        public bool RedirectOnPut = false;
        public string PostIdXPath = "/@id";

        // internal vars
        private string[] mediaTypes = null;
        private Utility util = new Utility();
        private string rex_sqex = "Description=\"(.*)\"";
        private string rex_notfound = "not found";
        private string absoluteUri = string.Empty;
        private Cache ch = new Cache();


        public XmlSqlResource()
        {
            if (this.ContentType == null || this.ContentType == string.Empty)
            {
                this.ContentType = Constants.cType_Xml;
            }

            //get first pattern (if none set already)
            if (this.UrlPattern == null || this.UrlPattern == string.Empty)
                this.UrlPattern = ((UriPattern)this.GetType().GetCustomAttributes(typeof(UriPattern), false)[0]).Patterns[0];

            if (this.Title == string.Empty)
                this.Title = this.GetType().ToString();

            mediaTypes = ((MediaTypes)this.GetType().GetCustomAttributes(typeof(MediaTypes), false)[0]).Types;
        }

        public override void Delete()
        {
            XmlDocument xmlout = new XmlDocument();
            XmlDocument xmlin = new XmlDocument();
            Hashtable arg_list = new Hashtable();
            string out_text = string.Empty;
            string xsd_file = string.Empty;
            string xsl_file = string.Empty;

            // determine mediatype for this request
            // and adjust for response, if need
            string mtype = util.SetMediaType(this, mediaTypes);
            string ftype = util.LookUpFileType(mtype);

            absoluteUri = this.Context.Request.RawUrl;

            // make sure we can do this
            if (this.AllowDelete == false)
                throw new HttpException((int)HttpStatusCode.MethodNotAllowed, "Cannot DELETE this resource.");

            string XslArgs = this.Context.Server.MapPath(this.DocumentsFolder + "args.xsl");
            string XslDeleteArgs = this.Context.Server.MapPath(this.DocumentsFolder + "delete_args.xsl");

            string xslDeleteRequest = this.Context.Server.MapPath(this.DocumentsFolder + "delete_request.xsl");
            string xslDeleteRequestContentType = this.Context.Server.MapPath(this.DocumentsFolder + (mtype == string.Empty ? "delete_request.xsl" : string.Format("delete_request_{0}.xsl", ftype)));

            string XsdFile = this.Context.Server.MapPath(this.DocumentsFolder + "delete.xsd");
            string XsdFileMtype = this.Context.Server.MapPath(this.DocumentsFolder + (mtype == string.Empty ? "delete.xsd" : string.Format("delete_{0}.xsd", ftype)));


            try
            {
                // use regexp pattern to covert url into collection
                arg_list = util.ParseUrlPattern(absoluteUri, this.UrlPattern);

                // transform arglist into xml document
                xsl_file = string.Empty;
                xsl_file = (File.Exists(XslDeleteArgs) ? XslDeleteArgs : XslArgs);
                if (File.Exists(xsl_file))
                {
                    XslTransformer xslt = new XslTransformer();
                    out_text = xslt.ExecuteText(xmlin, xsl_file, arg_list);

                    if (out_text == string.Empty)
                        throw new HttpException(400, "bad request");
                    else
                        xmlin.LoadXml(out_text);

                    out_text = string.Empty;
                }
                else
                {
                    xmlin.LoadXml("<args />");
                }

                // validate args before continuing
                xsd_file = string.Empty;
                xsd_file = (XsdFileMtype != string.Empty ? XsdFileMtype : XsdFile);
                if (File.Exists(xsd_file))
                {
                    SchemaValidator sv = new SchemaValidator();
                    string sch_error = sv.Execute(xmlin, xsd_file);
                    if (sch_error != string.Empty)
                        throw new HttpException(422, sch_error);
                }

                // transform xmldoc into sql
                xsl_file = string.Empty;
                xsl_file = (File.Exists(xslDeleteRequestContentType) ? xslDeleteRequestContentType : xslDeleteRequest);
                if (File.Exists(xsl_file))
                {
                    XslTransformer xslt = new XslTransformer();
                    string cmdtext = xslt.ExecuteText(xmlin, xsl_file);

                    // execute sql and return empty
                    SqlXmlCommand cmd = new SqlXmlCommand(util.GetConfigSectionItem(Constants.cfg_exyusSettings, this.ConnectionString));
                    cmd.CommandText = cmdtext;
                    cmd.ExecuteNonQuery();
                    this.StatusCode = HttpStatusCode.NoContent;

                    // cache invalidation
                    ch.ClearCache(this.ImmediateCacheUriTemplates, this.BackgroundCacheUriTemplates, "", arg_list, util.LoadUriCache());

                }
                else
                {
                    throw new HttpException(500, "missing transform");
                }


                xmlout = null;
            }
            catch (HttpException hex)
            {
                this.StatusCode = (HttpStatusCode)hex.GetHttpCode();
                xmlout.LoadXml(string.Format(Constants.fmt_xml_error, hex.Message));
                out_text = util.FixEncoding(xmlout.OuterXml);
            }
            catch (SqlXmlException sqex)
            {
                if (Regex.IsMatch(sqex.Message, rex_notfound))
                    this.StatusCode = HttpStatusCode.NotFound;
                else
                    this.StatusCode = HttpStatusCode.InternalServerError;
                xmlout.LoadXml(string.Format(Constants.fmt_xml_error_db, Regex.Match(sqex.Message, rex_sqex).Groups[1].Value));
                out_text = util.FixEncoding(xmlout.OuterXml);
            }
            catch (Exception ex)
            {
                this.StatusCode = HttpStatusCode.InternalServerError;
                xmlout.LoadXml(string.Format(Constants.fmt_xml_error, ex.Message));
                out_text = util.FixEncoding(xmlout.OuterXml);
            }

            this.Response = out_text;

            xmlin = null;
            xmlout = null;
        }

        public override void Head()
        {
            this.Get();
            this.Response = null;
        }

        public override void Get()
        {
            XmlDocument xmlout = new XmlDocument();
            XmlDocument xmlin = new XmlDocument();
            XslTransformer xslt = new XslTransformer();
            Hashtable arg_list = new Hashtable();
            string xsd_file = string.Empty;
            string xsl_file = string.Empty;

            absoluteUri = this.Context.Request.RawUrl;
            string out_text = string.Empty;

            // determine mediatype for this request
            // and adjust for response, if need
            string mtype = util.SetMediaType(this);
            string ftype = util.LookUpFileType(mtype);

            // possible control documents
            string XslArgs = this.Context.Server.MapPath(this.DocumentsFolder + "args.xsl");
            string XslGetArgs = this.Context.Server.MapPath(this.DocumentsFolder + "get_args.xsl");

            string xslGetRequest = this.Context.Server.MapPath(this.DocumentsFolder + "get_request.xsl");
            string xslGetRequestContentType = this.Context.Server.MapPath(this.DocumentsFolder + (mtype == string.Empty ? "get_request.xsl" : string.Format("get_request_{0}.xsl", ftype)));

            string xslGetResponse = this.Context.Server.MapPath(this.DocumentsFolder + "get_response.xsl");
            string xslGetResponseContentType = this.Context.Server.MapPath(this.DocumentsFolder + (mtype == string.Empty ? "get_response.xsl" : string.Format("get_response_{0}.xsl", ftype)));

            string XsdFile = this.Context.Server.MapPath(this.DocumentsFolder + "get.xsd");
            string XsdFileMtype = this.Context.Server.MapPath(this.DocumentsFolder + (mtype == string.Empty ? "get.xsd" : string.Format("get_{0}.xsd", ftype)));

            try
            {
                if (ch.CachedResourceIsValid((HTTPResource)this))
                    return;

                // ok, let's try to build a new one

                // use regexp pattern to covert url into xml document
                arg_list = util.ParseUrlPattern(absoluteUri, this.UrlPattern);
                util.SafeAdd(ref arg_list, "_title", this.Title);
                util.SafeAdd(ref arg_list, "_last-modified", string.Format("{0:s}Z", DateTime.UtcNow));

                // transform into proper argument list
                xsl_file = string.Empty;
                xsl_file = (File.Exists(XslGetArgs) ? XslGetArgs : XslArgs);
                if (File.Exists(xsl_file))
                {
                    xslt = new XslTransformer();
                    out_text = xslt.ExecuteText(xmlin, xsl_file, arg_list);
                    xmlin.LoadXml(out_text);
                }
                else
                {
                    xmlin.LoadXml("<args />");
                }

                // validate args before continuing
                xsd_file = string.Empty;
                xsd_file = (XsdFileMtype != string.Empty ? XsdFileMtype : XsdFile);
                if (File.Exists(xsd_file))
                {
                    SchemaValidator sv = new SchemaValidator();
                    string sch_error = sv.Execute(xmlin, xsd_file);
                    if (sch_error != string.Empty)
                        throw new HttpException(422, sch_error);
                }

                // transform args into sql and execute
                xsl_file = (xslGetRequestContentType!=string.Empty?xslGetRequestContentType : xslGetRequest);
                if (File.Exists(xsl_file))
                {
                    xslt = new XslTransformer();
                    string cmdtext = xslt.ExecuteText(xmlin, xsl_file);

                    // execute sql and return results
                    SqlXmlCommand cmd = new SqlXmlCommand(util.GetConfigSectionItem(Constants.cfg_exyusSettings, this.ConnectionString));
                    cmd.CommandText = cmdtext;

                    using (XmlReader rdr = cmd.ExecuteXmlReader())
                    {
                        xmlout.Load(rdr);
                        rdr.Close();
                    }

                    // transform outputs into representation
                    xsl_file = (xslGetResponseContentType!=string.Empty?xslGetResponseContentType:xslGetResponse);
                    if (File.Exists(xsl_file))
                    {
                        xslt = new XslTransformer();
                        out_text = xslt.ExecuteText(xmlout, xsl_file, arg_list);
                    }
                    else
                    {
                        out_text = util.FixEncoding(xmlout.OuterXml);
                    }

                    // handle caching of this resource
                    ch.CacheResource((HTTPResource)this,util.FixEncoding(out_text));
                }
                else
                    throw new HttpException(500, "missing transform");
            }
            catch (HttpException hex)
            {
                this.StatusCode = (HttpStatusCode)hex.GetHttpCode();
                xmlout.LoadXml(string.Format(Constants.fmt_xml_error, hex.Message));
                out_text = util.FixEncoding(xmlout.OuterXml);
            }
            catch (SqlXmlException sqex)
            {
                if (Regex.IsMatch(sqex.Message, rex_notfound))
                    this.StatusCode = HttpStatusCode.NotFound;
                else
                    this.StatusCode = (System.Net.HttpStatusCode)424;
                xmlout.LoadXml(string.Format(Constants.fmt_xml_error_db, Regex.Match(sqex.Message, rex_sqex).Groups[1].Value));
                out_text = util.FixEncoding(xmlout.OuterXml);
            }
            catch (Exception ex)
            {
                this.StatusCode = HttpStatusCode.InternalServerError;
                xmlout.LoadXml(string.Format(Constants.fmt_xml_error, ex.Message));
                out_text = util.FixEncoding(xmlout.OuterXml);
            }

            // return the results
            this.Response = out_text;

            xmlin = null;
            xmlout = null;
        }

        public override void Post()
        {
            XmlDocument xmlout = new XmlDocument();
            XmlDocument xmlin = new XmlDocument();
            XmlDocument xmlargs = new XmlDocument();
            XslTransformer xslt = new XslTransformer();
            Hashtable arg_list = new Hashtable();
            string xsl_file = string.Empty;
            string xsd_file = string.Empty;
            string id = string.Empty;
            string original_contentType = this.ContentType;

            absoluteUri = this.Context.Request.RawUrl;
            string out_text = string.Empty;

            // make sure we can do this
            if (this.AllowPost == false)
                throw new HttpException((int)HttpStatusCode.MethodNotAllowed, "Cannot POST this resource.");

            // determine mediatype for this request
            // and adjust for response, if need
            string mtype = util.SetMediaType(this);
            string ftype = util.LookUpFileType(mtype);

            // possible control documents
            string XslArgs = this.Context.Server.MapPath(this.DocumentsFolder + "args.xsl");
            string XslGetArgs = this.Context.Server.MapPath(this.DocumentsFolder + "post_args.xsl");

            string xslGetRequest = this.Context.Server.MapPath(this.DocumentsFolder + "post_request.xsl");
            string xslGetRequestContentType = this.Context.Server.MapPath(this.DocumentsFolder + (mtype == string.Empty ? "post_request.xsl" : string.Format("post_request_{0}.xsl", ftype)));

            string xslGetResponse = this.Context.Server.MapPath(this.DocumentsFolder + "post_response.xsl");
            string xslGetResponseContentType = this.Context.Server.MapPath(this.DocumentsFolder + (mtype == string.Empty ? "post_response.xsl" : string.Format("post_response_{0}.xsl", ftype)));

            string XsdFile = this.Context.Server.MapPath(this.DocumentsFolder + "post.xsd");
            string XsdFileMtype = this.Context.Server.MapPath(this.DocumentsFolder + (mtype == string.Empty ? "post.xsd" : string.Format("post_{0}.xsd", ftype)));

            try
            {
                // use regexp pattern to covert url into xml document
                arg_list = util.ParseUrlPattern(absoluteUri, this.UrlPattern);
                util.SafeAdd(ref arg_list, "_title", this.Title);
                util.SafeAdd(ref arg_list, "_last-modified", string.Format("{0:s}Z", DateTime.UtcNow));

                // since GET has no body, build 'stub xmldocument'
                xmlin.LoadXml("<root />");

                // transform into proper argument list
                xsl_file = string.Empty;
                xsl_file = (File.Exists(XslGetArgs) ? XslGetArgs : XslArgs);
                if (File.Exists(xsl_file))
                {
                    xslt = new XslTransformer();
                    out_text = xslt.ExecuteText(xmlin, xsl_file, arg_list);
                    xmlargs.LoadXml(out_text);
                }

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
                xsd_file = string.Empty;
                xsd_file = (XsdFileMtype != string.Empty ? XsdFileMtype : XsdFile);
                if (File.Exists(xsd_file))
                {
                    SchemaValidator sv = new SchemaValidator();
                    string sch_error = sv.Execute(xmlin, xsd_file);
                    if (sch_error != string.Empty)
                        throw new HttpException(422, sch_error);
                }

                // validate html
                util.ValidateXHtmlNodes(ref xmlin, this.XHtmlNodes);

                // transform xmldoc into sql command
                xsl_file = string.Empty;
                xsl_file = (xslGetRequestContentType != string.Empty ? xslGetRequestContentType : xslGetRequest);
                if (File.Exists(xsl_file))
                {
                    xslt = new XslTransformer();
                    string cmdtext = xslt.ExecuteText(xmlin, xsl_file);

                    // execute sql and return results
                    SqlXmlCommand cmd = new SqlXmlCommand(util.GetConfigSectionItem(Constants.cfg_exyusSettings, ConnectionString));
                    cmd.CommandText = cmdtext;

                    using (XmlReader rdr = cmd.ExecuteXmlReader())
                    {
                        xmlout.Load(rdr);
                        rdr.Close();
                    }

                    // transform outputs into representation
                    xsl_file = string.Empty;
                    xsl_file = (xslGetResponseContentType != string.Empty ? xslGetResponseContentType : xslGetResponse);
                    if (File.Exists(xsl_file))
                    {
                        xslt = new XslTransformer();
                        out_text = xslt.ExecuteText(xmlout, xsl_file, arg_list);
                    }
                    else
                    {
                        out_text = util.FixEncoding(xmlout.OuterXml);
                    }

                    // check for redirect to created item
                    this.StatusCode = (this.RedirectOnPost ? HttpStatusCode.Redirect : HttpStatusCode.Created);
                    if (arg_list.ContainsKey("id"))
                        arg_list["id"] = xmlout.SelectSingleNode(this.PostIdXPath).InnerText;
                    else
                        arg_list.Add("id", xmlout.SelectSingleNode(this.PostIdXPath).InnerText);

                    this.Location = util.ReplaceArgs(util.GetConfigSectionItem(Constants.cfg_exyusSettings, Constants.cfg_rootfolder) + this.PostLocationUri, arg_list);

                    // if we were using form-posting, reset to preferred content type (text/html, most likely)
                    if (this.ContentType == Constants.cType_FormUrlEncoded)
                    {
                        this.ContentType = original_contentType;
                    }

                    // cache invalidation
                    ch.ClearCache(this.ImmediateCacheUriTemplates, this.BackgroundCacheUriTemplates, "", arg_list, util.LoadUriCache());
                }
                else
                {
                    throw new HttpException(500, "missing transform");
                }
            }
            catch (HttpException hex)
            {
                this.StatusCode = (HttpStatusCode)hex.GetHttpCode();
                xmlout.LoadXml(string.Format(Constants.fmt_xml_error, hex.Message));
                out_text = util.FixEncoding(xmlout.OuterXml);
            }
            catch (SqlXmlException sqex)
            {
                if (Regex.IsMatch(sqex.Message, rex_notfound))
                    this.StatusCode = HttpStatusCode.NotFound;
                else
                    this.StatusCode = (System.Net.HttpStatusCode)424;
                xmlout.LoadXml(string.Format(Constants.fmt_xml_error_db, Regex.Match(sqex.Message, rex_sqex).Groups[1].Value));
                out_text = util.FixEncoding(xmlout.OuterXml);
            }
            catch (Exception ex)
            {
                this.StatusCode = HttpStatusCode.InternalServerError;
                xmlout.LoadXml(string.Format(Constants.fmt_xml_error, ex.Message));
                out_text = util.FixEncoding(xmlout.OuterXml);
            }

            this.Response = out_text;

            xmlin = null;
            xmlout = null;
        }

        public override void Put()
        {
            XmlDocument xmlout = new XmlDocument();
            XmlDocument xmlin = new XmlDocument();

            string XslFile = this.Context.Server.MapPath(this.DocumentsFolder + "put.xsl");
            string XsdFile = this.Context.Server.MapPath(this.DocumentsFolder + "put.xsd");

            try
            {
                // validate the uri
                string id = Regex.Match(this.Context.Request.RawUrl, this.UrlPattern).Groups[1].Value;
                if (id == string.Empty)
                    throw new HttpException(400, "missing id");

                // get the xmldoc from the entity
                xmlin.Load(this.Context.Request.InputStream);

                // validate the doc
                if (File.Exists(XsdFile))
                {
                    SchemaValidator sv = new SchemaValidator();
                    string sch_error = sv.Execute(xmlin, XsdFile);
                    if (sch_error != string.Empty)
                        throw new HttpException(422, sch_error);
                }

                // validate html nodes
                util.ValidateXHtmlNodes(ref xmlin, this.XHtmlNodes);

                // transform xmldoc into sql command
                if (File.Exists(XslFile))
                {
                    XslTransformer xslt = new XslTransformer();
                    string cmdtext = xslt.ExecuteText(xmlin, XslFile);

                    // execute sql and return results
                    SqlXmlCommand cmd = new SqlXmlCommand(util.GetConfigSectionItem(Constants.cfg_exyusSettings, this.ConnectionString));
                    cmd.CommandText = string.Format(cmdtext, id);

                    using (XmlReader rdr = cmd.ExecuteXmlReader())
                    {
                        xmlout.Load(rdr);
                        rdr.Close();
                    }

                    // cache invalidation
                    ch.ClearCache(this.ImmediateCacheUri, this.BackgroundCacheUri, id, util.LoadUriCache());
                }
                else
                    throw new HttpException(500, "missing transform");
            }
            catch (HttpException hex)
            {
                this.StatusCode = (HttpStatusCode)hex.GetHttpCode();
                xmlout.LoadXml(string.Format(Constants.fmt_xml_error, hex.Message));
            }
            catch (SqlXmlException sqex)
            {
                if (Regex.IsMatch(sqex.Message, rex_notfound))
                    this.StatusCode = HttpStatusCode.NotFound;
                else
                    this.StatusCode = (System.Net.HttpStatusCode)424;
                xmlout.LoadXml(string.Format(Constants.fmt_xml_error_db, Regex.Match(sqex.Message, rex_sqex).Groups[1].Value));
            }
            catch (Exception ex)
            {
                this.StatusCode = HttpStatusCode.InternalServerError;
                xmlout.LoadXml(string.Format(Constants.fmt_xml_error, ex.Message));
            }
            this.Response = xmlout;

            xmlin = null;
            xmlout = null;
        }
     }
}
