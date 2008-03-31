using System;
using System.Web;
using System.Xml;
using System.Collections.Specialized;
using System.Collections;
using System.Net;
using System.IO;
using System.Text.RegularExpressions;

using Exyus.Xml;
using Exyus.Security;
using Exyus.Caching;

namespace Exyus.Web
{
    [MediaTypes("text/xml")]
    public class XmlFileResource : HTTPResource
    {
        // public properties        
        public string FileExtension = ".xml";
        public string PostLocationUri = string.Empty;
        public string StorageFolder = string.Empty;
        public string DocumentsFolder = string.Empty;
        public string[] XHtmlNodes = null;
        public bool RedirectOnPost = false;
        public bool RedirectOnPut = false;

        // internal vars
        private Utility util = new Utility();
        private string rex_notfound = "not found";
        private string s_ext = string.Empty;
        Cache ch = new Cache();

        public XmlFileResource()
        {
            if (this.ContentType == null || this.ContentType == string.Empty)
            {
                this.ContentType = Constants.cType_Html;
            }

            // set system extension
            s_ext = util.GetConfigSectionItem(Constants.cfg_exyusSettings,Constants.cfg_fileExtension, Constants.msc_sys_file_ext);
        }

        // currently deletes a single file
        public override void Delete()
        {
            XmlDocument xmlin = new XmlDocument();
            XmlDocument xmlout = new XmlDocument();
            string fullname = string.Empty;
            string out_text = string.Empty;
            string stor_folder = string.Empty;

            try
            {
                string XslArgs = this.Context.Server.MapPath(this.DocumentsFolder + "args.xsl");
                string XslDeleteArgs = this.Context.Server.MapPath(this.DocumentsFolder + "delete_args.xsl");

                // resolve storagefolder template
                stor_folder = util.ReplaceArgs(this.StorageFolder, ArgumentList);

                // since DELETE has no body, build 'stub xmldocument'
                xmlin.LoadXml("<root />");

                string arg_doc = string.Empty;
                arg_doc = (File.Exists(XslDeleteArgs) ? XslDeleteArgs : XslArgs);

                // transform into file to delete
                if (File.Exists(arg_doc))
                {
                    XslTransformer xslt = new XslTransformer();
                    out_text = xslt.ExecuteText(xmlin, arg_doc, ArgumentList);
                }
                
                // get file to delete
                if(out_text == string.Empty)
                    throw new HttpException(400, "bad request");
                else
                    fullname = this.Context.Server.MapPath(string.Format("{0}{1}{2}",stor_folder,out_text,this.FileExtension));

                // delete the physical file
                if (File.Exists(fullname))
                {
                    File.Delete(fullname);
                    this.StatusCode = HttpStatusCode.NoContent;
                    ch.ClearCache(this.ImmediateCacheUriTemplates, this.BackgroundCacheUriTemplates, "", ArgumentList, util.LoadUriCache());
                }
                else
                    throw new FileNotFoundException(string.Format(rex_notfound + " [{0}]", this.AbsoluteUri.Replace(s_ext, "")));

                xmlout = null;
            }
            catch (HttpException hex)
            {
                this.StatusCode = (HttpStatusCode)hex.GetHttpCode();
                this.StatusDescription = hex.Message;
                out_text = util.RenderError("http error", hex.Message, CurrentMediaType);
            }
            catch (FileNotFoundException fex)
            {
                this.StatusCode = HttpStatusCode.NotFound;
                this.StatusDescription = fex.Message;
                out_text = util.RenderError("file error", fex.Message, CurrentMediaType);
            }
            catch (Exception ex)
            {
                this.StatusCode = HttpStatusCode.InternalServerError;
                this.StatusDescription = ex.Message;
                out_text = util.RenderError("unknown error", ex.Message, CurrentMediaType);
            }

            if (xmlout != null)
                this.Response = util.FixEncoding(xmlout.OuterXml);
            else
                this.Response = null;

            xmlout = null;
        }

