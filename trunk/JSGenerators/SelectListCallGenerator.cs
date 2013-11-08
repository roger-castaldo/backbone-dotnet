using System;
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

        public string GenerateJS(Type modelType, string host, List<string> readOnlyProperties, List<string> properties, List<string> viewIgnoreProperties, bool hasUpdate, bool hasAdd, bool hasDelete)
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
            List<MethodInfo> methods = new List<MethodInfo>();
            foreach (MethodInfo mi in modelType.GetMethods(Constants.LOAD_METHOD_FLAGS))
            {
                if (mi.GetCustomAttributes(typeof(ModelSelectListMethod), false).Length > 0)
                    methods.Add(mi);
            }
            if (methods.Count > 0)
            {
                sb.AppendLine(ModelNamespace.GetFullNameForModel(modelType, host) + " = _.extend(true," + ModelNamespace.GetFullNameForModel(modelType, host) + ",{SelectList : function(pars){");
                for (int x = 0; x < methods.Count; x++)
                {
                    sb.Append("\t"+(x == 0 ? "" : "else ") + "if(");
                    if (methods[x].GetParameters().Length == 0)
                    {
                        sb.AppendLine("pars==undefined || pars==null){");
                        sb.AppendLine("\t\turl='';");
                    }
                    else
                    {
                        ParameterInfo[] pars = methods[x].GetParameters();
                        StringBuilder code = new StringBuilder();
                        code.AppendLine("\t\turl='?';");
                        for (int y = 0; y < pars.Length; y++)
                        {
                            sb.Append((y!=0 ? " && " : "")+"pars." + pars[y].Name + "!=undefined");
                            code.AppendLine("\t\tpars." + pars[y].Name + " = (pars." + pars[y].Name + " == null ? 'NULL' : pars." + pars[y].Name + ");");
                            if (pars[y].ParameterType == typeof(bool))
                                code.AppendLine("pars." + pars[y].Name + " = (pars." + pars[y].Name + " == null ? 'false' : (pars." + pars[y].Name + " ? 'true' : 'false'));");
                            else if (pars[y].ParameterType == typeof(DateTime))
                            {
                                code.AppendLine("if (pars." + pars[y].Name + " != 'NULL'){");
                                code.AppendLine("\tif (!(pars." + pars[y].Name + " instanceof Date)){");
                                code.AppendLine("\t\tpars." + pars[y].Name + " = new Date(pars." + pars[y].Name + ");");
                                code.AppendLine("\t}");
                                code.AppendLine("\tpars." + pars[y].Name + " = Date.UTC(pars." + pars[y].Name + ".getUTCFullYear(), pars." + pars[y].Name + ".getUTCMonth(), pars." + pars[y].Name + ".getUTCDate(), pars." + pars[y].Name + ".getUTCHours(), pars." + pars[y].Name + ".getUTCMinutes(), pars." + pars[y].Name + ".getUTCSeconds());");
                                code.AppendLine("}");
                            }
                            code.AppendLine("\t\turl+='"+(y==0 ? "" : "&") + pars[y].Name + "='+pars." + pars[y].Name + ".toString();");
                        }
                        sb.AppendLine("){");
                        sb.Append(code.ToString());
                    }
                    sb.AppendLine("}");
                }
                sb.AppendLine(
@"  else{
        return null;
    }
    var ret=$.ajax(
        '" + urlRoot + @"'+url,{
            async:false,
            cache:false,
            type : 'SELECT'
}).responseText;
    var response = JSON.parse(ret);
    if(response.Backbone!=undefined){
        _.extend(Backbone,response.Backbone);
        response=response.response;
    }
return response;
}});");
                    
            }
            return sb.ToString();
        }

        #endregion
    }
}
