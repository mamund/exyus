using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace Exyus.Caching
{
    public class CacheObject
    {
        private DateTime _lastmodified = DateTime.MinValue;
        private string _etag = System.Guid.NewGuid().ToString();
        private string _payload = string.Empty;
        private int _maxage = 0;
        private int _localmaxage = 0;
        private DateTime _expires = DateTime.MinValue;
        private string _contenttype = "text/xml";

        public string ContentType
        {
            get { return _contenttype; }
            set { _contenttype = value; }
        }

        // will cause sliding caching expiration
        public int LocalMaxAge
        {
            get { return _localmaxage; }
            set { _localmaxage = value; }
        }

        // will use aboslute caching expiration
        // NOTE: overridden by LocalMaxAge!
        public int MaxAge
        {
            get { return _maxage; }
            set { _maxage = value; }
        }

        public DateTime Expires
        {
            get { return _expires; }
            set { _expires = value; }
        }
        public DateTime LastModified
        {
            get { return _lastmodified; }
            set { _lastmodified = value; }
        }

        public string Etag
        {
            get { return _etag; }
            set { _etag = value; }
        }

        // the actual string-pile/content to cache
        public string Payload
        {
            get { return _payload; }
            set { _payload = value; }
        }

        public CacheObject() {}
        public CacheObject(string payload)
        {
            this._payload = payload;
        }
        public CacheObject(DateTime lm, string tag, string payload)
        {
            this._payload = payload;
            this._etag = tag;
            this._lastmodified = lm;
        }
    }
}
