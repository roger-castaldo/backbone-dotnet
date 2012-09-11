using System;
using System.Collections.Generic;
using System.Text;

namespace Org.Reddragonit.BackBoneDotNet.Attributes
{
    /*
     * Used to Ignore a property for model generation.
     */
    [AttributeUsage(AttributeTargets.Property,AllowMultiple=false)]
    public class ModelIgnoreProperty : Attribute
    {
    }
}
