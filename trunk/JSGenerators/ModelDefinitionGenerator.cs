using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using Org.Reddragonit.BackBoneDotNet.Interfaces;
using Org.Reddragonit.BackBoneDotNet.Attributes;
using Org.Reddragonit.BackBoneDotNet;

namespace Org.Reddragonit.BackBoneDotNet.JSGenerators
{
    /*
     * This generator is used to generate the model definition code.
     * it will be at the path namespace.type.Model
     */
    internal class ModelDefinitionGenerator :IJSGenerator
    {
        private void _AppendDefaults(Type modelType,List<string> properties,StringBuilder sb)
        {
            if (modelType.GetConstructor(Type.EmptyTypes) != null)
            {
                sb.AppendLine("\tdefaults:{");
                object obj = modelType.GetConstructor(Type.EmptyTypes).Invoke(new object[0]);
                if (obj != null)
                {
                    foreach (string propName in properties)
                    {
                        if (propName != "id")
                        {
                            object pobj = modelType.GetProperty(propName).GetValue(obj, new object[0]);
                            sb.AppendLine("\t\t" + propName + ": " + (pobj == null ? "null" : JSON.JsonEncode(pobj)) + (properties.IndexOf(propName) == properties.Count - 1 ? "" : ","));
                        }
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
            sb.AppendLine("\tsave : function(key, value, options){return false;},");
        }

        private void _AppendBlockAdd(StringBuilder sb)
        {
            sb.AppendLine("\tsave : function(key, value, options){if (!this.isNew()){this._save(key,value,options);}else{return false;}},");
        }

        private void _AppendBlockUpdate(StringBuilder sb)
        {
            sb.AppendLine("\tsave : function(key, value, options){if (this.isNew()){this._save(key,value,options);}else{return false;}},");
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

        private void _AppendValidate(Type modelType, List<string> properties, StringBuilder sb)
        {
            bool add = false;
            foreach (string str in properties)
            {
                if (modelType.GetProperty(str).GetCustomAttributes(typeof(ModelRequiredField),false).Length > 0
                    || modelType.GetProperty(str).GetCustomAttributes(typeof(ModelFieldValidationRegex), false).Length > 0)
                {
                    add = true;
                    break;
                }
            }
            if (add)
            {
                sb.AppendLine("\tvalidate : function(attrs) {");
                sb.AppendLine("\t\tvar atts = this.attributes;");
                sb.AppendLine("\t\t_.extend(atts,attrs);");
                sb.AppendLine("\t\tvar errors = new Array();");
                foreach (string str in properties)
                {
                    if (modelType.GetProperty(str).GetCustomAttributes(typeof(ModelRequiredField), false).Length > 0)
                    {
                        ModelRequiredField mrf = (ModelRequiredField)modelType.GetProperty(str).GetCustomAttributes(typeof(ModelRequiredField), false)[0];
                        sb.AppendLine("\t\tif (atts." + str + "==null || atts." + str + "==undefined){");
                        sb.AppendLine("\t\t\terrors.push({field:'" + str + "',error:Backbone.TranslateValidationError('" + mrf.ErrorMessageName + "')});");
                        sb.AppendLine("\t\t}");
                    }
                    else if (modelType.GetProperty(str).GetCustomAttributes(typeof(ModelFieldValidationRegex), false).Length > 0)
                    {
                        ModelFieldValidationRegex mfvr = (ModelFieldValidationRegex)modelType.GetProperty(str).GetCustomAttributes(typeof(ModelFieldValidationRegex), false)[0];
                        sb.AppendLine("\t\tif (!new RegExp('"+mfvr.Regex.Replace("'","\'")+"').test((atts." + str + "==null || atts." + str + "==undefined ? '' : atts."+str+"))){");
                        sb.AppendLine("\t\t\terrors.push({field:'" + str + "',error:Backbone.TranslateValidationError('" + mfvr.ErrorMessageName + "')});");
                        sb.AppendLine("\t\t}");
                    }
                }
                sb.AppendLine("\t\tthis.errors = errors;");
                sb.AppendLine("\t\tif (errors.length>0){return errors;}");
                sb.AppendLine("},");
            }
        }

        private void _AppendParse(Type modelType,string host, List<string> properties,List<string> readOnlyProperties, StringBuilder sb)
        {
            bool add = false;
            foreach (string str in properties)
            {
                Type propType = modelType.GetProperty(str).PropertyType;
                if (propType.FullName.StartsWith("System.Nullable"))
                {
                    if (propType.IsGenericType)
                        propType = propType.GetGenericArguments()[0];
                    else
                        propType = propType.GetElementType();
                }
                if (propType.IsArray)
                    propType = propType.GetElementType();
                else if (propType.IsGenericType)
                {
                    if (propType.GetGenericTypeDefinition() == typeof(List<>))
                        propType = propType.GetGenericArguments()[0];
                }
                if (new List<Type>(propType.GetInterfaces()).Contains(typeof(IModel)) || (propType == typeof(DateTime)))
                {
                    add = true;
                    break;
                }
                else if (modelType.GetProperty(str).GetCustomAttributes(typeof(ReadOnlyModelProperty), false).Length > 0)
                {
                    add = true;
                    break;
                }
            }
            if (add)
            {
                StringBuilder jsonb = new StringBuilder();
                string lazyLoads = "";
                jsonb.AppendLine("\ttoJSON : function(){");
                jsonb.AppendLine("\t\tvar attrs = {};");

                sb.AppendLine("\tparse: function(response) {");
                sb.AppendLine("\t\tvar attrs = {};");
                sb.AppendLine("\t\tif(response.Backbone!=undefined){");
                sb.AppendLine("\t\t\t_.extend(Backbone,response.Backbone);");
                sb.AppendLine("\t\t\tresponse=response.response;");
                sb.AppendLine("\t\t}");
                sb.AppendLine("\t\tif (response!=true){");

                foreach (string str in properties)
                {
                    bool isReadOnly = modelType.GetProperty(str).GetCustomAttributes(typeof(ReadOnlyModelProperty), false).Length > 0;
                    Type propType = modelType.GetProperty(str).PropertyType;
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
                        bool isLazy = modelType.GetProperty(str).GetCustomAttributes(typeof(ModelPropertyLazyLoadExternalModel), false).Length > 0;
                        if (isLazy)
                            lazyLoads += ",'" + str + "'";
                        sb.AppendLine("\t\tif (response." + str + " != undefined){");
                        if (array)
                        {
                            sb.AppendLine("\t\t\tattrs." + str + " = [];");
                            sb.AppendLine("\t\t\tfor (x in response." + str + "){");
                            sb.AppendLine("\t\t\t\tattrs." + str + ".push(new " + ModelNamespace.GetFullNameForModel(propType, host) + ".Model({'id':response." + str + "[x].id}));");
                            sb.AppendLine("\t\t\t\tattrs." + str + "[x].attributes=attrs." + str + "[x].parse(response." + str + "[x]);");
                            sb.AppendLine("\t\t\t}");
                            if (isReadOnly)
                                jsonb.AppendLine("if (this.isNew()){");
                            jsonb.AppendLine("\t\t\tif(!_.isEqual(this.attributes['" + str + "'],this._previousAttributes['" + str + "'])){");
                            jsonb.AppendLine("\t\t\t\tif(this.attributes['" + str + "']!=null){");
                            jsonb.AppendLine("\t\t\t\t\tattrs." + str + " = [];");
                            jsonb.AppendLine("\t\t\t\t\tfor(x in this.attributes['" + str + "']){");
                            jsonb.AppendLine("\t\t\t\t\t\tattrs." + str + ".push({id:this.attributes['" + str + "'][x].id});");
                            jsonb.AppendLine("\t\t\t\t\t}");
                            jsonb.AppendLine("\t\t\t\t}");
                            jsonb.AppendLine("\t\t\t}");
                            if (isReadOnly)
                                jsonb.AppendLine("}");
                        }
                        else
                        {
                            sb.AppendLine("\t\t\tattrs." + str + " = new " + ModelNamespace.GetFullNameForModel(propType, host) + ".Model({'id':response." + str + ".id});");
                            sb.AppendLine("\t\t\tattrs." + str + ".attributes=attrs." + str + ".parse(response." + str + ");");
                            if (isReadOnly)
                                jsonb.AppendLine("if (this.isNew()){");
                            jsonb.AppendLine("\t\tif(!_.isEqual(this.attributes['" + str + "'],this._previousAttributes['" + str + "'])){");
                            jsonb.AppendLine("\t\t\tif(this.attributes['" + str + "']!=null){");
                            jsonb.AppendLine("\t\t\t\tattrs." + str + " = {id : this.attributes['" + str + "'].id};");
                            jsonb.AppendLine("\t\t\t}");
                            jsonb.AppendLine("\t\t}");
                            if (isReadOnly)
                                jsonb.AppendLine("}");
                        } 
                        sb.AppendLine("\t\t}");
                    }
                    else
                    {
                        sb.AppendLine("\t\tif (response." + str + " != undefined){");
                        if (propType == typeof(DateTime))
                            sb.AppendLine("\t\tattrs." + str + " = new Date(response." + str + ");");
                        else
                            sb.AppendLine("\t\tattrs." + str + " = response." + str + ";");
                        sb.AppendLine("\t\t}");
                        if (isReadOnly)
                            jsonb.AppendLine("if (this.isNew()){");
                        jsonb.AppendLine("\t\tattrs." + str + " = this.attributes['" + str + "'];");
                        if (isReadOnly)
                            jsonb.AppendLine("}");
                    }
                }
                sb.AppendLine("\t\t}");
                sb.AppendLine("\t\treturn attrs;");
                sb.AppendLine("\t},");
                sb.Append(jsonb.ToString());
                sb.AppendLine("\t\treturn attrs;");
                sb.AppendLine("\t},");
                if (lazyLoads.Length > 0)
                {
                    sb.AppendLine("\tLazyLoadAttributes : [" + lazyLoads.Substring(1) + "],");
                }
            }
            else
            {
                sb.AppendLine("\tparse: function(response) {");
                sb.AppendLine("\t\tvar attrs = {};");
                sb.AppendLine("\t\tif(response.Backbone!=undefined){");
                sb.AppendLine("\t\t\t_.extend(Backbone,response.Backbone);");
                sb.AppendLine("\t\t\tresponse=response.response;");
                sb.AppendLine("\t\t}");
                sb.AppendLine("\t\tattrs = response;");
                sb.AppendLine("\t\treturn attrs;");
                sb.AppendLine("\t},");
            }
        }

        #region IJSGenerator Members

        public string GenerateJS(Type modelType, string host, List<string> readOnlyProperties, List<string> properties, List<string> viewIgnoreProperties, bool hasUpdate, bool hasAdd, bool hasDelete)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("//Org.Reddragonit.BackBoneDotNet.JSGenerators.ModelDefinitionGenerator");
            sb.AppendLine(ModelNamespace.GetFullNameForModel(modelType, host) + " = _.extend(" + ModelNamespace.GetFullNameForModel(modelType, host) + ", {Model : Backbone.Model.extend({");
            sb.AppendLine("\tinitialize : function() {");
            sb.AppendLine("\t\tif (this._revertReadonlyFields != undefined){");
            sb.AppendLine("\t\t\tthis.on(\"change\",this._revertReadonlyFields);");
            sb.AppendLine("\t\t}");
            sb.AppendLine("\t},");
            _AppendDefaults(modelType,properties,sb);
            if (!hasDelete)
                _AppendBlockDestroy(sb);
            if (!hasAdd && !hasUpdate)
                _AppendBlockSave(sb);
            else if (!hasAdd)
                _AppendBlockAdd(sb);
            else if (!hasUpdate)
                _AppendBlockUpdate(sb);
            _AppendReadonly(readOnlyProperties, sb);
            _AppendValidate(modelType, properties, sb);
            _AppendParse(modelType,host, properties,readOnlyProperties, sb);
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
            sb.AppendLine("\turlRoot : \"" + urlRoot + "\"})});");
            return sb.ToString();
        }
        #endregion
    }
}
