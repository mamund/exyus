using System;
using System.Web;
using System.Xml;
using System.Collections.Specialized;
using System.Collections;
using System.Net;
using System.IO;
using System.Text.RegularExpressions;
using System.Net.Mail;

using Exyus.Xml;
using Exyus.Security;
using Exyus.Caching;

namespace Exyus.Web
{
  [MediaTypes("text/xml")]
  public class SMTPResource : HTTPResource
  {
    // public properties        
    public string FileExtension = ".xml";
    public string PostLocationUri = string.Empty;
    public string DocumentsFolder = string.Empty;
    public string[] XHtmlNodes = null;
    public bool RedirectOnPost = false;
    public string SMTPHost = string.Empty;

    // internal vars
    private Utility util = new Utility();
    private string s_ext = string.Empty;
    Cache ch = new Cache();

    public SMTPResource()
    {
      if (this.ContentType == null || this.ContentType == string.Empty)
        this.ContentType = Constants.cType_Html;

      // set system extension
      s_ext = util.GetConfigSectionItem(Constants.cfg_exyusSettings, Constants.cfg_fileExtension, Constants.msc_sys_file_ext);
    }

    public override void Post()
    {
      XmlDocument xmlout = new XmlDocument();
      XmlDocument xmlin = new XmlDocument();
      XmlDocument xmlargs = new XmlDocument();
      string stor_folder = string.Empty;
      string id = string.Empty;
      string xsl_file = string.Empty;
      string xsd_file = string.Empty;
      string original_contentType = this.ContentType;
      string out_text = string.Empty;

      string XslArgs = this.Context.Server.MapPath(this.DocumentsFolder + "args.xsl");
      string XslPostArgs = this.Context.Server.MapPath(this.DocumentsFolder + "post_args.xsl");
      string XsdFile = this.Context.Server.MapPath(this.DocumentsFolder + "post.xsd");
      string XsdFileMtype = this.Context.Server.MapPath(this.DocumentsFolder + (CurrentMediaType == string.Empty ? "post.xsd" : string.Format("post_{0}.xsd", CurrentFileType)));
      string XslPostRequest = this.Context.Server.MapPath(this.DocumentsFolder + "post_request.xsl");
      string XslPostRequestMtype = this.Context.Server.MapPath(this.DocumentsFolder + (CurrentMediaType == string.Empty ? "post_request.xsl" : string.Format("post_request_{0}.xsl", CurrentFileType)));

      try
      {
        // validate args
        xsl_file = (File.Exists(XslPostArgs) ? XslPostArgs : XslArgs);
        if (File.Exists(xsl_file))
        {
          xmlargs.LoadXml("<root />");
          XslTransformer xslt = new XslTransformer();
          id = xslt.ExecuteText(xmlargs, xsl_file, ArgumentList);
        }
        // transform *must not* return doc id!
        if (id != string.Empty)
          throw new HttpException(400, "Cannot POST using resource id");

        // get the xmldoc from the entity
        this.Context.Request.InputStream.Position = 0;
        switch (CurrentMediaType.ToLower())
        {
          case Constants.cType_FormUrlEncoded:
            xmlin = util.ProcessFormVars(this.Context.Request.Form);
            break;
          case Constants.cType_Json:
            xmlin = util.ProcessJSON(this.Context.Request.InputStream);
            break;
          default:
            xmlin.Load(this.Context.Request.InputStream);
            break;
        }

        // validate the doc
        xsd_file = (File.Exists(XsdFileMtype) ? XsdFileMtype : XsdFile);
        if (File.Exists(xsd_file))
        {
          SchemaValidator sv = new SchemaValidator();
          string sch_error = sv.Execute(xmlin, xsd_file);
          if (sch_error != string.Empty)
            throw new HttpException(422, sch_error);
        }

        // validate html
        util.ValidateXHtmlNodes(ref xmlin, this.XHtmlNodes);

        // transform xmldoc into final form (if needed)
        xsl_file = (File.Exists(XslPostRequestMtype) ? XslPostRequestMtype : XslPostRequest);
        if (File.Exists(xsl_file))
        {
          XslTransformer xslt = new XslTransformer();
          out_text = xslt.ExecuteText(xmlin, xsl_file, ArgumentList);
          xmlout.LoadXml(out_text);
        }
        else
          xmlout = xmlin;

        // now build up the email and send it
        MailAddress maFrom = null;
        maFrom = GetMailAddress(xmlout.SelectSingleNode("//from/address"));
        MailAddress maReplyTo = null;
        maReplyTo = GetMailAddress(xmlout.SelectSingleNode("//replyto/address"));

        XmlNodeList xmlTo = xmlout.SelectNodes("//to/address");
        MailAddressCollection maTo = new MailAddressCollection();
        for (int i = 0; i < xmlTo.Count; i++)
        {
          MailAddress ma = GetMailAddress(xmlTo[i]);
          if (ma != null)
          {
            maTo.Add(ma);
          }
        }

        XmlNodeList xmlCC = xmlout.SelectNodes("//cc/address");
        MailAddressCollection maCC = new MailAddressCollection();
        for (int i = 0; i < xmlCC.Count; i++)
        {
          MailAddress ma = GetMailAddress(xmlCC[i]);
          if (ma != null)
          {
            maCC.Add(ma);
          }
        }

        XmlNodeList xmlBCC = xmlout.SelectNodes("//bcc/address");
        MailAddressCollection maBCC = new MailAddressCollection();
        for (int i = 0; i < xmlBCC.Count; i++)
        {
          MailAddress ma = GetMailAddress(xmlBCC[i]);
          if (ma != null)
          {
            maBCC.Add(ma);
          }
        }
        string subject = xmlout.SelectSingleNode("//subject").InnerText.Trim();
        string body = xmlout.SelectSingleNode("//body").InnerXml.Trim();

        MailMessage mm = new MailMessage();
        mm.Body = body;
        mm.From = maFrom;
        mm.Subject = subject;
        if (maReplyTo != null)
        {
          mm.ReplyTo = maReplyTo;
        }
        for (int i = 0; i < maBCC.Count; i++)
        {
          mm.Bcc.Add(maBCC[i]);
        }
        for (int i = 0; i < maCC.Count; i++)
        {
          mm.CC.Add(maCC[i]);
        }
        if (maTo.Count > 0)
          for (int i = 0; i < maTo.Count; i++)
          {
            mm.To.Add(maTo[i]);
          }

        SmtpClient smtp = new SmtpClient();
        if (this.SMTPHost != string.Empty)
          smtp.Host = this.SMTPHost;
        smtp.Send(mm);

        // redirect to created item
        this.StatusCode = (this.RedirectOnPost ? HttpStatusCode.Redirect : HttpStatusCode.Created);
        this.Location = util.GetConfigSectionItem(Constants.cfg_exyusSettings, Constants.cfg_rootfolder) + util.ReplaceArgs(this.PostLocationUri.Replace("{id}", id), ArgumentList);


        xmlout = null;
      }
      catch (HttpException hex)
      {
        this.StatusCode = (HttpStatusCode)hex.GetHttpCode();
        this.StatusDescription = hex.Message;
        out_text = util.RenderError("http error", hex.Message, CurrentMediaType);
      }
      catch (Exception ex)
      {
        this.StatusCode = HttpStatusCode.InternalServerError;
        this.StatusDescription = ex.Message;
        out_text = util.RenderError("unknown error", ex.Message, CurrentMediaType);
      }

      // if we were using form-posting, reset to preferred content type (text/html, most likely)
      if (this.ContentType == Constants.cType_FormUrlEncoded)
      {
        this.ContentType = (original_contentType == Constants.cType_FormUrlEncoded ? Constants.cType_Html : original_contentType);
      }

      this.Response = out_text;

      xmlin = null;
      xmlout = null;
    }

    private MailAddress GetMailAddress(XmlNode node)
    {
      XmlNode xmlEmail = null;
      XmlNode xmlName = null;
      string email = string.Empty;
      string name = string.Empty;
      MailAddress ma = null;

      if (node != null)
      {
        xmlEmail = node.SelectSingleNode("email");
        if (xmlEmail != null)
        {
          email = xmlEmail.InnerText.Trim();
        }
        xmlName = node.SelectSingleNode("name");
        if (xmlName != null)
        {
          name = xmlName.InnerText.Trim();
        }
        else
        {
          name = email;
        }

        ma = new MailAddress(email, name);
      }

      return ma;
    }
  }
}
