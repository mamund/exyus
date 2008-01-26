using System;
using System.Web;
using Exyus.Web;

// 2008-01-25 (mca)
namespace Exyus.Samples
{
    // echo source code files to browser in plain text
    [UriPattern(@"/tasklist/source/(?:\.xcs)(?:.*)?")]
    [MediaTypes("text/plain")]
    class TaskListSource : PlainTextViewer
    {
        public TaskListSource()
        {
            this.MaxAge = 600;
            this.UseValidationCaching = true;

            this.Files.Add("tasklist.cs", "/xcs/documents/tasklist/source/tasklist.cs");
            this.Files.Add("tasklist.css", "/xcs/files/samples/tasklist.css");
            this.Files.Add("tasklist.js", "/xcs/files/samples/tasklist.js");
            this.Files.Add("args.xsl", "/xcs/documents/tasklist/args.xsl");
            this.Files.Add("get_response_html.xsl", "/xcs/documents/tasklist/get_response_html.xsl");
            this.Files.Add("get_response_xml.xsl", "/xcs/documents/tasklist/get_response_xml.xsl");
            this.Files.Add("post_request_form.xsl", "/xcs/documents/tasklist/post_request_form.xsl");
            this.Files.Add("post_form.xsd", "/xcs/documents/tasklist/post_form.xsd");
            this.Files.Add("post_xml.xsd", "/xcs/documents/tasklist/post_xml.xsd");
            this.Files.Add("put_xml.xsd", "/xcs/documents/tasklist/put_xml.xsd");
        }

    }
}
