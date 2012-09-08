using System;
using System.Collections.Generic;
using System.Text;

namespace Org.Reddragonit.BackBoneDotNet.Attributes
{
    [AttributeUsage(AttributeTargets.Class,AllowMultiple=false)]
    public class ModelEditAddType : Attribute
    {
        private ModelEditAddTypes _type;
        public ModelEditAddTypes Type
        {
            get { return _type; }
        }

        public ModelEditAddType(ModelEditAddTypes type)
        {
            _type = type;
        }
    }
}
