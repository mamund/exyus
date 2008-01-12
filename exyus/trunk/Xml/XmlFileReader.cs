using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;

using System.Xml;
using System.Xml.Xsl;
using System.Xml.XPath;

using Mvp.Xml.XInclude;
using System.Web.UI;

using System.Net;
using System.Web;
using System.IO;
using System.Text.RegularExpressions;

namespace Exyus.Xml
{
    public class XmlFileReader
    {
        // returns a string version of the xml file
        public string GetXmlFileText(string filePath, string requestUrl)
        {
            return GetXmlFileText(filePath, requestUrl, string.Empty,string.Empty);
        }
        public string GetXmlFileText(string filePath, string requestUrl, string user, string password)
        {
            string rtn = string.Empty;
            Utility util = new Utility();

            try
            {
                Hashtable ht = util.GetArgs();
                using (Stream fs = util.LoadAndTokenize(filePath, ht))
                {
                    // encoding??
                    using (StreamReader reader = new StreamReader(fs, Encoding.UTF8))
                    {
                        using (XIncludingReader ireader = new XIncludingReader(requestUrl, fs))
                        {
                            // handle credentials
                            XmlUrlResolver res = new XmlUrlResolver();
                            if(user==string.Empty && password==string.Empty)
                                res.Credentials = util.GetSystemCredentials();
                            else
                                res.Credentials = new NetworkCredential(user, password);
                            ireader.XmlResolver = res;

                            // get the document
                            XPathDocument doc = new XPathDocument(ireader);
                            XPathNavigator xpnav = doc.CreateNavigator();
                            rtn = util.FixEncoding(xpnav.OuterXml);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new HttpException((int)HttpStatusCode.InternalServerError, ex.Message);
            }

            return rtn;
        }

        // returns a full xmldocument version of the xml file
        public XmlDocument GetXmlFile(string filePath, string requestUrl)
        {
            return GetXmlFile(filePath,requestUrl,string.Empty,string.Empty);
        }
        public XmlDocument GetXmlFile(string filePath, string requestUrl,string user, string password)
        {
            XmlDocument xmldoc = new XmlDocument();
            Utility util = new Utility();
            try
            {
                Hashtable ht = util.GetArgs();
                using(Stream fs = util.LoadAndTokenize(filePath,ht))
                {
                    // encoding??
                    using (StreamReader reader = new StreamReader(fs,Encoding.UTF8))
                    {
                        using (XIncludingReader ireader = new XIncludingReader(requestUrl, fs))
                        {
                            // handle credentials
                            XmlUrlResolver res = new XmlUrlResolver();
                            if (user == string.Empty && password == string.Empty)
                                res.Credentials = util.GetSystemCredentials();
                            else
                                res.Credentials = new NetworkCredential(user, password);
                            ireader.XmlResolver = res;

                            // get the document
                            XPathDocument doc = new XPathDocument(ireader);
                            XPathNavigator xpnav = doc.CreateNavigator();
                            xmldoc.LoadXml(util.FixEncoding(xpnav.OuterXml));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new HttpException((int)HttpStatusCode.InternalServerError, ex.Message);
            }

            return xmldoc;
        }
    }
}
