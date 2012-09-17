using System;
using System.Collections.Generic;
using System.Text;

namespace Org.Reddragonit.BackBoneDotNet.Attributes
{
    /*
     * Used to specify special attributes to attach to a model view
     */
    [AttributeUsage(AttributeTargets.Class,AllowMultiple=true)]
    public class ModelViewAttribute : Attribute
    {
        private string _name;
        public string Name
        {
            get { return _name; }
        }

        private string _value;
        public string Value
        {
            get { return _value; }
        }

        public ModelViewAttribute(string name, string value)
        {
            _name = name;
            _value = value;
        }
    }
}
