﻿using System;
using System.Collections.Generic;
using System.Text;
using Org.Reddragonit.BackBoneDotNet.Interfaces;

namespace Org.Reddragonit.BackBoneDotNet.JSGenerators
{
    /*
     * This generator generates the namespaced object for the given model type
     */
    internal class NamespaceGenerator : IJSGenerator
    {
        #region IJSGenerator Members

        public string GenerateJS(Type modelType, string host, List<string> readOnlyProperties, List<string> properties, List<string> viewIgnoreProperties, bool hasUpdate, bool hasAdd, bool hasDelete)
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
