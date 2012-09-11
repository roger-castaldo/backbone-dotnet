using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

namespace Org.Reddragonit.BackBoneDotNet
{
    //thrown when no routes to a given model were specified by attributes
    public class NoRouteException : Exception
    {
        public NoRouteException(Type t)
            : base("The IModel type " + t.FullName + " is not valid as no Model Route has been specified.") { }
    }

    //thrown when more than one model is mapped to the same route
    public class DuplicateRouteException : Exception
    {
        public DuplicateRouteException(string path1, Type type1, string path2, Type type2)
            : base("The IModel type "+type2.FullName+" is not valid as its route "+path2+" is a duplicate for the route "+path1+" contained within the Model "+type1.FullName) { }
    }

    //thrown when more than one Load method exists in a given model
    public class DuplicateLoadMethodException : Exception
    {
        public DuplicateLoadMethodException(Type t, string methodName)
            : base("The IModel type " + t.FullName + " is not valid because the method " + methodName + " is tagged as a load method when a valid load method already exists.") { }
    }

    //thrown when no Load method is specified
    public class NoLoadMethodException : Exception
    {
        public NoLoadMethodException(Type t)
            : base("The IModel type " + t.FullName + " is not valid because there is no valid load method found.  A Load method must have the attribute ModelLoadMethod() as well as be similar to public static IModel Load(string id).") { }
    }

    //special exception designed to house all found validation exceptions
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

    //thrown when the id property of the model is tagged as block
    public class ModelIDBlockedException : Exception
    {
        public ModelIDBlockedException(Type t)
            : base("The IModel type " + t.FullName + " is not valid because the ID property has been tagged with ModelIgnoreProperty.") { }
    }

    //thrown when no empty constructor is specifed but adding the model has not been blocked
    public class NoEmptyConstructorException : Exception
    {
        public NoEmptyConstructorException(Type t)
            :base("The IModel type "+t.FullName+" is not valid because it does not block adding and has no empty constructor.")
        {
        }
    }

    //thrown when the return type is not valid for a ModelSelectList function
    public class InvalidModelSelectOptionValueReturnException : Exception
    {
        public InvalidModelSelectOptionValueReturnException(Type t, MethodInfo mi)
            : base("The IModel type "+t.FullName+" is not valid because the ModelSelectList function "+mi.Name+" does not return a valid type (List<sModelSelectOptionValue> or sModelSelectOptionValue[]).")
        { }
    }

    //thrown when no model select method is specified but another model uses that type as a property
    public class NoModelSelectMethodException : Exception
    {
        public NoModelSelectMethodException(Type t, PropertyInfo pi)
            : base("The IModel type " + t.FullName + " is not valid because the property " + pi.Name + " is linked to an IModel " + pi.PropertyType.FullName + " does not have a load select method.")
        { }
    }

    //thrown when more than one model select list function is specified
    public class MultipleSelectOptionValueMethodsException : Exception{
        public MultipleSelectOptionValueMethodsException(Type t,MethodInfo mi)
            : base("The IModel type "+t.FullName+" is not valid becaose there is more than one ModelSelectList load function, the additional function declared is "+mi.Name+".")
        {}
    }
}
