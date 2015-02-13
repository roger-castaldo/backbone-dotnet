using System;
using System.Collections.Generic;
using System.Text;
using Org.Reddragonit.BackBoneDotNet.Interfaces;
using System.Reflection;
using Org.Reddragonit.BackBoneDotNet.Attributes;

namespace Org.Reddragonit.BackBoneDotNet.JSGenerators
{
    /*
     * This generator generates the javascript code for a static exposed method
     * The function call object will exist as the path namespace.type.{FunctionName}
     * This also generates the non-static exposed methods
     * Those function calls will be accessibel from an instance of the Model().{FunctionName} as the load the model when the call is made.
     */
    internal class StaticExposedMethodGenerator : IJSGenerator
    {
        internal static void AppendMethodCall(string urlRoot,string host,MethodInfo mi,bool allowNull,ref WrappedStringBuilder sb,bool minimize){
            sb.Append(string.Format((minimize ? "" : "\t")+"{0}:function(", mi.Name));
            ParameterInfo[] pars = mi.GetParameters();
            for (int x = 0; x < pars.Length; x++)
                sb.Append(pars[x].Name + (x + 1 == pars.Length ? "" : ","));
            sb.Append((minimize ? "){var function_data={};":"){var function_data = {};"));
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
                        sb.AppendLine(string.Format((minimize ? 
                            "function_data.{0}=[];for(var x=0;x<{0}.length;x++){{function_data.{0}.push(({0}.at!=undefined?{0}.at(x).id:{0}[x].id));}}"
                            :@"function_data.{0}=[];
for(var x=0;x<{0}.length;x++){{
    function_data.{0}.push(({0}.at!=undefined ? {0}.at(x).id : {0}[x].id));
}}"), par.Name));
                    }
                    else
                        sb.AppendLine(string.Format((minimize ?"function_data.{0}={0}.id;" :"function_data.{0} = {0}.id;"), par.Name));
                }
                else
                    sb.AppendLine(string.Format((minimize ? "function_data.{0}={0};": "function_data.{0} = {0};"), par.Name));
            }
            sb.AppendLine(string.Format((minimize ?
"var response = $.ajax({{type:'{4}',url:'{0}/{3}{1}',processData:false,data:escape(JSON.stringify(function_data)),content_type:'application/json; charset=utf-8',dataType:'json',async:false,cache:false}});if(response.status==200){{{2}}}else{{throw new Exception(response.responseText);}}"
:@"var response = $.ajax({{
            type:'{4}',
            url:'{0}/{3}{1}',
            processData:false,
            data:escape(JSON.stringify(function_data)),
            content_type:'application/json; charset=utf-8',
            dataType:'json',
            async:false,
            cache:false
        }});
if (response.status==200){{
        {2}
}}else{{
    throw new Exception(response.responseText);
}}
"), new object[]{
            urlRoot,
            mi.Name,
            (mi.ReturnType==typeof(void) ? "" : (minimize ? "var ret=response.responseText; if(ret!=undefined){var response=JSON.parse(ret);if(response.Backbone!=undefined){_.extend(Backbone,response.Backbone);response=response.response;}" : @"var ret=response.responseText;
    if (ret!=undefined){
    var response = JSON.parse(ret);
    if(response.Backbone!=undefined){
        _.extend(Backbone,response.Backbone);
        response=response.response;
    }")),
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
                sb.AppendLine((minimize ? "if(response==null){":"if (response==null){"));
                if (!allowNull)
                    sb.AppendLine("throw \"A null response was returned by the server which is invalid.\";");
                else
                    sb.AppendLine("return response;");
                sb.AppendLine("}else{");
                if (new List<Type>(propType.GetInterfaces()).Contains(typeof(IModel)))
                {
                    if (array)
                    {
                        sb.AppendLine(string.Format((minimize ? 
                            "if({0}.Collection!=undefined){{ret = new {0}.Collection();for(var x=0;x<response.length;x++){{ret.add(new {0}.Model({{'id':response[x].id}}));ret.at(x).attributes=ret.at(x).parse(response[x]);}}}}else{{ret=[];for(var x=0;x<response.length;x++){{ret.push(new {0}.Model({{'id':response[x].id}}));ret[x].attributes=ret[x].parse(response[x]);}}}}response=ret;" 
                            : @"          if({0}.Collection!=undefined){{
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
            response = ret;"),
                                ModelNamespace.GetFullNameForModel(propType, host)));
                    }
                    else
                    {
                        sb.AppendLine(string.Format((minimize ? 
                            "ret=new {0}.Model({{id:response.id}});ret.attributes=ret.parse(response);response=ret;" 
                            :@"ret = new {0}.Model({{id:response.id}});
ret.attributes = ret.parse(response);
response=ret;"), ModelNamespace.GetFullNameForModel(propType, host)));
                    }
                }
                sb.AppendLine((minimize ? 
                    "}return response;}else{return null;}"
                    :@"}
return response;}else{return null;}"));
            }
            sb.AppendLine("},");
        }

        #region IJSGenerator Members

        public string GenerateJS(Type modelType, string host, List<string> readOnlyProperties, List<string> properties, List<string> viewIgnoreProperties, bool hasUpdate, bool hasAdd, bool hasDelete,bool minimize)
        {
            WrappedStringBuilder sb = new WrappedStringBuilder(minimize);
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
            sb.AppendFormat((minimize ?
                "{0}=_.extend(true,{0},{{"
                :@"//Org.Reddragonit.BackBoneDotNet.JSGenerators.StaticExposedMethodGenerator
{0} = _.extend(true,{0}, {{"),
                ModelNamespace.GetFullNameForModel(modelType, host));
            foreach (MethodInfo mi in modelType.GetMethods(BindingFlags.Public | BindingFlags.Static))
            {
                if (mi.GetCustomAttributes(typeof(ExposedMethod), false).Length > 0)
                    AppendMethodCall(urlRoot,host, mi,((ExposedMethod)mi.GetCustomAttributes(typeof(ExposedMethod), false)[0]).AllowNullResponse, ref sb,minimize);
            }
            while (sb.ToString().TrimEnd().EndsWith(","))
                sb.Length = sb.Length - 1;
            sb.AppendLine("});");
            return sb.ToString();
        }

        #endregion
    }
}
