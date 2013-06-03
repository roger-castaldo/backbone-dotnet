using System;
using System.Collections.Generic;
using System.Text;
using Org.Reddragonit.BackBoneDotNet.Interfaces;
using Org.Reddragonit.BackBoneDotNet.Attributes;
using System.Reflection;

namespace Org.Reddragonit.BackBoneDotNet.JSGenerators
{
    /*
     * This generator is used to generate the edit/add form code 
     * to be used by the view/model
     */
    internal class EditFormGenerator : IJSGenerator
    {

        private void _RenderFieldInput(string propName,Type propType,string host,StringBuilder sb)
        {
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
                sb.Append("<select class=\"'+view.className+' " + propName + "\" name=\"" + propName + "\" modeltype=\"" + ModelNamespace.GetFullNameForModel(propType, host) + "\" " + (array ? "multiple=\"multiple\"" : "") + "></select>");
            else if (propType.IsEnum)
            {
                sb.Append("<select class=\"'+view.className+' " + propName + "\" name=\"" + propName + "\" " + (array ? "multiple=\"multiple\"" : "") + ">");
                foreach (string str in Enum.GetNames(propType))
                    sb.Append("<option value=\"" + str + "\">" + str + "</option>");
                sb.Append("</select>");
            }
            else
            {
                if (array)
                    sb.Append("<input class=\"'+view.className+' " + propName + "\" type=\"text\" name=\"" + propName + "\" isarray=\"true\" proptype=\"" + propType.FullName + "\"/><span class=\"button add\">+</span>");
                else if (propType == typeof(bool))
                    sb.Append("<input class=\"'+view.className+' " + propName + " radTrue\" type=\"radio\" name=\"" + propName + "\" proptype=\"" + propType.FullName + "\" value=\"true\"/><label class=\"'+view.className+' " + propName + " lblTrue\">True</label><input class=\"'+view.className+' " + propName + " radFalse\" type=\"radio\" name=\"" + propName + "\" proptype=\"" + propType.FullName + "\" value=\"false\"/><label class=\"'+view.className+' " + propName + " lblFalse\">False</label>");
                else
                    sb.Append("<input class=\"'+view.className+' " + propName + "\" type=\"text\" name=\"" + propName + "\" proptype=\"" + propType.FullName + "\"/>");
            }
        }

        private void _AppendArrayInputsCode(StringBuilder sb)
        {
            sb.Append(Utility.ReadEmbeddedResource("Org.Reddragonit.BackBoneDotNet.resources.arrayInputFormCode.js"));
        }

        private void _AppendInputSetupCode(StringBuilder sb,string host, List<string> properties, List<string> readOnlyProperties, Type modelType)
        {
            foreach (string propName in properties)
            {
                if (propName != "id")
                {
                    if (!readOnlyProperties.Contains(propName))
                    {
                        Type propType = modelType.GetProperty(propName).PropertyType;
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
                            sb.AppendLine("\t\tvar sel" + propName + " = $(frm.find('select[name=\"" + propName + "\"]')[0]);");
                            sb.AppendLine("\t\tvar opts = " + ModelNamespace.GetFullNameForModel(propType, host) + ".SelectList();");
                            sb.AppendLine("\t\tfor(var x=0;x<opts.length;x++){");
                            sb.AppendLine("\t\t\tvar opt = opts[x];");
                            sb.AppendLine("\t\t\tsel" + propName + ".append($('<option value=\"'+opt.ID+'\">'+opt.Text+'</option>'));");
                            sb.AppendLine("\t\t}");
                            if (array)
                            {
                                sb.AppendLine("\t\tfor(var x=0;x<view.model.get('" + propName + "').length;x++){");
                                sb.AppendLine("\t\t\t$(sel" + propName + ".find('option[value=\"'+view.model.get('" + propName + "')[x].get('id')+'\"]')[0]).attr('selected', 'selected');");
                                sb.AppendLine("\t\t}");
                            }
                            else
                                sb.AppendLine("\t\tsel.val(view.model.get('" + propName + "').id);");
                        }
                        else if (propType.IsEnum)
                        {
                            if (array)
                            {
                                sb.AppendLine("\t\tvar sel" + propName + " = $(frm.find('select[name=\"" + propName + "\"]')[0]);");
                                sb.AppendLine("\t\tfor(var x=0;x<view.model.get('" + propName + "').length;x++){");
                                sb.AppendLine("\t\t\t$(sel.find('option[value=\"'+view.model.get('" + propName + "')[x]+'\"]')[0]).attr('selected', 'selected');");
                                sb.AppendLine("\t\t}");
                            }
                            else
                                sb.AppendLine("\t\t$(frm.find('select[name=\"" + propName + "\"]')[0]).val(view.model.get('" + propName + "'));");
                        }
                        else
                        {
                            if (array)
                            {
                                sb.AppendLine("\t\tvar ins = frm.find('input[name=\"" + propName + "\"]');");
                                sb.AppendLine("\t\tfor(var x=1;x<ins.length;x++){");
                                sb.AppendLine("\t\t\t$(ins[x]).remove();");
                                sb.AppendLine("\t\t}");
                                sb.AppendLine("\t\tvar inp = $(ins[0]);");
                                sb.AppendLine("\t\tfor(var x=0;x<view.model.get('" + propName + "').length;x++){");
                                sb.AppendLine("\t\t\tinp.val(view.model.get('" + propName + "'));");
                                sb.AppendLine("\t\t\tif (x<model.get('" + propName + "').length-1){");
                                sb.AppendLine("\t\t\t\tvar newInp = inp.clone();");
                                sb.AppendLine("\t\t\t\tinp.after(newInp);");
                                sb.AppendLine("\t\t\t\tinp.after('<br/>');");
                                sb.AppendLine("\t\t\t\tinp = newInp;");
                                sb.AppendLine("\t\t\t}");
                                sb.AppendLine("\t\t}");
                            }
                            else if (propType==typeof(bool))
                                sb.AppendLine("\t\t$(frm.find('input[name=\"" + propName + "\"][value=\"'+view.model.get('" + propName + "')+'\"]')[0]).prop('checked', true);");
                            else
                                sb.AppendLine("\t\t$(frm.find('input[name=\"" + propName + "\"]')[0]).val(view.model.get('" + propName + "'));");
                        }
                    }
                }
            }
            _AppendAcceptCode(sb);
        }

