## Quick summary ##
Exyus allows you to define more than one [Content-Type](MediaTypes.md) for a Resource class. This allows you to provide multiple representations of the same server-side data. The number and type of content-types supported by Exyus is unlimited. This is all defined when you implement a Resource class.

## A simple example ##
For example, you can use the existing [StaticResource](StaticResource.md) class to define a resource that returns the current server time. Below is one that returns the results as plain text:
```
    [UriPattern(@"/samples/server-time/.xcs(.*)")]
    public class serverTime : StaticResource
    {
        public serverTime()
        {
            this.ContentType = "text/plain";
        }

        public override void Get()
        {
            this.Response = DateTime.UtcNow.ToString();
        }
    }
```

The above example can be expanded to support more than one content-type. This is done by adding a [MediaTypes](MediaTypes.md) attribute to the class definition. You can then use utility methods to determine the best fit content-type to return to the caller and respond appropriately.
```
    [UriPattern(@"/samples/server-time/.xcs(.*)")]
    [MediaTypes("text/html","text/xml","text/plain")]
    public class serverTime: StaticResource
    {
        string xml_out = "<root><date-time>{0:yyyy-MM-ddThh:mm:ss}</date-time></root>";
        string html_out = "<div><span class=\"date-time\">{0:yyyy-MM-ddThh:mm:ss}</span></div>";
        string plain_out = "{0:yyyy-MM-ddThh:mm:ss}";

        public serverTime()
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
```

Using the example above, clients that pass any of the supported content-types (via the Accept header) will receive the representation they requested. Clients that pass a content-type that is not in the [MediaTypes](MediaTypes.md) list will receive an HTTP 406 (Unsupported) status code.