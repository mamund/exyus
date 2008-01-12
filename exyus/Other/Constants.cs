using System;
using System.Collections.Generic;
using System.Text;

namespace Exyus
{
    public class Constants
    {
        // methods
        public const string HttpGet = "get";
        public const string HttpPost = "post";
        public const string HttpPut = "put";
        public const string HttpDelete = "delete";
        public const string HttpHead = "head";
        public const string HttpOptions = "options";

        // mime types
        public const string cType_Xml = "text/xml";
        public const string cType_Html = "text/html";
        public const string cType_FormUrlEncoded = "application/x-www-form-urlencoded";

        // headers
        public const string hdr_last_modified = "Last-Modified";
        public const string hdr_if_modified_since = "If-Modified-Since";
        public const string hdr_if_unmodified_since = "If-Unmodified-Since";
        public const string hdr_if_none_match = "If-None-Match";
        public const string hdr_if_match = "If-Match";
        public const string hdr_etag = "ETag";
        public const string hdr_cache_control = "Cache-Control";
        public const string hdr_expires = "Expires";
        public const string hdr_exyus = "X-Exyus";
        public const string hdr_cookie = "Cookie";
        public const string hdr_authorization = "Authorization";
        public const string hdr_ExyusCache = "X-Exyus-Cache";
        public const string hdr_no_cache = "no-cache";

        // string formats
        public const string fmt_gmtdate = "{0:r}";
        public const string fmt_utcdate = "(0:s}";
        public const string fmt_max_age = "max-age={0}";
        public const string fmt_html_error = "<html><body><div class='error'><h2>error</h2><p>{0}</p></div></body></html>";
        public const string fmt_html_error_inc = "<html><body><div class='error'><h2>error</h2><p>{0}<br />{1}<br />{2}</p></div></body></html>";
        public const string fmt_html_error_db = "<html><body><div class='error'><h2>error</h2><p>{0} (db)</p></div></body></html>";
        public const string fmt_xml_error = "<error>{0}</error>";
        public const string fmt_xml_error_db = "<error>{0}(db)</error>";
        public const string fmt_xml_error_inc = "<error>{0}-{1}-{2}</error>";
        public const string fmt_exyus = "{0} {1}";
        public const string fmt_error_module = "<html><head><title>{0} Error</title></head><body><h1>{0} Error</h1><p>{1}</p></body></html>\n\n";

        // inline xsl processing 
        public const string xsl_pi = "/processing-instruction('xml-stylesheet')";
        public const string xsl_type = "type=[\"'](.*?)[\"']";
        public const string xsl_href = "href=[\"'](.*?)[\"']";

        // config items
        public const string cfg_rootfolder = "rootfolder";
        public const string cfg_caching = "caching";
        public const string cfg_templatefolder = "templatefolder";
        public const string cfg_exyusSecurity = "exyusSecurity";
        public const string cfg_authTimeout = "AuthTimeout";
        public const string cfg_fileExtension = "fileExtension";
        public const string cfg_mediaTypes = "mediaTypes";
        public const string cfg_exyusSettings = "exyusSettings";
        public const string cfg_redirectUrls = "redirectUrls";
        public const string cfg_rewriteUrls = "rewriteUrls";
        public const string cfg_authFormUrl = "authFormUrl";
        public const string cfg_authUrls = "authUrls";
        public const string cfg_authUsers = "authUsers";
        public const string cfg_systemUser = "systemUser";
        public const string cfg_authType = "authType";
        public const string cfg_authRealm = "authRealm";
        public const string cfg_sendSessionCookies = "send-session-cookies";
        public const string cfg_sendUserCookies = "send-user-cookies";

        // misc
        public const string msc_sys_file_ext = ".xcs";
        public const string msc_exyus_agent = "exyus/1.0";
    }
}
