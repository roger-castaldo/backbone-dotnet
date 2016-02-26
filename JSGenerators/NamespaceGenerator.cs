using System;
using System.Collections.Generic;
using System.Text;
using Org.Reddragonit.BackBoneDotNet.Interfaces;
using Org.Reddragonit.BackBoneDotNet.Attributes;
using Org.Reddragonit.BackBoneDotNet.Properties;

namespace Org.Reddragonit.BackBoneDotNet.JSGenerators
{
    /*
     * This generator generates the namespaced object for the given model type
     */
    internal class NamespaceGenerator : IJSGenerator
    {
        #region IJSGenerator Members

        public string GenerateJS(Type modelType, string host, List<string> readOnlyProperties, List<string> properties, List<string> viewIgnoreProperties, bool hasUpdate, bool hasAdd, bool hasDelete,bool minimize)
        {
            if (RequestHandler.UseAppNamespacing)
                return @"window.App=window.App||{};window.App.Models=window.App.Models||{};window.App.Views=window.App.Views||{};window.App.Collections=window.App.Collections||{};window.App.CollectionViews=window.App.CollectionViews||{};window.App.Forms=window.App.Forms||{};";
            else
            {
                string ret = "";
                string tmp = "";
                foreach (string str in ModelNamespace.GetFullNameForModel(modelType, host).Split('.'))
                {
                    ret += (minimize ?
                        (tmp.Length == 0 ? "window." + str : tmp + "." + str) + "=" + (tmp.Length == 0 ? "window." : tmp + ".") + str + "||{};"
                        : (tmp.Length == 0 ? "window." + str : tmp + "." + str) + " = " + (tmp.Length == 0 ? "window." : tmp + ".") + str + " || {};" + Environment.NewLine);
                    tmp += (tmp.Length == 0 ? "" : ".") + str;
                }
                return ret;
            }
        }

        #endregion
    }
}
