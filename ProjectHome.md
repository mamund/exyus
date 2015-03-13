# Exyus #
The goal is to create a light-weight highly-scalable HTTP server framework that is fully REST-compliant. The code will be built using C# (on Win32 to start) and have optional dependencies on SQL-based database (MS SQL to start).

## Version 0.06 Posted ##
(2008-03-16)
This version has the database support (via [XmlSqlResource](http://code.google.com/p/exyus/source/browse/trunk/exyus/Resources/XmlSqlResource.cs)) built-in. Now you can use MS-SQL Server (2005+) as a data store. Check out the [User Group Data Example](http://exyus.com/xcs/ugdata/) to see it in action.

## Latest News ##
Check out the latest demos at the live site:
  * [Codebreaker Game](http://exyus.com/xcs/codebreaker/)
  * [User Group Data Example](http://exyus.com/xcs/ugdata/)
  * [Server-side Mashup](http://exyus.com/xcs/server-mashup/)
  * [ZipCheck](http://exyus.com/xcs/zipcheck/client)
  * [TaskList](http://exyus.com/xcs/tasklist/)

Also check out the latest tutorials:
  * [UGData Tutorial](http://exyus.com/articles/ugdata/)
  * [Server-Side Mashup Tutorial](http://exyus.com/articles/server-mashup/)
  * [TaskList Desktop Client](http://exyus.com/articles/tasklist-client)
  * [TaskList Tutorial](http://exyus.com/articles/tasklist/)

## Features ##
The current version of **Exyus** has the following features:
  * [Resource-oriented coding](HTTPResource.md) - you create inbound endpoints by creating resource classes
  * Built-in support for standard HTTP Methods (GET,HEAD,PUT,POST,DELETE,OPTION as resource methods)
  * [Url-Mapping](UriPattern.md) - Resource classes can support multiple Uri patterns (via regular expressions)
  * [Multiple Representations](Multiple_Representations.md) - The same resource can support any number of Content Types (XML, JSON, Atom, HTML, etc.)
  * [Caching](Caching.md) Support - Mark your resource to support Validation and/or Expiration caching
  * [Authentication](Authentication.md) - Supports Digest and Basic Auth
  * [Authorization](Authorization.md) - Map Urls and HTTP Methods for user access
  * [Compression](Compression.md) - Automatically supports gzip/deflate per client headers.
  * Built-in [HTTPClient](HTTPClient.md) - Perform direct HTTP requests against other HTTP servers.

## Quick Example ##
Using the **Exyus** engine means you can define a read/write resource that allows live editing (tested w/ [Amaya](http://www.w3.org/Amaya/) browser) with just the following C# code:
```
using System;
using Exyus.Web;

namespace Exyus.Editable
{
    // full read/write via PUT w/ ETags
    [UriPattern(@"/editable/(?<docid>[^/?]*)?(?:\.xcs)(?:[?])?(?:.*)?")]
    [MediaTypes("text/html","application/json","application/atom+xml")]
    public class editPages : XmlFileResource
    {
        public editPages()
        {
            this.ContentType = "text/html";
            this.UpdateMediaTypes = new string[] { "text/html" };
            this.AllowPost = false;
            this.AllowCreateOnPut = true;
            this.DocumentsFolder = "~/documents/editable/";
            this.StorageFolder = "~/storage/editable/";
            this.XHtmlNodes = new string[] { "//body" };
            this.LocalMaxAge = 600;
            this.ImmediateCacheUriTemplates = new string[]
                {
                    "/editable/.xcs",
                    "/editable/{docid}.xcs"
                };
        }
    }

}
```