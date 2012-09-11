using System;
using System.Collections.Generic;
using System.Text;

namespace Org.Reddragonit.BackBoneDotNet.Attributes
{
    /*
     * Used to tag the Load All Models method
     */
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class ModelLoadAllMethod : Attribute
    {
    }
}
