using System;
using System.Web;
using System.Xml;
using System.Net;
using System.IO;
using Microsoft.Data.SqlXml;
using System.Text.RegularExpressions;
using System.Collections;
using System.Collections.Specialized;

using Exyus.Xml;
using Exyus.Caching;
using Exyus.Security;

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
        public string PostIdXPath = "//@id";

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
                    string cmdtext = xslt.ExecuteText(xmlin, xsl_file,arg_list);

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
            string mtype = util.SetMediaType(this,this.UpdateMediaTypes);
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

                    // udpate arg list with new id
                    util.SafeAdd(ref arg_list,"id",xmlout.SelectSingleNode(this.PostIdXPath).InnerText);

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
            XmlDocument xmlargs = new XmlDocument();
            XslTransformer xslt = new XslTransformer();
            Hashtable arg_list = new Hashtable();
            string out_text = string.Empty;
            string xsl_file = string.Empty;
            string xsd_file = string.Empty;
            string original_contentType = this.ContentType;

            absoluteUri = this.Context.Request.RawUrl;

            // settle on media type for the method
            string mtype = util.SetMediaType(this, this.UpdateMediaTypes);
            string ftype = util.LookUpFileType(mtype);

            // possible control documents
            string XslArgs = this.Context.Server.MapPath(this.DocumentsFolder + "args.xsl");
            string XslPutArgs = this.Context.Server.MapPath(this.DocumentsFolder + "put_args.xsl");

            string XsdArgs = this.Context.Server.MapPath(this.DocumentsFolder + "args.xsd");
            string XsdPutArgs = this.Context.Server.MapPath(this.DocumentsFolder + "put_args.xsd");

            string xslPutRequest = this.Context.Server.MapPath(this.DocumentsFolder + "put_request.xsl");
            string xslPutRequestContentType = this.Context.Server.MapPath(this.DocumentsFolder + (mtype == string.Empty ? "put_request.xsl" : string.Format("put_request_{0}.xsl", ftype)));

            string xslPutResponse = this.Context.Server.MapPath(this.DocumentsFolder + "put_response.xsl");
            string xslPutResponseContentType = this.Context.Server.MapPath(this.DocumentsFolder + (mtype == string.Empty ? "put_response.xsl" : string.Format("put_response_{0}.xsl", ftype)));

            string XsdFile = this.Context.Server.MapPath(this.DocumentsFolder + "put.xsd");
            string XsdFileMtype = this.Context.Server.MapPath(this.DocumentsFolder + (mtype == string.Empty ? "put.xsd" : string.Format("put_{0}.xsd", ftype)));
            try
            {
                // use regexp pattern to covert url into xml document
                arg_list = util.ParseUrlPattern(absoluteUri, this.UrlPattern);
                util.SafeAdd(ref arg_list, "_title", this.Title);
                util.SafeAdd(ref arg_list, "_last-modified", string.Format("{0:s}Z", DateTime.UtcNow));

                // transform into proper argument list
                xmlargs.LoadXml("<root />");
                xsl_file = string.Empty;
                xsl_file = (File.Exists(XslPutArgs) ? XslPutArgs : XslArgs);
                if (File.Exists(xsl_file))
                {
                    xslt = new XslTransformer();
                    out_text = xslt.ExecuteText(xmlargs, xsl_file, arg_list);
                    xmlargs.LoadXml(out_text);
                }

                // validate the args
                xsd_file = string.Empty;
                xsd_file = (File.Exists(XsdPutArgs) ? XsdPutArgs : XsdArgs);
                if (File.Exists(xsd_file))
                {
                    SchemaValidator sv = new SchemaValidator();
                    string sch_error = sv.Execute(xmlargs, xsd_file);
                    if (sch_error != string.Empty)
                        throw new HttpException(400, sch_error);
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

                // validate html nodes
                util.ValidateXHtmlNodes(ref xmlin, this.XHtmlNodes);

                // prepare for update/create
                bool save_item = false;
                string etag = string.Empty;
                string last_mod = string.Empty;
                string put_error = "Unable to complete PUT.";   // generic message

                // next, do a head request for this resource
                HTTPClient cl = new HTTPClient();
                ExyusPrincipal ep = (ExyusPrincipal)this.Context.User;
                cl.Credentials = new NetworkCredential(((ExyusIdentity)ep.Identity).Name, ((ExyusIdentity)ep.Identity).Password);

                // load headers for request
                PutHeaders ph = new PutHeaders(this.Context);
                if (ph.IfMatch != string.Empty)
                    cl.RequestHeaders.Set(Constants.hdr_if_none_match, ph.IfMatch);
                if (ph.IfUnmodifiedSince != string.Empty)
                    cl.RequestHeaders.Set(Constants.hdr_if_modified_since, ph.IfUnmodifiedSince);
                if (ph.IfUnmodifiedSince == string.Empty && ph.LastModified != string.Empty)
                    cl.RequestHeaders.Set(Constants.hdr_if_modified_since, ph.LastModified);
                // make request for existing resource
                try
                {
                    out_text = cl.Execute(
                        string.Format("{0}://{1}{2}",
                            this.Context.Request.Url.Scheme,
                            this.Context.Request.Url.DnsSafeHost,
                            this.Context.Request.RawUrl),
                        "head", mtype);

                    // record exists, this must be an update
                    etag = util.GetHttpHeader(Constants.hdr_etag, (NameValueCollection)cl.ResponseHeaders);
                    last_mod = util.GetHttpHeader(Constants.hdr_last_modified, (NameValueCollection)cl.ResponseHeaders);

                    // sort out update conditions
                    util.CheckPutUpdateCondition(ph, etag, last_mod, ref put_error, ref save_item);
                }
                catch (HttpException hex2)
                {
                    // record not there or some other error
                    int code = hex2.GetHttpCode();

                    switch (code)
                    {
                        // record exists w/o changes, we can update!
                        case (int)HttpStatusCode.NotModified:
                            save_item = true;
                            break;
                        // see if it's ok to save
                        case (int)HttpStatusCode.NotFound:
                            // sort out create conditions
                            util.CheckPutCreateCondition(ph, this.AllowCreateOnPut, etag, ref put_error, ref save_item);
                            break;
                        // some other error, omgz!
                        default:
                            put_error = hex2.Message + " Unable to create.";
                            save_item = false;
                            break;
                    }
                }

                if (save_item == true)
                {
                    // transform xmldoc into sql command
                    xsl_file = string.Empty;
                    xsl_file = (File.Exists(xslPutRequestContentType) ? xslPutRequestContentType : xslPutRequest);
                    if (File.Exists(xsl_file))
                    {
                        xslt = new XslTransformer();
                        string cmdtext = xslt.ExecuteText(xmlin, xsl_file, arg_list);

                        // execute sql and return results
                        SqlXmlCommand cmd = new SqlXmlCommand(util.GetConfigSectionItem(Constants.cfg_exyusSettings, this.ConnectionString));
                        cmd.CommandText = cmdtext;

                        using (XmlReader rdr = cmd.ExecuteXmlReader())
                        {
                            xmlout.Load(rdr);
                            rdr.Close();
                        }

                        // transform outputs into representation
                        xsl_file = string.Empty;
                        xsl_file = (xslPutResponseContentType != string.Empty ? xslPutResponseContentType : xslPutResponse);
                        if (File.Exists(xsl_file))
                        {
                            xslt = new XslTransformer();
                            out_text = xslt.ExecuteText(xmlout, xsl_file, arg_list);
                        }
                        else
                        {
                            out_text = util.FixEncoding(xmlout.OuterXml);
                        }

                        // cache invalidation
                        ch.ClearCache(this.ImmediateCacheUriTemplates, this.BackgroundCacheUriTemplates, "", arg_list, util.LoadUriCache());
                    }
                    else
                    {
                        throw new HttpException(500, "missing transform");
                    }
                }
                else
                {
                    throw new HttpException((int)HttpStatusCode.PreconditionFailed, put_error);
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

            xmlargs = null;
            xmlin = null;
            xmlout = null;
        }
     }
}
