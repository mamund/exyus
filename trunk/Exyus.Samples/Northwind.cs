using System;
using System.Collections.Generic;
using System.Text;

using Exyus.Web;

namespace Exyus.Samples
{
    [UriPattern(@"/northwind/\.xcs")]
    [MediaTypes("text/html")]
    class Northwind : StaticResource
    {
        public Northwind()
        {
            this.Content = @"<h1>Northwind</h1><body><ul><li><a href='products/'>Products</a></li><li><a href='categories/'>Categories</a></li></ul></body></html>";
        }
    }

    [UriPattern(@"/northwind/products/(?<id>[0-9]*)\.xcs")]
    [MediaTypes("text/html", "text/xml", "application/xml","application/json")]
    class Products : XmlSqlResource
    {
        public Products()
        {
            this.ContentType = "text/html";
            this.ConnectionString = "northwind_db";
            this.LocalMaxAge = 600;

            this.AllowPost = true;
            this.AllowDelete = true;
            this.DocumentsFolder = "~/documents/northwind/products/";
            this.RedirectOnPost = true;
            this.PostLocationUri = "/northwind/products/{id}";
            this.UpdateMediaTypes = new string[]
            {
                "application/x-www-form-urlencoded",
                "text/xml",
                "application/xml",
                "application/json"
            };

            // set cache invalidation rules
            this.ImmediateCacheUriTemplates = new string[]
            {
                "/northwind/products/.xcs",
                "/northwind/products/{id}.xcs"
            };
        }
    }

    [UriPattern(@"/northwind/categories/(?<id>[0-9]*)\.xcs")]
    [MediaTypes("text/html", "text/xml", "application/xml", "application/json")]
    class Categories : XmlSqlResource
    {
        public Categories()
        {
            this.ContentType = "text/html";
            this.ConnectionString = "northwind_db";
            this.LocalMaxAge = 600;

            this.AllowPost = true;
            this.AllowDelete = true;
            this.DocumentsFolder = "~/documents/northwind/categories/";
            this.RedirectOnPost = true;
            this.PostLocationUri = "/northwind/categories/{id}";
            this.UpdateMediaTypes = new string[]
            {
                "application/x-www-form-urlencoded",
                "text/xml",
                "application/xml",
                "application/json"
            };

            // set cache invalidation rules
            this.ImmediateCacheUriTemplates = new string[]
            {
                "/northwind/categories/.xcs",
                "/northwind/categories/{id}.xcs"
            };
        }
    }

}
