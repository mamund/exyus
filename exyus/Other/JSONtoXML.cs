/*
 * 2008-02-02 (mca)
 * uses Nii.JSON library (http://json.org/cs.zip)
 * provides valid XML document from supplied JSON string
 */ 
using System;
using System.Collections;
using System.Text;

using System.Xml;
using Nii.JSON;

namespace Exyus
{
    public class JSONtoXML
    {
        public JSONtoXML() { }

        public XmlDocument GetXml(string s)
        {
            XmlDocument doc = new XmlDocument();
            string xml = (string.Format("<root>{0}</root>", ConvertToXML(s)));
            doc.LoadXml(xml);
            return doc;
        }

        private string ConvertToXML(string s)
        {
            object obj = null;
            string xkey = string.Empty;
            StringBuilder sb = new StringBuilder();
            JSONObject jo = new JSONObject();
            Hashtable myHashMap = (Hashtable)JsonFacade.fromJSON(s);

            foreach (string key in myHashMap.Keys)
            {
                xkey = XmlConvert.EncodeName(key.Replace(":", "_"));
                sb.AppendFormat("<{0}>", xkey);

                obj = myHashMap[key];
                if (obj != null)
                {
                    if (obj is string)
                    {
                        sb.Append(XmlEncodeData(obj.ToString()));
                    }
                    else if (obj is float || obj is double)
                    {
                        sb.Append(jo.numberToString(obj));
                    }
                    else if (obj is bool)
                    {
                        sb.Append(obj.ToString().ToLower());
                    }
                    else
                    {
                        sb.Append(HandleNesting(obj.ToString()));
                    }
                }
                sb.AppendFormat("</{0}>", xkey);
            }
            return sb.ToString();
        }

        private string HandleNesting(string data)
        {
            string rtn = string.Empty;

            if (data.IndexOf("{") == 0)
            {
                rtn = ConvertToXML(data);
            }
            else if (data.IndexOf("[") == 0)
            {
                JSONArray ja = new JSONArray(data);
                for (int i = 0; i < ja.Count; i++)
                {
                    rtn += string.Format("<item>{0}</item>", HandleNesting(ja[i].ToString()));
                }
            }
            else
            {
                rtn = XmlEncodeData(data);
            }
            return rtn;
        }

        private string XmlEncodeData(string data)
        {

            XmlDocument doc = new XmlDocument();
            XmlElement elm = doc.CreateElement("temp");
            elm.AppendChild(doc.CreateTextNode(data));
            string rtn = elm.InnerXml;
            return rtn;
        }

    }
}
