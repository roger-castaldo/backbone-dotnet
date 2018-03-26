using System;
using System.Collections.Generic;
using System.Text;
using Org.Reddragonit.BackBoneDotNet.Interfaces;
using Org.Reddragonit.BackBoneDotNet.Attributes;
using System.Reflection;


namespace Org.Reddragonit.BackBoneDotNet.JSGenerators
{
    /*
     * This generator is used to generate the javascript for the view.
     * The view is accessible at namespace.type.View
     */
    internal class ViewGenerator : IJSGenerator
    {
        private void _AppendClassName(Type modelType, string host, WrappedStringBuilder sb, bool minimize)
        {
            sb.Append((minimize ? "className:\"" : "\tclassName : \""));
            foreach (string str in ModelNamespace.GetFullNameForModel(modelType, host).Split('.'))
                sb.Append(str + " ");
            foreach (ModelViewClass mvc in modelType.GetCustomAttributes(typeof(ModelViewClass), false))
                sb.Append(mvc.ClassName + " ");
            sb.AppendLine(" View\",");
        }

        private void _AppendAttributes(Type modelType, WrappedStringBuilder sb,bool minimize)
        {
            if (modelType.GetCustomAttributes(typeof(ModelViewAttribute), false).Length > 0)
            {
                sb.Append((minimize ? "attributes:{":"\tattributes: {"));
                object[] atts = modelType.GetCustomAttributes(typeof(ModelViewAttribute),false);
                for (int x = 0; x < atts.Length; x++)
                    sb.Append((minimize ? "" : "\t\t")+"\"" + ((ModelViewAttribute)atts[x]).Name + "\" : '" + ((ModelViewAttribute)atts[x]).Value + "'" + (x < atts.Length - 1 ? "," : ""));
                sb.Append((minimize ? "" : "\t")+"},");
            }
        }

