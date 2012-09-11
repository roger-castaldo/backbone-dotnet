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
                    fstring = "'<td class=\"'+this.className+' {0}\">'+this.model.get('{0}')+'</td>'{1}";
                    break;
                case "ul":
                case "ol":
                    fstring = "'<li class=\"'+this.className+' {0}\">'+this.model.get('{0}')+'</li>'{1}";
                    break;
                default:
                    fstring = "'<" + tag + " class=\"'+this.className+' {0}\">'+this.model.get('{0}')+'</" + tag + ">'{1}";
                    break;
            }
            sb.Append("\t\t$(this.el).html(");
            foreach (string prop in properties)
                sb.Append(string.Format(fstring, prop,(properties.IndexOf(prop) == properties.Count-1 ? "" : "+")));
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
