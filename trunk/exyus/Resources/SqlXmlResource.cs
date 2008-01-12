/*
 * NOTICE: not working yet
 */
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
    public class SqlXmlResource : WebResource
    {
        private Utility util = new Utility();
        private string rex_sqex = "Description=\"(.*)\"";
        private string rex_notfound = "not found";

        public string ConnectionString = string.Empty;
        public string UrlPattern = string.Empty;
        public string PostLocationUri = string.Empty;
        public string DocumentsFolder = string.Empty;
        public string[] XHtmlNodes = null;
        public string[] ImmediateCacheUri = null;
        public string[] BackgroundCacheUri = null;
        Cache ch = new Cache();

        public SqlXmlResource()
        {
            this.ContentType = Constants.cType_Xml;

            //get first pattern (if none set already)
            if (this.UrlPattern == null || this.UrlPattern == string.Empty)
                this.UrlPattern = ((UriPattern)this.GetType().GetCustomAttributes(typeof(UriPattern), false)[0]).Patterns[0];
        }
        public override void Delete()
        {
            XmlDocument xmlout = new XmlDocument();
            XmlDocument xmlin = new XmlDocument();

            string XslFile = this.Context.Server.MapPath(this.DocumentsFolder + "delete.xsl");
            string XsdFile = this.Context.Server.MapPath(this.DocumentsFolder + "delete.xsd");

            try
            {
                // validate the uri and compose doc
                string id = Regex.Match(this.Context.Request.RawUrl, UrlPattern).Groups[1].Value;
                if (id == string.Empty)
                    throw new HttpException(400, "bad uri");
                else 
                    xmlin.LoadXml(string.Format("<root><id>{0}</id></root>",id));

                // validate schema
                if (File.Exists(XsdFile))
                {
                    SchemaValidator sv = new SchemaValidator();
                    string sch_error = sv.Execute(xmlin, XsdFile);
                    if (sch_error != string.Empty)
                        throw new HttpException(422, sch_error);
                }

                // transform xmldoc into sql command
                if (File.Exists(XslFile))
                {
                    XslTransformer xslt = new XslTransformer();
                    string cmdtext = xslt.ExecuteText(xmlin, XslFile);

                    // execute sql and return empty
                    SqlXmlCommand cmd = new SqlXmlCommand(util.GetConfigSectionItem(Constants.cfg_exyusSettings, this.ConnectionString));
                    cmd.CommandText = cmdtext;
                    cmd.ExecuteNonQuery();
                    this.StatusCode = HttpStatusCode.OK;

                    // cache invalidation
                    ch.ClearCache(this.ImmediateCacheUri, this.BackgroundCacheUri, id, util.LoadUriCache());
                }
                else
                    throw new HttpException(500, "missing transform");


                xmlout = null;
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
                    this.StatusCode = HttpStatusCode.InternalServerError;
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

        public override void Head()
        {
            this.Get();
            this.Response = null;
        }

        public override void Get()
        {
            XmlDocument xmlout = new XmlDocument();
            XmlDocument xmlin = new XmlDocument();

            string XslFile = this.Context.Server.MapPath(this.DocumentsFolder + "get.xsl");
            string XsdFile = this.Context.Server.MapPath(this.DocumentsFolder + "get.xsd");

            // determine mediatype for this request
            // and adjust for response, if need
            string mtype = util.SetMediaType(this);
            string ftype = util.LookUpFileType(mtype);

            try
            {
                if (ch.CachedResourceIsValid((WebResource)this))
                    return;

                // ok, let's try to build a new one

                // get the id (if it's there)
                string id = Regex.Match(this.Context.Request.RawUrl,this.UrlPattern).Groups[1].Value;
                string args = Regex.Match(this.Context.Request.RawUrl, this.UrlPattern).Groups[2].Value;

                // transform xmldoc into sql command
                xmlin.LoadXml(string.Format("<root><id>{0}</id></root>", id));

                // validate schema
                if (File.Exists(XsdFile))
                {
                    SchemaValidator sv = new SchemaValidator();
                    string sch_error = sv.Execute(xmlin, XsdFile);
                    if (sch_error != string.Empty)
                        throw new HttpException(422, sch_error);
                }

                // transform
                if (File.Exists(XslFile))
                {
                    XslTransformer xslt = new XslTransformer();
                    string cmdtext = xslt.ExecuteText(xmlin, XslFile);


                    // execute sql and return results
                    SqlXmlCommand cmd = new SqlXmlCommand(util.GetConfigSectionItem(Constants.cfg_exyusSettings, this.ConnectionString));
                    cmd.CommandText = cmdtext;

                    using (XmlReader rdr = cmd.ExecuteXmlReader())
                    {
                        xmlout.Load(rdr);
                        rdr.Close();
                    }

                    // handle caching of this resource
                    ch.CacheResource((WebResource)this,util.FixEncoding(xmlout.OuterXml));
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

            // return the results
            this.Response = xmlout;

            xmlin = null;
            xmlout = null;
        }

        public override void Post()
        {
            XmlDocument xmlout = new XmlDocument();
            XmlDocument xmlin = new XmlDocument();

            string XslFile = this.Context.Server.MapPath(this.DocumentsFolder + "post.xsl");
            string XsdFile = this.Context.Server.MapPath(this.DocumentsFolder + "post.xsd");

            try
            {
                // validate the uri
                string id = Regex.Match(this.Context.Request.RawUrl, this.UrlPattern).Groups[1].Value;
                if (id != string.Empty)
                    throw new HttpException(400, "bad uri");

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

                // validate html
                util.ValidateXHtmlNodes(ref xmlin, this.XHtmlNodes);

                // transform xmldoc into sql command
                if (File.Exists(XslFile))
                {
                    XslTransformer xslt = new XslTransformer();
                    string cmdtext = xslt.ExecuteText(xmlin, XslFile);

                    // execute sql and return results
                    SqlXmlCommand cmd = new SqlXmlCommand(util.GetConfigSectionItem(Constants.cfg_exyusSettings, ConnectionString));
                    cmd.CommandText = cmdtext;

                    using (XmlReader rdr = cmd.ExecuteXmlReader())
                    {
                        xmlout.Load(rdr);
                        rdr.Close();
                    }
                    this.StatusCode = HttpStatusCode.Created;
                    if (xmlout.SelectSingleNode("//id") != null)
                    {
                        id = xmlout.SelectSingleNode("//id").Value;
                        this.Location = util.GetConfigSectionItem(Constants.cfg_exyusSettings, Constants.cfg_rootfolder) + this.PostLocationUri + id;
                    }

                    // cache invalidation
                    ch.ClearCache(this.ImmediateCacheUri, this.BackgroundCacheUri, "", util.LoadUriCache());
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
