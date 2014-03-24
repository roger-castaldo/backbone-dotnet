using System;
using System.Collections.Generic;
using System.Text;

namespace Org.Reddragonit.BackBoneDotNet.Attributes
{
    /*
     * Used to block the inheritance of a model, through the inheritance 
     * of a class.  This is used to ensure that validations won't cause an issue when inheriting a 
     * IModel class that is not being used as a Model.
     */
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class ModelBlockInheritance : Attribute
    {
    }
}
