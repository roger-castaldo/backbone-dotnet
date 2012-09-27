using System;
using System.Collections.Generic;
using System.Text;

namespace Org.Reddragonit.BackBoneDotNet.Interfaces
{
    /*
     * Used to allow for a break down of javascript generating functions
     */
    internal interface IJSGenerator
    {
        string GenerateJS(Type modelType,string host,List<string> readOnlyProperties,List<string> properties,List<string> viewIgnoreProperties,bool hasUpdate,bool hasAdd,bool hasDelete);
    }
}
