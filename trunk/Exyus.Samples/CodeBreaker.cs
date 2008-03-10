using System;
using System.Web;
using System.IO;
using Exyus.Web;

namespace Exyus.Samples.CodeBreak
{
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
            string codekey = string.Empty;
            string codekey_id = "codebreaker-id";

            // get (or create id cookie)
            codekey = util.CookieRead(codekey_id);
            if (codekey == string.Empty)
            {
                codekey = util.UID();
            }
            util.CookieWrite(codekey_id, codekey, 30, "");

            // forward to URI based on id cookie
            this.Context.Response.Redirect(util.GetConfigSectionItem(Constants.cfg_exyusSettings, Constants.cfg_rootfolder) + "/codebreaker/{uid}/games/".Replace("{uid}",codekey));
        }
    }

    [UriPattern(@"/codebreaker/(?<uid>.*)/games/(?<id>.*)?\.xcs")]
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
            this.PostLocationUri = "/xcs/codebreaker/{uid}/games/{id}";
        }

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

        public override void Post()
        {
            // if no userid, go get one
            System.Collections.Hashtable arg_list = util.ParseUrlPattern(this.Context.Request.RawUrl, this.UrlPattern);
            if (!arg_list.Contains("uid"))
            {
                this.Context.Response.Redirect(util.GetConfigSectionItem(Constants.cfg_exyusSettings, Constants.cfg_rootfolder) + "/codebreaker/");
            }

            // get new codeset and date-time
            Answer ans = new Answer();
            ans.GenerateKey();
            arg_list.Add("place1",ans.place1);
            arg_list.Add("place2",ans.place2);
            arg_list.Add("place3",ans.place3);
            arg_list.Add("place4",ans.place4);
            arg_list.Add("date-created",DateTime.UtcNow);
            
            base.Post();
        }
    }

    [UriPattern(@"/codebreaker/(?<uid>.*)/games/(?<gid>.*)/move\.xcs")]
    [MediaTypes("text/html")]
    class MoveResource : XmlFileResource
    {
        Utility util = new Utility();

        public MoveResource()
        {
            this.AllowCreateOnPut = false;
            this.AllowDelete = false;
            this.RedirectOnPost = true;
            this.ContentType = "text/html";
            this.DocumentsFolder = "~/documents/codebreaker/move/";
            this.StorageFolder = "~/storage/codebreaker/{uid}/games/";
            this.UseValidationCaching = true;
            this.LocalMaxAge = 600;
            this.PostLocationUri = "/xcs/codebreaker/{uid}/games/{gid}";
        }

        public override void Get()
        {

            throw new HttpException(415, "Cannot GET this Resource");
        }

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
