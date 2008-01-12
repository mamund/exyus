using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Principal;

namespace Exyus.Security
{
    public class ExyusIdentity : IIdentity 
    {
        string _name = "guest";
        string _password = "";

        public ExyusIdentity(string user)
        {
            _name = user;
        }

        public ExyusIdentity(string user, string password)
        {
            _name = user;
            _password = password;
        }

        public string AuthenticationType
        {
            get { return "Exyus"; }
        }

        public bool IsAuthenticated
        {
            get { return (_name.ToLower()!="guest"); }
        }

        public string Name
        {
            get { return _name; }
        }

        public string Password
        {
            get { return _password; }
        }

    }
}
