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

        public string GenerateJS(Type modelType, string host, List<string> readOnlyProperties, List<string> properties, List<string> viewIgnoreProperties, bool hasUpdate, bool hasAdd, bool hasDelete,bool minimize)
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
            WrappedStringBuilder sb = new WrappedStringBuilder(minimize);
            if (!minimize)
                sb.AppendLine("//Org.Reddragonit.BackBoneDotNet.JSGenerators.SelectListCallGenerator");
            List<MethodInfo> methods = new List<MethodInfo>();
            foreach (MethodInfo mi in modelType.GetMethods(Constants.LOAD_METHOD_FLAGS))
            {
                if (mi.GetCustomAttributes(typeof(ModelSelectListMethod), false).Length > 0)
                    methods.Add(mi);
            }
            int curLen = 0;
            for (int x = 0; x < methods.Count; x++)
                curLen = Math.Max(methods[x].GetParameters().Length, curLen);
            int index = 0;
            while (index < methods.Count)
            {
                for (int x = index; x < methods.Count; x++)
                {
                    if (curLen == methods[x].GetParameters().Length)
                    {
                        MethodInfo mi = methods[x];
                        methods.RemoveAt(x);
                        methods.Insert(index, mi);
                        index++;
                        x = index - 1;
                    }
                }
                curLen = 0;
                for (int x = index; x < methods.Count; x++)
                    curLen = Math.Max(methods[x].GetParameters().Length, curLen);
            }
            if (methods.Count > 0)
            {
                sb.AppendLine(string.Format((minimize ?
                    "{0}=_.extend(true,{0},{{SelectList:function(pars){{"
                    : "{0} = _.extend(true,{0},{{SelectList : function(pars){{"
                ),ModelNamespace.GetFullNameForModel(modelType, host)));
                for (int x = 0; x < methods.Count; x++)
                {
                    sb.Append((minimize ? "" : "\t") + (x == 0 ? "" : "else ") + "if(");
                    if (methods[x].GetParameters().Length == 0)
                    {
                        sb.AppendLine((minimize? 
                            "pars==undefined||pars==null){url='';"
                        : @"pars==undefined || pars==null){
        url='';"));
                    }
                    else
                    {
                        ParameterInfo[] pars = methods[x].GetParameters();
                        WrappedStringBuilder code = new WrappedStringBuilder(minimize);
                        code.AppendLine((minimize ? "url='?';" : "\t\turl='?';"));
                        sb.Append((minimize ? "pars!=undefined&&pars!=null&&(" : "pars!=undefined && pars!=null && ("));
                        for (int y = 0; y < pars.Length; y++)
                        {
                            sb.Append((y != 0 ? (minimize ? "&&" : " && ") : "") + "pars." + pars[y].Name + "!=undefined");
                            code.AppendLine(string.Format((minimize?
                                "pars.{0}=(pars.{0}==null?'NULL':pars.{0});"
                                :"\t\tpars.{0} = (pars.{0} == null ? 'NULL' : pars.{0});"),
                                pars[y].Name));
                            if (pars[y].ParameterType == typeof(bool))
                                code.AppendLine(string.Format((minimize?
                                "pars.{0}=(pars.{0}==null?'false':(pars.{0}?'true':'false'));"
                                :"\t\tpars.{0} = (pars.{0} == null ? 'false' : (pars.{0} ? 'true' : 'false'));"),
                                pars[y].Name));
                            else if (pars[y].ParameterType == typeof(DateTime)||pars[y].ParameterType == typeof(DateTime?))
                            {
                                code.AppendLine(string.Format((minimize ?
                                    "if(pars.{0}!='NULL'){{if(!(pars.{0} instanceof Date)){{pars.{0}=new Date(pars.{0});}}pars.{0}=Date.UTC(pars.{0}.getUTCFullYear(), pars.{0}.getUTCMonth(), pars.{0}.getUTCDate(), pars.{0}.getUTCHours(), pars.{0}.getUTCMinutes(), pars.{0}.getUTCSeconds());}}"
                                    : @"if (pars.{0} != 'NULL'){{
    if (!(pars.{0} instanceof Date)){{
        pars.{0} = new Date(pars.{0});
    }}
    pars.{0} = Date.UTC(pars.{0}.getUTCFullYear(), pars.{0}.getUTCMonth(), pars.{0}.getUTCDate(), pars.{0}.getUTCHours(), pars.{0}.getUTCMinutes(), pars.{0}.getUTCSeconds());
}}"),pars[y].Name));
                            }
                            code.AppendLine((minimize ? "" : "\t\t")+"url+='" + (y == 0 ? "" : "&") + pars[y].Name + "='+pars." + pars[y].Name + ".toString();");
                        }
                        sb.AppendLine(")){");
                        sb.Append(code.ToString());
                    }
                    sb.AppendLine("}");
                }
                sb.AppendLine(string.Format((minimize ? 
                    "else{{return null;}}var ret=$.ajax('{0}'+url,{{async:false,cache:false,type : 'SELECT'}}).responseText;var response=JSON.parse(ret);if(response.Backbone!=undefined){{_.extend(Backbone,response.Backbone);response=response.response;}}return response;}}}});"
                    :@"  else{{
        return null;
    }}
    var ret=$.ajax(
        '{0}'+url,{{
            async:false,
            cache:false,
            type : 'SELECT'
}}).responseText;
    var response = JSON.parse(ret);
    if(response.Backbone!=undefined){{
        _.extend(Backbone,response.Backbone);
        response=response.response;
    }}
return response;
}}}});"),urlRoot));

            }
            return sb.ToString();
        }

        #endregion
    }
}
