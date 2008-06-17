using System;
using System.Collections.Generic;
using System.Text;

using System.IO;
using System.Xml;
using System.Xml.XPath;

using Exyus.Web;

namespace codebreaker_cmd
{
  class Program
  {
    static void Main(string[] args)
    {
      UserInterface ui = new UserInterface();
      CodeBreakerService cbs = new CodeBreakerService();
      string id = string.Empty;
      int c = 0;
      string[] cmd = new string[]
      {
        "S)ummary, P)layers, G)ame list, Q)uit :",
        "I)ndividual, B)ack :",
        "N)ew game, R)eview game, B)ack :",
        "M)ove, B)ack :"
      };

      Console.WriteLine("Codebreaker Command");
      Console.WriteLine("ver 1.0 - 2008-04-15 (mca)");
      Console.WriteLine("");

      // get user code for this session
      id = cbs.Init();

      // loop to get user inputs
      getkey:
      Console.Write(cmd[c]);
      ConsoleKeyInfo key = Console.ReadKey();
      
      switch (key.KeyChar)
      {
        case 'b':
        case 'B':
          c--;
          Console.WriteLine("");
          break;
        case 'p':
        case 'P':
          ui.Players(id);
          c = 1;
          break;
        case 'i':
        case 'I':
          ui.PlayerInfo(id);
          break;
        case 'g':
        case 'G':
          ui.GameList(id);
          break;
        case 's':
        case 'S':
          ui.Summary(id);
          break;
        case 'q':
        case 'Q':
          return;
        default:
          Console.WriteLine("\n***Invalid command***");
          break;
      }
      goto getkey;
    }

  }

  struct CBLinks
  {
    public string Games;
    public string NewGame;
    public string Players;

    public string GetLink(string key)
    {
      switch (key.ToLower())
      {
        case "games":
          return this.Games;
        case "newgame":
          return this.NewGame;
        case "players":
          return this.Players;
        default:
          return string.Empty;
      }
    }

    public void SetLink(string key, string value)
    {
      switch (key.ToLower())
      {
        case "games":
          this.Games=value;
          break;
        case "newgame":
          this.NewGame=value;
          break;
        case "players":
          this.Players=value;
          break;
      }
    }
  }

  class CBData
  {
    public static string GameKey = "codebreaker-id";
    public static string GameUrl = "http://localhost/xcs/codebreaker/";
  }

  class UserInterface
  {
    CodeBreakerService cbs = new CodeBreakerService();

    public void PlayerInfo(string id)
    {
      XmlDocument doc = new XmlDocument();
      string pid = string.Empty;

      Console.Write("\nPlayer ID:");
      pid = Console.ReadLine();

      doc.LoadXml(cbs.GetPlayer(id,pid));

      string name = doc.SelectSingleNode("//name").InnerText;
      string lastplayed = doc.SelectSingleNode("//last-played").InnerText;
      string winpct = doc.SelectSingleNode("//win-pct").InnerText;
      string highscore = doc.SelectSingleNode("//high-score").InnerText;
      string inprogress = doc.SelectSingleNode("//in-progress").InnerText;
      string wins = doc.SelectSingleNode("//wins").InnerText;
      string losses = doc.SelectSingleNode("//losses").InnerText;
      string games = doc.SelectSingleNode("//total-games").InnerText;

      Console.WriteLine("name: {0}\nlast-played: {1}\nwin-pct: {2}\nhigh-score: {3}\nin-progress: {4}\nwins: {5}\nlosses: {6}\ntotal-games: {7}",
        name, lastplayed, winpct, highscore, inprogress, wins, losses, games);
    }

