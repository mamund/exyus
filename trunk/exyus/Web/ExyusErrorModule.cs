using System;
using System.Web;

namespace Exyus.Web
{
    public class ErrorModule : IHttpModule
    {
        public String ModuleName
        {
            get { return "ErrorModule"; }
        }

        public void Init(HttpApplication application)
        {
            application.Error += (new EventHandler(this.Application_Error));
        }

        private void Application_Error(Object source, EventArgs e)
        {
            HttpApplication app = (HttpApplication)source;
            HttpResponse response = app.Response;
            Utility util = new Utility();

            try
            {
                HttpException hex = (HttpException)app.Context.Error;
                response.StatusCode = hex.GetHttpCode();
                response.StatusDescription = util.FixUpHttpError(hex.GetHttpCode(), hex.Message);
                response.Write(
                    string.Format(
                        Constants.fmt_error_module, 
                        hex.GetHttpCode(), 
                        util.FixUpHttpError(hex.GetHttpCode(),hex.Message))
                    );
            }
            catch (Exception ex)
            {
                Exception nex = (Exception)app.Context.Error;
                response.StatusCode = 500;
                response.StatusDescription = nex.Message;
                response.Write(
                    string.Format(
                        Constants.fmt_error_module, 
                        500,
                        nex.Message)
                    );
            }

            response.Flush();
            response.End();
            response.Close();
        }

        public void Dispose() { }
    }
}