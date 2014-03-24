using System;
using System.Collections.Generic;
using System.Text;

namespace Org.Reddragonit.BackBoneDotNet.Attributes
{
    /*
     * Used to indicate a non-default code for a delete button on a given model.  This is used when 
     * using the built in View generating code instead of specifying your own.  It also implements the 
     * delete button event call that will call model.delete
     */
    [AttributeUsage(AttributeTargets.Class,AllowMultiple=false)]
    public class DeleteButtonDefinition : Attribute
    {
        private string _host;
        public string Host
        {
            get { return _host; }
        }

        private string[] _class;
        public string[] Class
        {
            get { return _class; }
        }

        private string _tag;
        public string Tag
        {
            get { return _tag; }
        }

        private string _text;
        public string Text
        {
            get { return _text; }
        }

        public DeleteButtonDefinition(string tag, string[] clazz, string text)
            : this(null, tag, clazz, text) { }

        public DeleteButtonDefinition(string host,string tag,string[] clazz,string text)
        {
            _host = (host==null ? "*" : host);
            _class = clazz;
            _tag = (tag==null ? "span" : tag);
            _text = text;
        }
    }
}
