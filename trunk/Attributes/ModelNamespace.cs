using System;
using System.Collections.Generic;
using System.Text;

namespace Org.Reddragonit.BackBoneDotNet.Attributes
{
    [AttributeUsage(AttributeTargets.Class,AllowMultiple=true)]
    public class ModelNamespace : Attribute
    {
        private string _host;
        public string Host
        {
            get { return _host; }
        }

        private string _namespace;
        public string Namespace
        {
            get { return _namespace; }
        }

        public ModelNamespace(string NameSpace)
            : this(null,NameSpace)
        {}

        public ModelNamespace(string host, string NameSpace)
        {
            _host = (host == null ? "*" : host);
            _namespace = NameSpace;
        }

        internal static string GetFullNameForModel(Type modelType, string host)
        {
            string ret = null;
            foreach (ModelNamespace mn in modelType.GetCustomAttributes(typeof(ModelNamespace), false))
            {
                if (mn.Host == host)
                {
                    ret = mn.Namespace;
                    break;
                }
            }
            if (ret == null)
            {
                foreach (ModelNamespace mn in modelType.GetCustomAttributes(typeof(ModelNamespace), false))
                {
                    if (mn.Host == "*")
                    {
                        ret = mn.Namespace;
                        break;
                    }
                }
            }
            ret = (ret == null ? modelType.Namespace : ret);
            return ret+(ret.EndsWith(".") ? "" : ".")+modelType.Name;
        }
    }
}
