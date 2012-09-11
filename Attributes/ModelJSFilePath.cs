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

        public ModelJSFilePath(string host,string path)
        {
            _path = path;
            _host = host;
        }

        public ModelJSFilePath(string path)
        {
            _path = path;
            _host = "*";
        }
    }
}
