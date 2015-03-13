## Summary ##
The **Exyus** engine is designed to make it relatively easy to build scalable read/write web applications while staying close to the prinicples of [REST](http://en.wikipedia.org/wiki/Representational_State_Transfer). The core of the **Exyus** engine is in two classes: [HTTPResource](HTTPResource.md) and [HTTPClient](HTTPClient.md). The [HTTPResource](HTTPResource.md) class is designed to handle incoming HTTP requests. The [HTTPClient](HTTPClient.md) class is designed to make outgoing HTTP requests.

Programming with the **Exyus** engine requires the following:
  * Define a Resource using [HTTPResource](HTTPResource.md) or one of it's sub-classes (see below).
  * Define one or more [UriPattern](UriPattern.md) elements that map to that resource.
  * Define one or more HTTP Methods that are allowed for that resource.
  * Define one or more [MediaTypes](MediaTypes.md) that are supported by that resource.
  * Define one or more dependent [CacheUri](CacheUri.md) elements that will be cleared when the resource is successfully updated.

In addition, many of the built-in Resource Classes require the definition of one or more [Transform Documents](Transform_Documents.md) to handle validation and transformation of passed parameters, incoming requests, and outgoing responses.

In the initial release, **Exyus** provides the following Resource Classes (all subclasses of the [HTTPResource](HTTPResource.md) class:
  * [StaticResource](StaticResource.md) - Supports literal string and file-based read-only content.
  * [XmlFileResource](XmlFileResource.md) - Supports full read/write access to any file that can (ultimately) be transformed into a valid XML document (more on this later).
  * [SqlXmlResource](SqlXmlResource.md) - Supports access to MS-SQL database that returns valid XML documents.
  * [XmlPageResource](XmlPageResource.md) - Supports producing typical XHTML pages either whole or via the [XmlTemplateResource](XmlTemplateResource.md).
  * [XmlTemplateResource](XmlTemplateResource.md) - Supports partial-page generation. Can be used to build [XmlPageResource](XmlPageResource.md) output.