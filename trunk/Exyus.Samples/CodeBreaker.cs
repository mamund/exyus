using System;
using System.Web;
using System.IO;
using Exyus.Web;

namespace Exyus.Samples.CodeBreak
{
    // landing page for the game site
    [UriPattern(@"/codebreaker/\.xcs")]
    [MediaTypes("text/html")]
    class CodeBreaker : XmlFileResource
    {
        Utility util = new Utility();

        public CodeBreaker()
        {
            this.ContentType = "text/html";
            this.AllowDelete = false;
            this.AllowPost = false;
            this.DocumentsFolder = "~/documents/codebreaker/";
            this.StorageFolder = "~/storage/codebreaker/";
        }

        public override void Get()
        {
            string codekey = string.Empty;
            string codekey_id = "codebreaker-id";

            // get or create cookie)
            codekey = util.CookieRead(codekey_id);
            if (codekey == string.Empty)
            {
                codekey = util.UID();
            }
            util.CookieWrite(codekey_id, codekey, 30, "");

            base.Get();
        }
    }

    // temp location in case current user has no cookie id or no games folder
    [UriPattern(@"/codebreaker/games/\.xcs",@"/codebreaker/(?<uid>[^/]*)/\.xcs")]
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
            // forward to entry URI
            this.Context.Response.Redirect(util.GetConfigSectionItem(Constants.cfg_exyusSettings, Constants.cfg_rootfolder) + "/codebreaker/");
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
                    "/codebreaker/{uid}/games/.xcs",
                    "/codebreaker/{uid}/games/{id}.xcs",
                };
        }

        // return either this user's games or the current game
        public override void Get()
        {

            // if no userid, go get one
            System.Collections.Hashtable arg_list = util.ParseUrlPattern(this.Context.Request.RawUrl, this.UrlPattern);
            if (!arg_list.Contains("uid"))
            {
                this.Context.Response.Redirect(util.GetConfigSectionItem(Constants.cfg_exyusSettings, Constants.cfg_rootfolder) + "/codebreaker/");
            }
            base.Get();
        }

        // create a new game
        public override void Post()
        {
            // if no userid, go get one
            System.Collections.Hashtable arg_list = util.ParseUrlPattern(this.Context.Request.RawUrl, this.UrlPattern);
            if (!arg_list.Contains("uid"))
            {
                this.Context.Response.Redirect(util.GetConfigSectionItem(Constants.cfg_exyusSettings, Constants.cfg_rootfolder) + "/codebreaker/");
            }

            // get new codeset and set max-attempts
            // add it to the local shared collection
            Answer ans = new Answer();
            ans.GenerateKey();
            shared_args.Add("place1",ans.place1);
            shared_args.Add("place2", ans.place2);
            shared_args.Add("place3", ans.place3);
            shared_args.Add("place4", ans.place4);
            shared_args.Add("max-attempts", 10);
            
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
            this.UpdateMediaTypes = new string[] 
                    { "application/x-www-form-urlencoded" };

            this.ImmediateCacheUriTemplates = new string[]
                {
                    "/codebreaker/{uid}/games/.xcs",
                    "/codebreaker/{uid}/games/{gid}.xcs",
                    "/codebreaker/{uid}/games/{gid}/moves/.xcs",
                    "/codebreaker/{uid}/games/{gid}/moves/{id}.xcs"
                };
        }

        // get list of moves for this game or show a single move
        public override void Get()
        {
            
            // if no userid, go get one
            System.Collections.Hashtable arg_list = util.ParseUrlPattern(this.Context.Request.RawUrl, this.UrlPattern);
            if (!arg_list.Contains("uid"))
            {
                this.Context.Response.Redirect(util.GetConfigSectionItem(Constants.cfg_exyusSettings, Constants.cfg_rootfolder) + "/codebreaker/");
            }

            base.Get();
        }

        // create a new move for this game
        public override void Post()
        {
            // if no userid, go get one
            System.Collections.Hashtable arg_list = util.ParseUrlPattern(this.Context.Request.RawUrl, this.UrlPattern);
            if (!arg_list.Contains("uid"))
            {
                this.Context.Response.Redirect(util.GetConfigSectionItem(Constants.cfg_exyusSettings, Constants.cfg_rootfolder) + "/codebreaker/");
            }
            base.Post();
        }
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
