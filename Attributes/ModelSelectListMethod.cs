using System;
using System.Collections.Generic;
using System.Text;

namespace Org.Reddragonit.BackBoneDotNet.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class ModelSelectListMethod : Attribute
    {
        public ModelSelectListMethod() { }
    }
}
