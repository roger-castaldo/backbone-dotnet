using System;
using System.Collections.Generic;
using System.Text;
using Org.Reddragonit.BackBoneDotNet.Interfaces;
using System.Reflection;
using Org.Reddragonit.BackBoneDotNet.Attributes;

namespace Org.Reddragonit.BackBoneDotNet.JSGenerators
{
    internal class StaticExposedMethodGenerator : IJSGenerator
    {
        internal static void AppendMethodCall(string urlRoot,string host,MethodInfo mi,ref StringBuilder sb){
            sb.Append(string.Format("\t{0}:function(", mi.Name));
            ParameterInfo[] pars = mi.GetParameters();
            for (int x = 0; x < pars.Length; x++)
                sb.Append(pars[x].Name + (x + 1 == pars.Length ? "" : ","));
            sb.Append("){var function_data = {};");
            foreach (ParameterInfo par in pars)
            {
                Type propType = par.ParameterType;
                bool array = false;
                if (propType.FullName.StartsWith("System.Nullable"))
                {
                    if (propType.IsGenericType)
                        propType = propType.GetGenericArguments()[0];
                    else
                        propType = propType.GetElementType();
                }
                if (propType.IsArray)
                {
                    array = true;
                    propType = propType.GetElementType();
                }
                else if (propType.IsGenericType)
                {
                    if (propType.GetGenericTypeDefinition() == typeof(List<>))
                    {
                        array = true;
                        propType = propType.GetGenericArguments()[0];
                    }
                }
                if (new List<Type>(propType.GetInterfaces()).Contains(typeof(IModel)))
                {
                    if (array)
                    {
                        sb.AppendLine(string.Format(@"function_data.{0}=[];
for(var x=0;x<{0}.length;x++){{
    function_data.{0}.push(({0}.at!=undefined ? {0}.at(x).id : {0}[x].id));
}}", par.Name));
                    }
                    else
                        sb.AppendLine(string.Format("function_data.{0} = {0}.id;", par.Name));
                }
                else
                    sb.AppendLine(string.Format("function_data.{0} = {0};", par.Name));
            }
            sb.AppendLine(string.Format(@"{2}$.ajax({{
            type:'{6}',
            url:'{0}/{5}{1}',
            processData:false,
            data:escape(JSON.stringify(function_data)),
            content_type:""application/json; charset=utf-8"",
            dataType:'json',
            async:false,
            cache:false
        }}){3};
        {4}
", new object[]{
            urlRoot,
            mi.Name,
            (mi.ReturnType == typeof(void) ? "" : "var ret = "),
            (mi.ReturnType == typeof(void) ? "" : ".responseText"),
            (mi.ReturnType==typeof(void) ? "" : @"var response = JSON.parse(ret);
    if(response.Backbone!=undefined){
        _.extend(Backbone,response.Backbone);
        response=response.response;
    }"),
      (mi.IsStatic ? "" : "'+this.id+'/"),
      (mi.IsStatic ? "SMETHOD" : "METHOD")
        }));
            if (mi.ReturnType != typeof(void))
            {
                Type propType = mi.ReturnType;
                bool array = false;
                if (propType.FullName.StartsWith("System.Nullable"))
                {
                    if (propType.IsGenericType)
                        propType = propType.GetGenericArguments()[0];
                    else
                        propType = propType.GetElementType();
                }
                if (propType.IsArray)
                {
                    array = true;
                    propType = propType.GetElementType();
                }
                else if (propType.IsGenericType)
                {
                    if (propType.GetGenericTypeDefinition() == typeof(List<>))
                    {
                        array = true;
                        propType = propType.GetGenericArguments()[0];
                    }
                }
                if (new List<Type>(propType.GetInterfaces()).Contains(typeof(IModel)))
                {
                    if (array)
                    {
                        sb.AppendLine(string.Format(@"          if({0}.Collection!=undefined){{
                ret = new {0}.Collection();
                for (var x=0;x<response.length;x++){{
                    ret.add(new {0}.Model({{'id':response[x].id}}));
                    ret.at(x).attributes=ret.at(x).parse(response[x]);
                }}
            }}else{{
                ret=[];
                for (var x=0;x<response.length;x++){{
                    ret.push(new {0}.Model({{'id':response[x].id}}));
                    ret[x].attributes=ret[x].parse(response[x]);
                }}
            }}
            response = ret;",
                                ModelNamespace.GetFullNameForModel(propType, host)));
                    }
                    else
                    {
                        sb.AppendLine(string.Format(@"ret = new {0}.Model({{id:response.id}});
ret.attributes = ret.parse(response);
response=ret;", ModelNamespace.GetFullNameForModel(propType, host)));
                    }
                }
                sb.AppendLine("return response;");
            }
            sb.AppendLine("},");
        }

        #region IJSGenerator Members

        public string GenerateJS(Type modelType, string host, List<string> readOnlyProperties, List<string> properties, List<string> viewIgnoreProperties, bool hasUpdate, bool hasAdd, bool hasDelete)
        {
            StringBuilder sb = new StringBuilder();
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
            sb.AppendFormat(
@"//Org.Reddragonit.BackBoneDotNet.JSGenerators.StaticExposedMethodGenerator
{0} = _.extend(true,{0}, {{",
                ModelNamespace.GetFullNameForModel(modelType, host));
            foreach (MethodInfo mi in modelType.GetMethods(BindingFlags.Public | BindingFlags.Static))
            {
                if (mi.GetCustomAttributes(typeof(ExposedMethod), false).Length > 0)
                    AppendMethodCall(urlRoot,host, mi, ref sb);
            }
            if (sb.ToString().EndsWith(","))
                sb.Length = sb.Length - 1;
            sb.AppendLine("});");
            return sb.ToString();
        }

        #endregion
    }
}
