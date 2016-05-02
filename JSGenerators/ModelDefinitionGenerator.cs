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
            sb.AppendLine((minimize ? "destroy:function(options){if((options==undefined?undefined:options.error)!=undefined){options.error(this,'501',options);return false;}else{return false;}}," : @"  destroy : function(options){
        if ((options==undefined ? undefined : options.error) != undefined){
            options.error(this,'501',options);
            return false;
        }else{
            return false;
        }
    },"));
        }

        private void _AppendSave(WrappedStringBuilder sb, bool minimize, bool blockAdd, bool blockUpdate, List<string> readOnlyProperties)
        {
            if (blockAdd&&blockUpdate)
                sb.AppendLine((minimize ? "save:function(key,value,options){return false;}," : "\tsave : function(key, value, options){return false;},"));
            else if (readOnlyProperties.Count > 1 || !readOnlyProperties.Contains("id")||blockAdd||blockUpdate)
            {
                sb.AppendLine((minimize ? "save:function(attrs,options){if(attrs==undefined){attrs=this.toJSON();}" : @"  save : function(attrs,options) {
        if (attrs==undefined){
            attrs = this.toJSON();
        }"));
                if (blockAdd)
                {
                    sb.AppendLine((minimize ? "if(this.isNew()){if((options==undefined?undefined:options.error)!=undefined){options.error(model,'501',options);return false;}else{return false;}}" : @"       if(this.isNew()){
            if ((options==undefined ? undefined : options.error) != undefined){
                options.error(model,'501',options);
                return false;
            }else{
                return false;
            }
        }"));
                }
                if (blockUpdate)
                {
                    sb.AppendLine((minimize ? "if(!this.isNew()){if((options==undefined?undefined:options.error)!=undefined){options.error(model,'501',options);return false;}else{return false;}}" : @"       if(!this.isNew()){
            if ((options==undefined ? undefined : options.error) != undefined){
                options.error(model,'501',options);
                return false;
            }else{
                return false;
            }
        }"));
                }
                if (readOnlyProperties.Count > 1 || !readOnlyProperties.Contains("id"))
                {
                    sb.AppendLine((minimize ? "if((!_.isEmpty(this._previousAttributes)&&(this.id!=undefined))||!this.isNew()){" : "        if ((!_.isEmpty(this._previousAttributes)&&(this.id!=undefined))||!this.isNew()){"));
                    foreach (string str in readOnlyProperties)
                    {
                        if (str != "id")
                        {
                            sb.AppendFormat((minimize ? "if(attrs.{0}!=undefined){{delete attrs.{0};}}" : @"          if (attrs.{0} != undefined) {{ 
                delete attrs.{0};
            }}"), str);
                        }
                    }
                    sb.AppendLine((minimize ? "}" : "\t\t\t}"));
                }
                sb.AppendLine((minimize ? "Backbone.Model.prototype.save.apply(this, arguments);}," : @"        Backbone.Model.prototype.save.apply(this, [attrs,options]);
    },"));
            }
        }

        private void _AppendSet(WrappedStringBuilder sb, bool minimize, List<string> readOnlyProperties)
        {
            if (readOnlyProperties.Count > 1 || !readOnlyProperties.Contains("id"))
            {
                sb.AppendLine((minimize ? "set:function(key,val,options){if(key==null)return this;if(typeof key === 'object'){attrs = key;options = val;}else{(attrs = {})[key] = val;}" : @"   set:function (key, val, options) {
        if (key == null) return this;
		    if (typeof key === 'object') {
			    attrs = key;
				options = val;
            } else {
			    (attrs = {})[key] = val;
        }"));
                sb.AppendLine((minimize ? "if((!_.isEmpty(this._previousAttributes)&&(this.id!=undefined))||!this.isNew()){" : "        if ((!_.isEmpty(this._previousAttributes)&&(this.id!=undefined))||!this.isNew()){"));
                foreach (string str in readOnlyProperties)
                {
                    if (str != "id")
                    {
                        sb.AppendFormat((minimize ? "if(attrs.{0}!=undefined){{delete attrs.{0};}}" : @"          if (attrs.{0} != undefined) {{ 
                delete attrs.{0};
            }}"), str);
                    }
                }
                sb.AppendLine((minimize ? "if(_.isEmpty(attrs)){return this;}}" : @"       if (_.isEmpty(attrs)) {
            return this;
        }
    }"));
                sb.AppendLine((minimize ? "return Backbone.Model.prototype.set.apply(this,[attrs,options]);}," : @"     return Backbone.Model.prototype.set.apply(this,[attrs,options]);
    },"));
            }
        }

        private void _AppendGet(WrappedStringBuilder sb, bool minimize, Type modelType, List<string> properties)
        {
            bool add = false;
            foreach (string str in properties)
            {
                if (str != "id")
                {
                    if (modelType.GetProperty(str).GetCustomAttributes(typeof(ModelPropertyLazyLoadExternalModel), false).Length > 0)
                    {
                        add = true;
                        break;
                    }
                }
            }
            if (add)
            {
                sb.AppendLine((minimize ? "get:function(attribute){var ret=Backbone.Model.prototype.get.apply(this,arguments);if(ret!=undefined){" : @"   get : function(attribute) { 
        var ret = Backbone.Model.prototype.get.apply(this, arguments);
        if (ret != undefined) {"));
                foreach (string str in properties)
                {
                    if (modelType.GetProperty(str).GetCustomAttributes(typeof(ModelPropertyLazyLoadExternalModel), false).Length > 0)
                    {
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
                        sb.AppendLine(string.Format((minimize ? "if(attribute=='{0}'){{" : "           if (attribute=='{0}'){{"), str));
                        if (array)
                        {
                            sb.AppendLine((minimize ? "if(ret.at!=undefined){for(var x=0;x<ret.length;x++){if(!(ret.at(x).IsLoaded==undefined?false:ret.at(x).IsLoaded)){ret.at(x).fetch({async:false,silent:true});ret.at(x).IsLoaded=true;}}}else{for(var x=0;x<ret.length;x++){if(!(ret[x].IsLoaded==undefined?false:ret[x].IsLoaded)){ret[x].fetch({async:false,silent:true});ret[x].IsLoaded=true;}}}" : @"            if (ret.at != undefined) { 
                    for(var x=0;x<ret.length;x++){
                        if (!(ret.at(x).IsLoaded==undefined ? false : ret.at(x).IsLoaded)){
                            ret.at(x).fetch({async:false,silent:true});
                            ret.at(x).IsLoaded=true;
                        }
                    }
                } else {
                    for(var x=0;x<ret.length;x++){
                        if (!(ret[x].IsLoaded==undefined ? false : ret[x].IsLoaded)){
                            ret[x].fetch({async:false,silent:true});
                            ret[x].IsLoaded=true;
                        }
                    }
                }"));
                        }
                        else
                        {
                            sb.AppendLine((minimize ? "if(!(ret.IsLoaded==undefined?false:ret.IsLoaded)){ret.fetch({async:false,silent:true});ret.IsLoaded=true;}" : @"             if (!(ret.IsLoaded==undefined ? false : ret.IsLoaded)){
                    ret.fetch({async:false,silent:true});
                    ret.IsLoaded=true;
                }"));
                        }
                        sb.AppendLine((minimize ? "}" : "           }"));
                    }
                }
                sb.AppendLine((minimize ? "}return ret;}," : @"         }
        return ret;
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

        private void _AppendToJSON(WrappedStringBuilder sb, bool minimize, Type modelType, List<string> properties, List<string> readOnlyProperties)
        {
            sb.AppendLine((minimize ? "toJSON:function(){var attrs={};" : @"   toJSON : function() {
        var attrs = {};"));
            foreach (string str in properties)
            {
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
                    sb.AppendLine(string.Format((minimize ? "if(this.attributes.{0}!=undefined){{if(this.attributes{0}==null){{attrs.{0}=null;}}else{{" : @"     if (this.attributes.{0}!=undefined) {{
            if (this.attributes{0}==null) {{
                attrs.{0} = null;
            }} else {{"), str));
                    if (array)
                    {
                        sb.AppendLine(string.Format((minimize ? "for(var x=0;x<this.attributes.{0}.length;x++){{if(this.attributes.{0}.at!=undefined){{attrs.{0}.push({{id:this.attributes.{0}.at(x).id}});}}else{{attrs.{0}.push({{id:this.attributes.{0}[x].id}});}}}}" : @"         for(var x=0;x<this.attributes.{0}.length;x++){{
                            if(this.attributes.{0}.at!=undefined){{
                                attrs.{0}.push({{id:this.attributes.{0}.at(x).id}});
                            }}else{{
                                attrs.{0}.push({{id:this.attributes.{0}[x].id}});
                            }}
                        }}"), str));
                    }
                    else
                        sb.AppendLine(string.Format((minimize ? "      attrs.{0} = {{id:this.attributes.{0}}};" : "      attrs.{0} = {{id:this.attributes.{0}}};"),str));
                    sb.AppendLine((minimize ? "}}" : @"           }
        }"));
                }
                else
                    sb.AppendLine(string.Format((minimize ? "attrs.{0}=this.attributes.{0};" : "        attrs.{0}=this.attributes.{0};"), str));
            }
            if (readOnlyProperties.Count > 1 || !readOnlyProperties.Contains("id"))
            {
                sb.AppendLine((minimize ? "if((!_.isEmpty(this._previousAttributes)&&(this.id!=undefined))||!this.isNew()){" : "        if ((!_.isEmpty(this._previousAttributes)&&(this.id!=undefined))||!this.isNew()){"));
                foreach (string str in readOnlyProperties)
                {
                    if (str != "id")
                    {
                        sb.AppendFormat((minimize ? "if(attrs.{0}!=undefined){{delete attrs.{0};}}" : @"          if (attrs.{0} != undefined) {{ 
                delete attrs.{0};
            }}"), str);
                    }
                }
                sb.AppendLine((minimize ? "}" : "\t\t\t}"));
            }
            sb.AppendLine((minimize ? "return attrs;}," : "\treturn attrs;\n\t},"));
        }

        private void _AppendParse(Type modelType,string host, List<string> properties, WrappedStringBuilder sb,bool minimize)
        {
            bool add = false;
            foreach (string str in properties)
            {
                if (str != "id")
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
                }
            }
            if (add)
            {
                sb.AppendLine((minimize ? 
                    "parse:function(response){var attrs={};if(response!=true){"
                    :@"  parse: function(response) {
        var attrs = {};
        if (response!=true){"));
                foreach (string str in properties)
                {
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
                    sb.AppendLine(string.Format((minimize ? "if(response.{0}!=undefined){{" : "         if (response.{0} != undefined){{"), str));
                    if (new List<Type>(propType.GetInterfaces()).Contains(typeof(IModel)))
                    {
                        bool isLazy = modelType.GetProperty(str).GetCustomAttributes(typeof(ModelPropertyLazyLoadExternalModel), false).Length > 0;
                        if (isLazy)
                        {
                            if (array)
                            {
                                sb.AppendFormat((minimize ?
                                @"if({0}.{2}!=undefined){{attrs.{1} = new {0}.{2}();for (var x=0;x<response.{1}.length;x++){{attrs.{1}.add(new {3}.{4}({{'id':response.{1}[x].id}}));}}}}else{{attrs.{1}=[];for (var x=0;x<response.{1}.length;x++){{attrs.{1}.push(new {3}.{4}({{'id':response.{1}[x].id}}));}}}}"
                                : @"          if({0}.{2}!=undefined){{
                attrs.{1} = new {0}.{2}();
                for (var x=0;x<response.{1}.length;x++){{
                    attrs.{1}.add(new {3}.{4}({{'id':response.{1}[x].id}}));
                }}
            }}else{{
                attrs.{1}=[];
                for (var x=0;x<response.{1}.length;x++){{
                    attrs.{1}.push(new {3}.{4}({{'id':response.{1}[x].id}}));
                }}
            }}"), new object[]{
                                (RequestHandler.UseAppNamespacing ? "App.Collections" : ModelNamespace.GetFullNameForModel(propType, host)),
                                str,
                                (RequestHandler.UseAppNamespacing ? propType.Name : "Collection"),
                                (RequestHandler.UseAppNamespacing ? "App.Models" : ModelNamespace.GetFullNameForModel(propType, host)),
                                (RequestHandler.UseAppNamespacing ? propType.Name : "Model")
               });
                            }
                            else
                            {
                                sb.AppendFormat((minimize ? "attrs.{0}=new {1}.{2}({{'id':response.{0}.id}});" : "          attrs.{0} = new {1}.{2}({{'id':response.{0}.id}});"), new object[]{
                                                                    str, 
                                                                    (RequestHandler.UseAppNamespacing ? "App.Models" : ModelNamespace.GetFullNameForModel(propType, host)),
                                                                    (RequestHandler.UseAppNamespacing ? propType.Name : "Model")
                                                                });
                            }
                        }
                        if (array)
                        {
                            sb.AppendFormat((minimize ?
                                @"if({0}.{2}!=undefined){{attrs.{1} = new {0}.{2}();for (var x=0;x<response.{1}.length;x++){{attrs.{1}.add(new {3}.{4}({{'id':response.{1}[x].id}}));attrs.{1}.at(x).attributes=attrs.{1}.at(x).parse(response.{1}[x]);}}}}else{{attrs.{1}=[];for (var x=0;x<response.{1}.length;x++){{attrs.{1}.push(new {3}.{4}({{'id':response.{1}[x].id}}));attrs.{1}[x].attributes=attrs.{1}[x].parse(response.{1}[x]);}}}}"
                                : @"          if({0}.{2}!=undefined){{
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
            }}"), new object[]{
                                (RequestHandler.UseAppNamespacing ? "App.Collections" : ModelNamespace.GetFullNameForModel(propType, host)),
                                str,
                                (RequestHandler.UseAppNamespacing ? propType.Name : "Collection"),
                                (RequestHandler.UseAppNamespacing ? "App.Models" : ModelNamespace.GetFullNameForModel(propType, host)),
                                (RequestHandler.UseAppNamespacing ? propType.Name : "Model")
               });
                        }
                        else
                        {
                            sb.AppendFormat((minimize ?
                                "attrs.{0}=new {1}.{2}({{'id':response.{0}.id}});attrs.{0}.attributes=attrs.{0}.parse(response.{0});"
                                : @"          attrs.{0} = new {1}.{2}({{'id':response.{0}.id}});
            attrs.{0}.attributes=attrs.{0}.parse(response.{0});"), new object[]{
                                                                    str, 
                                                                    (RequestHandler.UseAppNamespacing ? "App.Models" : ModelNamespace.GetFullNameForModel(propType, host)),
                                                                    (RequestHandler.UseAppNamespacing ? propType.Name : "Model")
                                                                });
                        }
                    }
                    else
                    {
                        if (propType == typeof(DateTime))
                            sb.AppendLine(string.Format((minimize ? "attrs.{0}=new Date(response.{0});" : "\t\tattrs.{0} = new Date(response.{0});"), str));
                        else
                            sb.AppendLine(string.Format((minimize ? "attrs.{0}=response.{0};" : "\t\tattrs.{0} = response.{0};"), str));
                    }
                    sb.AppendLine((minimize ? "}" : "         }"));
                }
                sb.AppendLine((minimize ? 
                    "}return attrs;},"
                    :@"      }
        return attrs;
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
                @"{0}=_.extend(true,{0},{{{1}:Backbone.Model.extend({{"
                :@"//Org.Reddragonit.BackBoneDotNet.JSGenerators.ModelDefinitionGenerator
{0} = _.extend(true,{0}, {{{1} : Backbone.Model.extend({{
    "),new object[]{
                (RequestHandler.UseAppNamespacing ? "App.Models" : ModelNamespace.GetFullNameForModel(modelType, host)),
                (RequestHandler.UseAppNamespacing ? modelType.Name : "Model")
        });
            _AppendDefaults(modelType,properties,sb,minimize);
            if (!hasDelete)
                _AppendBlockDestroy(sb, minimize);
            _AppendSave(sb, minimize, !hasAdd, !hasUpdate, readOnlyProperties);
            _AppendSet(sb, minimize, readOnlyProperties);
            _AppendGet(sb, minimize, modelType, properties);
            _AppendValidate(modelType, properties, sb, minimize);
            _AppendToJSON(sb, minimize, modelType, properties, readOnlyProperties);
            _AppendParse(modelType, host, properties, sb, minimize);
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
