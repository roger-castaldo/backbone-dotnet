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
                            sb.Append(ModelNamespace.GetFullNameForModel(modelType, host) + " = _.extend(" + ModelNamespace.GetFullNameForModel(modelType, host) + ", {" + mi.Name + " : function(");
                            for (int x = 0; x < (mlm.Paged ? mi.GetParameters().Length-3 : mi.GetParameters().Length); x++)
                            {
                                sb.Append((x == 0 ? "" : ",") + mi.GetParameters()[x].Name);
                            }
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
                                sb.AppendLine("pageStartIndex = (pageStartIndex == undefined ? 0 : (pageStartIndex == null ? 0 : pageStartIndex));");
                                sb.AppendLine("pageSize = (pageSize == undefined ? 0 : (pageSize == null ? 0 : pageSize));");
                                sb.AppendLine("var ret = Backbone.Collection.extend({url:url+'"+(mlm.Path.Contains("?") ? "&" : "?")+"PageStartIndex='+pageStartIndex+'&PageSize='+pageSize,");
                                sb.AppendLine("\tcurrentIndex : pageStartIndex*pageSize,");
                                sb.AppendLine("\tcurrentPageSize : pageSize,");
                                sb.AppendLine("\tparse : function(response){");
                                sb.AppendLine("\t\tif(response.Backbone!=undefined){");
                                sb.AppendLine("\t\t\t_.extend(Backbone,response.Backbone);");
                                sb.AppendLine("\t\t}");
                                sb.AppendLine("\t\tresponse = response.response;");
                                sb.AppendLine("\t\tthis.TotalPages = response.Pager.TotalPages;");
                                sb.AppendLine("\t\treturn response.response;");
                                sb.AppendLine("\t},");
                                sb.AppendLine("\tMoveToPage : function(pageNumber){");
                                sb.AppendLine("\t\tif (pageNumber>=0 && pageNumber<this.TotalPages){");
                                sb.AppendLine("\t\t\tthis.currentIndex = pageNumber*this.currentPageSize;");
                                if (mlm.Path.Contains("?"))
                                    sb.AppendLine("\t\t\tthis.url = this.url.substring(0,this.url.indexOf('&PageStartIndex='))+'&PageStartIndex='+this.currentIndex+'&PageSize='+this.currentPageSize;");
                                else
                                    sb.AppendLine("\t\t\tthis.url = this.url.substring(0,this.url.indexOf('?'))+'?PageStartIndex='+this.currentIndex+'&PageSize='+this.currentPageSize;");
                                sb.AppendLine("\t\t\tthis.fetch();");
                                sb.AppendLine("\t\t}");
                                sb.AppendLine("\t},");
                                sb.AppendLine("\tChangePageSize : function(pageSize){");
                                sb.AppendLine("\t\tthis.currentPageSize = pageSize;");
                                sb.AppendLine("\t\tthis.MoveToPage(Math.floor(this.currentIndex/pageSize));");
                                sb.AppendLine("\t},");
                                sb.AppendLine("\tMoveToNextPage : function(){");
                                sb.AppendLine("\t\tif(Math.floor(this.currentIndex/pageSize)+1<this.TotalPages){");
                                sb.AppendLine("\t\t\tthis.MoveToPage(Math.floor(this.currentIndex/pageSize)+1);");
                                sb.AppendLine("\t\t}");
                                sb.AppendLine("\t},");
                                sb.AppendLine("\tMoveToPreviousPage : function(){");
                                sb.AppendLine("\t\tif(Math.floor(this.currentIndex/pageSize)-1>=0){");
                                sb.AppendLine("\t\t\tthis.MoveToPage(Math.floor(this.currentIndex/pageSize)-1);");
                                sb.AppendLine("\t\t}");
                                sb.AppendLine("\t},");
                                if (mi.GetParameters().Length > 0)
                                {
                                    sb.Append("\tChangeParameters: function(");
                                    for (int x = 0; x < mi.GetParameters().Length - 3; x++)
                                    {
                                        sb.Append((x == 0 ? "" : ",") + mi.GetParameters()[x].Name);
                                    }
                                    sb.AppendLine("){");
                                    sb.AppendLine(urlCode);
                                    sb.AppendLine("url+='" + (mlm.Path.Contains("?") ? "&" : "?") + "PageStartIndex='+this.currentIndex+'&PageSize='+this.currentPageSize;");
                                    sb.AppendLine("\t\tthis.currentIndex=0;");
                                    sb.AppendLine("\t\tthis.url=url;");
                                    sb.AppendLine("\t\tthis.fetch();");
                                    sb.AppendLine("},");
                                }
                                sb.AppendLine("\tmodel:" + ModelNamespace.GetFullNameForModel(modelType, host) + ".Model");
                                sb.AppendLine("});");
                            }
                            else
                            {
                                sb.AppendLine("var ret = Backbone.Collection.extend({url:url,parse : function(response){");
                                sb.AppendLine("\tif(response.Backbone!=undefined){");
                                sb.AppendLine("\t\t_.extend(Backbone,response.Backbone);");
                                sb.AppendLine("\t\treturn response.response;");
                                sb.AppendLine("\t}else{");
                                sb.AppendLine("\treturn response;");
                                sb.AppendLine("\t}");
                                sb.AppendLine("},");
                                if (mi.GetParameters().Length > 0)
                                {
                                    sb.Append("\tChangeParameters: function(");
                                    for (int x = 0; x < mi.GetParameters().Length; x++)
                                    {
                                        sb.Append((x == 0 ? "" : ",") + mi.GetParameters()[x].Name);
                                    }
                                    sb.AppendLine("){");
                                    sb.AppendLine(urlCode);
                                    sb.AppendLine("\t\tthis.currentIndex=0;");
                                    sb.AppendLine("\t\tthis.url=url;");
                                    sb.AppendLine("\t\tthis.fetch();");
                                    sb.AppendLine("},");
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
