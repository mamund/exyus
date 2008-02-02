using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Collections;

using Mvp.Xml.XInclude;
using Mvp.Xml.Common.Xsl;

using System.Xml;
using System.Xml.Xsl;
using System.Xml.XPath;

namespace Exyus.Xml
{
    public class XslTransformer
    {
        // return an xml document
        public XmlDocument ExecuteXml(XmlDocument xmldoc, string xslFileName)
        {
            XmlDocument rtndoc = new XmlDocument();
            rtndoc.PreserveWhitespace = false;
            rtndoc.LoadXml(ExecuteText(xmldoc, xslFileName));
            return rtndoc;
        }

        // return a string pile
        public string ExecuteText(XmlDocument xmldoc, string xslFileName)
        {
            return ExecuteText(xmldoc, xslFileName, new Hashtable());
        }
        public string ExecuteText(XmlDocument xmldoc, string xslFileName, Hashtable htargs)
        {
            string rtn = string.Empty;
            Utility util = new Utility();

            // transform it and send it out
            MvpXslTransform xsldoc = new MvpXslTransform();
            xsldoc = (MvpXslTransform)System.Web.HttpContext.Current.Cache.Get(xslFileName);
            if (xsldoc == null)
            {
                xsldoc = new MvpXslTransform();
                XmlResolver xmlres = new XmlUrlResolver();
                using (XmlTextReader xtr = new XmlTextReader(xslFileName))
                {
                    //xtr.ProhibitDtd = false;
                    xsldoc.Load(xtr, new XsltSettings(true, false), xmlres);
                    xtr.Close();
                }
                System.Web.HttpContext.Current.Cache.Add(
                    xslFileName,
                    xsldoc,
                    new System.Web.Caching.CacheDependency(xslFileName),
                    System.Web.Caching.Cache.NoAbsoluteExpiration,
                    System.Web.Caching.Cache.NoSlidingExpiration,
                    System.Web.Caching.CacheItemPriority.Default,
                    null);
            }

            using (TextWriter sw = new StringWriter())
            {
                //xsldoc.Transform(
                //    new XmlInput(xmldoc.CreateNavigator()),
                //    util.GetXsltArgs(htargs),
                //    new XmlOutput(sw)
                //    );

                xsldoc.Transform(
                    new XmlInput(new XmlNodeReader(xmldoc)),
                    util.GetXsltArgs(htargs),
                    new XmlOutput(sw)
                );

                using (TextReader sr = new StringReader(sw.ToString()))
                {
                    rtn = sr.ReadToEnd();
                }
            }
            return rtn;
        }

        // use the supplied writer object
        public void Execute(XmlDocument xmldoc, string xslFileName, ref XmlTextWriter output)
        {
            // transform it and send it out
            Utility util = new Utility();
            MvpXslTransform xsldoc = new MvpXslTransform();
            xsldoc = (MvpXslTransform)System.Web.HttpContext.Current.Cache.Get(xslFileName);
            if (xsldoc == null)
            {
                xsldoc = new MvpXslTransform();
                XmlResolver xmlres = new XmlUrlResolver();
                using (XmlTextReader xtr = new XmlTextReader(xslFileName))
                {
                    //xtr.ProhibitDtd = false;
                    xsldoc.Load(xtr, new XsltSettings(true, false), xmlres);
                    xtr.Close();
                }

                System.Web.HttpContext.Current.Cache.Add(
                    xslFileName,
                    xsldoc,
                    new System.Web.Caching.CacheDependency(xslFileName),
                    System.Web.Caching.Cache.NoAbsoluteExpiration,
                    System.Web.Caching.Cache.NoSlidingExpiration,
                    System.Web.Caching.CacheItemPriority.Default,
                    null);
            }

            //xsldoc.Transform(
            //    new XmlInput(xmldoc.CreateNavigator()),
            //    util.GetXsltArgs(),
            //    new XmlOutput(output)
            //    );
            xsldoc.Transform(
                new XmlInput(new XmlNodeReader(xmldoc)),
                util.GetXsltArgs(),
                new XmlOutput(output)
                );
        }
    }
}
