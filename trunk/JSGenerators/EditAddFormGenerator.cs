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
    internal class EditAddFormGenerator : IJSGenerator
    {

        private void _RenderDialogConstructCode(Type modelType,List<string> readOnlyProperties, List<string> properties, StringBuilder sb){
            sb.AppendLine("\t\tif($('#" + modelType.FullName.Replace(".","_") + "_dialog').length==0){");
            sb.AppendLine("\t\t\tvar dlog = $('<div></div>');");
            sb.AppendLine("\t\t\tdlog.attr('id','" + modelType.FullName.Replace(".","_") + "_dialog');");
            sb.AppendLine("\t\t\tdlog.attr('class',view.className+' dialog');");
            sb.AppendLine("\t\t\tvar tbl = $('<table></table>');");
            sb.AppendLine("\t\t\tdlog.append(tbl);");
            sb.AppendLine("\t\t\ttbl.append('<thead><tr><th colspan=\"2\"></th></tr></thead>');");
            sb.AppendLine("\t\t\ttbl.append('<tbody></tbody>');");
            sb.AppendLine("\t\t\ttbl = $(tbl.children()[1]);");
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
                        sb.Append("\t\t\ttbl.append($('<tr><td class=\"fieldName\">" + propName + "</td><td class=\"fieldInput " + propType.Name + "\" proptype=\"" + propType.Name + "\">");
                        if (new List<Type>(propType.GetInterfaces()).Contains(typeof(IModel)))
                            sb.Append("<select name=\"" + propName + "\" "+(array ? "multiple=\"multiple\"" : "")+"></select>");
                        else if (propType.IsEnum)
                        {
                            sb.Append("<select name=\"" + propName + "\" " + (array ? "multiple=\"multiple\"" : "") + ">");
                            foreach (string str in Enum.GetNames(propType))
                                sb.Append("<option value=\"" + str + "\">" + str + "</option>");
                            sb.Append("</select>");
                        }
                        else
                        {
                            if (array)
                                sb.Append("<input type=\"text\" name=\"" + propName + "\" proptype=\"" + propType.FullName + "\"/><span class=\"button add\">+</span>");
                            else
                                sb.Append("<input type=\"text\" name=\"" + propName + "\" proptype=\""+propType.FullName+"\"/>");
                        }
                        sb.AppendLine("</td></tr>'));");
                    }
                }
            }
            sb.AppendLine("\t\t\tvar butAdds = tbl.find('span.button.add');");
            sb.AppendLine("\t\t\tfor(var x=0;x<butAdds.length;x++){");
            sb.AppendLine("\t\t\t\t$(butAdds[x]).bind('click',{button:$(butAdds[x])},function(event){");
            sb.AppendLine("\t\t\t\t\tvar inp = $(event.data.button.prev()).clone();");
            sb.AppendLine("\t\t\t\t\tinp.val('');");
            sb.AppendLine("\t\t\t\t\tevent.data.button.before('<br/>');");
            sb.AppendLine("\t\t\t\t\tevent.data.button.before(inp);");
            sb.AppendLine("\t\t\t\t});");
            sb.AppendLine("\t\t\t}");
            sb.AppendLine("\t\t\ttbl.append($('<tr><td colspan=\"2\" style=\"text-align:center\"><span class=\"button accept\">Okay</span><span class=\"button cancel\">Cancel</span></td></tr>'));");
            sb.AppendLine("\t\t\tvar butCancel = $(dlog.find('tr>td>span.cancel')[0]);");
            sb.AppendLine("\t\t\tbutCancel.bind('click',function(){");
            sb.AppendLine("\t\t\t\t$('#" + modelType.FullName.Replace(".","_") + "_dialog').hide();");
            sb.AppendLine("\t\t\t\t$('#Org_Reddragonit_BackBoneDotNet_DialogBackground').hide();");
            sb.AppendLine("\t\t\t});");
            sb.AppendLine("\t\t\t$(document.body).append(dlog);");
            sb.AppendLine("\t\t}");
        }

        private void _RenderDialogCode(Type modelType, List<string> readOnlyProperties, List<string> properties, StringBuilder sb)
        {
            _RenderDialogConstructCode(modelType,readOnlyProperties,properties,sb);
            sb.AppendLine("\t\tif($('#Org_Reddragonit_BackBoneDotNet_DialogBackground').length==0){");
            sb.AppendLine("\t\t\t$(document.body).append($('<div id=\"Org_Reddragonit_BackBoneDotNet._DialogBackground\" class=\"Org Reddragonit BackBoneDotNet DialogBackground\"></div>'));");
            sb.AppendLine("\t\t}");
            sb.AppendLine("\t\t$('#Org_Reddragonit_BackBoneDotNet_DialogBackground').show();");
            sb.AppendLine("\t\tvar dlog = $('#" + modelType.FullName.Replace(".","_") + "_dialog');");
            StringBuilder sbAcceptFunction = new StringBuilder();
            sbAcceptFunction.AppendLine("\t\t\tvar dlog = $('#" + modelType.FullName.Replace(".","_") + "_dialog');");
            sbAcceptFunction.AppendLine("\t\t\tvar model = event.data.view.model;");
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
                            sb.AppendLine("\t\tvar sel" + propName + " = $(dlog.find('select[name=\"" + propName + "\"]')[0]);");
                            sb.AppendLine("\t\tvar opts = " + propType.FullName + ".SelectList();");
                            sb.AppendLine("\t\tfor(var x=0;x<opts.length;x++){");
                            sb.AppendLine("\t\t\tvar opt = opts[x];");
                            sb.AppendLine("\t\t\tsel" + propName + ".append($('<option value=\"'+opt.ID+'\">'+opt.Text+'</option>'));");
                            sb.AppendLine("\t\t}");
                            if (array)
                            {
                                sb.AppendLine("\t\tfor(var x=0;x<view.model.get('" + propName + "').length;x++){");
                                sb.AppendLine("\t\t\t$(sel.find('option[value=\"'+view.model.get('" + propName + "')[x].get('id')+'\"]')[0]).attr('selected', 'selected');");
                                sb.AppendLine("\t\t}");
                                sbAcceptFunction.AppendLine("\t\t\tvar ar" + propName + " = [];");
                                sbAcceptFunction.AppendLine("\t\t\tvar opts" + propName + " = dlog.find('select[name=\"" + propName + "\"]'>option:selected');");
                                sbAcceptFunction.AppendLine("\t\t\tfor(var x=0;x<opts"+propName+".length;x++){");
                                sbAcceptFunction.AppendLine("\t\t\t\tar" + propName + ".push(new " + propType.FullName + ".Model({id:$(opts" + propName + "[x]).val()}));");
                                sbAcceptFunction.AppendLine("\t\t\t}");
                                sbAcceptFunction.AppendLine("\t\t\tmodel.set({" + propName + " : ar" + propName + ");");
                            }
                            else
                            {
                                sb.AppendLine("\t\tsel.val(view.model.get('" + propName + "').id);");
                                sbAcceptFunction.AppendLine("\t\t\tmodel.set({" + propName + ": new " + propType.FullName + ".Model({id:$(dlog.find('select[name=\"" + propName + "\"]>option:selected')[0]).val()})});");
                            }
                        }
                        else if (propType.IsEnum)
                        {
                            if (array)
                            {
                                sb.AppendLine("\t\tvar sel" + propName + " = $(dlog.find('select[name=\"" + propName + "\"]')[0]);");
                                sb.AppendLine("\t\tfor(var x=0;x<view.model.get('" + propName + "').length;x++){");
                                sb.AppendLine("\t\t\t$(sel.find('option[value=\"'+view.model.get('" + propName + "')[x]+'\"]')[0]).attr('selected', 'selected');");
                                sb.AppendLine("\t\t}");
                                sbAcceptFunction.AppendLine("\t\t\tvar ar" + propName + " = [];");
                                sbAcceptFunction.AppendLine("\t\t\tvar opts" + propName + " = dlog.find('select[name=\"" + propName + "\"]'>option:selected');");
                                sbAcceptFunction.AppendLine("\t\t\tfor(var x=0;x<opts" + propName + ".length;x++){");
                                sbAcceptFunction.AppendLine("\t\t\t\tar" + propName + ".push($(opts" + propName + "[x]).val());");
                                sbAcceptFunction.AppendLine("\t\t\t}");
                                sbAcceptFunction.AppendLine("\t\t\tmodel.set({" + propName + " : ar" + propName + ");");
                            }
                            else
                            {
                                sb.AppendLine("\t\t$(dlog.find('select[name=\"" + propName + "\"]')[0]).val(view.model.get('" + propName + "'));");
                                sbAcceptFunction.AppendLine("\t\t\tmodel.set({" + propName + ": $(dlog.find('select[name=\"" + propName + "\"]>option:selected')[0]).val()});");
                            }
                        }
                        else
                        {
                            if (array)
                            {
                                sb.AppendLine("\t\tvar ins = dlog.find('input[name=\""+propName+"\"]');");
                                sb.AppendLine("\t\tfor(var x=1;x<ins.length;x++){");
                                sb.AppendLine("\t\t\t$(ins[x]).remove();");
                                sb.AppendLine("\t\t}");
                                sb.AppendLine("\t\tvar inp = $(ins[0]);");
                                sb.AppendLine("\t\tfor(var x=0;x<view.model.get('" + propName + "').length;x++){");
                                sb.AppendLine("\t\t\tinp.val(view.model.get('" + propName + "'));");
                                sb.AppendLine("\t\t\tif (x<model.get('"+propName+"').length-1){");
                                sb.AppendLine("\t\t\t\tvar newInp = inp.clone();");
                                sb.AppendLine("\t\t\t\tinp.after(newInp);");
                                sb.AppendLine("\t\t\t\tinp.after('<br/>');");
                                sb.AppendLine("\t\t\t\tinp = newInp;");
                                sb.AppendLine("\t\t\t}");
                                sb.AppendLine("\t\t}");
                                sbAcceptFunction.AppendLine("\t\t\tvar ar" + propName + " = [];");
                                sbAcceptFunction.AppendLine("\t\t\tvar opts" + propName + " = dlog.find('input[name=\"" + propName + "\"] > option:selected');");
                                sbAcceptFunction.AppendLine("\t\t\tfor(var x=0;x<opts"+propName+".length;x++){");
                                sbAcceptFunction.AppendLine("\t\t\t\tar" + propName + ".push($(opts" + propName + "[x]).val());");
                                sbAcceptFunction.AppendLine("\t\t\t}");
                                sbAcceptFunction.AppendLine("\t\t\tmodel.set({" + propName + ": ar"+propName+"});");
                            }
                            else
                            {
                                sb.AppendLine("\t\t$(dlog.find('input[name=\"" + propName + "\"]')[0]).val(view.model.get('" + propName + "'));");
                                sbAcceptFunction.AppendLine("\t\t\tmodel.set({" + propName + ": $(dlog.find('input[name=\"" + propName + "\"]')[0]).val()});");
                            }
                        }
                    }
                }
            }
            sb.AppendLine("\t\tvar butAccept = $(dlog.find('tr>td>span.accept')[0]);");
            sb.AppendLine("\t\tbutAccept.unbind('click');");
            sb.AppendLine("\t\tbutAccept.bind('click',{view:view},function(event){");
            sb.Append(sbAcceptFunction.ToString());
            sb.AppendLine("\t\t});");
            sb.AppendLine("\t\tdlog.show();");
        }

        private void _RenderInlineCode(Type modelType, List<string> readOnlyProperties, List<string> properties, StringBuilder sb)
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

            sb.AppendLine("\t\tvar el = view.$el;");
            sb.AppendLine("\t\t$(el.find('" + tstring + ".buttons>span.button')).hide();");
            StringBuilder sbAcceptFunction = new StringBuilder();
            sbAcceptFunction.AppendLine("\t\t\tvar view = event.data.view;");
            sbAcceptFunction.AppendLine("\t\t\tvar el = view.$el;");
            sbAcceptFunction.AppendLine("\t\t\tvar model = view.model;");
            sbAcceptFunction.AppendLine("\t\t\tvar changes = {};");
            
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
                            sb.AppendLine("\t\t$(el.find('" + tstring + "." + propName + "')[0]).html($('<select name=\"" + propName + "\" "+(array ? "multiple=\"multiple\"" : "")+"></select>'));");
                            sb.AppendLine("\t\tvar sel" + propName + " = $(el.find('select[name=\"" + propName + "\"]')[0]);");
                            sb.AppendLine("\t\tvar opts = " + propType.FullName + ".SelectList();");
                            sb.AppendLine("\t\tfor(var x=0;x<opts.length;x++){");
                            sb.AppendLine("\t\t\tvar opt = opts[x];");
                            sb.AppendLine("\t\t\tsel"+propName+".append($('<option value=\"'+opt.ID+'\">'+opt.Text+'</option>'));");
                            sb.AppendLine("\t\t}");
                            if (array)
                            {
                                sb.AppendLine("\t\tfor(var x=0;x<view.model.get('"+propName+"').length;x++){");
                                sb.AppendLine("\t\t\t$(sel" + propName + ".find('option[value=\"'+view.model.get('" + propName + "')[x].get('id')+'\"]')[0]).attr('selected','selected');");
                                sb.AppendLine("\t\t}");
                                sbAcceptFunction.AppendLine("\t\t\t\tvar opts" + propName + " = el.find('select[name=\"" + propName + "\"]>option:selected');");
                                sbAcceptFunction.AppendLine("\t\t\t\tif (opts"+propName+".length>0){");
                                sbAcceptFunction.AppendLine("\t\t\t\t\tchanges." + propName + " = [];");
                                sbAcceptFunction.AppendLine("\t\t\t\t\tfor(var x=0;x<opts" + propName + ".length;x++){");
                                sbAcceptFunction.AppendLine("\t\t\t\t\t\tchanges." + propName + ".push(new " + propType.FullName + ".Model({id:$(opts" + propName + "[x]).val()}));");
                                sbAcceptFunction.AppendLine("\t\t\t\t\t}");
                                sbAcceptFunction.AppendLine("\t\t\t\t}");
                            }
                            else
                            {
                                sb.AppendLine("\t\t$(el.find('select[name=\"" + propName + "\"]')[0]).val(view.model.get('" + propName + "').id);");
                                sbAcceptFunction.AppendLine("\t\t\t\tif (model.get('" + propName + "').get('id') != $(el.find('select[name=\"" + propName + "\"]>option:selected')[0]).val()){");
                                sbAcceptFunction.AppendLine("\t\t\t\t\tchanges." + propName + " = new " + propType.FullName + ".Model({id:$(el.find('select[name=\"" + propName + "\"]>option:selected')[0]).val()});");
                                sbAcceptFunction.AppendLine("\t\t\t\t}");
                            }
                        }
                        else if (propType.IsEnum)
                        {
                            sb.AppendLine("\t\t$(el.find('" + tstring + "." + propName + "')[0]).html($('<select name=\"" + propName + "\" "+(array ? "multiple=\"multiple\"" : "")+"></select>'));");
                            sb.AppendLine("\t\tvar sel" + propName + " = $(el.find('select[name=\"" + propName + "\"]')[0]);");
                            foreach (string str in Enum.GetNames(propType))
                                sb.AppendLine("\t\tsel.append('<option value=\"" + str + "\">" + str + "</option>');");
                            if (array)
                            {
                                sb.AppendLine("\t\tfor(var x=0;x<view.model.get('" + propName + "').length;x++){");
                                sb.AppendLine("\t\t\t$(sel.find('option[value=\"'+view.model.get('" + propName + "')[x]+'\"]')[0]).attr('selected','selected');");
                                sb.AppendLine("\t\t}");
                                sbAcceptFunction.AppendLine("\t\t\t\tvar opts" + propName + " = el.find('select[name=\"" + propName + "\"]>option:selected');");
                                sbAcceptFunction.AppendLine("\t\t\t\tif (opts" + propName + ".length>0){");
                                sbAcceptFunction.AppendLine("\t\t\t\t\tchanges." + propName + " = [];");
                                sbAcceptFunction.AppendLine("\t\t\t\t\tfor(var x=0;x<opts.length;x++){");
                                sbAcceptFunction.AppendLine("\t\t\t\t\t\tchanges.push($(opts[x]).val());");
                                sbAcceptFunction.AppendLine("\t\t\t\t\t}");
                                sbAcceptFunction.AppendLine("\t\t\t\t}");
                            }
                            else
                            {
                                sb.AppendLine("\t\t$(el.find('select[name=\"" + propName + "\"]')[0]).val(view.model.get('" + propName + "'));");
                                sbAcceptFunction.AppendLine("\t\t\t\tif (model.get('" + propName + "') != $(el.find('select[name=\"" + propName + "\"]>option:selected')[0]).val()){");
                                sbAcceptFunction.AppendLine("\t\t\t\t\tchanges." + propName + " = $(el.find('select[name=\"" + propName + "\"]>option:selected')[0]).val();");
                                sbAcceptFunction.AppendLine("\t\t\t\t}");
                            }
                        }
                        else
                        {
                            sb.AppendLine("\t\t$(el.find('" + tstring + "." + propName + "')[0]).html($('<input type=\"text\" name=\"" + propName + "\" proptype=\"" + propType.FullName + "\"/>"+(array ? "<span class=\"button add\">+</span>" : "")+"'));");
                            if (array)
                            {
                                sb.AppendLine("\t\tvar inp = $(el.find('input[name=\"" + propName + "\"]')[0]);");
                                sb.AppendLine("\t\tfor(var x=0;x<view.model.get('" + propName + "').length;x++){");
                                sb.AppendLine("\t\t\tinp.val(view.model.get('" + propName + "'));");
                                sb.AppendLine("\t\t\tif (x<model.get('" + propName + "').length-1){");
                                sb.AppendLine("\t\t\t\tvar newInp = inp.clone();");
                                sb.AppendLine("\t\t\t\tinp.after(newInp);");
                                sb.AppendLine("\t\t\t\tinp.after('<br/>');");
                                sb.AppendLine("\t\t\t\tinp = newInp;");
                                sb.AppendLine("\t\t\t}");
                                sb.AppendLine("\t\t}");
                                sbAcceptFunction.AppendLine("\t\t\tvar ar" + propName + " = [];");
                                sbAcceptFunction.AppendLine("\t\t\tvar opts" + propName + " = dlog.find('input[name=\"" + propName + "\"]');");
                                sbAcceptFunction.AppendLine("\t\t\tfor(var x=0;x<opts" + propName + ".length;x++){");
                                sbAcceptFunction.AppendLine("\t\t\t\tar" + propName + ".push($(opts" + propName + "[x]).val());");
                                sbAcceptFunction.AppendLine("\t\t\t}");
                                sbAcceptFunction.AppendLine("\t\t\tvar changed" + propName + "=false;");
                                sbAcceptFunction.AppendLine("\t\t\tif(ar"+propName+".length!=model.get('"+propName+"').length){");
                                sbAcceptFunction.AppendLine("\t\t\t\tchanged=true;");
                                sbAcceptFunction.AppendLine("\t\t\t}else{");
                                sbAcceptFunction.AppendLine("\t\t\t\tfor(var x=0;x<ar" + propName + ".length;x++){");
                                sbAcceptFunction.AppendLine("\t\t\t\t\tif(ar" + propName + " != model.get('" + propName + "')[x]){");
                                sbAcceptFunction.AppendLine("\t\t\t\t\t\tchanged=true;");
                                sbAcceptFunction.AppendLine("\t\t\t\t\t\tx=ar" + propName + ".length;");
                                sbAcceptFunction.AppendLine("\t\t\t\t\t}");
                                sbAcceptFunction.AppendLine("\t\t\t\t}");
                                sbAcceptFunction.AppendLine("\t\t\t}");
                                sbAcceptFunction.AppendLine("\t\t\tif(changed){");
                                sbAcceptFunction.AppendLine("\t\t\t\tchanges." + propName + " = ar" + propName + ";");
                                sbAcceptFunction.AppendLine("\t\t\t}");
                            }
                            else
                            {
                                sb.AppendLine("\t\t$(el.find('input[name=\"" + propName + "\"]')[0]).val(view.model.get('" + propName + "'));");
                                sbAcceptFunction.AppendLine("\t\t\t\tif (model.get('" + propName + "') != $(el.find('input[name=\"" + propName + "\"]')[0]).val()){");
                                sbAcceptFunction.AppendLine("\t\t\t\t\tchanges." + propName + " = $(el.find('input[name=\"" + propName + "\"]')[0]).val();");
                                sbAcceptFunction.AppendLine("\t\t\t\t}");
                            }
                        }
                    }
                }
            }

            sb.AppendLine("\t\t\t$(el.find('"+tstring+".buttons')[0]).append($('<span class=\"button accept\">Okay</span><span class=\"button cancel\">Cancel</span>'));");
            sb.AppendLine("\t\t\tvar butCancel = $(el.find('"+tstring+".buttons>span.cancel')[0]);");
            sb.AppendLine("\t\t\tbutCancel.bind('click',{view:view},function(event){");
            sb.AppendLine("\t\t\t\tevent.data.view.render();");
            sb.AppendLine("\t\t\t});");

            sb.AppendLine("\t\t\tvar butOkay = $(el.find('"+tstring+".buttons>span.accept')[0]);");
            sb.AppendLine("\t\t\tbutOkay.bind('click',{view:view},function(event){");
            sb.AppendLine(sbAcceptFunction.ToString());
            sb.AppendLine("\t\t\t\tif (!_.isEmpty(changes)){");
            sb.AppendLine("\t\t\t\t\tmodel.set(changes);");
            sb.AppendLine("\t\t\t\t\tmodel.save();");
            sb.AppendLine("\t\t\t\t}else{");
            sb.AppendLine("\t\t\t\t\tview.render();");
            sb.AppendLine("\t\t\t\t}");
            sb.AppendLine("\t\t\t});");
        }

        #region IJSGenerator Members

        public string GenerateJS(Type modelType, string host, List<string> readOnlyProperties, List<string> properties)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("//Org.Reddragonit.BackBoneDotNet.JSGenerators.EditAddFormGenerator");
            sb.AppendLine(modelType.FullName + ".editModel = function(view){");

            ModelEditAddTypes meat = ModelEditAddTypes.dialog;
            if (modelType.GetCustomAttributes(typeof(ModelEditAddType), false).Length > 0)
                meat = ((ModelEditAddType)modelType.GetCustomAttributes(typeof(ModelEditAddType), false)[0]).Type;
            switch (meat)
            {
                case ModelEditAddTypes.dialog:
                    _RenderDialogCode(modelType, readOnlyProperties, properties,sb);
                    break;
                case ModelEditAddTypes.inline:
                    _RenderInlineCode(modelType, readOnlyProperties, properties, sb);
                    break;
            }

            sb.AppendLine("}");
            return sb.ToString();
        }

        #endregion
    }
}