        private void _AppendAcceptCode(StringBuilder sb)
        {
            sb.Append(Utility.ReadEmbeddedResource("Org.Reddragonit.BackBoneDotNet.resources.editFormAccept.js"));
        }

        private void _RenderDialogConstructCode(Type modelType,string host,List<string> readOnlyProperties, List<string> properties, StringBuilder sb){
            sb.AppendLine("\t\tif($('#" + ModelNamespace.GetFullNameForModel(modelType, host).Replace(".", "_") + "_dialog').length==0){");
            sb.AppendLine("\t\t\tvar dlog = $('<div></div>');");
            sb.AppendLine("\t\t\tdlog.attr('id','" + ModelNamespace.GetFullNameForModel(modelType, host).Replace(".", "_") + "_dialog');");
            sb.AppendLine("\t\t\tdlog.attr('class',view.className+' dialog');");
            sb.AppendLine("\t\t\tvar frm = $('<table></table>');");
            sb.AppendLine("\t\t\tdlog.append(frm);");
            sb.AppendLine("\t\t\tfrm.append('<thead><tr><th colspan=\"2\"></th></tr></thead>');");
            sb.AppendLine("\t\t\tfrm.append('<tbody></tbody>');");
            sb.AppendLine("\t\t\tfrm = $(frm.children()[1]);");
            foreach (string propName in properties)
            {
                if (propName != "id")
                {
                    if (!readOnlyProperties.Contains(propName))
                    {
                        Type propType = modelType.GetProperty(propName).PropertyType;
                        sb.Append("\t\t\tfrm.append($('<tr><td class=\"fieldName\">" + propName + "</td><td class=\"fieldInput " + propType.Name + "\" proptype=\"" + propType.Name + "\">");
                        _RenderFieldInput(propName,propType,host, sb);
                        sb.AppendLine("</td></tr>'));");
                    }
                }
            }
            _AppendArrayInputsCode(sb);
            sb.AppendLine("\t\t\tfrm.append($('<tr><td colspan=\"2\" style=\"text-align:center\"><span class=\"button accept\">Okay</span><span class=\"button cancel\">Cancel</span></td></tr>'));");
            sb.AppendLine("\t\t\tvar butCancel = $(dlog.find('tr>td>span.cancel')[0]);");
            sb.AppendLine("\t\t\tbutCancel.bind('click',function(){");
            sb.AppendLine("\t\t\t\t$('#" + ModelNamespace.GetFullNameForModel(modelType, host).Replace(".", "_") + "_dialog').hide();");
            sb.AppendLine("\t\t\t\t$('#Org_Reddragonit_BackBoneDotNet_DialogBackground').hide();");
            sb.AppendLine("\t\t\t});");
            sb.AppendLine("\t\t\t$(document.body).append(dlog);");
            sb.AppendLine("\t\t}");
        }

        private void _RenderDialogCode(Type modelType,string host, List<string> readOnlyProperties, List<string> properties, StringBuilder sb)
        {
            _RenderDialogConstructCode(modelType,host,readOnlyProperties,properties,sb);
            sb.AppendLine("\t\tif($('#Org_Reddragonit_BackBoneDotNet_DialogBackground').length==0){");
            sb.AppendLine("\t\t\t$(document.body).append($('<div id=\"Org_Reddragonit_BackBoneDotNet._DialogBackground\" class=\"Org Reddragonit BackBoneDotNet DialogBackground\"></div>'));");
            sb.AppendLine("\t\t}");
            sb.AppendLine("\t\t$('#Org_Reddragonit_BackBoneDotNet_DialogBackground').show();");
            sb.AppendLine("\t\tvar frm = $('#" + ModelNamespace.GetFullNameForModel(modelType, host).Replace(".", "_") + "_dialog');");
            _AppendInputSetupCode(sb,host, properties, readOnlyProperties, modelType);
            _AppendAcceptCode(sb);
            sb.AppendLine("\t\tfrm.show();");
        }

