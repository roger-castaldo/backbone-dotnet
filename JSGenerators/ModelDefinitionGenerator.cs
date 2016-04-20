using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using Org.Reddragonit.BackBoneDotNet.Interfaces;
using Org.Reddragonit.BackBoneDotNet.Attributes;
using Org.Reddragonit.BackBoneDotNet;
using Org.Reddragonit.BackBoneDotNet.Properties;

namespace Org.Reddragonit.BackBoneDotNet.JSGenerators
{
    /*
     * This generator is used to generate the model definition code.
     * it will be at the path namespace.type.Model
     */
    internal class ModelDefinitionGenerator :IJSGenerator
    {
        private void _AppendDefaults(Type modelType,List<string> properties,WrappedStringBuilder sb,bool minimize)
        {
            if (modelType.GetConstructor(Type.EmptyTypes) != null)
            {
                sb.AppendLine((minimize ? "" : "\t")+"defaults:{");
                object obj = modelType.GetConstructor(Type.EmptyTypes).Invoke(new object[0]);
                if (obj != null)
                {
                    WrappedStringBuilder sbProps = new WrappedStringBuilder(minimize);
                    foreach (string propName in properties)
                    {
                        if (propName != "id")
                        {
                            object pobj = modelType.GetProperty(propName).GetValue(obj, new object[0]);
                            sbProps.AppendLine((minimize ? "" : "\t\t") + propName + ":" + (pobj == null ? "null" : JSON.JsonEncode(pobj)) + (properties.IndexOf(propName) == properties.Count - 1 ? "" : ","));
                        }
                    }
                    sb.Append(sbProps.ToString().TrimEnd(",\r\n".ToCharArray()));
                }
                sb.AppendLine((minimize ? "" : "\t")+"},");
            }
        }

        private void _AppendBlockDestroy(WrappedStringBuilder sb,bool minimize)
        {
            sb.AppendLine((minimize ? "destroy:function(options){return false;}," : "\tdestroy : function(options){return false;},"));
        }

        private void _AppendBlockSave(WrappedStringBuilder sb, bool minimize)
        {
            sb.AppendLine((minimize ? "save:function(key,value,options){return false;}," : "\tsave : function(key, value, options){return false;},"));
        }

        private void _AppendBlockAdd(WrappedStringBuilder sb,bool minimize)
        {
            sb.AppendLine((minimize ? "save:function(key,value,options){if(!this.isNew()){this._save(key,value,options);}else{return false;}}," : "\tsave : function(key, value, options){if (!this.isNew()){this._save(key,value,options);}else{return false;}},"));
        }

        private void _AppendBlockUpdate(WrappedStringBuilder sb,bool minimize)
        {
            sb.AppendLine((minimize ? "save:function(key,value,options){if(this.isNew()){this._save(key,value,options);}else{return false;}}," : "\tsave : function(key, value, options){if (this.isNew()){this._save(key,value,options);}else{return false;}},"));
        }

