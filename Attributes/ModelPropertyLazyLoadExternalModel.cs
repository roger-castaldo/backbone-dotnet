using System;
using System.Collections.Generic;
using System.Text;

namespace Org.Reddragonit.BackBoneDotNet.Attributes
{
    /*
     * Used to indicate that the property, which is an IModel object is loaded through lazy loading.
     * IE, the id is passed to the front end, then on the first get call it uses the id to load the model 
     * from the server
     */
    [AttributeUsage(AttributeTargets.Property,AllowMultiple=false)]
    public class ModelPropertyLazyLoadExternalModel : Attribute
    {
    }
}
