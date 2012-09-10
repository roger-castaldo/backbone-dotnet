using System;
using System.Collections.Generic;
using System.Text;

namespace Org.Reddragonit.BackBoneDotNet.Interfaces
{
    internal interface IJSGenerator
    {
        string GenerateJS(Type modelType,string host,List<string> readOnlyProperties,List<string> properties);
    }
}