        // get a single item or a list of items in storage
        public override void Get()
        {
            XmlDocument xmlout = new XmlDocument();
            XmlDocument xmlin = new XmlDocument();

            string out_text = string.Empty;
            string xsl_file = string.Empty;
            string stor_folder = string.Empty;

            // possible control documents
            string XslArgs = this.Context.Server.MapPath(this.DocumentsFolder + "args.xsl");
            string XslGetArgs = this.Context.Server.MapPath(this.DocumentsFolder + "get_args.xsl");
            
            string xslGetRequest = this.Context.Server.MapPath(this.DocumentsFolder + "get_request.xsl");
            string xslGetRequestContentType = this.Context.Server.MapPath(this.DocumentsFolder + (CurrentMediaType == string.Empty ? "get_request.xsl" : string.Format("get_request_{0}.xsl", CurrentFileType)));

            string xslGetResponse = this.Context.Server.MapPath(this.DocumentsFolder + "get_response.xsl");
            string xslGetResponseContentType = this.Context.Server.MapPath(this.DocumentsFolder + (CurrentMediaType == string.Empty ? "get_response.xsl" : string.Format("get_response_{0}.xsl", CurrentFileType)));

            try
            {
                // see if we have a current copy
                if(ch.CachedResourceIsValid((HTTPResource)this))
                    return;

                // since GET has no body, build 'stub xmldocument'
                xmlin.LoadXml("<root />");

                // transform into file to get
                xsl_file = (File.Exists(XslGetArgs) ? XslGetArgs : XslArgs);
                if (File.Exists(xsl_file))
                {
                    XslTransformer xslt = new XslTransformer();
                    out_text = xslt.ExecuteText(xmlin, xsl_file, ArgumentList);
                }

                // fix up storage folder
                stor_folder = util.ReplaceArgs(this.StorageFolder, ArgumentList);

                // must be a single document
                if (out_text != string.Empty)
                {
                    string fullname = this.Context.Server.MapPath(string.Format("{0}{1}{2}",stor_folder,out_text,this.FileExtension));
                    if (File.Exists(fullname))
                    {
                        using (XmlTextReader xtr = new XmlTextReader(fullname))
                        {
                            xmlout.Load(xtr);
                            xtr.Close();
                        }
                    }
                    else
                        throw new FileNotFoundException(string.Format(rex_notfound + " [{0}]", this.AbsoluteUri.Replace(s_ext, "")));

                    xsl_file = (File.Exists(xslGetResponseContentType) ? xslGetResponseContentType : xslGetResponse);
                    util.SafeAdd(ref ArgumentList, "_mode", "item");
                    if (File.Exists(xsl_file))
                    {
                        XslTransformer xslt = new XslTransformer();
                        out_text = xslt.ExecuteText(xmlout, xsl_file, ArgumentList);
                    }
                }
                else
                {
                    // create list of files 
                    xmlin = new XmlDocument();
                    out_text = "<root>";
                    string path = this.Context.Server.MapPath(stor_folder);
                    if (Directory.Exists(path))
                    {
                        foreach (string file in System.IO.Directory.GetFiles(path, "*"+this.FileExtension))
                        {
                            FileInfo fi = new FileInfo(file);
                            out_text += string.Format("<item dref=\"{0}\">{1}</item>", fi.FullName, fi.Name.Replace(fi.Extension,""));
                        }
                        out_text += "</root>";
                        xmlin.LoadXml(out_text);

                        // transform into list
                        util.SafeAdd(ref ArgumentList, "_mode", "list");
                        xsl_file = (File.Exists(xslGetResponseContentType) ? xslGetResponseContentType : xslGetResponse);
                        if (File.Exists(xsl_file))
                        {
                            XslTransformer xslt = new XslTransformer();
                            out_text = xslt.ExecuteText(xmlin, xsl_file, ArgumentList);
                        }
                        else
                            out_text = xmlin.OuterXml;
                    }
                    else
                        throw new FileNotFoundException(string.Format(rex_notfound + " [{0}]", this.Context.Request.RawUrl.Replace(s_ext, "")));
                }

                ch.CacheResource((HTTPResource)this, util.FixEncoding(out_text));
            }
            catch (HttpException hex)
            {
                this.StatusCode = (HttpStatusCode)hex.GetHttpCode();
                this.StatusDescription = hex.Message;
                out_text = util.RenderError("http error", hex.Message, CurrentMediaType);
            }
            catch (FileNotFoundException fex)
            {
                this.StatusCode = HttpStatusCode.NotFound;
                this.StatusDescription = fex.Message;
                out_text = util.RenderError("file error", fex.Message, CurrentMediaType);
            }
            catch (Exception ex)
            {
                this.StatusCode = HttpStatusCode.InternalServerError;
                this.StatusDescription = ex.Message;
                out_text = util.RenderError("unknown error", ex.Message, CurrentMediaType);
            }

            // return the results
            this.Response = util.FixEncoding(out_text);

            xmlin = null;
            xmlout = null;
        }

        public override void Head()
        {
            this.Get();
            this.Response = null;
        }

