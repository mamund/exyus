## Authentication ##
**Exyus** currently supports two type of authentication Basic and Digest. Authentication is built into the engine itself. You only need to select configuration settings to enable authentication support.

Once you enable authentication support, you need to update the {{{config/auth-urls.xml}} configuration file in order to identify the URLs that require authentication. Adding URLs to this file will cause the runtime to throw an HTTP Auth dialog box for whenever an un-auth'ed user requests that URL.

Below is an example of the `config/auth-urls.xml` file:
```
<urls>
   <!-- root -->
   <url path="/xcs/" auth="false" />
   <!-- samples -->
   <url path="/xcs/samples/" auth="false" />
   <!-- editable -->
   <url path="/xcs/editable/" auth="true" />
   <!-- editable -->
   <url path="/xcs/postable/" auth="false" />
</urls>
```

Once you define the URLs that require authentication, you also need to set up [Authorization](Authorization.md). You do this by updating the `config/auth-users.xml` file to contain users (and passwords) along with the URLs and their corresponding access rights.