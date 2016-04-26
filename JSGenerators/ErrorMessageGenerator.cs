using System;
using System.Collections.Generic;
using System.Text;
using Org.Reddragonit.BackBoneDotNet.Interfaces;
using Org.Reddragonit.BackBoneDotNet.Attributes;
using System.Collections;

namespace Org.Reddragonit.BackBoneDotNet.JSGenerators
{
    /*
     * This generator extends the Backbone Error Messages component to include the Error Messages defined in the given model class.
     */
    internal class ErrorMessageGenerator : IJSGenerator
    {
        #region IJSGenerator Members

        public string GenerateJS(Type modelType, string host, List<string> readOnlyProperties, List<string> properties, List<string> viewIgnoreProperties, bool hasUpdate, bool hasAdd, bool hasDelete,bool minimize)
        {
            WrappedStringBuilder sb = new WrappedStringBuilder(minimize);
            sb.Append((!minimize ? "//Org.Reddragonit.BackBoneDotNet.JSGenerators.ErrorMessageGenerator\n" : ""));
            foreach (ModelErrorMessage mem in modelType.GetCustomAttributes(typeof(ModelErrorMessage), false))
            {
                sb.AppendLine(string.Format("Backbone.DefineErrorMessage('{0}','{1}','{2}');", new object[]{
                    mem.language,
                    mem.MessageName,
                    mem.Message.Replace("'","\\'")
                }));
            }
            return sb.ToString();
        }

        #endregion
    }
}