        public override void Post()
        {
            XmlDocument xmlout = new XmlDocument();
            XmlDocument xmlin = new XmlDocument();
            XmlDocument xmlargs = new XmlDocument();
            string stor_folder = string.Empty;
            string id = string.Empty;
            string xsl_file = string.Empty;
            string xsd_file = string.Empty;
            string original_contentType = this.ContentType;
            string out_text = string.Empty;

            string XslArgs = this.Context.Server.MapPath(this.DocumentsFolder + "args.xsl");
            string XslPostArgs = this.Context.Server.MapPath(this.DocumentsFolder + "post_args.xsl");
            string XsdFile = this.Context.Server.MapPath(this.DocumentsFolder + "post.xsd");
            string XsdFileMtype = this.Context.Server.MapPath(this.DocumentsFolder + (CurrentMediaType == string.Empty ? "post.xsd" : string.Format("post_{0}.xsd", CurrentFileType)));
            string XslPostRequest = this.Context.Server.MapPath(this.DocumentsFolder + "post_request.xsl");
            string XslPostRequestMtype = this.Context.Server.MapPath(this.DocumentsFolder + (CurrentMediaType == string.Empty ? "post_request.xsl" : string.Format("post_request_{0}.xsl", CurrentFileType)));

            try
            {
                // validate args
                xsl_file = (File.Exists(XslPostArgs) ? XslPostArgs : XslArgs);
                if (File.Exists(xsl_file))
                {
                    xmlargs.LoadXml("<root />");
                    XslTransformer xslt = new XslTransformer();
                    id = xslt.ExecuteText(xmlargs, xsl_file, ArgumentList);
                }
                // transform *must not* return doc id!
                if (id != string.Empty)
                    throw new HttpException(400, "Cannot POST using resource id");

                // fix up the storage folder
                stor_folder = util.ReplaceArgs(this.StorageFolder, ArgumentList);

                // get the xmldoc from the entity
                this.Context.Request.InputStream.Position = 0;
                switch (CurrentMediaType.ToLower())
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
                    out_text = xslt.ExecuteText(xmlin, xsl_file, ArgumentList);
                    xmlout.LoadXml(out_text);
                }
                else
                    xmlout = xmlin;

                // save it by creating a unique key
                id = util.UID();
                string fullname = this.Context.Server.MapPath(string.Format("{0}{1}{2}",stor_folder,id,this.FileExtension));
                if (File.Exists(fullname))
                    throw new HttpException(400, "already exists");
                else
                {
                    if (!Directory.Exists(this.Context.Server.MapPath(stor_folder)))
                        Directory.CreateDirectory(this.Context.Server.MapPath(stor_folder));
                    xmlout.Save(fullname);
                }

                // redirect to created item
                this.StatusCode = (this.RedirectOnPost?HttpStatusCode.Redirect:HttpStatusCode.Created);
                this.Location = util.GetConfigSectionItem(Constants.cfg_exyusSettings, Constants.cfg_rootfolder) + util.ReplaceArgs(this.PostLocationUri.Replace("{id}", id), ArgumentList);

                xmlout = null;

                // cache invalidation
                ch.ClearCache(this.ImmediateCacheUriTemplates, this.BackgroundCacheUriTemplates, "", ArgumentList, util.LoadUriCache());
            }
            catch (HttpException hex)
            {
                this.StatusCode = (HttpStatusCode)hex.GetHttpCode();
                this.StatusDescription = hex.Message;
                out_text = util.RenderError("http error", hex.Message, CurrentMediaType);
            }
            catch (Exception ex)
            {
                this.StatusCode = HttpStatusCode.InternalServerError;
                this.StatusDescription = ex.Message;
                out_text = util.RenderError("unknown error", ex.Message, CurrentMediaType);
            }

            if (xmlout != null)
                this.Response = util.FixEncoding(xmlout.OuterXml);
            else
            {
                if (out_text != string.Empty)
                    this.Response = out_text;
                else
                    this.Response = null;
            }

            // if we were using form-posting, reset to preferred content type (text/html, most likely)
            if (this.ContentType == Constants.cType_FormUrlEncoded)
            {
                this.ContentType = (original_contentType == Constants.cType_FormUrlEncoded ? Constants.cType_Html : original_contentType);
            }

            xmlin = null;
            xmlout = null;
        }

