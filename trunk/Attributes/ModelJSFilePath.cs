using System;
using System.Collections.Generic;
using System.Text;

namespace Org.Reddragonit.BackBoneDotNet.Attributes
{
    /*
     * Used to specify the js path that the javascript code for this model will be written to
     */
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class ModelJSFilePath: Attribute
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

        internal string MinPath
        {
            get { return (_path.EndsWith(".min.js") ? _path : _path.Substring(0, _path.LastIndexOf(".")) + ".min.js"); }
        }

        public ModelJSFilePath(string host,string path)
        {
            _path = (!path.EndsWith(".js") ? path+".js" : path);
            _host = host;
        }

        public ModelJSFilePath(string path)
        {
            _path = path;
            _host = "*";
        }
    }
}
