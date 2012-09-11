using System;
using System.Collections.Generic;
using System.Text;

namespace Org.Reddragonit.BackBoneDotNet.Attributes
{
    /*
     * Used to specify additional class names to attach to a view for the model
     */
    [AttributeUsage(AttributeTargets.Class,AllowMultiple=true)]
    public class ModelViewClass : Attribute
    {
        private string _className;
        public string ClassName
        {
            get { return _className; }
        }

        public ModelViewClass(string className)
        {
            _className = className;
        }
    }
}
