using System;
using System.Collections.Generic;
using System.Text;
using Org.Reddragonit.BackBoneDotNet.Interfaces;
using System.Reflection;
using Org.Reddragonit.BackBoneDotNet.Attributes;

namespace Org.Reddragonit.BackBoneDotNet.JSGenerators
{
    /*
     * This generator generates the javascript code for a model list call
     * The function call object will exist as the path namespace.type.{FunctionName}
     */
    internal class ModelListCallGenerators : IJSGenerator
    {
        #region IJSGenerator Members

        public string GenerateJS(Type modelType, string host, List<string> readOnlyProperties, List<string> properties, List<string> viewIgnoreProperties, bool hasUpdate, bool hasAdd, bool hasDelete,bool minimize)
        {
            WrappedStringBuilder sb = new WrappedStringBuilder(minimize);
            if (!minimize)
                sb.AppendLine("//Org.Reddragonit.BackBoneDotNet.JSGenerators.ModelListCallGenerators");
            foreach (MethodInfo mi in modelType.GetMethods(Constants.LOAD_METHOD_FLAGS))
            {
                if (mi.GetCustomAttributes(typeof(ModelListMethod), false).Length > 0)
                {
                    foreach (ModelListMethod mlm in mi.GetCustomAttributes(typeof(ModelListMethod), false))
                    {
                        if (mlm.Host == host || mlm.Host == "*")
                        {
                            WrappedStringBuilder sbCurParameters = new WrappedStringBuilder(minimize);
                            sbCurParameters.Append((minimize ? "function(){return{":"function(){return {"));
                            sb.Append(string.Format((minimize ?
                                "{0}=_.extend({0},{{{1}:function(" 
                                : "{0} = _.extend({0}, {{{1}:function("),ModelNamespace.GetFullNameForModel(modelType, host),mi.Name));
                            for (int x = 0; x < (mlm.Paged ? mi.GetParameters().Length-3 : mi.GetParameters().Length); x++)
                            {
                                sb.Append((x == 0 ? "" : ",") + mi.GetParameters()[x].Name);
                                sbCurParameters.AppendLine((x == 0 ? "" : ",") + string.Format("'{0}':{0}", mi.GetParameters()[x].Name));
                            }
                            sbCurParameters.Append("};}");
                            if (mlm.Paged)
                            {
                                if (mi.GetParameters().Length != 3)
                                    sb.Append(",");
                                sb.Append("pageStartIndex,pageSize");
                            }
                            sb.AppendLine("){");
                            string urlCode = URLUtility.CreateJavacriptUrlCode(mlm, mi, modelType);
                            sb.Append(urlCode);
                            if (mlm.Paged)
                            {
                                sb.AppendLine(string.Format((minimize ? 
                                    "pageStartIndex=(pageStartIndex==undefined?0:(pageStartIndex==null?0:pageStartIndex));pageSize=(pageSize==undefined?10:(pageSize==null?10:pageSize));var ret=Backbone.Collection.extend({{url:url+'{0}PageStartIndex='+pageStartIndex+'&PageSize='+pageSize,CurrentParameters:{1},currentIndex:pageStartIndex*pageSize,currentPageSize:pageSize,CurrentPage:Math.floor(pageStartIndex/pageSize),parse:function(response){{if(response.Backbone!=undefined){{_.extend(Backbone,response.Backbone);}}response=response.response;this.TotalPages=response.Pager.TotalPages;return response.response;}},MoveToPage:function(pageNumber){{if(pageNumber>=0&&pageNumber<this.TotalPages){{this.currentIndex=pageNumber*this.currentPageSize;{2}this.fetch();this.CurrentPage=pageNumber;}}}},ChangePageSize:function(pageSize){{this.currentPageSize=pageSize;this.MoveToPage(Math.floor(this.currentIndex/pageSize));}},MoveToNextPage:function(){{if(Math.floor(this.currentIndex/this.currentPageSize)+1<this.TotalPages){{this.MoveToPage(Math.floor(this.currentIndex/this.currentPageSize)+1);}}}},MoveToPreviousPage:function(){{if(Math.floor(this.currentIndex/this.currentPageSize)-1>=0){{this.MoveToPage(Math.floor(this.currentIndex/this.currentPageSize)-1);}}}},"
                                    :@"pageStartIndex = (pageStartIndex == undefined ? 0 : (pageStartIndex == null ? 0 : pageStartIndex));
pageSize = (pageSize == undefined ? 10 : (pageSize == null ? 10 : pageSize));
var ret = Backbone.Collection.extend({{url:url+'{0}PageStartIndex='+pageStartIndex+'&PageSize='+pageSize,
    CurrentParameters:{1},
    currentIndex : pageStartIndex*pageSize,
    currentPageSize : pageSize,
    CurrentPage : Math.floor(pageStartIndex/pageSize),
    parse : function(response){{
        if(response.Backbone!=undefined){{
            _.extend(Backbone,response.Backbone);
        }}
        response = response.response;
        this.TotalPages = response.Pager.TotalPages;
        return response.response;
    }},
    MoveToPage : function(pageNumber){{
        if (pageNumber>=0 && pageNumber<this.TotalPages){{
            this.currentIndex = pageNumber*this.currentPageSize;
            {2}
            this.fetch();
            this.CurrentPage=pageNumber;
        }}
    }},
    ChangePageSize : function(pageSize){{
        this.currentPageSize = pageSize;
        this.MoveToPage(Math.floor(this.currentIndex/pageSize));
    }},
    MoveToNextPage : function(){{
        if(Math.floor(this.currentIndex/this.currentPageSize)+1<this.TotalPages){{
            this.MoveToPage(Math.floor(this.currentIndex/this.currentPageSize)+1);
        }}
    }},
    MoveToPreviousPage : function(){{
        if(Math.floor(this.currentIndex/this.currentPageSize)-1>=0){{
            this.MoveToPage(Math.floor(this.currentIndex/this.currentPageSize)-1);
        }}
    }},"),new object[]{
           (mlm.Path.Contains("?") ? "&" : "?"),
           sbCurParameters.ToString(),
           (mlm.Path.Contains("?") ? 
                (minimize ? "" : "\t\t\t")+"this.url = this.url.substring(0,this.url.indexOf('&PageStartIndex='))+'&PageStartIndex='+this.currentIndex+'&PageSize='+this.currentPageSize;" : 
                (minimize ? "" : "\t\t\t")+"this.url = this.url.substring(0,this.url.indexOf('?'))+'?PageStartIndex='+this.currentIndex+'&PageSize='+this.currentPageSize;")
       }));
                                if (mi.GetParameters().Length > 0)
                                {
                                    sb.Append((minimize ? "" : "\t")+"ChangeParameters: function(");
                                    for (int x = 0; x < mi.GetParameters().Length - 3; x++)
                                    {
                                        sb.Append((x == 0 ? "" : ",") + mi.GetParameters()[x].Name);
                                    }
                                    sb.AppendLine(string.Format((minimize ? 
                                        "){{{0}url+='{1}PageStartIndex='+this.currentIndex+'&PageSize='+this.currentPageSize;this.CurrentParameters={2};this.currentIndex=0;this.url=url;this.fetch();}},"
                                        :@"){{{0}
        url+='{1}PageStartIndex='+this.currentIndex+'&PageSize='+this.currentPageSize;
        this.CurrentParameters = {2};
        this.currentIndex=0;
        this.url=url;
        this.fetch();
}},"),new object[]{urlCode,(mlm.Path.Contains("?") ? "&" : "?"),sbCurParameters.ToString()}));
                                }
                                sb.AppendLine(string.Format((minimize ? 
                                    "model:{0}.Model}});"
                                    :@" model:{0}.Model
}});"),ModelNamespace.GetFullNameForModel(modelType, host)));
                            }
                            else
                            {
                                sb.AppendLine(string.Format((minimize ? 
                                    "var ret=Backbone.Collection.extend({{url:url,CurrentParameters:{0},parse : function(response){{if(response.Backbone!=undefined){{_.extend(Backbone,response.Backbone);return response.response;}}else{{return response;}}}},"
                                    :@" var ret = Backbone.Collection.extend({{url:url,CurrentParameters:{0},parse : function(response){{
    if(response.Backbone!=undefined){{
        _.extend(Backbone,response.Backbone);
        return response.response;
    }}else{{
        return response;
    }}
}},"),sbCurParameters.ToString()));
                                if (mi.GetParameters().Length > 0)
                                {
                                    sb.Append((minimize ? "" : "\t")+"ChangeParameters: function(");
                                    for (int x = 0; x < mi.GetParameters().Length; x++)
                                    {
                                        sb.Append((x == 0 ? "" : ",") + mi.GetParameters()[x].Name);
                                    }
                                    sb.AppendLine(string.Format((minimize ? 
                                        "){{{0}this.CurrentParameters={1};this.currentIndex=0;this.url=url;this.fetch();}}," 
                                        :@"){{{0}
        this.CurrentParameters = {1};
        this.currentIndex=0;
        this.url=url;
        this.fetch();
}},"),urlCode,sbCurParameters.ToString()));
                                }
                                sb.AppendLine("model:" + ModelNamespace.GetFullNameForModel(modelType, host) + ".Model});");
                            }
                            sb.AppendLine((minimize ? 
                                "ret=new ret();return ret;}});"
                                :@"ret = new ret();
    return ret;
}});"));
                            break;
                        }
                    }
                }
            }
            return sb.ToString();
        }

        #endregion
    }
}
