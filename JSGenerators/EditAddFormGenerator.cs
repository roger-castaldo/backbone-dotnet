using System;
using System.Collections.Generic;
using System.Text;
using Org.Reddragonit.BackBoneDotNet.Interfaces;
using Org.Reddragonit.BackBoneDotNet.Attributes;

namespace Org.Reddragonit.BackBoneDotNet.JSGenerators
{
    internal class EditAddFormGenerator : IJSGenerator
    {
        /* if (hasUpdate || hasDelete)
            {
                ret += "\tevents : {\n" + (hasUpdate ? "\t\t'click .button.edit' : 'editModel'" + (hasDelete ? ",\n" : "\n") : "") + (hasDelete ? "\t\t'click .button.delete' : 'deleteModel'\n" : "") + "\t},\n";
                if (hasUpdate)
                {
                    ret += "\teditModel :{\n";
                    ModelEditAddTypes meat = ModelEditAddTypes.dialog;
                    if (modelType.GetCustomAttributes(typeof(ModelEditAddType), false).Length > 0)
                        meat = ((ModelEditAddType)modelType.GetCustomAttributes(typeof(ModelEditAddType), false)[0]).Type;
                    switch (meat)
                    {
                        case ModelEditAddTypes.dialog:
                            ret += "\t\tif ($('#" + modelType.FullName + ".dialog').length==0){\n";
                            ret += "\t\t\tvar dlog = $('<div></div>');\n";
                            ret += "\t\t\tdlog.attr('id','" + modelType.FullName + ".dialog');\n";
                            ret += "\t\t\tdlog.attr('class',this.className+' dialog');\n";
                            ret += "\t\t\tvar tbl = $('<table></table>');\n";
                            ret += "\t\t\ttbl.append('<thead><tr><th colspan=\"2\"></th></tr></thead>');\n";
                            ret += "\t\t\ttbl.append($('<tbody></tbody>'));\n";
                            ret += "\t\t\ttbl = $(tbl.children()[1]);\n";
                            foreach (string propName in properties)
                            {
                                if (!readOnlyProperties.Contains(propName))
                                {
                                    Type propType = modelType.GetProperty(propName).PropertyType;
                                    ret += "\t\t\ttbl.append('<tr><td class=\"fieldName\">" + propName + "</td><td class=\"fieldInput " + propType.Name + "\" proptype=\"" + propType.Name + "\">";
                                    if (new List<Type>(propType.GetInterfaces()).Contains(typeof(IModel)))
                                    {
                                        List<string> dispFields = new List<string>();
                                        foreach (PropertyInfo pi in propType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                                        {
                                            //if (pi.GetCustomAttributes(typeof(ModelListSelectDisplayField), false).Length > 0)
                                            //    dispFields.Add(pi.Name);
                                        }
                                        ret += "<select name=\"" + propName + "\" ";
                                        if (dispFields.Count > 0)
                                        {
                                            ret += "dispFields=\"";
                                            foreach (string fld in dispFields)
                                                ret += fld + " ";
                                            ret += "\"";
                                        }
                                        ret += " collection=\"" + propType.FullName + ".Collection\"></select>";
                                    }
                                    else if (propType.IsEnum)
                                    {
                                        ret += "<select name=\"" + propName + "\" enumtype=\"" + propType.FullName + "\">";
                                        foreach (string str in Enum.GetNames(propType))
                                            ret += "<option value=\"" + str + "\">" + str + "</option>";
                                        ret += "</select>";
                                    }
                                    else
                                        ret += "<input name=\"" + propName + "\"/>";
                                    ret += "</td></tr>');\n";
                                }
                                else
                                    ret += "\t\t\tdlog.append('<input type=\"hidden\" name=\"" + propName + "\"/>');\n";
                            }
                            ret += "\t\t\ttbl.append($('<tr><td colspan=\"2\" style=\"text-align:center\"><span class=\"button accept\">Okay</span><span class=\"button cancel\">Cancel</span></td></tr>');\n";
                            ret += "\t\t\tvar butCancel = $(dlog.find('tr>td>span.cancel')[0]);\n";
                            ret += "\t\t\tbutCancel.bind('click',function(){\n";
                            ret += "\t\t\t\t$('#" + modelType.FullName + ".dialog').hide();\n";
                            ret += "\t\t\t\t$('#Org.Reddragonit.BackBoneDotNet.DialogBackground').hide();\n";
                            ret += "\t\t});\n";
                            ret += "\t\t$(document.body).append(dlog);\n";
                            ret += "\t\t}\n";
                            ret += "\t\tif($('#Org.Reddragonit.BackBoneDotNet.DialogBackground').length==0){\n";
                            ret += "\t\t\t$(document.body).append($('<div id=\"Org.Reddragonit.BackBoneDotNet.DialogBackground\" class=\"Org Reddragonit BackBoneDotNet DialogBackground\"></div>'));\n";
                            ret += "\t\t}\n";
                            ret += "\t\t$('#Org.Reddragonit.BackBoneDotNet.DialogBackground').show();\n";
                            ret += "\t\tvar dlog = $('#" + modelType.FullName + ".dialog');\n";
                            foreach (string propName in properties)
                            {
                                Type propType = modelType.GetProperty(propName).PropertyType;
                                if (new List<Type>(propType.GetInterfaces()).Contains(typeof(IModel)))
                                {
                                    if (readOnlyProperties.Contains(propName))
                                        ret += "\t\t$(dlog.find('input[name=\"" + propName + "\"]')[0]).val(this.model.get('" + propName + "').id);\n";
                                    else
                                        ret += "\t\t$(dlog.find('select[name=\"" + propName + "\"]')[0]).val(this.model.get('" + propName + "').id);\n";
                                }
                                else if (propType.IsEnum)
                                {
                                    if (readOnlyProperties.Contains(propName))
                                        ret += "\t\t$(dlog.find('input[name=\"" + propName + "\"]')[0]).val(this.model.get('" + propName + "'));\n";
                                    else
                                        ret += "\t\t$(dlog.find('select[name=\"" + propName + "\"]')[0]).val(this.model.get('" + propName + "'));\n";
                                }
                                else
                                {
                                    if (readOnlyProperties.Contains(propName))
                                        ret += "\t\t$(dlog.find('input[name=\"" + propName + "\"]')[0]).val(this.model.get('" + propName + "'));\n";
                                    else
                                        ret += "\t\t$(dlog.find('input[name=\"" + propName + "\"]')[0]).val(this.model.get('" + propName + "'));\n";
                                }
                            }
                            ret += "\t\tvar butAccept = $(dlog.find('tr>td>span.accept')[0]);\n";
                            ret += "\t\tbutAccept.unbind('click');\n";
                            ret += "\t\tbutAccept.bind('click',{el:this.el,model:this.model},function(event){\n'";

                            ret += "\t\t});\n";
                            ret += "\t\tdlog.show();\n";
                            break;
                        case ModelEditAddTypes.inline:
                            break;
                    }
                    ret += "\t},\n";
                }
            }
            if (ret.EndsWith(",\n"))
                ret = ret.Substring(0, ret.Length - 2);
            ret += "});\n"; */


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
                    break;
                case ModelEditAddTypes.inline:
                    break;
            }

            sb.AppendLine("}");
            return sb.ToString();
        }

        #endregion
    }
}
