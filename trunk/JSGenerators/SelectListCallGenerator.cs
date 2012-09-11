﻿using System;
using System.Collections.Generic;
using System.Text;
using Org.Reddragonit.BackBoneDotNet.Interfaces;
using System.Reflection;
using Org.Reddragonit.BackBoneDotNet.Attributes;

namespace Org.Reddragonit.BackBoneDotNet.JSGenerators
{
    /*
     * This generator generates code to access a select list.
     * the select list function is available at namespace.type.SelectListCallGenerator
     */
    internal class SelectListCallGenerator : IJSGenerator
    {
        #region IJSGenerator Members

        public string GenerateJS(Type modelType, string host, List<string> readOnlyProperties, List<string> properties)
        {
            string urlRoot = "";
            foreach (ModelRoute mr in modelType.GetCustomAttributes(typeof(ModelRoute), false))
            {
                if (mr.Host == host)
                {
                    urlRoot = mr.Path;
                    break;
                }
            }
            if (urlRoot == "")
            {
                foreach (ModelRoute mr in modelType.GetCustomAttributes(typeof(ModelRoute), false))
                {
                    if (mr.Host == "*")
                    {
                        urlRoot = mr.Path;
                        break;
                    }
                }
            }
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("//Org.Reddragonit.BackBoneDotNet.JSGenerators.SelectListCallGenerator");
            foreach (MethodInfo mi in modelType.GetMethods(BindingFlags.Public | BindingFlags.Static))
            {
                if (mi.GetCustomAttributes(typeof(ModelSelectListMethod), false).Length > 0)
                {
                    sb.AppendLine(modelType.FullName+".SelectList = function(){");
                    sb.AppendLine("\tvar ret=$.ajax(");
                    sb.AppendLine("\t\t'" + urlRoot + "',{");
                    sb.AppendLine("\t\t\tasync:false,");
                    sb.AppendLine("\t\t\tcache:false,");
                    sb.AppendLine("\t\t\ttype : 'SELECT'");
                    sb.AppendLine("}).responseText;");
                    sb.AppendLine("return JSON.parse(ret);");
                    sb.AppendLine("}");
                    break;
                }
            }
            return sb.ToString();
        }

        #endregion
    }
}
