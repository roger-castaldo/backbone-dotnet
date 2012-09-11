using System;
using System.Collections.Generic;
using System.Text;

namespace Org.Reddragonit.BackBoneDotNet.Attributes
{
    /*
     * Used to block given actions for a model, those being Add,Edit,Delete
     */
    [AttributeUsage(AttributeTargets.Class,AllowMultiple=false)]
    public class ModelBlockActions : Attribute
    {
        private ModelActionTypes _type;
        public ModelActionTypes Type
        {
            get { return _type; }
        }

        public ModelBlockActions(ModelActionTypes type)
        {
            _type = type;
        }
    }
}
