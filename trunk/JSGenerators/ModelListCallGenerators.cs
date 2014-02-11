using System;
using System.Collections.Generic;
using System.Text;
using Org.Reddragonit.BackBoneDotNet.Interfaces;
using System.Reflection;
using Org.Reddragonit.BackBoneDotNet.Attributes;

namespace Org.Reddragonit.BackBoneDotNet.JSGenerators
{
    internal class ModelListCallGenerators : IJSGenerator
    {
        #region IJSGenerator Members

        public string GenerateJS(Type modelType, string host, List<string> readOnlyProperties, List<string> properties, List<string> viewIgnoreProperties, bool hasUpdate, bool hasAdd, bool hasDelete)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("//Org.Reddragonit.BackBoneDotNet.JSGenerators.ModelListCallGenerators");
            foreach (MethodInfo mi in modelType.GetMethods(Constants.LOAD_METHOD_FLAGS))
            {
                if (mi.GetCustomAttributes(typeof(ModelListMethod), false).Length > 0)
                {
                    foreach (ModelListMethod mlm in mi.GetCustomAttributes(typeof(ModelListMethod), false))
                    {
                        if (mlm.Host == host || mlm.Host == "*")
                        {
                            StringBuilder sbCurParameters = new StringBuilder();
                            sbCurParameters.Append("function(){return {");
                            sb.Append(ModelNamespace.GetFullNameForModel(modelType, host) + " = _.extend(" + ModelNamespace.GetFullNameForModel(modelType, host) + ", {" + mi.Name + " : function(");
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
                                sb.AppendLine(
@"pageStartIndex = (pageStartIndex == undefined ? 0 : (pageStartIndex == null ? 0 : pageStartIndex));
pageSize = (pageSize == undefined ? 10 : (pageSize == null ? 10 : pageSize));
var ret = Backbone.Collection.extend({url:url+'"+(mlm.Path.Contains("?") ? "&" : "?")+@"PageStartIndex='+pageStartIndex+'&PageSize='+pageSize,
    CurrentParameters:" + sbCurParameters.ToString() + @",
    currentIndex : pageStartIndex*pageSize,
    currentPageSize : pageSize,
    parse : function(response){
        if(response.Backbone!=undefined){
            _.extend(Backbone,response.Backbone);
        }
        response = response.response;
        this.TotalPages = response.Pager.TotalPages;
        return response.response;
    },
    MoveToPage : function(pageNumber){
        if (pageNumber>=0 && pageNumber<this.TotalPages){
            this.currentIndex = pageNumber*this.currentPageSize;
" +
                                                                   (mlm.Path.Contains("?") ? 
                                                                   "\t\t\tthis.url = this.url.substring(0,this.url.indexOf('&PageStartIndex='))+'&PageStartIndex='+this.currentIndex+'&PageSize='+this.currentPageSize;" : 
                                                                   "\t\t\tthis.url = this.url.substring(0,this.url.indexOf('?'))+'?PageStartIndex='+this.currentIndex+'&PageSize='+this.currentPageSize;") +
@"          this.fetch();
        }
    },
    ChangePageSize : function(pageSize){
        this.currentPageSize = pageSize;
        this.MoveToPage(Math.floor(this.currentIndex/pageSize));
    },
    MoveToNextPage : function(){
        if(Math.floor(this.currentIndex/pageSize)+1<this.TotalPages){
            this.MoveToPage(Math.floor(this.currentIndex/pageSize)+1);
        }
    },
    MoveToPreviousPage : function(){
        if(Math.floor(this.currentIndex/pageSize)-1>=0){
            this.MoveToPage(Math.floor(this.currentIndex/pageSize)-1);
        }
    },");
                                if (mi.GetParameters().Length > 0)
                                {
                                    sb.Append("\tChangeParameters: function(");
                                    for (int x = 0; x < mi.GetParameters().Length - 3; x++)
                                    {
                                        sb.Append((x == 0 ? "" : ",") + mi.GetParameters()[x].Name);
                                    }
                                    sb.AppendLine("){"+urlCode);
                                    sb.AppendLine("url+='" + (mlm.Path.Contains("?") ? "&" : "?") + "PageStartIndex='+this.currentIndex+'&PageSize='+this.currentPageSize;");
                                    sb.AppendLine("this.CurrentParameters = " + sbCurParameters.ToString() + ";");
                                    sb.AppendLine(
@"      this.currentIndex=0;
        this.url=url;
        this.fetch();
},");
                                }
                                sb.AppendLine("\tmodel:" + ModelNamespace.GetFullNameForModel(modelType, host) + ".Model");
                                sb.AppendLine("});");
                            }
                            else
                            {
                                sb.AppendLine(
"var ret = Backbone.Collection.extend({url:url,CurrentParameters:" + sbCurParameters.ToString() + @",parse : function(response){
    if(response.Backbone!=undefined){
        _.extend(Backbone,response.Backbone);
        return response.response;
    }else{
        return response;
    }
},");
                                if (mi.GetParameters().Length > 0)
                                {
                                    sb.Append("\tChangeParameters: function(");
                                    for (int x = 0; x < mi.GetParameters().Length; x++)
                                    {
                                        sb.Append((x == 0 ? "" : ",") + mi.GetParameters()[x].Name);
                                    }
                                    sb.AppendLine("){"+urlCode);
                                    sb.AppendLine("this.CurrentParameters = " + sbCurParameters.ToString() + ";");
                                    sb.AppendLine(
@"      this.currentIndex=0;
        this.url=url;
        this.fetch();
},");
                                }
                                sb.AppendLine("model:" + ModelNamespace.GetFullNameForModel(modelType, host) + ".Model});");
                            }
                            sb.AppendLine("ret = new ret();");
                            sb.AppendLine("return ret;");
                            sb.AppendLine("}});");
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
