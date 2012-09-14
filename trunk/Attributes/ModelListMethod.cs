using System;
using System.Collections.Generic;
using System.Text;

namespace Org.Reddragonit.BackBoneDotNet.Attributes
{
    [AttributeUsage(AttributeTargets.Method,AllowMultiple=true)]
    public class ModelListMethod : Attribute
    {
        private string _host;
        public string Host
        {
            get { return _host; }
        }

        private string _path;
        public string Path
        {
            get { return _path; }
        }

        public ModelListMethod(string host,string path)
        {
            _path = path;
            _host = host;
        }

        public ModelListMethod(string path)
        {
            _path = path;
            _host = "*";
        }
    }
}