        private void _RenderInlineCode(Type modelType,string host, List<string> readOnlyProperties, List<string> properties, List<string> viewIgnoreProperties, StringBuilder sb)
        {
            string tag = "div";
            if (modelType.GetCustomAttributes(typeof(ModelViewTag), false).Length > 0)
                tag = ((ModelViewTag)modelType.GetCustomAttributes(typeof(ModelViewTag), false)[0]).TagName;

            string tstring = "";
            switch (tag.ToLower())
            {
                case "tr":
                    tstring = "td";
                    break;
                case "ul":
                case "ol":
                    tstring = "li";
                    break;
                default:
                    tstring = tag;
                    break;
            }

            sb.AppendLine("\t\tvar frm = view.$el;");
            sb.AppendLine("\t\t$(frm.find('" + tstring + ".buttons>span.button')).hide();");
            
            foreach (string propName in properties)
            {
                if (propName != "id")
                {
                    if (!readOnlyProperties.Contains(propName))
                    {
                        Type propType = modelType.GetProperty(propName).PropertyType;
                        sb.Append("\t\tvar inp = $('");
                        _RenderFieldInput(propName,propType,host, sb);
                        sb.AppendLine("');");
                        if (viewIgnoreProperties.Contains(propName))
                        {
                            sb.AppendLine("\t\t$(frm.find('" + tstring + ":last')[0]).before($('<" + tstring + " class=\"'+view.className+' " + propName + "\"><span class=\"'+view.className+' FieldTitle\">"+propName+"</span><br/></" + tstring + ">'));");
                            sb.AppendLine("\t\t$(frm.find('" + tstring + "." + propName + "')[0]).append(inp);");
                        }else
                            sb.AppendLine("\t\t$(frm.find('" + tstring + "." + propName + "')[0]).html(inp);");
                    }
                }
            }

            _AppendInputSetupCode(sb,host, properties, readOnlyProperties, modelType);
            _AppendArrayInputsCode(sb);

            sb.AppendLine("\t\t\t$(frm.find('"+tstring+".buttons')[0]).append($('<span class=\"button accept\">Okay</span><span class=\"button cancel\">Cancel</span>'));");
            sb.AppendLine("\t\t\tvar butCancel = $(frm.find('"+tstring+".buttons>span.cancel')[0]);");
            sb.AppendLine("\t\t\tbutCancel.bind('click',{view:view},function(event){");
            sb.AppendLine("\t\t\t\tevent.data.view.render();");
            sb.AppendLine("\t\t\t});");

            _AppendAcceptCode(sb);
        }

        #region IJSGenerator Members

        public string GenerateJS(Type modelType, string host, List<string> readOnlyProperties, List<string> properties, List<string> viewIgnoreProperties, bool hasUpdate, bool hasAdd, bool hasDelete)
        {
            if (!hasUpdate)
                    return "";
            else if (modelType.GetCustomAttributes(typeof(ModelBlockJavascriptGeneration), false).Length > 0)
            {
                if (((int)((ModelBlockJavascriptGeneration)modelType.GetCustomAttributes(typeof(ModelBlockJavascriptGeneration), false)[0]).BlockType & (int)ModelBlockJavascriptGenerations.EditForm) == (int)ModelBlockJavascriptGenerations.EditForm)
                    return "";
            }
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("//Org.Reddragonit.BackBoneDotNet.JSGenerators.EditAddFormGenerator");
            sb.AppendLine(ModelNamespace.GetFullNameForModel(modelType, host) + " = _.extend(true," + ModelNamespace.GetFullNameForModel(modelType, host) + ",{editModel : function(view){");

            ModelEditAddTypes meat = ModelEditAddTypes.dialog;
            if (modelType.GetCustomAttributes(typeof(ModelEditAddType), false).Length > 0)
                meat = ((ModelEditAddType)modelType.GetCustomAttributes(typeof(ModelEditAddType), false)[0]).Type;
            switch (meat)
            {
                case ModelEditAddTypes.dialog:
                    _RenderDialogCode(modelType,host, readOnlyProperties, properties,sb);
                    break;
                case ModelEditAddTypes.inline:
                    _RenderInlineCode(modelType,host, readOnlyProperties, properties,viewIgnoreProperties, sb);
                    break;
            }

            sb.AppendLine("}});");
            return sb.ToString();
        }

        #endregion
    }
}
