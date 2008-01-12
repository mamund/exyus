using System;
using System.Web;
using System.IO.Compression;

namespace Exyus.Web
{
    public class HttpCompressionModule : IHttpModule
    {
        void IHttpModule.Init(HttpApplication context)
        {
            context.BeginRequest += new EventHandler(context_BeginRequest);
        }
        void IHttpModule.Dispose(){}

        void context_BeginRequest(object sender, EventArgs args)
        {
            HttpApplication app = sender as HttpApplication;

            if(IsEncodingAccepted("gzip"))
            {
                app.Response.Filter = new GZipStream(app.Response.Filter,CompressionMode.Compress);
                SetEncoding("gzip");
            }
            else if (IsEncodingAccepted("deflate"))
            {
                app.Response.Filter = new DeflateStream(app.Response.Filter,CompressionMode.Compress);
                SetEncoding("deflate");
            }
        }

        private bool IsEncodingAccepted(string encoding)
        {
            return HttpContext.Current.Request.Headers["Accept-encoding"] != null &&
                HttpContext.Current.Request.Headers["Accept-encoding"].Contains(encoding);
        }

        private void SetEncoding(string encoding)
        {
            HttpContext.Current.Response.AppendHeader("Content-encoding", encoding);
        }
    }
}