        private void _AppendReadonly(List<string> readOnlyProperties,WrappedStringBuilder sb,bool minimize){
            if (readOnlyProperties.Count > 0)
            {
                sb.AppendLine((minimize ? "_revertReadonlyFields:function(){" : "\t_revertReadonlyFields : function(){"));
                foreach (string str in readOnlyProperties)
                {
                    sb.AppendFormat((minimize ? 
                        "if(this.changedAttributes.{0}!=this.previousAttributes.{0}){{this.set({{{0}:this.previousAttributes.{0}}});}}"
                        :@"      if (this.changedAttributes.{0} != this.previousAttributes.{0}){{
            this.set({{{0}:this.previousAttributes.{0}}});
        }}"),str);
                }
                sb.AppendLine((minimize ? "" : "\t")+"},");
                sb.AppendLine((minimize ? 
                    "_save:function(key,val,options){if(key==null||typeof key==='object'){attrs=key;options=val;}else{(attrs={})[key]=val;}if(!this.isNew()){"
                    :@"_save : function (key, val, options) {
        if (key == null || typeof key === 'object') {
            attrs = key;
            options = val;
        } else {
            (attrs = {})[key] = val;
        }
        if (!this.isNew()){"));
                foreach (string str in readOnlyProperties)
                    sb.AppendLine(string.Format((minimize ? "if(attrs.{0}!=undefined){{delete attrs.{0};}}" 
                        :"if (attrs.{0}!=undefined){{delete attrs.{0};}}"), str));
        sb.AppendLine((minimize ? 
"}options=_.extend({validate:true},options);this._baseSave(attrs, options);},"
:@"}
        options = _.extend({ validate: true }, options);
        this._baseSave(attrs, options);
    },"));
            }
        }

        private void _AppendValidate(Type modelType, List<string> properties, WrappedStringBuilder sb,bool minimize)
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
                sb.AppendLine((minimize ? 
                    "validate:function(attrs){var atts=this.attributes;_.extend(atts,attrs);var errors = new Array();"
                    :@"  validate : function(attrs) {
        var atts = this.attributes;
        _.extend(atts,attrs);
        var errors = new Array();"));
                foreach (string str in properties)
                {
                    if (modelType.GetProperty(str).GetCustomAttributes(typeof(ModelRequiredField), false).Length > 0)
                    {
                        ModelRequiredField mrf = (ModelRequiredField)modelType.GetProperty(str).GetCustomAttributes(typeof(ModelRequiredField), false)[0];
                        sb.AppendFormat((minimize ? 
                            "if(atts.{0}==null||atts.{0}==undefined){{errors.push({{field:'{0}',error:Backbone.TranslateValidationError('{1}')}});}}"
                            :@"      if (atts.{0}==null || atts.{0}==undefined){{
            errors.push({{field:'{0}',error:Backbone.TranslateValidationError('{1}')}});
        }}"),str,mrf.ErrorMessageName);
                    }
                    if (modelType.GetProperty(str).GetCustomAttributes(typeof(ModelFieldValidationRegex), false).Length > 0)
                    {
                        ModelFieldValidationRegex mfvr = (ModelFieldValidationRegex)modelType.GetProperty(str).GetCustomAttributes(typeof(ModelFieldValidationRegex), false)[0];
                        sb.AppendFormat((minimize ? 
                            "if(!new RegExp('{0}').test((atts.{1}==null||atts.{1}==undefined?'':atts.{1}))){{errors.push({{field:'{1}',error:Backbone.TranslateValidationError('{2}')}});}}"
                            :@"      if (!new RegExp('{0}').test((atts.{1}==null || atts.{1}==undefined ? '' : atts.{1}))){{
            errors.push({{field:'{1}',error:Backbone.TranslateValidationError('{2}')}});
        }}"), mfvr.Regex.Replace("'", "\'"),str,mfvr.ErrorMessageName);
                    }
                }
                sb.AppendLine((minimize ? 
                    "this.errors=errors;if(errors.length>0){return errors;}},"
                    :@"      this.errors = errors;
        if (errors.length>0){return errors;}
},"));
            }
        }

        private void _AppendParse(Type modelType,string host, List<string> properties,List<string> readOnlyProperties, WrappedStringBuilder sb,bool minimize)
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
                else if (readOnlyProperties.Contains(str))
                {
                    add = true;
                    break;
                }
            }
            if (add)
            {
                WrappedStringBuilder jsonb = new WrappedStringBuilder(minimize);
                string lazyLoads = "";
                jsonb.AppendLine((minimize ? 
                    "toJSON:function(){var attrs={};this._changedFields=(this._changedFields==undefined?[]:this._changedFields);"
                    :@"  toJSON : function(){
        var attrs = {};
        this._changedFields = (this._changedFields == undefined ? [] : this._changedFields);"));

                sb.AppendLine((minimize ? 
                    "parse:function(response){var attrs={};this._origAttributes=(this._origAttributes==undefined?{}:this._origAttributes);if(response!=true){"
                    :@"  parse: function(response) {
        var attrs = {};
        this._origAttributes = (this._origAttributes==undefined ? {} : this._origAttributes);
        if (response!=true){"));

                foreach (string str in properties)
                {
                    bool isReadOnly = modelType.GetProperty(str).GetCustomAttributes(typeof(ReadOnlyModelProperty), false).Length > 0
                        || !modelType.GetProperty(str).CanWrite;
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
                        sb.AppendLine(string.Format((minimize ? "if(response.{0}!=undefined){{" : "\t\tif (response.{0} != undefined){{"),str));
                        if (array)
                        {
                            sb.AppendFormat((minimize ?
                                @"if({0}.{2}!=undefined){{attrs.{1} = new {0}.{2}();for (var x=0;x<response.{1}.length;x++){{attrs.{1}.add(new {3}.{4}({{'id':response.{1}[x].id}}));attrs.{1}.at(x).attributes=attrs.{1}.at(x).parse(response.{1}[x]);}}}}else{{attrs.{1}=[];for (var x=0;x<response.{1}.length;x++){{attrs.{1}.push(new {3}.{4}({{'id':response.{1}[x].id}}));attrs.{1}[x].attributes=attrs.{1}[x].parse(response.{1}[x]);}}}}" 
                                :@"          if({0}.{2}!=undefined){{
                attrs.{1} = new {0}.{2}();
                for (var x=0;x<response.{1}.length;x++){{
                    attrs.{1}.add(new {3}.{4}({{'id':response.{1}[x].id}}));
                    attrs.{1}.at(x).attributes=attrs.{1}.at(x).parse(response.{1}[x]);
                }}
            }}else{{
                attrs.{1}=[];
                for (var x=0;x<response.{1}.length;x++){{
                    attrs.{1}.push(new {3}.{4}({{'id':response.{1}[x].id}}));
                    attrs.{1}[x].attributes=attrs.{1}[x].parse(response.{1}[x]);
                }}
            }}"),new object[]{
                                (RequestHandler.UseAppNamespacing ? "App.Collections" : ModelNamespace.GetFullNameForModel(propType, host)),
                                str,
                                (RequestHandler.UseAppNamespacing ? propType.Name : "Collection"),
                                (RequestHandler.UseAppNamespacing ? "App.Models" : ModelNamespace.GetFullNameForModel(propType, host)),
                                (RequestHandler.UseAppNamespacing ? propType.Name : "Model")
               });
                            if (isReadOnly)
                                jsonb.AppendLine((minimize ? "if(this.isNew()){" : "if (this.isNew()){"));
                            jsonb.AppendFormat((minimize ?
                                "if(this._changedFields.indexOf('{0}')>=0||this.isNew()){{attrs.{0}=[];if(this.attributes['{0}']!=null){{for(var x=0;x<this.attributes['{0}'].length;x++){{if(this.attributes['{0}'].at!=undefined){{attrs.{0}.push({{id:this.attributes['{0}'].at(x).id}});}}else{{attrs.{0}.push({{id:this.attributes['{0}'][x].id}});}}}}}}}}"
                                :@"           if(this._changedFields.indexOf('{0}')>=0||this.isNew()){{
                    attrs.{0} = [];
                    if (this.attributes['{0}']!=null){{
                        for(var x=0;x<this.attributes['{0}'].length;x++){{
                            if(this.attributes['{0}'].at!=undefined){{
                                attrs.{0}.push({{id:this.attributes['{0}'].at(x).id}});
                            }}else{{
                                attrs.{0}.push({{id:this.attributes['{0}'][x].id}});
                            }}
                        }}
                    }}
            }}"),str);
                            if (isReadOnly)
                                jsonb.AppendLine("}");
                        }
                        else
                        {
                            sb.AppendFormat((minimize ?
                                "attrs.{0}=new {1}.{2}({{'id':response.{0}.id}});attrs.{0}.attributes=attrs.{0}.parse(response.{0});"
                                :@"          attrs.{0} = new {1}.{2}({{'id':response.{0}.id}});
            attrs.{0}.attributes=attrs.{0}.parse(response.{0});"),new object[]{
                                                                    str, 
                                                                    (RequestHandler.UseAppNamespacing ? "App.Models" : ModelNamespace.GetFullNameForModel(propType, host)),
                                                                    (RequestHandler.UseAppNamespacing ? propType.Name : "Model")
                                                                });
                            if (isReadOnly)
                                jsonb.AppendLine((minimize? "if(this.isNew()){": "if (this.isNew()){"));
                            jsonb.AppendFormat((minimize ?
                                "if(this._changedFields.indexOf('{0}')>=0||this.isNew()){{if(this.attributes['{0}']!=null){{attrs.{0}={{id:this.attributes['{0}'].id}};}}else{{attrs.{0}=null;}}}}"
                                :@"      if(this._changedFields.indexOf('{0}')>=0||this.isNew()){{
            if (this.attributes['{0}']!=null){{
                attrs.{0} = {{id : this.attributes['{0}'].id}};
            }}else{{
                attrs.{0} = null;
            }}
        }}"),str);
                            if (isReadOnly)
                                jsonb.AppendLine("}");
                        } 
                        sb.AppendLine((minimize ? "" : "\t\t")+"}");
                    }
                    else
                    {
                        sb.AppendLine(string.Format((minimize ? "if(response.{0}!=undefined){{" : "\t\tif (response.{0} != undefined){{"),str));
                        if (propType == typeof(DateTime))
                            sb.AppendLine(string.Format((minimize ? "attrs.{0}=new Date(response.{0});" :"\t\tattrs.{0} = new Date(response.{0});"), str));
                        else
                            sb.AppendLine(string.Format((minimize ? "attrs.{0}=response.{0};":"\t\tattrs.{0} = response.{0};"),str));
                        sb.AppendLine((minimize ? "" : "\t\t")+"}");
                        if (isReadOnly)
                            jsonb.AppendLine((minimize ? "if(this.isNew()){":"if (this.isNew()){"));
                        jsonb.AppendFormat((minimize ?
                            "if(this._changedFields.indexOf('{0}')>=0||this.isNew()){{attrs.{0}=this.attributes['{0}'];}}"
                            :@"      if(this._changedFields.indexOf('{0}')>=0||this.isNew()){{
                attrs.{0} = this.attributes['{0}'];
        }}"), str);
                        if (isReadOnly)
                            jsonb.AppendLine("}");
                    }
                }
                sb.AppendFormat((minimize ? 
                    "}}return attrs;}},{0}return attrs;}},"
                    :@"      }}
        return attrs;
    }},{0}
        return attrs;
    }},"), jsonb.ToString());
                if (lazyLoads.Length > 0)
                {
                    sb.AppendLine(string.Format((minimize ? "LazyLoadAttributes:[{0}],": "\tLazyLoadAttributes : [{0}],"),lazyLoads.Substring(1)));
                }
            }
            else
            {
                sb.AppendLine((minimize ? 
                    "parse:function(response){return response;},"
                    :@"  parse: function(response) {
        return response;
    },"));
            }
        }

        private void _AppendExposedMethods(Type modelType, string urlRoot,string host, WrappedStringBuilder sb,bool minimize)
        {
            foreach (MethodInfo mi in modelType.GetMethods(BindingFlags.Public | BindingFlags.Instance))
            {
                if (mi.GetCustomAttributes(typeof(ExposedMethod), false).Length > 0)
                    StaticExposedMethodGenerator.AppendMethodCall(urlRoot,host, mi,((ExposedMethod)mi.GetCustomAttributes(typeof(ExposedMethod), false)[0]).AllowNullResponse, ref sb,minimize);
            }
        }

        #region IJSGenerator Members

        public string GenerateJS(Type modelType, string host, List<string> readOnlyProperties, List<string> properties, List<string> viewIgnoreProperties, bool hasUpdate, bool hasAdd, bool hasDelete,bool minimize)
        {
            WrappedStringBuilder sb = new WrappedStringBuilder(minimize);
            sb.AppendFormat((minimize ?
                @"{0}=_.extend(true,{0},{{{1}:Backbone.Model.extend({{initialize:function(){{if(this._revertReadonlyFields!=undefined){{this.on(""change"",this._revertReadonlyFields);}}}},"
                :@"//Org.Reddragonit.BackBoneDotNet.JSGenerators.ModelDefinitionGenerator
{0} = _.extend(true,{0}, {{{1} : Backbone.Model.extend({{
    initialize : function() {{
        if (this._revertReadonlyFields != undefined){{
            this.on(""change"",this._revertReadonlyFields);
        }}
    }},"),new object[]{
                (RequestHandler.UseAppNamespacing ? "App.Models" : ModelNamespace.GetFullNameForModel(modelType, host)),
                (RequestHandler.UseAppNamespacing ? modelType.Name : "Model")
        });
            _AppendDefaults(modelType,properties,sb,minimize);
            if (!hasDelete)
                _AppendBlockDestroy(sb, minimize);
            if (!hasAdd && !hasUpdate)
                _AppendBlockSave(sb, minimize);
            else if (!hasAdd)
                _AppendBlockAdd(sb, minimize);
            else if (!hasUpdate)
                _AppendBlockUpdate(sb, minimize);
            _AppendReadonly(readOnlyProperties, sb, minimize);
            _AppendValidate(modelType, properties, sb, minimize);
            _AppendParse(modelType, host, properties, readOnlyProperties, sb, minimize);
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
            _AppendExposedMethods(modelType, urlRoot, host, sb, minimize);
            sb.AppendLine(string.Format((minimize ? "urlRoot:\"{0}\"}})}});" : "\turlRoot : \"{0}\"}})}});"),urlRoot));
            return sb.ToString();
        }
        #endregion
    }
}
