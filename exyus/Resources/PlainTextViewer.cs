using System;
using System.Web;
using Exyus.Web;

namespace Exyus.Web
{
    [MediaTypes("text/plain","text/html")]
    public class PlainTextViewer : StaticResource
    {
        Utility util = new Utility();
        Caching.Cache ch = new Exyus.Caching.Cache();
        private string[] mediaTypes = null;
        public System.Collections.SortedList Files = new System.Collections.SortedList();
        public string Title = "Source Documents";
        public bool ShowList = true;
        public string CSSUrl = string.Empty;
        public string CSSContent = string.Empty;
        public string HTMLContent = string.Empty;

        public PlainTextViewer()
        {
            this.ContentType = "text/plain";
            mediaTypes = ((MediaTypes)this.GetType().GetCustomAttributes(typeof(MediaTypes), false)[0]).Types;
        }

        public override void Get()
        {
            // validate media type (may throw 416 error)
            util.SetMediaType(this, mediaTypes);

            // handle args
            string f = (this.Context.Request["f"] != null ? this.Context.Request["f"] : string.Empty);

            //if (f == string.Empty)
            //    throw new HttpException(400, "Missing argument [f]");

            // check cache first
            if (ch.CachedResourceIsValid((HTTPResource)this))
            {
                return;
            }


            // make sure it's a valid item
            if (f!=string.Empty && !Files.ContainsKey(f))
            {
                throw new HttpException(400, "Invalid argument [f]");
            }

            // do the work
            string results = string.Empty;
            if (f != string.Empty)
            {
                results = Helper.ReadFile((string)Files[f]);
            }
            else
            {
                if (this.ShowList==false)
                {
                    throw new HttpException(400, "Missing argument [f]");
                }
                else
                {
                    // generate link list
                    System.Text.StringBuilder sb = new System.Text.StringBuilder();
                    sb.Append("<html><head>");

                    // title
                    sb.AppendFormat("<title>{0}</title>", this.Title);

                    // css link
                    if (this.CSSUrl != string.Empty)
                    {
                        sb.AppendFormat("<link href=\"{0}\" type=\"text/css\" rel=\"stylesheet\" />", this.CSSUrl);
                    }
                    // css indoc
                    if (this.CSSContent != string.Empty)
                    {
                        sb.AppendFormat("<style type=\"text/css\">{0}</style>", this.CSSContent);
                    }

                    sb.Append("</head><body>");

                    // title element
                    if(this.Title!=string.Empty)
                    {
                        sb.AppendFormat("<h1>{0}</h1>", this.Title);
                    }
                    // custom content
                    if (this.HTMLContent != string.Empty)
                    {
                        sb.AppendFormat("<div id=\"content\">{0}</div>", this.HTMLContent);
                    }

                    // doc list
                    sb.Append("<ol>");
                    foreach (Object key in Files.Keys)
                    {
                        sb.AppendFormat("<li><a href=\"?f={0}\" title=\"{1}\">{1}</a></li>",
                            key, key, this.Files[key]);
                    }
                    sb.Append("</ol></body></html>");

                    results = sb.ToString();
                    this.ContentType = "text/html";
                }
            }
            
            // cache and return

            ch.CacheResource((HTTPResource)this, results);
            this.Response = results;
        }
    }
}
