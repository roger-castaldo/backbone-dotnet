using System;
using System.Collections.Generic;
using System.Text;

namespace Org.Reddragonit.BackBoneDotNet.Attributes
{
    /*
     * Used to hide the field from the view when using the auto-generated view code.
     */
    [AttributeUsage(AttributeTargets.Property,AllowMultiple=false)]
    public class ViewIgnoreField : Attribute
    {
    }
}
