using System;
using System.Collections.Generic;
using System.Text;

namespace Org.Reddragonit.BackBoneDotNet.Attributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class ModelViewTag :Attribute
    {
        private ModelViewTagTypes _type = ModelViewTagTypes.div;
        public ModelViewTagTypes Type
        {
            get { return _type; }
        }

        private string _tagName;
        public string TagName
        {
            get { return (_tagName == null ? _type.ToString() : _tagName); }
        }

        public ModelViewTag(ModelViewTagTypes type){
            _type = type;
            _tagName = null;
        }

        public ModelViewTag(string tagName)
        {
            _tagName = tagName;
        }
    }
}
