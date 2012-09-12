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
            sb.AppendLine("\",");
        }

        private void _AppendRenderFunction(Type modelType,string tag,List<string> properties,bool hasUpdate,bool hasDelete, StringBuilder sb)
        {
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
            sb.Append("\t\t$(this.el).html(");
            foreach (string prop in properties)
            {
                Type PropType = modelType.GetProperty(prop).PropertyType;
                if (new List<Type>(PropType.GetInterfaces()).Contains(typeof(IModel)))
                {
                    string code = _RecurAddRenderModelPropertyCode(prop,PropType,"this.model.get('"+prop+"').get('{0}')");
                    sb.Append(string.Format(fstring, prop,code, (properties.IndexOf(prop) == properties.Count - 1 ? "" : "+")));
                }else
                    sb.Append(string.Format(fstring, prop,string.Format("this.model.get('{0}')",prop), (properties.IndexOf(prop) == properties.Count - 1 ? "" : "+")));
            }
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
                sb.Append("+'<span class=\"'+this.className+' button edit\">Edit</span>'");
            if (hasDelete)
                sb.Append("+'<span class=\"'+this.className+' button delete\">Delete</span>'");
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
            sb.AppendLine("\t\treturn this;");
            sb.AppendLine("\t}"+(hasUpdate || hasDelete ? "," : ""));
            if (hasUpdate || hasDelete)
            {
                sb.AppendLine("\tevents : {");
                if (hasUpdate)
                    sb.AppendLine("\t\t'click .button.edit' : 'editModel'" + (hasDelete ? "," : ""));
                if (hasDelete)
                    sb.AppendLine("\t\t'click .button.delete' : 'deleteModel'");
                sb.AppendLine("\t},");
                if (hasUpdate)
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

		private string _RecurAddRenderModelPropertyCode(string prop,Type PropType,string modelstring)
		{
            string className = PropType.FullName.Replace(".", " ");
            foreach (ModelViewClass mvc in PropType.GetCustomAttributes(typeof(ModelViewClass), false))
                className += mvc.ClassName + " ";
            string code = "";
			foreach (PropertyInfo pi in PropType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
		    {
			    if (pi.GetCustomAttributes(typeof(ModelIgnoreProperty), false).Length == 0)
			    {
				    if (pi.GetCustomAttributes(typeof(ReadOnlyModelProperty), false).Length == 0){
                        Type ptype = pi.PropertyType;
                        if (new List<Type>(ptype.GetInterfaces()).Contains(typeof(IModel))){
                            code += (code.EndsWith(">'") ? "+" : "") + "'<span class=\"" + className + " " + pi.Name + "\">'+" + _RecurAddRenderModelPropertyCode(pi.Name,ptype,string.Format(modelstring, pi.Name)+".get('{0}')") + "+'</span>'";
                        }else{
                            code += (code.EndsWith(">'") ? "+" : "")+"'<span class=\"" + className + " " + pi.Name + "\">'+" + string.Format(modelstring, pi.Name) + "+'</span>'";
                        }
                    }
			    }
		    }
            return code;
		}

        #region IJSGenerator Members

        public string GenerateJS(Type modelType, string host, List<string> readOnlyProperties, List<string> properties)
        {
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
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("//Org.Reddragonit.BackBoneDotNet.JSGenerators.ViewGenerator");
            sb.AppendLine(modelType.FullName + ".View = Backbone.View.extend({");
            sb.AppendLine("\tinitialize : function(){");
            sb.AppendLine("\t\tthis.model.on('change',this.render,this);");
            sb.AppendLine("\t},");
            string tag = "div";
            if (modelType.GetCustomAttributes(typeof(ModelViewTag), false).Length > 0)
                tag = ((ModelViewTag)modelType.GetCustomAttributes(typeof(ModelViewTag), false)[0]).TagName;
            sb.AppendLine("\ttagName : \"" + tag + "\",");
            
            _AppendClassName(modelType, sb);
            _AppendRenderFunction(modelType,tag, properties, hasUpdate, hasDelete, sb);

            sb.AppendLine("});");
            return sb.ToString();
        }

        #endregion
    }
}
