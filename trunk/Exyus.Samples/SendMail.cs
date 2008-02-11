using System;
using System.Collections.Generic;
using System.Text;

using Exyus.Web;

namespace Exyus.Samples
{
    [UriPattern(@"/sendmail/\.xcs")]
    [MediaTypes("application/x-www-form-urlencoded", "text/xml")]
    class SendMail : SMTPResource
    {
        public SendMail()
        {
            this.AllowPost = true;
            this.ContentType = "text/xml";
            this.DocumentsFolder = "~/documents/sendmail/";
            this.PostLocationUri = "/sendmail/thankyou";
            this.RedirectOnPost = true;
            this.XHtmlNodes = new string[] {"//body"};
        }
    }

    [UriPattern(@"/sendmail/thankyou\.xcs")]
    [MediaTypes("text/html")]
    class SendMailThankyou : StaticResource
    {
        public SendMailThankyou()
        {
            this.Content = Helper.ReadFile("/xcs/content/sendmail/thankyou.html");
        }
    }
}