    public void Summary(string id)
    {
      XmlDocument doc = new XmlDocument();
      doc.LoadXml(cbs.GetSummary(id));

      string players = doc.SelectSingleNode("//players").InnerText;
      string games = doc.SelectSingleNode("//games").InnerText;
      string wins = doc.SelectSingleNode("//wins").InnerText;
      string losses = doc.SelectSingleNode("//losses").InnerText;
      string inprogress = doc.SelectSingleNode("//in-progress").InnerText;
      string highscore = doc.SelectSingleNode("//high-score").InnerText;
      string percentage = doc.SelectSingleNode("//win-pct").InnerText;

      Console.WriteLine("\nplayers: {0}\ngames: {1}\nwins: {2}\nlosses: {3}\nin-progress: {4}\nhigh-score: {5}\npercentage: {6}\n",
        players, games, wins, losses, inprogress, highscore, percentage);
    }

    public void GameList(string id)
    {
      XmlDocument doc = new XmlDocument();
      doc.LoadXml(cbs.GetGameList(id));

      XmlNodeList games = doc.SelectNodes("//past-games");
      for (int i = 0; i < games.Count; i++)
      {

      }
    }

    public void Players(string id)
    {
      string name = string.Empty;
      string pid = string.Empty;
      XmlDocument doc = new XmlDocument();

      Console.WriteLine("");

      doc.LoadXml(cbs.GetPlayers(id));

      XmlNodeList players = doc.SelectNodes("//player");
      for (int i = 0; i < players.Count; i++)
      {
        name = players[i].SelectSingleNode("name").InnerText;
        pid = players[i].SelectSingleNode("link/@href").InnerText.Replace("./", "");

        Console.WriteLine("{0} - {1} ({2})", i, name, pid);
      }
    }
  }

  class CodeBreakerService
  {
    public string GetGameCode()
    {
      string rtn = string.Empty;
      string file = string.Format("{0}.txt", CBData.GameKey);

      if (File.Exists(file))
      {
        using (StreamReader sr = new StreamReader(file))
        {
          rtn = sr.ReadToEnd();
          sr.Close();
        }
      }
      else
      {
        rtn = string.Empty;
      }

      return rtn;
    }

    public void SaveGameCode(string value)
    {
      string file = string.Format("{0}.txt", CBData.GameKey);

      using (StreamWriter sw = new StreamWriter(file))
      {
        sw.Write(value);
        sw.Close();
      }
    }

    public string Init()
    {
      HTTPClient client = new HTTPClient();
      System.Net.CookieCollection cookies = new System.Net.CookieCollection();

      // get existing id (if we have one)
      string id = GetGameCode();
      if (id != string.Empty)
      {
        client.CookieCollection.SetCookies(new Uri(CBData.GameUrl), string.Format("{0}={1}", CBData.GameKey, id));
      }

      // visit site and get returned cookie
      string rtn = client.Execute(CBData.GameUrl, "get", "text/xml");
      cookies = client.CookieCollection.GetCookies(new Uri(CBData.GameUrl));
      id = cookies[CBData.GameKey].Value;

      // save for later use
      SaveGameCode(id);

      // return for current use
      return id;
    }

    public string GetSummary(string id)
    {
      HTTPClient client = new HTTPClient();
      client.CookieCollection.SetCookies(new Uri(CBData.GameUrl), string.Format("{0}={1}",CBData.GameKey, id));
      return  client.Execute(CBData.GameUrl, "get", "text/xml");
    }

    public string GetPlayers(string id)
    {
      HTTPClient client = new HTTPClient();
      client.CookieCollection.SetCookies(new Uri(CBData.GameUrl), string.Format("{0}={1}", CBData.GameKey, id));
      return client.Execute(CBData.GameUrl+"players/", "get", "text/xml");
    }

    public string GetPlayer(string id, string pid)
    {
      HTTPClient client = new HTTPClient();
      client.CookieCollection.SetCookies(new Uri(CBData.GameUrl), string.Format("{0}={1}", CBData.GameKey, id));
      return client.Execute(string.Format("{0}players/{1}",CBData.GameUrl,pid), "get", "text/xml");
    }

    public string GetGameList(string id)
    {
      HTTPClient client = new HTTPClient();
      client.CookieCollection.SetCookies(new Uri(CBData.GameUrl), string.Format("{0}={1}", CBData.GameKey, id));
      return  client.Execute(CBData.GameUrl+"games/", "get", "text/xml");
    }
  }
}
