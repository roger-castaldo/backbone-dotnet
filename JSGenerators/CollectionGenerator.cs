using System;
using System.Collections.Generic;
using System.Text;
using Org.Reddragonit.BackBoneDotNet.Interfaces;
using Org.Reddragonit.BackBoneDotNet.Attributes;
using Org.Reddragonit.BackBoneDotNet.Properties;

namespace Org.Reddragonit.BackBoneDotNet.JSGenerators
{
    /*
     * This generator generates the javascript code for a model collection.
     * The collection object will exist as the path namespace.type.Collection
     */
    internal class CollectionGenerator : IJSGenerator
    {
        #region IJSGenerator Members

        public string GenerateJS(Type modelType, string host, List<string> readOnlyProperties, List<string> properties, List<string> viewIgnoreProperties, bool hasUpdate, bool hasAdd, bool hasDelete,bool minimize)
        {
            if (modelType.GetCustomAttributes(typeof(ModelBlockJavascriptGeneration), false).Length > 0)
            {
                if (((int)((ModelBlockJavascriptGeneration)modelType.GetCustomAttributes(typeof(ModelBlockJavascriptGeneration), false)[0]).BlockType & (int)ModelBlockJavascriptGenerations.Collection) == (int)ModelBlockJavascriptGenerations.Collection)
                    return "";
            }
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
            return string.Format((minimize ?
                @"{0}=_.extend(true,{0},{{{2}:Backbone.Collection.extend({{model:{3}.{4},parse:function(response){{return response;}},url:""{1}""}})}});"
                :@"//Org.Reddragonit.BackBoneDotNet.JSGenerators.CollectionGenerator
{0} = _.extend(true,{0},{{{2}: Backbone.Collection.extend({{
    model : {3}.{4},
    parse : function(response){{return response;}},
    url : ""{1}""
    }})
}});"),
                new object[]{
                    (RequestHandler.UseAppNamespacing ? "App.Collections" : ModelNamespace.GetFullNameForModel(modelType, host)),
                    (urlRoot.StartsWith("/") ? "" : "/") + urlRoot,
                    (RequestHandler.UseAppNamespacing ? modelType.Name : "Collection"),
                    (RequestHandler.UseAppNamespacing ? "App.Models" : ModelNamespace.GetFullNameForModel(modelType, host)),
                    (RequestHandler.UseAppNamespacing ? modelType.Name : "Model")
                });
        }

        #endregion
    }
}
