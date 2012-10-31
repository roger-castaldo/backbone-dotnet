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
        private void _AppendClassName(Type modelType, StringBuilder sb)
        {
            sb.Append("\tclassName : \"");
            foreach (string str in modelType.FullName.Split('.'))
                sb.Append(str + " ");
            foreach (ModelViewClass mvc in modelType.GetCustomAttributes(typeof(ModelViewClass), false))
                sb.Append(mvc.ClassName + " ");
            sb.AppendLine(" View\",");
        }

        private void _AppendAttributes(Type modelType, StringBuilder sb)
        {
            if (modelType.GetCustomAttributes(typeof(ModelViewAttribute), false).Length > 0)
            {
                sb.Append("\tattributes: {");
                object[] atts = modelType.GetCustomAttributes(typeof(ModelViewAttribute),false);
                for (int x = 0; x < atts.Length; x++)
                    sb.Append("\t\t\"" + ((ModelViewAttribute)atts[x]).Name + "\" : '" + ((ModelViewAttribute)atts[x]).Value + "'" + (x < atts.Length - 1 ? "," : ""));
                sb.Append("\t},");
            }
        }

        private void _AppendRenderFunction(Type modelType,string tag,List<string> properties,bool hasUpdate,bool hasDelete, StringBuilder sb, List<string> viewIgnoreProperties,string editImage,string deleteImage)
        {
            bool hasUpdateFunction = true;
            if (modelType.GetCustomAttributes(typeof(ModelBlockJavascriptGeneration), false).Length > 0)
            {
                if (((int)((ModelBlockJavascriptGeneration)modelType.GetCustomAttributes(typeof(ModelBlockJavascriptGeneration), false)[0]).BlockType & (int)ModelBlockJavascriptGenerations.EditForm) == (int)ModelBlockJavascriptGenerations.EditForm)
                    hasUpdateFunction = false;
            }
            sb.AppendLine("\trender : function(){");
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
            StringBuilder sbHtml = new StringBuilder();
            sbHtml.Append("\t\t$(this.el).html(");
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
                            string tcode = _RecurAddRenderModelPropertyCode(prop, PropType, "this.model.get('" + prop + "')[x].get('{0}')", out tsets, true);
                            if (tsets != "")
                                sb.Append(tsets);
                            sb.AppendLine("\t\tvar ars" + arIndex.ToString() + " = '';");
                            sb.AppendLine("\t\tfor(x in this.model.get('" + prop + "')){");
                            sb.AppendLine("\t\t\tars" + arIndex.ToString() + "+=" + string.Format(tcode, prop) + ";");
                            sb.AppendLine("\t\t}");
                            sbHtml.Append(string.Format(fstring, prop, "ars" + arIndex.ToString(), (properties.IndexOf(prop) == properties.Count - 1 ? "" : "+")));
                            arIndex++;
                        }
                        else
                        {
                            string tsets = "";
                            string code = _RecurAddRenderModelPropertyCode(prop, PropType, "this.model.get('" + prop + "').get('{0}')", out tsets, false);
                            if (tsets != "")
                                sb.Append(tsets);
                            sbHtml.Append(string.Format(fstring, prop, code, (properties.IndexOf(prop) == properties.Count - 1 ? "" : "+")));
                        }
                    }
                    else
                    {
                        if (array)
                        {
                            sb.AppendLine("\t\tvar ars" + arIndex.ToString() + " = '';");
                            sb.AppendLine("\t\tfor(x in this.model.get('" + prop + "')){");
                            sb.AppendLine("\t\t\tars" + arIndex.ToString() + "+='<span class=\"'+this.className+' " + prop + " els\">+this.model.get('" + prop + "')[x]+'</span>';");
                            sb.AppendLine("\t\t}");
                            sbHtml.Append(string.Format(fstring, prop, "ars" + arIndex.ToString(), (properties.IndexOf(prop) == properties.Count - 1 ? "" : "+")));
                            arIndex++;
                        }
                        else
                            sbHtml.Append(string.Format(fstring, prop, string.Format("this.model.get('{0}')", prop), (properties.IndexOf(prop) == properties.Count - 1 ? "" : "+")));
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
                sb.Append("+'<span class=\"'+this.className+' button edit\">"+(editImage==null ? "Edit" : "<img src=\""+editImage+"\"/>")+"</span>'");
            if (hasDelete)
                sb.Append("+'<span class=\"'+this.className+' button delete\">"+(deleteImage==null ? "Delete" : "<img src=\""+deleteImage+"\"/>")+"</span>'");
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
            sb.AppendLine(");");
            sb.AppendLine("\t\t$(this.el).attr('name',this.model.id);");
            sb.AppendLine("\t\tthis.trigger('render',this);");
            sb.AppendLine("\t\treturn this;");
            sb.AppendLine("\t}"+(hasUpdate || hasDelete ? "," : ""));
            if ((hasUpdate&&hasUpdateFunction) || hasDelete)
            {
                sb.AppendLine("\tevents : {");
                if (hasUpdate && hasUpdateFunction)
                    sb.AppendLine("\t\t'click .button.edit' : 'editModel'" + (hasDelete ? "," : ""));
                if (hasDelete)
                    sb.AppendLine("\t\t'click .button.delete' : 'deleteModel'");
                sb.AppendLine("\t},");
                if (hasUpdate && hasUpdateFunction)
                {
                    sb.AppendLine("\teditModel : function(){");
                    sb.AppendLine("\t\t" + modelType.FullName + ".editModel(this);");
                    sb.AppendLine("\t}" + (hasDelete ? "," : ""));
                }
                if (hasDelete)
                {
                    sb.AppendLine("\tdeleteModel : function(){");
                    sb.AppendLine("\t\tthis.model.destroy();");
                    sb.AppendLine("\t}");
                }
            }
        }

		private string _RecurAddRenderModelPropertyCode(string prop,Type PropType,string modelstring,out string arstring,bool addEls)
		{
            string className = PropType.FullName.Replace(".", " ")+(addEls ? " els ": "");
            foreach (ModelViewClass mvc in PropType.GetCustomAttributes(typeof(ModelViewClass), false))
                className += mvc.ClassName + " ";
            string code = "";
            int arIndex = 0;
            StringBuilder sb = new StringBuilder();
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
                                string tcode = _RecurAddRenderModelPropertyCode(pi.Name, ptype, string.Format(modelstring, pi.Name) + "[x].get('{0}')", out tsets,true);
                                if (tsets != "")
                                    sb.Append(tsets);
                                sb.AppendLine("\t\tvar ars" + prop + arIndex.ToString() + " = '';");
                                sb.AppendLine("\t\tfor(x in " + string.Format(modelstring, pi.Name) + "){");
                                sb.AppendLine("\t\t\tars" + prop + arIndex.ToString() + " += '<span class=\"" + className + " " + pi.Name + " els\">'+" + tcode + "+'</span>'");
                                sb.AppendLine("\t\t}");
                                code += (code.EndsWith(">'") ? "+" : "") + "'<span class=\"" + className + " " + pi.Name + "\">'+ars" + prop + arIndex.ToString() + "+'</span>'";
                                arIndex++;
                            }
                            else
                            {
                                string tsets = "";
                                code += (code.EndsWith(">'") ? "+" : "") + "'<span class=\"" + className + " " + pi.Name + "\">'+" + _RecurAddRenderModelPropertyCode(pi.Name, ptype, string.Format(modelstring, pi.Name) + ".get('{0}')",out tsets,false) + "+'</span>'";
                                if (tsets != "")
                                    sb.Append(tsets);
                            }
                        }else{
                            if (array)
                            {
                                sb.AppendLine("\t\tvar ars" + prop + arIndex.ToString() + " = '';");
                                sb.AppendLine("\t\tfor(x in " + string.Format(modelstring, pi.Name) + "){");
                                sb.AppendLine("\t\t\tars" + prop + arIndex.ToString() + " += '<span class=\"" + className + " " + pi.Name + " els\">'+" + string.Format(modelstring,pi.Name) + "[x]+'</span>'");
                                sb.AppendLine("\t\t}");
                                code += (code.EndsWith(">'") ? "+" : "") + "'<span class=\"" + className + " " + pi.Name + "\">'+ars" + prop + arIndex.ToString() + "+'</span>'";
                                arIndex++;
                            }else
                                code += (code.EndsWith(">'") ? "+" : "")+"'<span class=\"" + className + " " + pi.Name + "\">'+" + string.Format(modelstring, pi.Name) + "+'</span>'";
                        }
                    }
			    }
		    }
            arstring = sb.ToString();
            return code;
		}

        private void _LocateButtonImages(Type modelType, string host, out string editImage, out string deleteImage)
        {
            editImage = null;
            deleteImage = null;
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
        }

        #region IJSGenerator Members

        public string GenerateJS(Type modelType, string host, List<string> readOnlyProperties, List<string> properties, List<string> viewIgnoreProperties, bool hasUpdate, bool hasAdd, bool hasDelete)
        {
            if (modelType.GetCustomAttributes(typeof(ModelBlockJavascriptGeneration), false).Length > 0)
            {
                if (((int)((ModelBlockJavascriptGeneration)modelType.GetCustomAttributes(typeof(ModelBlockJavascriptGeneration), false)[0]).BlockType & (int)ModelBlockJavascriptGenerations.View) == (int)ModelBlockJavascriptGenerations.View)
                    return "";
            }
            string editImage = null;
            string deleteImage = null;
            _LocateButtonImages(modelType, host, out editImage, out deleteImage);
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("//Org.Reddragonit.BackBoneDotNet.JSGenerators.ViewGenerator");
            sb.AppendLine(modelType.FullName + " = _.extend("+modelType.FullName+",{View : Backbone.View.extend({");
            sb.AppendLine("\tinitialize : function(){");
            sb.AppendLine("\t\tthis.model.on('change',this.render,this);");
            sb.AppendLine("\t},");
            string tag = "div";
            if (modelType.GetCustomAttributes(typeof(ModelViewTag), false).Length > 0)
                tag = ((ModelViewTag)modelType.GetCustomAttributes(typeof(ModelViewTag), false)[0]).TagName;
            sb.AppendLine("\ttagName : \"" + tag + "\",");
            
            _AppendClassName(modelType, sb);
            _AppendAttributes(modelType, sb);
            _AppendRenderFunction(modelType,tag, properties, hasUpdate, hasDelete, sb,viewIgnoreProperties,editImage,deleteImage);

            sb.AppendLine("})});");
            return sb.ToString();
        }

        #endregion
    }
}