        private void _AppendRenderFunction(Type modelType,string host,string tag,List<string> properties,bool hasUpdate,bool hasDelete, WrappedStringBuilder sb, List<string> viewIgnoreProperties,string editImage,string deleteImage,EditButtonDefinition edDef,DeleteButtonDefinition delDef,bool minimize)
        {
            bool hasUpdateFunction = true;
            if (modelType.GetCustomAttributes(typeof(ModelBlockJavascriptGeneration), false).Length > 0)
            {
                if (((int)((ModelBlockJavascriptGeneration)modelType.GetCustomAttributes(typeof(ModelBlockJavascriptGeneration), false)[0]).BlockType & (int)ModelBlockJavascriptGenerations.EditForm) == (int)ModelBlockJavascriptGenerations.EditForm)
                    hasUpdateFunction = false;
            }
            sb.AppendLine((minimize ? "render:function(){" : "\trender : function(){"));
            string fstring = "";
            switch (tag.ToLower())
            {
                case "tr":
                    fstring = "'<td class=\"'+this.className+' {0}\">'+{1}+'</td>'{2}";
                    break;
                case "ul":
                case "ol":
                    fstring = "'<li class=\"'+this.className+' {0}\">'+{1}+'</li>'{2}";
                    break;
                default:
                    fstring = "'<" + tag + " class=\"'+this.className+' {0}\">'+{1}+'</" + tag + ">'{2}";
                    break;
            }
            int arIndex = 0;
            WrappedStringBuilder sbHtml = new WrappedStringBuilder(minimize);
            sbHtml.Append((minimize ? "" : "\t\t")+"$(this.el).html(");
            foreach (string prop in properties)
            {
                if (!viewIgnoreProperties.Contains(prop)&&prop!="id")
                {
                    Type PropType = modelType.GetProperty(prop).PropertyType;
                    bool array = false;
                    if (PropType.FullName.StartsWith("System.Nullable"))
                    {
                        if (PropType.IsGenericType)
                            PropType = PropType.GetGenericArguments()[0];
                        else
                            PropType = PropType.GetElementType();
                    }
                    if (PropType.IsArray)
                    {
                        array = true;
                        PropType = PropType.GetElementType();
                    }
                    else if (PropType.IsGenericType)
                    {
                        if (PropType.GetGenericTypeDefinition() == typeof(List<>))
                        {
                            array = true;
                            PropType = PropType.GetGenericArguments()[0];
                        }
                    }
                    if (new List<Type>(PropType.GetInterfaces()).Contains(typeof(IModel)))
                    {
                        if (array)
                        {
                            string tsets = "";
                            string tcode = _RecurAddRenderModelPropertyCode(prop, PropType,host, "this.model.get('" + prop + "').at(x).get('{0}')", out tsets, true,minimize);
                            if (tsets != "")
                                sb.Append(tsets);
                            sb.AppendFormat((minimize ?
                                "var ars{0}='';if(this.model.get('{1}')!=null){{for(var x=0;x<this.model.get('{1}').length;x++){{ars{0}+={2};}}}}"
                                :@"      var ars{0} = '';
        if(this.model.get('{1}')!=null){{
            for(var x=0;x<this.model.get('{1}').length;x++){{
                ars{0}+={2};
            }}
        }}"), arIndex, prop, string.Format(tcode, prop));
                            sbHtml.Append(string.Format(fstring, prop, "ars" + arIndex.ToString(), (properties.IndexOf(prop) == properties.Count - 1 ? "" : "+")));
                            arIndex++;
                        }
                        else
                        {
                            string tsets = "";
                            string code = _RecurAddRenderModelPropertyCode(prop, PropType,host, "this.model.get('" + prop + "').get('{0}')", out tsets, false,minimize);
                            if (tsets != "")
                                sb.Append(tsets);
                            sbHtml.Append(string.Format(fstring, prop, "(this.model.get('" + prop + "') == null ? '' : "+code+")", (properties.IndexOf(prop) == properties.Count - 1 ? "" : "+")));
                        }
                    }
                    else
                    {
                        if (array)
                        {
                            sb.AppendFormat((minimize?
                                "var ars{0}='';if(this.model.get('{1}')!=null){{for(x in this.model.get('{1}')){{if(this.model.get('{1}')[x]!=null){{ars{0}+='<span class=\"'+this.className+' {1} els\">'+this.model.get('{1}')[x]+'</span>';}}}}}}"
                                :@"      var ars{0} = '';
        if(this.model.get('{1}')!=null){{
            for(x in this.model.get('{1}')){{
                if(this.model.get('{1}')[x]!=null){{
                    ars{0}+='<span class=""'+this.className+' {1} els"">'+this.model.get('{1}')[x]+'</span>';
                }}
            }}
        }}"),arIndex,prop);
                            sbHtml.Append(string.Format(fstring, prop, "ars" + arIndex.ToString(), (properties.IndexOf(prop) == properties.Count - 1 ? "" : "+")));
                            arIndex++;
                        }
                        else
                            sbHtml.Append(string.Format(fstring, prop, string.Format((minimize ? "(this.model.get('{0}')==null?'':this.model.get('{0}'))":"(this.model.get('{0}')==null ? '' : this.model.get('{0}'))"), prop), (properties.IndexOf(prop) == properties.Count - 1 ? "" : "+")));
                    }
                }
            }
            sb.Append(sbHtml.ToString().Trim('+'));
            if (hasUpdate || hasDelete)
            {
                switch (tag.ToLower())
                {
                    case "tr":
                        sb.Append("+'<td class=\"'+this.className+' buttons\">'");
                        break;
                    case "ul":
                    case "ol":
                        sb.Append("+'<li class=\"'+this.className+' buttons\">'");
                        break;
                    default:
                        sb.Append("+'<"+tag+" class=\"'+this.className+' buttons\">'");
                        break;
                }
            }
            if (hasUpdate)
            {
                if (edDef == null)
                    sb.Append("+'<span class=\"'+this.className+' button edit\">" + (editImage == null ? "Edit" : "<img src=\"" + editImage + "\"/>") + "</span>'");
                else
                {
                    sb.Append("+'<"+edDef.Tag+" class=\"'+this.className+' button edit");
                    if (edDef.Class != null)
                    {
                        foreach (string str in edDef.Class)
                            sb.Append(" "+str);
                    }
                    sb.Append("\">" + (editImage == null ? "" : "<img src=\"" + editImage + "\"/>") + (edDef.Text == null ? "" : edDef.Text) + "</" + edDef.Tag+">'");
                }
            }
            if (hasDelete)
            {
                if (delDef == null)
                    sb.Append("+'<span class=\"'+this.className+' button delete\">" + (deleteImage == null ? "Delete" : "<img src=\"" + deleteImage + "\"/>") + "</span>'");
                else
                {
                    sb.Append("+'<" + delDef.Tag + " class=\"'+this.className+' button edit");
                    if (delDef.Class != null)
                    {
                        foreach (string str in delDef.Class)
                            sb.Append(" " + str);
                    }
                    sb.Append("\">" + (deleteImage == null ? "" : "<img src=\"" + deleteImage + "\"/>") + (delDef.Text == null ? "" : delDef.Text) + "</" + delDef.Tag + ">'");
                }
            }
            if (hasUpdate || hasDelete)
            {
                switch (tag.ToLower())
                {
                    case "tr":
                        sb.Append("+'</td>'");
                        break;
                    case "ul":
                    case "ol":
                        sb.Append("+'</li>'");
                        break;
                    default:
                        sb.Append("+'</" + tag + ">'");
                        break;
                }
            }
            sb.AppendLine((minimize ? 
                ");$(this.el).attr('name',this.model.id);this.trigger('pre_render_complete',this);this.trigger('render',this);return this;}"
                : @");
        $(this.el).attr('name',this.model.id);
        this.trigger('pre_render_complete',this);
        this.trigger('render',this);
        return this;
    }") + (hasUpdate || hasDelete ? "," : ""));
            if ((hasUpdate&&hasUpdateFunction) || hasDelete)
            {
                sb.AppendLine((minimize ? "events:{" : "\tevents : {"));
                if (hasUpdate && hasUpdateFunction)
                    sb.AppendLine((minimize ? "'click .button.edit':'editModel'" : "\t\t'click .button.edit' : 'editModel'") + (hasDelete ? "," : ""));
                if (hasDelete)
                    sb.AppendLine((minimize ? "'click .button.delete':'deleteModel'" : "\t\t'click .button.delete' : 'deleteModel'"));
                sb.AppendLine((minimize ? "" : "\t")+"},");
                if (hasUpdate && hasUpdateFunction)
                {
                    sb.AppendLine(string.Format((minimize ? 
                        "editModel:function(){{{0}.{1}(this);}}"
                        : @"    editModel : function(){{
        {0}.{1}(this);
    }}"),new object[]{
           (RequestHandler.UseAppNamespacing ? "App.Forms" : ModelNamespace.GetFullNameForModel(modelType, host)),
           (RequestHandler.UseAppNamespacing ? modelType.Name : "editModel")
       }) + (hasDelete ? "," : ""));
                }
                if (hasDelete)
                {
                    sb.AppendLine((minimize ? 
                        "deleteModel:function(){this.model.destroy();}"
                        :@"  deleteModel : function(){
        this.model.destroy();
    }"));
                }
            }
        }

		private string _RecurAddRenderModelPropertyCode(string prop,Type PropType,string host,string modelstring,out string arstring,bool addEls,bool minimize)
		{
            string className = ModelNamespace.GetFullNameForModel(PropType, host).Replace(".", " ") + (addEls ? " els " : "");
            foreach (ModelViewClass mvc in PropType.GetCustomAttributes(typeof(ModelViewClass), false))
                className += mvc.ClassName + " ";
            string code = "";
            int arIndex = 0;
            WrappedStringBuilder sb = new WrappedStringBuilder(minimize);
			foreach (PropertyInfo pi in PropType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
		    {
			    if (pi.GetCustomAttributes(typeof(ModelIgnoreProperty), false).Length == 0)
			    {
				    if (pi.GetCustomAttributes(typeof(ReadOnlyModelProperty), false).Length == 0
                        && pi.GetCustomAttributes(typeof(ViewIgnoreField), false).Length == 0)
                    {
                        Type ptype = pi.PropertyType;
                        bool array = false;
                        if (ptype.FullName.StartsWith("System.Nullable"))
                        {
                            if (ptype.IsGenericType)
                                ptype = ptype.GetGenericArguments()[0];
                            else
                                ptype = ptype.GetElementType();
                        }
                        if (ptype.IsArray)
                        {
                            array = true;
                            ptype = ptype.GetElementType();
                        }
                        else if (ptype.IsGenericType)
                        {
                            if (ptype.GetGenericTypeDefinition() == typeof(List<>))
                            {
                                array = true;
                                ptype = ptype.GetGenericArguments()[0];
                            }
                        }
                        if (new List<Type>(ptype.GetInterfaces()).Contains(typeof(IModel))){
                            if (array)
                            {
                                string tsets = "";
                                string tcode = _RecurAddRenderModelPropertyCode(pi.Name, ptype,host, string.Format(modelstring, pi.Name) + ".at(x).get('{0}')", out tsets,true,minimize);
                                if (tsets != "")
                                    sb.Append(tsets);
                                sb.AppendLine(string.Format((minimize ? 
                                    "var ars{0}{1}='';if({2}!=null{{for(var x=0;x<{2}).length;x++){{ars{0}{1}+='<span class=\"{3} {4} els\">'+{5}+'</span>'}}}}"
                                    :@"     var ars{0}{1} = '';
        if({2}!=null{{
            for(var x=0;x<{2}.length;x++){{
                ars{0}{1} += '<span class=""{3} {4} els"">'+{5}+'</span>'
            }}
        }}"
                                    ),new object[]{
                                        prop,
                                        arIndex,
                                        string.Format(modelstring, pi.Name),
                                        className,
                                        pi.Name,
                                        tcode
                                    }));
                                code += (code.EndsWith(">'") || code.EndsWith(">')") ? "+" : "") + "'<span class=\"" + className + " " + pi.Name + "\">'+ars" + prop + arIndex.ToString() + "+'</span>'";
                                arIndex++;
                            }
                            else
                            {
                                string tsets = "";
                                code += (code.EndsWith(">'") || code.EndsWith(">')") ? "+" : "") + "("+string.Format(modelstring,prop)+"==null ? '' : '<span class=\"" + className + " " + pi.Name + "\">'+" + _RecurAddRenderModelPropertyCode(pi.Name, ptype,host, string.Format(modelstring, pi.Name) + ".get('{0}')",out tsets,false,minimize) + "+'</span>')";
                                if (tsets != "")
                                    sb.Append(tsets);
                            }
                        }else{
                            if (array)
                            {
                                sb.AppendLine(string.Format((minimize ?
                                    "var ars{0}{1}='';if ({2}!=null){{for(x in {2}){{ars{0}{1}+='<span class=\"{3} {4} els\">'+{2}[x]+'</span>';}}}}"
                                    : @"     var ars{0}{1} = '';
        if ({2}!=null){{
            for(x in {2}){{
                ars{0}{1} += '<span class=""{3} {4} els"">'+{2}[x]+'</span>';
            }}
        }}"), new object[]{
               prop,
               arIndex,
               string.Format(modelstring, pi.Name),
               className,
               pi.Name
           }));
                                code += (code.EndsWith(">'") || code.EndsWith(">')") ? "+" : "") + "'<span class=\"" + className + " " + pi.Name + "\">'+ars" + prop + arIndex.ToString() + "+'</span>'";
                                arIndex++;
                            }else
                                code += (code.EndsWith(">'") || code.EndsWith(">')") ? "+" : "")+"'<span class=\"" + className + " " + pi.Name + "\">'+(" + string.Format(modelstring, pi.Name) + "==null ? '' : "+string.Format(modelstring,pi.Name)+")+'</span>'";
                        }
                    }
			    }
		    }
            arstring = sb.ToString();
            return code;
		}

        private void _LocateButtonImages(Type modelType, string host, out string editImage, out string deleteImage, out EditButtonDefinition edDef, out DeleteButtonDefinition delDef)
        {
            editImage = null;
            deleteImage = null;
            delDef = null;
            edDef = null;
            foreach (EditButtonImagePath edip in modelType.GetCustomAttributes(typeof(EditButtonImagePath), false))
            {
                if (edip.Host == host)
                {
                    editImage = edip.URL;
                    break;
                }
            }
            if (editImage == null)
            {
                foreach (EditButtonImagePath edip in modelType.GetCustomAttributes(typeof(EditButtonImagePath), false))
                {
                    if (edip.Host =="*")
                    {
                        editImage = edip.URL;
                        break;
                    }
                }
            }
            foreach (DeleteButtonImagePath dbip in modelType.GetCustomAttributes(typeof(DeleteButtonImagePath), false))
            {
                if (dbip.Host == host)
                {
                    deleteImage = dbip.URL;
                    break;
                }
            }
            if (deleteImage == null)
            {
                foreach (DeleteButtonImagePath dbip in modelType.GetCustomAttributes(typeof(DeleteButtonImagePath), false))
                {
                    if (dbip.Host == "*")
                    {
                        deleteImage = dbip.URL;
                        break;
                    }
                }
            }
            foreach (EditButtonDefinition ebd in modelType.GetCustomAttributes(typeof(EditButtonDefinition),false)){
                if (ebd.Host == host)
                {
                    edDef = ebd;
                    break;
                }
            }
            if (edDef == null)
            {
                foreach (EditButtonDefinition ebd in modelType.GetCustomAttributes(typeof(EditButtonDefinition), false))
                {
                    if (ebd.Host == "*")
                    {
                        edDef = ebd;
                        break;
                    }
                }
            }
            foreach (DeleteButtonDefinition dbd in modelType.GetCustomAttributes(typeof(DeleteButtonDefinition), false))
            {
                if (dbd.Host == host)
                {
                    delDef = dbd;
                    break;
                }
            }
            if (delDef == null)
            {
                foreach (DeleteButtonDefinition dbd in modelType.GetCustomAttributes(typeof(DeleteButtonDefinition), false))
                {
                    if (dbd.Host == "*")
                    {
                        delDef = dbd;
                        break;
                    }
                }
            }
        }

        #region IJSGenerator Members

        public string GenerateJS(Type modelType, string host, List<string> readOnlyProperties, List<string> properties, List<string> viewIgnoreProperties, bool hasUpdate, bool hasAdd, bool hasDelete,bool minimize)
        {
            if (modelType.GetCustomAttributes(typeof(ModelBlockJavascriptGeneration), false).Length > 0)
            {
                if (((int)((ModelBlockJavascriptGeneration)modelType.GetCustomAttributes(typeof(ModelBlockJavascriptGeneration), false)[0]).BlockType & (int)ModelBlockJavascriptGenerations.View) == (int)ModelBlockJavascriptGenerations.View)
                    return "";
            }
            string editImage = null;
            string deleteImage = null;
            EditButtonDefinition edDef;
            DeleteButtonDefinition delDef;
            _LocateButtonImages(modelType, host, out editImage, out deleteImage,out edDef,out delDef);
            WrappedStringBuilder sb = new WrappedStringBuilder(minimize);
            sb.AppendFormat((minimize ? 
                "{0}=_.extend(true,{0},{{{1} : Backbone.View.extend({{" 
                :@"//Org.Reddragonit.BackBoneDotNet.JSGenerators.ViewGenerator
{0} = _.extend(true,{0},{{{1} : Backbone.View.extend({{
    "),new object[]{
         (RequestHandler.UseAppNamespacing ? "App.Views" : ModelNamespace.GetFullNameForModel(modelType, host)),
         (RequestHandler.UseAppNamespacing ? modelType.Name : "View")
     });
            string tag = "div";
            if (modelType.GetCustomAttributes(typeof(ModelViewTag), false).Length > 0)
                tag = ((ModelViewTag)modelType.GetCustomAttributes(typeof(ModelViewTag), false)[0]).TagName;
            sb.AppendLine(string.Format((minimize ?  "tagName:\"{0}\",":"\ttagName : \"{0}\","),tag));
            
            _AppendClassName(modelType,host, sb,minimize);
            _AppendAttributes(modelType, sb,minimize);
            _AppendRenderFunction(modelType,host,tag, properties, hasUpdate, hasDelete, sb,viewIgnoreProperties,editImage,deleteImage,edDef,delDef,minimize);

            sb.AppendLine("})});");
            return sb.ToString();
        }

        #endregion
    }
}
