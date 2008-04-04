using System;
using System.Web;
using System.IO;
using Exyus.Web;

namespace Exyus.Samples.CodeBreak
{
  // landing page for the game site
  [UriPattern(@"/codebreaker/\.xcs")]
  [MediaTypes("text/html")]
  class CodeBreaker : XmlSqlResource
  {
    Utility util = new Utility();

    public CodeBreaker()
    {
      this.AllowPut = false;
      this.AllowCreateOnPut = false;
      this.AllowDelete = false;
      this.AllowPost = false;
      this.ContentType = "text/html";
      this.ConnectionString = "exyus_samples";
      this.DocumentsFolder = "~/documents/codebreaker/";
      this.UseValidationCaching = true;
      this.LocalMaxAge = 600;
    }

    public override void Get()
    {
      string uid = util.CookieRead(CBData.CodeKeyID);
      if (uid == string.Empty)
      {
        uid = util.UID();
      }
      util.CookieWrite(CBData.CodeKeyID, uid, 30, "");
      this.ArgumentList.Add(CBData.CodeKeyID, uid);

      base.Get();
    }
  }

  // temp location in case current user has no cookie id or no user folder
  [UriPattern(@"/codebreaker/games/\.xcs")]
  [MediaTypes("text/html")]
  class Games : HTTPResource
  {
    Utility util = new Utility();

    public Games()
    {
      this.ContentType = "text/html";
      this.AllowDelete = false;
      this.AllowPost = false;
    }

    public override void Get()
    {
      string uid = util.CookieRead(CBData.CodeKeyID);
      if (uid == string.Empty)
      {
        this.Context.Response.Redirect(util.GetConfigSectionItem(Constants.cfg_exyusSettings, Constants.cfg_rootfolder) + "/codebreaker/");
      }
      else
      {
        this.Context.Response.Redirect(util.GetConfigSectionItem(Constants.cfg_exyusSettings, Constants.cfg_rootfolder) + string.Format("/codebreaker/{0}/games/", uid));
      }
    }
  }

  // actual game resource - note the <uid>
  [UriPattern(@"/codebreaker/(?<uid>.*)/games/(?<id>[0-9]*)?\.xcs")]
  [MediaTypes("text/html")]
  class GameResource : XmlSqlResource
  {
    Utility util = new Utility();

    public GameResource()
    {
      this.AllowPut = false;
      this.AllowCreateOnPut = false;
      this.AllowDelete = false;
      this.RedirectOnPost = true;
      this.ContentType = "text/html";
      this.ConnectionString = "exyus_samples";
      this.DocumentsFolder = "~/documents/codebreaker/games/";
      this.UseValidationCaching = true;
      this.LocalMaxAge = 600;
      this.PostLocationUri = "/codebreaker/{uid}/games/{id}";
      this.UpdateMediaTypes = new string[] { "application/x-www-form-urlencoded" };

      this.ImmediateCacheUriTemplates = new string[]
                {
                    "/codebreaker/.xcs",
                    "/codebreaker/{uid}/games/.xcs",
                    "/codebreaker/{uid}/games/{id}.xcs",
                };
    }

    // return either this user's games or the current game
    public override void Get()
    {
      // no cookie, go to start again
      string uid = util.CookieRead(CBData.CodeKeyID);
      if (uid.Length == 0)
      {
        this.Context.Response.Redirect(util.GetConfigSectionItem(Constants.cfg_exyusSettings, Constants.cfg_rootfolder) + "/codebreaker/");
      }

      // if cookie is not the same, go to the proper url
      if (uid != ArgumentList["uid"].ToString())
      {
        this.Context.Response.Redirect(util.GetConfigSectionItem(Constants.cfg_exyusSettings, Constants.cfg_rootfolder) + string.Format("/codebreaker/{0}/games/", uid));
      }

      // otheriwse, just keep going
      base.Get();
    }

    // create a new game
    public override void Post()
    {
      // no cookie or wrong cookie, throw error
      string uid = util.CookieRead(CBData.CodeKeyID);
      if (uid.Length == 0 || uid != ArgumentList["uid"].ToString())
      {
        throw new HttpException(403, "access denied");
      }

      // get new codeset and set max-attempts
      // add it to the local shared collection
      Answer ans = new Answer();
      ans.GenerateKey();
      ArgumentList.Add("place1", ans.place1);
      ArgumentList.Add("place2", ans.place2);
      ArgumentList.Add("place3", ans.place3);
      ArgumentList.Add("place4", ans.place4);
      ArgumentList.Add("max-attempts", 10);

      base.Post();
    }
  }

  // handle moves (plays) in the game - note the <gid>
  [UriPattern(@"/codebreaker/(?<uid>.*)/games/(?<gid>[0-9]*)/moves/(?<id>[0-9]*)?\.xcs")]
  [MediaTypes("text/html")]
  class MoveResource : XmlSqlResource
  {
    Utility util = new Utility();

