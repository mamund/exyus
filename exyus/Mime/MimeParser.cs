using System;
using System.Collections;
using System.Text;
using System.Text.RegularExpressions;

namespace Exyus
{
    public struct MimeType : IComparable
    {
        public string ContentType;
        public double QValue;

        public MimeType(string ctype,double qval)
        {
            this.ContentType = ctype;
            this.QValue = qval;
        }

        int IComparable.CompareTo(object obj)
        {
            MimeType tmpObj = (MimeType)obj;
            return (this.QValue.CompareTo(tmpObj.QValue));
        }
    }

    public class MimeParser
    {
        public ArrayList ContentTypes = new ArrayList();
        public MimeParser(){}
        public MimeParser(string acceptHeader)
        {
            ParseAcceptHeader(acceptHeader);
        }

        public string GetBestFit(string[] supportedTypes, string defaultType)
        {
            if (this.ContentTypes == null || this.ContentTypes.Count == 0)
                return defaultType;

            string rtn = string.Empty;
            string mtype = string.Empty;
            for (int i = ContentTypes.Count; i > 0; i--)
            {
                mtype = ((MimeType)ContentTypes[i - 1]).ContentType;

                //check against defaultType first (we favor that!)
                if (mtype == defaultType)
                {
                    rtn = defaultType;
                    break;
                }

                // ok, check against the list of alternates
                for (int j = 0; j < supportedTypes.Length; j++)
                {
                    // exact match
                    if (supportedTypes[j] == mtype)
                    {
                        rtn = supportedTypes[j];
                        break;
                    }

                    // now content-type match w/ wildcard subtype
                    if(supportedTypes[j].Split('/')[0]+"/*" == mtype)
                    {
                        rtn = supportedTypes[j];
                        break;
                    }

                    // now compete wildcard
                    if (mtype == "*/*")
                    {
                        rtn = defaultType;
                        break;
                    }
                }
            }

            return rtn;
        }

        private void ParseAcceptHeader(string accept)
        {
            if (accept == null)
                accept = "*/*";

            string[] types = accept.Split(',');
            for (int i = 0; i < types.Length; i++)
            {
                string[] cq_val = types[i].Split(';');
                string c = cq_val[0];
                string q = (cq_val.Length>1?cq_val[1].Replace("q=",""):"1.0");
                if (q.IndexOf("charset") != -1)
                    q = "1.0";
                ContentTypes.Add(new MimeType(c.Trim(), Convert.ToDouble((q==string.Empty?"1.0":q))));
            }

            this.ContentTypes.Sort();

        }


    }
}
