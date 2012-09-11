using System;
using System.Collections.Generic;
using System.Text;
using Org.Reddragonit.BackBoneDotNet.Interfaces;
using Org.Reddragonit.BackBoneDotNet.Attributes;

namespace Org.Reddragonit.BackBoneDotNet.JSGenerators
{
    /*
     * This generator generates the javascript code for a model collection.
     * The collection object will exist as the path namespace.type.Collection
     */
    internal class CollectionGenerator : IJSGenerator
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
            return "//Org.Reddragonit.BackBoneDotNet.JSGenerators.CollectionGenerator\n" +
                modelType.FullName + ".Collection = Backbone.Collection.extend({\n"
                +"\tmodel : " + modelType.FullName + ".Model,\n"
                +"\turl : \"" + (urlRoot.StartsWith("/") ? "" : "/") + urlRoot + "\"\n"
                +"});\n";
        }

        #endregion
    }
}
