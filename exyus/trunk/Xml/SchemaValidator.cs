using System;
using System.Collections.Generic;
using System.Text;

using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using System.Xml.XPath;

using System.IO;

namespace Exyus.Xml
{
    public class SchemaValidator
    {
        public string Execute(XmlDocument xmldoc, string xsdFileName)
        {
            string rtn = string.Empty;

            try
            {
                // prep the xmldoc w/ the schema
                // load and validate
                xmldoc.Schemas.Add(null, xsdFileName);
                xmldoc.Validate(null);
            }
            catch (Exception ex)
            {
                rtn = ex.Message;
            }

            return rtn;
        }

        public string Execute(string xmlFileName, string xsdFileName)
        {
            string rtn = string.Empty;

            XmlDocument xd = null;

            try
            {
                // prep the xmldoc w/ the schema
                // load and validate
                xd = new XmlDocument();
                xd.Schemas.Add(null, xsdFileName);
                using (XmlTextReader xtr = new XmlTextReader(xmlFileName))
                {
                    xd.Load(xtr);
                    xtr.Close();
                }
                xd.Validate(null);
            }
            catch (Exception ex)
            {
                rtn = ex.Message;
            }

            return rtn;
        }

        public string Execute(object objectToValidate, string xsdFileName)
        {
            string rtn = string.Empty;

            XmlDocument xd = null;
            XmlSerializer xs = null;
            MemoryStream ms = null;
            StreamReader sr = null;

            try
            {
                // prep the xmldoc w/ the schema
                xd = new XmlDocument();
                xd.Schemas.Add(null, xsdFileName);

                // prep the serializer
                xs = new XmlSerializer(objectToValidate.GetType());

                // move the object into an xmldocument
                using (ms = new MemoryStream())
                {
                    xs.Serialize(ms, objectToValidate);
                    ms.Position = 0;
                    using (sr = new StreamReader(ms))
                    {
                        xd.LoadXml(sr.ReadToEnd());
                        sr.Close();
                    }
                    ms.Close();
                }

                // now validate it
                xd.Validate(null);
            }
            catch (Exception ex)
            {
                rtn = ex.Message;
            }

            return rtn;
        }
    }
}
