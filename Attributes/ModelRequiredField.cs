using System;
using System.Collections.Generic;
using System.Text;

namespace Org.Reddragonit.BackBoneDotNet.Attributes
{
    [AttributeUsage(AttributeTargets.Property,AllowMultiple=false)]
    public class ModelRequiredField : Attribute
    {
        private string _errorMessageName;
        public string ErrorMessageName
        {
            get { return _errorMessageName; }
        }

        public ModelRequiredField()
            : this(null) { }

        public ModelRequiredField(string errorMessageName)
        {
            _errorMessageName = (errorMessageName == null ? "" : errorMessageName);
        }
    }
}
