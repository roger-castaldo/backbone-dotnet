using System;
using System.Collections.Generic;
using System.Text;

namespace Org.Reddragonit.BackBoneDotNet.Attributes
{
    [AttributeUsage(AttributeTargets.Class,AllowMultiple=false)]
    public class EditButtonDefinition : Attribute
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

        public EditButtonDefinition(string tag, string[] clazz, string text)
            : this(null, tag, clazz, text) { }


        public EditButtonDefinition(string host,string tag,string[] clazz,string text)
        {
            _host = (host == null ? "*" : host);
            _class = clazz;
            _tag = (tag==null ? "span" : tag);
            _text = text;
        }
    }
}
