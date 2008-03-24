using System;
using System.Collections.Generic;
using System.Text;

using Exyus.Web;
using System.Xml;

/**************************************************************************************
 * ugdata-cmd.exe
 * 2008-03-15 (mca)
 * 
 * command-line utility to talk to /xcs/ugdata/
 * 
 * commands:
 * -list 
 * -get [id]
 * -add [firstname] [lastname] [birthdate] [experience]
 * -update [id] [firstname] [lastname] [birthdate] [experience]
 * -delete [id]
 * -clear (deletes all records)
 * 
 **************************************************************************************/ 
namespace ugdata_cmd
{
    class Program
    {
        static void Main(string[] args)
        {
            UGData ug = new UGData();
            string rtn = string.Empty;

            Console.WriteLine("\nUGData Utility\n2008-03-14 (mca)\n"+ug.url);

            if (args.Length == 0)
            {
                ShowHelp();
                return;
            }

            Console.WriteLine("Request:");
            ShowCommand(args);
            Console.WriteLine("Response:");

            try
            {
                switch (args[0].ToLower())
                {
                    case "-list":
                        rtn = ug.ShowList();
                        break;
                    case "-get":
                        rtn = ug.ShowItem(args[1]);
                        break;
                    case "-add":
                        ug.AddItem(args[1],args[2],args[3],args[4]);
                        rtn = ug.ShowList();
                        break;
                    case "-update":
                        ug.UpdateItem(args[1], args[2], args[3], args[4],args[5]);
                        rtn = ug.ShowList();
                        break;
                    case "-delete":
                        ug.DeleteItem(args[1]);
                        rtn = ug.ShowList();
                        break;
                    case "-clear":
                        ug.DeleteAll();
                        rtn = ug.ShowList();
                        break;
                    default:
                        throw new IndexOutOfRangeException("Unknown command [" + args[0] + "]");
                }

                Console.WriteLine(rtn);
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR: " + ex.Message);
                ShowHelp();
            }

        }

        static void ShowCommand(string[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                Console.Write(args[i] + " ");
            }
            Console.WriteLine("\n");
        }

        static void ShowHelp()
        {
            Console.WriteLine("\nvalid commands:\n-list\n-read [id]\n-add [firstname] [lastname] [birthdate] [experience]\n-update [id] [firstname] [lastname] [birthdate] [experience]\n-delete [id]\n-clear");
        }
    }

    class UGData
    {
        string media_type = "text/xml";
        string etag = string.Empty;
        string ua = "ugdata-cmd/1.0";
        HTTPClient client = new HTTPClient();
        string fmt_item = "<member><firstname>{0}</firstname><lastname>{1}</lastname><birthdate>{2}</birthdate><experience>{3}</experience></member>";
        string valid_experience = "None, WhatIsREST, ICanHitF1, GoogleRocks, CallMeRoy,";

        public string url = "http://exyus.com/xcs/ugdata/";

        public UGData(){}
        public UGData(string url)
        {
            this.url = url;
        }
        public UGData(string url, string agent)
        {
            this.url = url;
            this.ua = agent;
        }

        private XmlDocument GetList()
        {
            client.UserAgent = ua;
            client.RequestHeaders.Set("cache-control", "no-cache");
            string results = client.Execute(url, "get", media_type);
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(results);
            return doc;
        }

        private XmlDocument GetItem(string id)
        {
            client.UserAgent = ua;
            string results = client.Execute(url + id, "get", media_type);
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(results);
            return doc;
        }

        private bool ValidExperience(string experience)
        {
            if (valid_experience.IndexOf(experience+",") == -1)
                return false;
            else
                return true;
        }

        public void AddItem(string fname, string lname, string bdate, string experience)
        {
            if(ValidExperience(experience))
            {
                client.UserAgent = ua;
                client.Execute(url, "post", media_type, string.Format(fmt_item, fname, lname, bdate, experience));
            }
            else
            {
                throw new ArgumentException("invalid experience value. must be one of the following:\n"+valid_experience);
            }
        }

        public void UpdateItem(string id, string fname, string lname, string bdate, string experience)
        {
            if(ValidExperience(experience))
            {
                client.UserAgent = ua;
                client.Execute(url + id, "head", media_type);
                etag = client.ResponseHeaders["etag"];

                client.RequestHeaders.Add("if-match",etag);
                client.Execute(url + id, "put", media_type, string.Format(fmt_item, fname, lname, bdate, experience));
            }
            else
            {
                throw new ArgumentException("invalid experience value. must be one of the following:\n"+valid_experience);
            }
        }

        public void DeleteItem(string id)
        {
            client.UserAgent = ua;
            client.Execute(url + id, "delete", media_type);
        }

        public void DeleteAll()
        {
            XmlDocument doc = GetList();
            XmlNodeList members = doc.SelectNodes("//member");
            for (int i = 0; i < members.Count; i++)
            {
                try
                {
                    DeleteItem(members[i].Attributes["id"].Value);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("ERROR: "+ex.Message);
                }
            }
        }

        public string ShowItem(string id)
        {
            XmlDocument doc = GetItem(id);
            XmlNode member = doc.SelectSingleNode("//member");
            StringBuilder sb = new StringBuilder();

            sb.AppendFormat("id ...........: {0}\n", member.Attributes["id"].Value);
            sb.AppendFormat("firstname ....: {0}\n", member.SelectSingleNode("//firstname").InnerText);
            sb.AppendFormat("lastname .....: {0}\n", member.SelectSingleNode("//lastname").InnerText);
            sb.AppendFormat("birthdate ....: {0}\n", member.SelectSingleNode("//birthdate").InnerText);
            sb.AppendFormat("experience ...: {0}\n", member.SelectSingleNode("//experience").InnerText);

            return sb.ToString();
        }

        public string ShowList()
        {
            XmlDocument doc = GetList();
            XmlNodeList members = doc.SelectNodes("//member");
            StringBuilder sb = new StringBuilder();

            if (members.Count == 0)
                return "list is empty.";

            for (int i = 0; i < members.Count; i++)
            {
                sb.AppendFormat(
                    "{0} - {2},{3} ({1})\n",
                    i+1,
                    members[i].Attributes["id"].Value,
                    members[i].SelectSingleNode("lastname").InnerText,
                    members[i].SelectSingleNode("firstname").InnerText
                    );
            }
            return sb.ToString();
        }


 
    }
}
