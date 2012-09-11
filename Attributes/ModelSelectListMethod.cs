using System;
using System.Collections.Generic;
using System.Text;

namespace Org.Reddragonit.BackBoneDotNet.Attributes
{
    /*
     * Used to tag the Model Select List Load method
     */
    [AttributeUsage(AttributeTargets.Method)]
    public class ModelSelectListMethod : Attribute
    {
        public ModelSelectListMethod() { }
    }
}
