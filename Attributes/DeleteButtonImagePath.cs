using System;
using System.Collections.Generic;
using System.Text;

namespace Org.Reddragonit.BackBoneDotNet.Attributes
{
    [AttributeUsage(AttributeTargets.Class,AllowMultiple=false)]
    public class DeleteButtonImagePath : Attribute
    {
        private string _url;
        public string URL
        {
            get { return _url; }
        }

        private string _host;
        public string Host
        {
            get { return _host; }
        }

        public DeleteButtonImagePath(string url)
            : this(null, url) { }

        public DeleteButtonImagePath(string host,string url)
        {
            _host = (host == null ? "*" : (host == "" ? "*" : host));
            _url = url;
        }
    }
}
