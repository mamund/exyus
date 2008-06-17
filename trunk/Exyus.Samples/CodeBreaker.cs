using System;
using System.Web;
using System.IO;
using Exyus.Web;

namespace Exyus.Samples.CodeBreak
{
  // landing page for the game site
  [UriPattern(@"/codebreaker/\.xcs")]
  [MediaTypes("text/html","text/xml")]
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
      // create the codebreaker-id cookie if none exists
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
  [MediaTypes("text/html","text/xml")]
  class Games : HTTPResource
  {
    Utility util = new Utility();

    public Games()
    {
      this.ContentType = "text/html";
      this.AllowDelete = false;
      this.AllowPost = false;
    }

    // re-route based on cookie discovery
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
  [MediaTypes("text/html","text/xml")]
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
      this.UpdateMediaTypes = new string[] 
        { 
          "application/x-www-form-urlencoded",
          "text/xml"
        };

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
  [MediaTypes("text/html","text/xml")]
  class Moves : XmlSqlResource
  {
    Utility util = new Utility();

    public Moves()
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
      this.UpdateMediaTypes = new string[] 
        { 
          "application/x-www-form-urlencoded",
          "text/xml"
        };

      this.ImmediateCacheUriTemplates = new string[]
          {
              "/codebreaker/.xcs",
              "/codebreaker/{uid}/games/.xcs",
              "/codebreaker/{uid}/games/{gid}.xcs",
              "/codebreaker/{uid}/games/{gid}/moves/.xcs",
              "/codebreaker/{uid}/games/{gid}/moves/{id}.xcs",
              "/codebreaker/players/.xcs",
              "/codebreaker/players/{uid}.xcs"
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

  // handle display of players
  // noe4 that no cookie is needed here
  [UriPattern(@"/codebreaker/players/(?<id>[\w-]*)\.xcs")]
  [MediaTypes("text/html","text/xml")]
  class Players : XmlSqlResource
  {
    public Players()
    {
      this.AllowCreateOnPut = false;
      this.AllowDelete = false;
      this.AllowPut = false;
      this.AllowPost = false;
      this.ContentType = "text/html";
      this.DocumentsFolder = "~/documents/codebreaker/players/";
      this.ConnectionString = "exyus_samples";
      this.UseValidationCaching = true;
      this.LocalMaxAge = 600;

      this.ImmediateCacheUriTemplates = new string[]
      {
          "/codebreaker/players/.xcs",
          "/codebreaker/players/{id}.xcs"
      };
    }
  }

  // handle redirect for proper POSTing of player name
  [UriPattern(@"/codebreaker/name/.xcs")]
  [MediaTypes("text/html","text/xml")]
  class NameRoot : HTTPResource
  {
    Utility util = new Utility();

    public NameRoot()
    {
      this.ContentType = "text/html";
      this.AllowDelete = false;
      this.AllowPost = false;
    }

    // use as a redirect based on cookie
    public override void Get()
    {
      string uid = util.CookieRead(CBData.CodeKeyID);
      if (uid == string.Empty)
      {
        this.Context.Response.Redirect(util.GetConfigSectionItem(Constants.cfg_exyusSettings, Constants.cfg_rootfolder) + "/codebreaker/");
      }
      else
      {
        this.Context.Response.Redirect(util.GetConfigSectionItem(Constants.cfg_exyusSettings, Constants.cfg_rootfolder) + string.Format("/codebreaker/{0}/name/", uid));
      }
    }
  }

  // handle POSTing of name for a user
  // requires the cookie is set properly
  [UriPattern(@"/codebreaker/(?<uid>.*)/name/(?<id>[\w]*)\.xcs")]
  [MediaTypes("text/html","text/xml")]
  class Names : XmlSqlResource
  {
    Utility util = new Utility();

    public Names()
    {
      this.AllowCreateOnPut = false;
      this.AllowDelete = false;
      this.RedirectOnPost = true;
      this.ContentType = "text/html";
      this.DocumentsFolder = "~/documents/codebreaker/name/";
      this.ConnectionString = "exyus_samples";
      this.UseValidationCaching = true;
      this.LocalMaxAge = 600;
      this.PostLocationUri = "/codebreaker/players/{id}";
      this.UpdateMediaTypes = new string[] 
        { 
          "application/x-www-form-urlencoded",
          "text/xml"
        };

      this.ImmediateCacheUriTemplates = new string[]
      {
          "/codebreaker/{uid}/name/.xcs",
          "/codebreaker/{uid}/name/{id}.xcs",
          "/codebreaker/players/.xcs",
          "/codebreaker/players/{id}.xcs",
          "/codebreaker/{uid}/games/.xcs"
      };
    }

    // handle form if *no* id
    public override void Get()
    {
      // no cookie, go to start again
      string uid = util.CookieRead(CBData.CodeKeyID);
      string id = (ArgumentList["id"]!=null?ArgumentList["id"].ToString():string.Empty);

      // no cookie or existing doc id
      if (uid.Length == 0 || id.Length!=0)
      {
        this.Context.Response.Redirect(util.GetConfigSectionItem(Constants.cfg_exyusSettings, Constants.cfg_rootfolder) + string.Format("/codebreaker/players/{0}",id));
      }

      // if cookie is not the same, go to the proper url
      if (uid != ArgumentList["uid"].ToString())
      {
        this.Context.Response.Redirect(util.GetConfigSectionItem(Constants.cfg_exyusSettings, Constants.cfg_rootfolder) + string.Format("/codebreaker/{0}/names/", id));
      }

      base.Get();
    }

    // create a new name for this user
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
  [MediaTypes("text/html","text/xml")]
  class About : XmlPageResource
  {
    public About()
    {
      this.ContentType = "text/html";
      this.LocalMaxAge = 600;
      this.TemplateXsl = "~/documents/codebreaker/about/about_{ftype}.xsl";
    }
  }

  // feedback form page
  [UriPattern(@"/codebreaker/feedback/\.xcs")]
  [MediaTypes("text/html","text/xml")]
  class Feedback : XmlPageResource
  {
    public Feedback()
    {
      this.ContentType = "text/html";
      this.LocalMaxAge = 600;
      this.TemplateXsl = "~/documents/codebreaker/feedback/form_{ftype}.xsl";
    }
  }

  // feedback (sendmail) page
  [UriPattern(@"/codebreaker/feedback/post\.xcs")]
  [MediaTypes("text/html","text/xml")]
  class FeedbackPost : SMTPResource
  {
    public FeedbackPost()
    {
      this.AllowPost = true;
      this.AllowGet = true;
      this.ContentType = "text/xml";
      this.DocumentsFolder = "~/documents/codebreaker/feedback/";
      this.PostLocationUri = "/codebreaker/feedback/thankyou";
      this.RedirectOnPost = true;
      this.XHtmlNodes = new string[] { "//body" };
      this.UpdateMediaTypes = new string[]
      {
        "application/x-www-form-urlencoded",
        "text/xml"
      };
    }
  }

  // feedback thankyou page
  [UriPattern(@"/codebreaker/feedback/thankyou\.xcs")]
  [MediaTypes("text/html","text/xml")]
  class FeedbackThankyou :  XmlPageResource
  {
    public FeedbackThankyou()
    {
      this.ContentType = "text/html";
      this.LocalMaxAge = 600;
      this.TemplateXsl = "~/documents/codebreaker/feedback/thankyou_{ftype}.xsl";
    }
  }

  // deliver css file
  [UriPattern(@"/codebreaker/css/main\.xcs")]
  [MediaTypes("text/css")]
  class CSSMain : StaticResource
  {
    public CSSMain()
    {
      this.ContentType = "text/css";
      this.Content = Helper.ReadFile("~/documents/codebreaker/css/main.css");
    }
  }

  // source code viewer
  [UriPattern(@"/codebreaker/source/\.xcs")]
  [MediaTypes("text/html")]
  class Source : PlainTextViewer
  {
    public Source()
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
