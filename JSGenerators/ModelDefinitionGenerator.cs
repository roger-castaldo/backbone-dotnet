using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using Org.Reddragonit.BackBoneDotNet.Interfaces;
using Org.Reddragonit.BackBoneDotNet.Attributes;
using Org.Reddragonit.BackBoneDotNet;

namespace Org.Reddragonit.BackBoneDotNet.JSGenerators
{
    internal class ModelDefinitionGenerator :IJSGenerator
    {
        private void _AppendDefaults(Type modelType,List<string> properties,StringBuilder sb)
        {
            if (modelType.GetConstructor(Type.EmptyTypes) != null)
            {
                sb.AppendLine("\tdefaults:{");
                object obj = modelType.GetConstructor(Type.EmptyTypes).Invoke(new object[0]);
                foreach (string propName in properties)
                {
                    if (obj != null)
                    {
                        object pobj = modelType.GetProperty(propName).GetValue(obj, new object[0]);
                        sb.AppendLine("\t\t"+propName+": "+(pobj == null ? "null" : JSON.JsonEncode(pobj))+(properties.IndexOf(propName)==properties.Count-1 ? "" : ","));
                    }
                }
                sb.AppendLine("\t},");
            }
        }

        private void _AppendBlockDestroy(StringBuilder sb)
        {
            sb.AppendLine("\tdestroy : function(options){return false;},");
        }

        private void _AppendBlockSave(StringBuilder sb)
        {
            sb.AppendLine("\tsave : function(attributes,options){return false;}");
        }

        private void _AppendBlockAdd(StringBuilder sb)
        {
            sb.AppendLine("\tsave : function(attributes,options){if (!this.isNew()){Backbone.Model.save(attributes,options);}else{return false;}},");
        }

        private void _AppendBlockUpdate(StringBuilder sb)
        {
            sb.AppendLine("\tsave : function(attributes,options){if (this.isNew()){Backbone.Model.save(attributes,options);}else{return false;}},");
        }

        private void _AppendReadonly(List<string> readOnlyProperties,StringBuilder sb){
            if (readOnlyProperties.Count > 0)
            {
                sb.AppendLine("\t_revertReadonlyFields : function(){");
                foreach (string str in readOnlyProperties)
                {
                    sb.AppendLine("\t\tif (this.changedAttributes."+str+" != this.previousAttributes."+str+"){");
                    sb.AppendLine("\t\t\tthis.set({"+str+":this.previousAttributes."+str+"});");
                    sb.AppendLine("\t\t}");
                }
                sb.AppendLine("\t},");
            }
        }

        #region IJSGenerator Members

        public string GenerateJS(Type modelType, string host, List<string> readOnlyProperties, List<string> properties)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("//Org.Reddragonit.BackBoneDotNet.JSGenerators.ModelDefinitionGenerator");
            sb.AppendLine(modelType.FullName+".Model = Backbone.Model.extend({");
            sb.AppendLine("\tinitialize : function() {");
            sb.AppendLine("\t\tif (this._revertReadonlyFields != undefined){");
            sb.AppendLine("\t\t\tthis.on(\"change\",this._revertReadonlyFields);");
            sb.AppendLine("\t\t}");
            sb.AppendLine("\t},");
            _AppendDefaults(modelType,properties,sb);
            bool hasAdd = true;
            bool hasUpdate = true;
            bool hasDelete = true;
            if (modelType.GetCustomAttributes(typeof(ModelBlockActions), false).Length > 0)
            {
                ModelBlockActions mba = (ModelBlockActions)modelType.GetCustomAttributes(typeof(ModelBlockActions), false)[0];
                hasAdd = ((int)mba.Type & (int)ModelActionTypes.Add) == 0;
                hasUpdate = ((int)mba.Type & (int)ModelActionTypes.Edit) == 0;
                hasDelete = ((int)mba.Type & (int)ModelActionTypes.Delete) == 0;
            }
            if (!hasDelete)
                _AppendBlockDestroy(sb);
            if (!hasAdd && !hasUpdate)
                _AppendBlockSave(sb);
            else if (!hasAdd)
                _AppendBlockAdd(sb);
            else if (!hasUpdate)
                _AppendBlockUpdate(sb);
            _AppendReadonly(readOnlyProperties, sb);
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
            sb.AppendLine("\turlRoot : \"" + urlRoot + "\"});");
            return sb.ToString();
        }

        #endregion
    }
}