    public MoveResource()
    {
      this.AllowCreateOnPut = false;
      this.AllowDelete = false;
      this.RedirectOnPost = true;
      this.ContentType = "text/html";
      this.DocumentsFolder = "~/documents/codebreaker/moves/";
      this.ConnectionString = "exyus_samples";
      this.UseValidationCaching = true;
      this.LocalMaxAge = 600;
      this.PostLocationUri = "/codebreaker/{uid}/games/{gid}";
      this.UpdateMediaTypes = new string[] { "application/x-www-form-urlencoded" };

      this.ImmediateCacheUriTemplates = new string[]
                {
                    "/codebreaker/.xcs",
                    "/codebreaker/{uid}/games/.xcs",
                    "/codebreaker/{uid}/games/{gid}.xcs",
                    "/codebreaker/{uid}/games/{gid}/moves/.xcs",
                    "/codebreaker/{uid}/games/{gid}/moves/{id}.xcs"
                };
    }

    // get list of moves for this game or show a single move
    public override void Get()
    {
      // no cookie, go to start again
      string uid = util.CookieRead(CBData.CodeKeyID);
      if (uid.Length == 0)
      {
        this.Context.Response.Redirect(util.GetConfigSectionItem(Constants.cfg_exyusSettings, Constants.cfg_rootfolder) + "/codebreaker/");
      }

      // if cookie is not the same, go to the proper url
      if (uid != ArgumentList["uid"].ToString())
      {
        this.Context.Response.Redirect(util.GetConfigSectionItem(Constants.cfg_exyusSettings, Constants.cfg_rootfolder) + string.Format("/codebreaker/{0}/games/", uid));
      }

      base.Get();
    }

    // create a new move for this game
    public override void Post()
    {
      // no cookie or wrong cookie, throw error
      string uid = util.CookieRead(CBData.CodeKeyID);
      if (uid.Length == 0 || uid != ArgumentList["uid"].ToString())
      {
        throw new HttpException(403, "access denied");
      }

      base.Post();
    }
  }

  // about page
  [UriPattern(@"/codebreaker/about/\.xcs")]
  [MediaTypes("text/html")]
  class CodeBreakerAbout : XmlPageResource
  {
    public CodeBreakerAbout()
    {
      this.ContentType = "text/html";
      this.LocalMaxAge = 600;
      this.TemplateXsl = "~/documents/codebreaker/about/about_{ftype}.xsl";
    }
  }
  // source code viewer
  [UriPattern(@"/codebreaker/source/\.xcs")]
  [MediaTypes("text/html")]
  class CodeBreakerSource : PlainTextViewer
  {
    public CodeBreakerSource()
    {
      this.MaxAge = 600;
      this.UseValidationCaching = true;

      this.Files.Add("codebreaker.cs", "/xcs/documents/codebreaker/source/codebreaker.cs");
      this.Files.Add("codebreaker.sql", "/xcs/documents/codebreaker/source/codebreaker.sql");
      this.Files.Add("codebreaker.css", "/xcs/files/codebreaker/codebreaker.css");
      this.Files.Add("get_request_html.xsl", "/xcs/documents/codebreaker/get_request_html.xsl");
      this.Files.Add("get_response_html.xsl", "/xcs/documents/codebreaker/get_response_html.xsl");
      this.Files.Add("content.xsl", "/xcs/documents/codebreaker/content.xsl");
      this.Files.Add("about/about_html.xsl", "/xcs/documents/codebreaker/about/about_html.xsl");
      this.Files.Add("games/get_response_html.xsl", "/xcs/documents/codebreaker/games/get_response_html.xsl");
      this.Files.Add("games/get_request_html.xsl", "/xcs/documents/codebreaker/games/get_request_html.xsl");
      this.Files.Add("games/post_request_form.xsl", "/xcs/documents/codebreaker/games/post_request_form.xsl");
      this.Files.Add("moves/get_response_html.xsl", "/xcs/documents/codebreaker/moves/get_response_html.xsl");
      this.Files.Add("moves/get_request_html.xsl", "/xcs/documents/codebreaker/moves/get_request_html.xsl");
      this.Files.Add("moves/post_request_form.xsl", "/xcs/documents/codebreaker/moves/post_request_form.xsl");
      this.Files.Add("moves/post_form.xsd", "/xcs/documents/codebreaker/moves/post_form.xsd");
    }
  }

  // shared data
  class CBData
  {
    public static string CodeKeyID = "codebreaker-id";
    public static string cacheBot = "exyus-cache-bot";
  }

  // generate a new answer set
  struct Answer
  {
    public string place1;
    public string place2;
    public string place3;
    public string place4;

    public void GenerateKey()
    {
      string options = "ABCDEF";
      int max = options.Length;

      Random rnd = new Random();

      place1 = options[rnd.Next(max)].ToString();
      place2 = options[rnd.Next(max)].ToString();
      place3 = options[rnd.Next(max)].ToString();
      place4 = options[rnd.Next(max)].ToString();

    }
  }
}
