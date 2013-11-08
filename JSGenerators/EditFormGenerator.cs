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
                            sb.AppendFormat(
@"      var sel{0} = $(frm.find('select[name=""{0}""]')[0]);
        var opts = {1}.SelectList();
        for(var x=0;x<opts.length;x++){{
            var opt = opts[x];
            sel{0}.append($('<option value=""'+opt.ID+'"">'+opt.Text+'</option>'));
        }}",
                                propName,
                                ModelNamespace.GetFullNameForModel(propType, host));
                            if (array)
                            {
                                sb.AppendFormat(
@"      for(var x=0;x<view.model.get('{0}').length;x++){{;
            $(sel{0}.find('option[value=""'+view.model.get('{0}')[x].get('id')+'""]')[0]).attr('selected', 'selected');
        }}",
                                    propName);
                            }
                            else
                                sb.AppendLine("\t\tsel.val(view.model.get('" + propName + "').id);");
                        }
                        else if (propType.IsEnum)
                        {
                            if (array)
                            {
                                sb.AppendFormat(
@"      var sel{0} = $(frm.find('select[name=""{0}""]')[0]);
        for(var x=0;x<view.model.get('{0}').length;x++){{
            $(sel.find('option[value=""'+view.model.get('{0}')[x]+'""]')[0]).attr('selected', 'selected');
        }}",
                                    propName);
                            }
                            else
                                sb.AppendLine("\t\t$(frm.find('select[name=\"" + propName + "\"]')[0]).val(view.model.get('" + propName + "'));");
                        }
                        else
                        {
                            if (array)
                            {
                                sb.AppendFormat(
@"      var ins = frm.find('input[name={0}');
        for(var x=1;x<ins.length;x++){{
            $(ins[x]).remove();
        }}
        var inp = $(ins[0]);
        for(var x=0;x<view.model.get('{0}').length;x++){{
            inp.val(view.model.get('{0}'));
            if (x<model.get('{0}').length-1){{
                var newInp = inp.clone();
                inp.after(newInp);
                inp.after('<br/>');
                inp = newInp;
            }}
        }}",propName);
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
            sb.AppendFormat(
@"      if($('#{0}_dialog').length==0){{
            var dlog = $('<div></div>');
            dlog.attr('id','{0}_dialog');
            dlog.attr('class',view.className+' dialog');
            var frm = $('<table></table>');
            dlog.append(frm);
            frm.append('<thead><tr><th colspan=""2""></th></tr></thead>');
            frm.append('<tbody></tbody>');
            frm = $(frm.children()[1]);",
                ModelNamespace.GetFullNameForModel(modelType, host).Replace(".", "_"));
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
            sb.AppendFormat(
@"          frm.append($('<tr><td colspan=""2"" style=""text-align:center""><span class=""button accept"">Okay</span><span class=""button cancel"">Cancel</span></td></tr>'));
            var butCancel = $(dlog.find('tr>td>span.cancel')[0]);
            butCancel.bind('click',function(){{
                $('#{0}_dialog').hide();
                $('#Org_Reddragonit_BackBoneDotNet_DialogBackground').hide();
            }});
            $(document.body).append(dlog);
        }}",
                ModelNamespace.GetFullNameForModel(modelType, host).Replace(".", "_"));
        }

        private void _RenderDialogCode(Type modelType,string host, List<string> readOnlyProperties, List<string> properties, StringBuilder sb)
        {
            _RenderDialogConstructCode(modelType,host,readOnlyProperties,properties,sb);
            sb.AppendLine(
@"      if($('#Org_Reddragonit_BackBoneDotNet_DialogBackground').length==0){
            $(document.body).append($('<div id=""Org_Reddragonit_BackBoneDotNet._DialogBackground"" class=""Org Reddragonit BackBoneDotNet DialogBackground""></div>'));
        }
        $('#Org_Reddragonit_BackBoneDotNet_DialogBackground').show();
        var frm = $('#" + ModelNamespace.GetFullNameForModel(modelType, host).Replace(".", "_") + "_dialog');");
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

            sb.AppendFormat(
@"      var frm = view.$el;
        $(frm.find('{0}.buttons>span.button')).hide();",tstring);
            
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
                            sb.AppendFormat(
@"      $(frm.find('{0}:last')[0]).before($('<{0} class=""'+view.className+' {0}""><span class=""'+view.className+' FieldTitle"">{1}</span><br/></{0}>'));
        $(frm.find('{0}.{1}')[0]).append(inp);",tstring,propName);
                        }else
                            sb.AppendLine("\t\t$(frm.find('" + tstring + "." + propName + "')[0]).html(inp);");
                    }
                }
            }

            _AppendInputSetupCode(sb,host, properties, readOnlyProperties, modelType);
            _AppendArrayInputsCode(sb);

            sb.AppendFormat(
@"          $(frm.find('{0}.buttons')[0]).append($('<span class=""button accept"">Okay</span><span class=""button cancel"">Cancel</span>'));
            var butCancel = $(frm.find('{0}.buttons>span.cancel')[0]);
            butCancel.bind('click',{{view:view}},function(event){{
                event.data.view.render();
            }});",tstring);

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
            sb.AppendFormat(
@"//Org.Reddragonit.BackBoneDotNet.JSGenerators.EditAddFormGenerator
{0} = _.extend(true,{0},{{editModel : function(view){{",
                      ModelNamespace.GetFullNameForModel(modelType, host));

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
