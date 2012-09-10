using System;
using System.Collections.Generic;
using System.Text;
using Org.Reddragonit.BackBoneDotNet.Interfaces;

namespace Org.Reddragonit.BackBoneDotNet.JSGenerators
{
    internal class NamespaceGenerator : IJSGenerator
    {
        #region IJSGenerator Members

        public string GenerateJS(Type modelType, string host, List<string> readOnlyProperties, List<string> properties)
        {
            string ret = "";
            string tmp = "";
            foreach (string str in modelType.FullName.Split('.'))
            {
                ret += (tmp.Length == 0 ? "var " + str : tmp + "." + str) + " = " + (tmp.Length == 0 ? "" : tmp + ".") + str + " || {};\n";
                tmp += (tmp.Length == 0 ? "" : ".") + str;
            }
            return ret;
        }

        #endregion
    }
}