        // overwrites existing item
        public override void Put()
        {
            XmlDocument xmlout = new XmlDocument();
            XmlDocument xmlin = new XmlDocument();
            XmlDocument xmlargs = new XmlDocument();
            string out_text = string.Empty;
            string id = string.Empty;
            string stor_folder = string.Empty;
            string xsl_file = string.Empty;
            string original_contentType = this.ContentType;

            string XslArgs = this.Context.Server.MapPath(this.DocumentsFolder + "args.xsl");
            string XslPutArgs = this.Context.Server.MapPath(this.DocumentsFolder + "put_args.xsl");
            string XsdFile = this.Context.Server.MapPath(this.DocumentsFolder + "put.xsd");
            string XslPutRequest = this.Context.Server.MapPath(this.DocumentsFolder + "put_request.xsl");
            string XslPutRequestMtype = this.Context.Server.MapPath(this.DocumentsFolder + (CurrentMediaType == string.Empty ? "put_request.xsl" : string.Format("put_request_{0}.xsl", CurrentFileType)));

            try
            {
                stor_folder = util.ReplaceArgs(this.StorageFolder, ArgumentList);

                // validate args
                xsl_file = (File.Exists(XslPutArgs) ? XslPutArgs : XslArgs);
                if (File.Exists(xsl_file))
                {
                    xmlargs.LoadXml("<root />");
                    XslTransformer xslt = new XslTransformer();
                    id = xslt.ExecuteText(xmlargs, xsl_file, ArgumentList);
                }
                // transform *must* return doc id!
                if (id == string.Empty)
                    throw new HttpException(400, "missing resource id");

                // get the xmldoc from the entity body
                xmlin = new XmlDocument();
                this.Context.Request.InputStream.Position=0;
                switch (CurrentMediaType.ToLower())
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
                if (File.Exists(XsdFile))
                {
                    SchemaValidator sv = new SchemaValidator();
                    string sch_error = sv.Execute(xmlin, XsdFile);
                    if (sch_error != string.Empty)
                        throw new HttpException(422, sch_error);
                }

                // validate html nodes
                util.ValidateXHtmlNodes(ref xmlin, this.XHtmlNodes);

                // transform xmldoc if needed
                xsl_file = (File.Exists(XslPutRequestMtype) ? XslPutRequestMtype : XslPutRequest);
                if (File.Exists(xsl_file))
                {
                    XslTransformer xslt = new XslTransformer();
                    out_text = xslt.ExecuteText(xmlin, xsl_file, ArgumentList);
                    xmlout.LoadXml(out_text);
                }
                else
                    xmlout = xmlin;

                // get ready to save it
                string fullname = this.Context.Server.MapPath(string.Format("{0}{1}{2}", stor_folder, id, this.FileExtension));
                bool file_exists = File.Exists(fullname);

                // set to only allow updates? (quick check)
                if (this.AllowCreateOnPut == false && !file_exists)
                    throw new FileNotFoundException(string.Format(rex_notfound + " [{0}]", this.Context.Request.RawUrl.Replace(s_ext, "")));

                // ok, we allow update and create
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
                if(ph.IfUnmodifiedSince==string.Empty && ph.LastModified!=string.Empty)
                    cl.RequestHeaders.Set(Constants.hdr_if_modified_since, ph.LastModified);

                // make request for existing resource
                try
                {
                    out_text = cl.Execute(
                        string.Format("{0}://{1}{2}",
                            this.Context.Request.Url.Scheme,
                            this.Context.Request.Url.DnsSafeHost,
                            this.Context.Request.RawUrl),
                        "head", (CurrentMediaType == Constants.cType_FormUrlEncoded ? "text/html" : CurrentMediaType));

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
                            put_error = hex2.Message + " Unable to PUT.";
                            save_item = false;
                            break;
                    }
                }

                // ok, all passed - try to save it
                if (save_item==true)
                {
                    // save it (include the folder, if needed)
                    if (!Directory.Exists(this.Context.Server.MapPath(stor_folder)))
                        Directory.CreateDirectory(this.Context.Server.MapPath(stor_folder));
                    
                    xmlout.Save(fullname);

                    this.StatusCode = (this.RedirectOnPut?HttpStatusCode.Redirect:HttpStatusCode.OK);
                    this.Location = this.AbsoluteUri;
                    xmlout = null;

                    // cache invalidation
                    ch.ClearCache(this.ImmediateCacheUriTemplates, this.BackgroundCacheUriTemplates, "", ArgumentList, util.LoadUriCache());
                }
                else
                    throw new HttpException((int)HttpStatusCode.PreconditionFailed, put_error);
            }
            catch (HttpException hex)
            {
                this.StatusCode = (HttpStatusCode)hex.GetHttpCode();
                this.StatusDescription = hex.Message;
                out_text = util.RenderError("http error", hex.Message, CurrentMediaType);
            }
            catch (FileNotFoundException fex)
            {
                this.StatusCode = HttpStatusCode.NotFound;
                this.StatusDescription = fex.Message;
                out_text = util.RenderError("file error", fex.Message, CurrentMediaType);
            }
            catch (Exception ex)
            {
                this.StatusCode = HttpStatusCode.InternalServerError;
                this.StatusDescription = ex.Message;
                out_text = util.RenderError("unknown error", ex.Message, CurrentMediaType);
            }

            if (xmlout != null)
                this.Response = util.FixEncoding(xmlout.OuterXml);
            else
            {
                if (out_text != string.Empty)
                    this.Response = out_text;
                else 
                    this.Response = null;
            }

            // if we were using form-posting, reset to preferred content type (text/html, most likely)
            if (this.ContentType == Constants.cType_FormUrlEncoded)
            {
                this.ContentType = (original_contentType == Constants.cType_FormUrlEncoded ? Constants.cType_Html : original_contentType);
            }

            xmlin = null;
            xmlout = null;
        }
    }
}
