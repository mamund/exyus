using System;

namespace Exyus.Web
{
    [AttributeUsage(AttributeTargets.Class)]
    public class UriPattern : Attribute
    {
        private string[] _patterns;

        public UriPattern(params string[] uriPatterns)
        {
            _patterns = (string[])(uriPatterns.Clone());
        }

        public string[] Patterns
        {
            get {return _patterns;}
        }
    }
}
