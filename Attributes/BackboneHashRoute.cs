using System;
using System.Collections.Generic;
using System.Text;

namespace Org.Reddragonit.BackBoneDotNet.Attributes
{
    /*
     * Used to add a route to the Backbone Hash Router, specify the name, path and the javascript code
     * called by the route.  Suggest using the name Router for the router name as it is the standard.
     */
    [AttributeUsage(AttributeTargets.Class,AllowMultiple=true,Inherited=false)]
    public class BackboneHashRoute : Attribute
    {
        private string _routerName;
        public string RouterName
        {
            get { return _routerName; }
        }

        private string _path;
        public string Path
        {
            get { return _path; }
        }

        private string _code;
        public string Code {
            get { return _code; }
        }

        public string FunctionName
        {
            get { return Path.Replace("/", "_").Replace(":", "__"); }
        }

        public BackboneHashRoute(string routerName, string path, string code)
        {
            _routerName = routerName;
            _path = path.TrimStart('/');
            _code = code;
        }
    }
}
