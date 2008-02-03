/*
 * tasklist-cmd
 * 2008-01-29 (mca)
 * 2008-02-01 (mca) : cleaned up UI, added arg display
 * 
 * sample command-line app that uses HTTPClient to execute against server
 * uses the http://exyus.com/xcs/tasklist endpoint as a target.
 * list, add, update, delete tasks via commandline 
 * 
 * usage:
 *    tasklist-cmd cmd [arg1] [arg2]
 *    -list                 : returns list of tasks at the public site
 *    -add "my task"        : creates new task in pending state
 *    -add "my task" 1      : creates new task in completed state
 *    -toggle x1234abcd     : toggles the state of the task (using id)
 *    -delete x1234abcd     : deletes existing task (using id)
 *    -clear                : removes all tasks from the list
 */ 

using System;
using System.Collections.Generic;
using System.Text;

using Exyus.Web;
using System.Xml;

namespace tasklist_cmd
{
    class Program
    {
        static void Main(string[] args)
        {
            TaskList tl = new TaskList("http://exyus.com/xcs/tasklist/");

            Console.WriteLine("\nTaskList Utility\n2008-02-02 (mca)\n" + tl.Uri + "\n");

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
                        break;
                    case "-add":
                        if (args.Length > 2)
                            tl.AddItem(args[1], args[2]);
                        else
                            tl.AddItem(args[1]);
                        break;
                    case "-toggle":
                        tl.ToggleItem(args[1]);
                        break;
                    case "-delete":
                        tl.DeleteItem(args[1]);
                        break;
                    case "-clear":
                        tl.DeleteAll();
                        break;
                    default:
                        throw new IndexOutOfRangeException("Unknown command [" + args[0] + "]");
                }

                // show current list
                Console.WriteLine(tl.ShowList());
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR: "+ex.Message);
                ShowHelp();
            }

            return;
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
            Console.WriteLine("\nvalid commands:\n-list\n-add [name]\n-toggle [id]\n-delete [id]\n-clear");
        }
    }

    public class TaskList
    {
        string p_etag = string.Empty;
        string p_ua = "tasklist-cmd/1.0";
        string p_done = "<is-completed>1</is-completed>";
        string p_pending = "<is-completed>0</is-completed>";
        string p_new_task = "<task><name>{0}</name><is-completed>{1}</is-completed></task>";
        HTTPClient client = new HTTPClient();

        public string Uri = "http://exyus.com/xcs/tasklist/";

        public TaskList() { }
        public TaskList(string uri)
        {
            this.Uri = uri;
            client.UserAgent = p_ua;
        }

        public XmlDocument GetList()
        {
            client.RequestHeaders.Set("cache-control", "no-cache");
            string results = client.Execute(Uri, "get", "text/xml");
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(results);
            return doc;
        }

        public XmlDocument GetItem(string id)
        {
            string results = client.Execute(Uri + id, "get", "text/xml");
            p_etag = client.ResponseHeaders["etag"];
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(results);
            return doc;
        }

        public void AddItem(string name)
        {
            AddItem(name, "0");
        }

        public void AddItem(string name, string completed)
        {
            client.Execute(Uri, "post", "text/xml", string.Format(p_new_task, name, completed));
        }

        public void ToggleItem(string id)
        {
            XmlDocument doc = GetItem(id);
            string results = doc.OuterXml;
            XmlNode completed = doc.SelectSingleNode("//is-completed");
            
            if (completed.InnerText == "0")
                results = results.Replace(p_pending, p_done);
            else
                results = results.Replace(p_done, p_pending);

            client.RequestHeaders.Set("if-match", p_etag);
            client.Execute(Uri + id, "put", "text/xml", results);
        }

        public void DeleteItem(string id)
        {
            client.Execute(Uri + id, "delete", "text/xml");
        }

        public void DeleteAll()
        {
            XmlDocument doc = GetList();
            XmlNodeList tasks = doc.SelectNodes("//task");
            for (int i = 0; i < tasks.Count; i++)
            {
                try
                {
                    DeleteItem(tasks[i].Attributes["href"].Value);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("ERROR: "+ex.Message);
                }
            }
        }

        public string ShowList()
        {
            XmlDocument doc = GetList();
            XmlNodeList tasks = doc.SelectNodes("//task");
            StringBuilder sb = new StringBuilder();

            if (tasks.Count == 0)
                return "list is empty.";

            for (int i = 0; i < tasks.Count; i++)
            {
                sb.AppendFormat(
                    "{0} {1}({2})\n",
                    tasks[i].Attributes["href"].Value,
                    tasks[i].SelectSingleNode("name").InnerText,
                    tasks[i].SelectSingleNode("is-completed").InnerText
                    );
            }
            return sb.ToString();
        }
    }
}
