using System;
using System.Collections.Generic;
using System.Text;

namespace Org.Reddragonit.BackBoneDotNet.Attributes
{
    /*
     * Used to specify a regular expression for validating the field specified
     * and allow for the error message to be specified when the regex check fails,
     * this checks get called on model.validate()
     */
    [AttributeUsage(AttributeTargets.Property,AllowMultiple=true)]
    public class ModelFieldValidationRegex : Attribute
    {
        private string _regex;
        public string Regex
        {
            get { return _regex; }
        }

        private string _errorMessageName;
        public string ErrorMessageName
        {
            get { return _errorMessageName; }
        }

        public ModelFieldValidationRegex(string regex)
            : this(regex, null) { }

        public ModelFieldValidationRegex(string regex, string errorMessageName)
        {
            _regex = regex;
            _errorMessageName = (errorMessageName == null ? "" : errorMessageName);
        }
    }
}
