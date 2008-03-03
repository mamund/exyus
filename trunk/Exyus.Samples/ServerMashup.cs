/*
 * 2008-02-09 (mca)
 * ServerMashup source
 */
 
using System;
using System.Collections;

using System.IO;
using System.Net;
using System.Web.Caching;

using Exyus.Web;


namespace Exyus.Samples
{
    // call up the XML and XSL set to do mashup
    [UriPattern(@"/server-mashup/\.xcs")]
    [MediaTypes("text/html")]
    class ServerMashUp : XmlPageResource
    {
        public ServerMashUp()
        {
            this.AllowCreateOnPut = false;
            this.AllowDelete = false;
            this.AllowPost = false;
            this.ContentType = "text/html";
            this.LocalMaxAge = 60;
            this.MaxAge = 60;
            this.TemplateXml = "/server-mashup/index.xml";
            this.TemplateXsl = "/server-mashup/index.xsl";
            this.UseValidationCaching = true;
        }
    }

    // echo source code files to browser in plain text
    [UriPattern(@"/server-mashup/source/\.xcs")]
    [MediaTypes("text/plain")]
    class ServerMashupSource : PlainTextViewer
    {
        public ServerMashupSource()
        {
            this.MaxAge = 600;
            this.UseValidationCaching = true;

            this.Files.Add("servermashup.cs", "/xcs/files/server-mashup/source/servermashup.cs");
            this.Files.Add("index.xml", "/xcs/templates/server-mashup/index.xml");
            this.Files.Add("index.xsl", "/xcs/templates/server-mashup/index.xsl");
            this.Files.Add("notes.xml", "/xcs/templates/server-mashup/notes.xml");
            this.Files.Add("server-mashup.css", "/xcs/files/server-mashup/server-mashup.css");
        }
    }

}
