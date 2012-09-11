using System;
using System.Collections.Generic;
using System.Text;

namespace Org.Reddragonit.BackBoneDotNet.Attributes
{
    /*
     * Used to specify the type of form to use when adding/editing a model (inline or creating a dialog)
     */
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
