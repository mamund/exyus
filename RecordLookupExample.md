## Record Look-up ##

Often, when you post content to a server, some of the data elements are actually references to other existing data elements. For example when posting a new "Product" resource, you might  include a "Category" element in the resource. This category should be a pointer to an existng "Category" resource somewhere else on the Web. So, when the "Product" resource is received by the server, the server should validate the existence of the "Category" resource. This is sometimes referred to as a 'record look-up' or a 'foreign-key validation.'

The code example below shows one way to do a record look-up with **Exyus**. In this case, any `PUT` of a resource will include a record look-up.

```
public override void Put()
{
    Utility util = new Utility();
    XmlDocument xmldoc = new XmlDocument();
    XmlNode node = null;
    string cid = string.Empty;

    // get category from the incoming body
    xmldoc.Load(this.Context.Request.InputStream);
    node = xmldoc.SelectSingleNode("//category");
    cid = (node != null ? node.InnerXml : string.Empty);
    if (cid == string.Empty)
        throw new HttpException(400, "category missing");

    try
    {
        // make sure the category already exists!
        HTTPClient client = new HTTPClient();
        client.Credentials = util.GetUserCredentials(this.Context.User);
        string url = string.Format("{0}://{1}{2}{3}",
          this.Context.Request.Url.Scheme,
          this.Context.Request.Url.DnsSafeHost,
          "/xcs/data/products/categories/",
          cid);
        client.Execute(url,"head");
    }
    catch (HttpException hex)
    {
        // not found!
        if (hex.GetHttpCode() == 404)
            throw new HttpException(400, "category [" + cid + "] not found");
        else
            throw new HttpException(hex.GetHttpCode(), hex.Message);
    }

    // go ahead and do the rest of the work
    base.Put();
}
```

Notice the use of the [HTTPClient](HTTPClient.md) to make a `HEAD` call to the server to see if the "Category" resource exists. While this example makes a call to the same server, you can do this same work to any valid HTTP server that understands the `HEAD` method.