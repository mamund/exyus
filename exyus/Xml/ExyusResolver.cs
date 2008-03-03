using System;
using System.Collections.Generic;
using System.Text;

using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using System.Xml.XPath;
using System.Xml.Xsl;
using System.Web;
using System.Net;
using System.IO;


namespace Exyus.Xml
{
    public class ExyusResolver : XmlUrlResolver
    {
        public string Accept = string.Empty;
        public string AcceptLangugage = string.Empty;

        public override ICredentials Credentials
        {
            set
            {
                base.Credentials = value;
            }
        }

        public override object GetEntity(Uri absoluteUri, string role, Type ofObjectToReturn)
        {
            WebResponse wRes;
            return GetResource(absoluteUri.AbsoluteUri, this.Accept, this.AcceptLangugage, out wRes);
            //return base.GetEntity(absoluteUri, role, ofObjectToReturn);
        }

        internal static Stream GetResource(string includeLocation,
            string accept, string acceptLanguage, out WebResponse response)
        {
            WebRequest wReq;
            try
            {
                wReq = WebRequest.Create(includeLocation);
            }
            catch (Exception ex)
            {
                throw new HttpException(500, ex.Message);
            }

            //Add accept headers if this is HTTP request
            HttpWebRequest httpReq = wReq as HttpWebRequest;
            if (httpReq != null)
            {
                if (accept != null)
                {
                    //TextUtils.CheckAcceptValue(accept);
                    if (httpReq.Accept == null || httpReq.Accept == String.Empty)
                        httpReq.Accept = accept;
                    else
                        httpReq.Accept += "," + accept;
                }
                if (acceptLanguage != null)
                {
                    if (httpReq.Headers["Accept-Language"] == null)
                        httpReq.Headers.Add("Accept-Language", acceptLanguage);
                    else
                        httpReq.Headers["Accept-Language"] += "," + acceptLanguage;
                }
            }
            try
            {
                response = wReq.GetResponse();
            }
            catch (WebException we)
            {
                throw new HttpException(500, we.Message);
            }
            return response.GetResponseStream();
        }
    }
}
