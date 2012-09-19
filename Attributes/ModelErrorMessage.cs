using System;
using System.Collections.Generic;
using System.Text;

namespace Org.Reddragonit.BackBoneDotNet.Attributes
{
    [AttributeUsage(AttributeTargets.Class,AllowMultiple=true)]
    public class ModelErrorMessage : Attribute
    {
        private string _language;
        public string language
        {
            get { return _language; }
        }

        private string _messageName;
        public string MessageName
        {
            get { return _messageName; }
        }

        private string _message;
        public string Message
        {
            get { return _message; }
        }

        public ModelErrorMessage(string language, string messageName, string message)
        {
            _language = language;
            _messageName = messageName;
            _message = message;
        }
    }
}
