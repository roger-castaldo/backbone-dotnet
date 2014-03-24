using System;
using System.Collections.Generic;
using System.Text;

namespace Org.Reddragonit.BackBoneDotNet.Attributes
{
    /*
     * Used to define error messages that can be accessed in the javascript code.
     * Language is a 2 character language ISO code
     * message Name is the name of it, this would then be called through Backbone.TranslateValidationError(MessageName)
     * Message is the actual message value
     */
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
