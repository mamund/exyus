using System;
using System.Collections;
using System.Text;
using System.Security.Principal;
using System.Text.RegularExpressions;

namespace Exyus.Security
{
    // NOTE: roles is not used right now, just permissions
    // do we *need* roles? http method + resource is coverd by perms
    // roles might be a predefined collection of perms, but that can get messy
    public class ExyusPrincipal : IPrincipal
    {
        private IIdentity _identity;
        private string[] _roles;
        private SortedList _permissions = new SortedList();

        public ExyusPrincipal(IIdentity identity)
        {
            _identity = identity;
            _roles = new string[0];
        }

        public ExyusPrincipal(IIdentity identity, string[] roles)
        {
            _identity = identity;
            _roles = new string[roles.Length];
            roles.CopyTo(_roles, 0);
            Array.Sort(_roles);
        }

        public ExyusPrincipal(IIdentity identity, SortedList permissions)
        {
            _identity = identity;
            _permissions = permissions;
        }

        public ExyusPrincipal(IIdentity identity, string[] roles,SortedList permissions)
        {
            _identity = identity;
            _permissions = permissions;
            _roles = new string[roles.Length];
            roles.CopyTo(_roles, 0);
            Array.Sort(_roles);
        }

        public IIdentity Identity
        {
            get { return _identity; }
        }

        public bool IsInRole(string role)
        {
            return Array.BinarySearch(_roles, role) >= 0 ? true : false;
        }

        // can the current user commit the action on the supplied uri?
        public bool HasPermission(string uri, string action)
        {
            bool rtn = false;

            // walk the collection (from the bottom up!) to find first match
            for (int i = _permissions.Count-1; i > -1; i--)
            {
                if (new Regex(_permissions.GetKey(i).ToString(), RegexOptions.IgnoreCase).IsMatch(uri))
                {
                    // * = all
                    if(_permissions.GetByIndex(i).ToString()=="*")
                    {
                        rtn = true;
                        goto exit;
                    }
                    // ! = none
                    if(_permissions.GetByIndex(i).ToString()=="!")
                    {
                        rtn=false;
                        goto exit;
                    }
                    // check for actual method
                    if (_permissions.GetByIndex(i).ToString().ToLower().IndexOf(action.ToLower()) != -1)
                    {
                        rtn = true;
                        goto exit;
                    }
                }
            }
            exit:
            return rtn;
        }
    }
}
