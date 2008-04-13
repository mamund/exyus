using System;
using System.Collections.Generic;
using System.Text;
using System.Web;

namespace Exyus.Xml
{
  // handy functions to import into XSLT Transforms
  class XsltFunctions
  {
    Utility util = new Utility();

    public XsltFunctions() { }

    public string MD5(string data)
    {
      return util.MD5(data);
    }
    public string MD5(string data, string removeTail)
    {
      return util.MD5(data, (removeTail.ToLower() == "true" ? true : false));
    }

    public string MD5BinHex(string val)
    {
      return util.MD5BinHex(val);
    }

    public string SHA1(string data)
    {
      return util.SHA1(data);
    }

    public string Base64Encode(string str)
    {
      return util.Base64Encode(str);
    }

    public string Base64Decode(string str)
    {
      return util.Base64Decode(str);
    }

    public string UID()
    {
      return util.UID();
    }

    public string GUID()
    {
      return System.Guid.NewGuid().ToString();
    }

    public string HttpHeader(string name)
    {
      if (HttpContext.Current.Request.Headers[name] != null)
        return HttpContext.Current.Request.Headers[name];
      else
        return string.Empty;
    }

    public string ServerVar(string key)
    {
      return HttpContext.Current.Request.ServerVariables[key];
    }

    public string UrlEncode(string data)
    {
      return HttpContext.Current.Server.UrlEncode(data);
    }
  }
}
