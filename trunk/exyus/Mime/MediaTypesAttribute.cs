using System;

namespace Exyus.Web
{
    [AttributeUsage(AttributeTargets.Class)]
    public class MediaTypes : Attribute
    {
        private string[] _types;

        public MediaTypes(params string[] mediatypes)
        {
            _types = (string[])(mediatypes.Clone());
        }

        public string[] Types
        {
            get { return _types; }
        }
    }
}
