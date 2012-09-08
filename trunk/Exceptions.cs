using System;
using System.Collections.Generic;
using System.Text;

namespace Org.Reddragonit.BackBoneDotNet
{
    public class NoRouteException : Exception
    {
        public NoRouteException(Type t)
            : base("The IModel type " + t.FullName + " is not valid as no Model Route has been specified.") { }
    }

    public class DuplicateRouteException : Exception
    {
        public DuplicateRouteException(string path1, Type type1, string path2, Type type2)
            : base("The IModel type "+type2.FullName+" is not valid as its route "+path2+" is a duplicate for the route "+path1+" contained within the Model "+type1.FullName) { }
    }

    public class DuplicateLoadMethodException : Exception
    {
        public DuplicateLoadMethodException(Type t, string methodName)
            : base("The IModel type " + t.FullName + " is not valid because the method " + methodName + " is tagged as a load method when a valid load method already exists.") { }
    }

    public class NoLoadMethodException : Exception
    {
        public NoLoadMethodException(Type t)
            : base("The IModel type " + t.FullName + " is not valid because there is no valid load method found.  A Load method must have the attribute ModelLoadMethod() as well as be similar to public static IModel Load(string id).") { }
    }

    public class ModelValidationException : Exception
    {
        private List<Exception> _innerExceptions;
        public List<Exception> InnerExceptions
        {
            get { return _innerExceptions; }
        }

        public ModelValidationException(List<Exception> exceptions)
            : base("Model Definition Validations have failed.")
        {
            _innerExceptions = exceptions;
        }
    }

    public class ModelIDBlockedException : Exception
    {
        public ModelIDBlockedException(Type t)
            : base("The IModel type " + t.FullName + " is not valid because the ID property has been tagged with ModelIgnoreProperty.") { }
    }

    public class NoEmptyConstructorException : Exception
    {
        public NoEmptyConstructorException(Type t)
            :base("The IModel type "+t.FullName+" is not valid because it does not block adding and has no empty constructor.")
        {
        }
    }
}
