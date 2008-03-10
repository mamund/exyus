using System;
using Exyus.Web;

namespace Exyus.Samples.StaticExamples
{
    // exmaple of 'literal page'
    [UriPattern(@"/samples/(?:\.xcs)(?:.*)?")]
    [MediaTypes("text/html")]
    public class samplesRoot : StaticResource
    {
        public samplesRoot()
        {
            this.Content =
@"<html>
<head>
<title>samples</title>
</head>
<body>
<h1>samples</h1>
<ul>
<li><a href='./helloworld'>./helloworld</a></li>
<li><a href='./virtual/?name=exyus'>./virtual/?name=exyus</a></li>
<li><a href='./virtual/hello?name=guest'>./virtual/hello?name=guest</a></li>
<li><a href='./time/'>./time/</a></li>
<li><a href='./time/?_accept=text/xml'>./time/?_accept=text/xml</a></li>
<li><a href='./time/?_accept=text/plain'>./time/?_accept=text/plain</a></li>
<li><a href='./page/'>./page/</a></li>
<li><a href='./page/testing-exyus'>./page/testing-exyus</a></li>
<li><a href='./testing'>./testing</a></li>
<li><a href='./svg/'>./svg/</a></li>
<li><a href='./../editable/'>./../editable/</a></li>
<li><a href='./../postable/'>./../postable/</a></li>
<li><a href='./remote/'>./remote/</a></li>
</ul>
</body>
</html>
";
        }
    }

    // use loadurl to 'wget' a page
    [UriPattern(@"/samples/remote/(?:\.xcs)(?:.*)?")]
    [MediaTypes("text/html")]
    public class remotePage : StaticResource
    {
        public remotePage()
        {
            this.Content = Helper.LoadFromUrl("http://www.amundsen.com/blog/");
        }
    }

    // echo back simple content
    [UriPattern(@"/samples/virtual/.xcs", @"/virtual/hello(?:\.xcs)(?:.*)?")]
    public class StartPage : StaticResource
    {
        public StartPage()
        {
            this.Content = "<h1>hello $name$</h1>";
        }
    }

    // simple handler for time
    [UriPattern(@"/samples/time/(?:\.xcs)(?:.*)?")]
    [MediaTypes("text/html","text/xml","text/plain")]
    public class timeResource : StaticResource
    {
        string xml_out = "<root><date-time>{0:yyyy-MM-ddThh:mm:ss}</date-time></root>";
        string html_out = "<div><span class=\"date-time\">{0:yyyy-MM-ddThh:mm:ss}</span></div>";
        string plain_out = "{0:yyyy-MM-ddThh:mm:ss}";

        public timeResource()
        {
            this.ContentType = "text/html";
        }

        public override void Get()
        {
            Utility util = new Utility();
            string out_text = string.Empty;
            util.SetMediaType(this);

            switch (this.ContentType)
            {
                case "text/xml":
                    out_text = string.Format(xml_out, DateTime.UtcNow);
                    break;
                case "text/html":
                    out_text = string.Format(html_out, DateTime.UtcNow);
                    break;
                case "text/plain":
                    out_text = string.Format(plain_out, DateTime.UtcNow);
                    break;
                default:
                    out_text = string.Empty;
                    break;
            }

            this.Response = out_text;
        }
    }

    // return static document from disk
    [UriPattern(@"/samples/page/([^.]*)?(?:\.xcs)(?:.*)?")]
    public class getPage : StaticResource
    {
        public getPage()
        {
            this.Content = Helper.ReadFile("/xcs/content/samples/getpage.html");
        }
    }

    // return document
    [UriPattern(@"/samples/testing(?:\.xcs)(?:.*)?")]
    public class TestingPage : StaticResource
    {
        public TestingPage()
        {
            this.Content = Helper.ReadFile("/xcs/content/samples/testing.html");
        }
    }
    
    // return static svg document
    [UriPattern(@"/samples/svg/(?:\.xcs)")]
    [MediaTypes("image/svg+xml")]
    public class svgHello : StaticResource
    {
        public svgHello()
        {
            this.ContentType = "image/svg+xml";
            this.Content = Helper.ReadFile("/xcs/content/samples/hello.svg");
        }
    }

    // the exyus (C#) version of "/helloworld"
    [UriPattern(@"/samples/helloworld\.xcs")]
    [MediaTypes("text/plain")]
    public class HelloWorldResource : StaticResource
    {
        public HelloWorldResource()
        {
            this.Content = "Hello World! Here is $_absolute-path$";
        }
    }
}




