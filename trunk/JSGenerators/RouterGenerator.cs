using System;
using System.Collections.Generic;
using System.Text;
using Org.Reddragonit.BackBoneDotNet.Interfaces;
using Org.Reddragonit.BackBoneDotNet.Attributes;
using System.Text.RegularExpressions;

namespace Org.Reddragonit.BackBoneDotNet.JSGenerators
{
    /*
     * This generator extends the Router code with additional hash routers found in the Model Definition
     */
    public class RouterGenerator : IJSGenerator
    {
        private static readonly Regex _REG_PARS = new Regex(":([^/\\)]+)", RegexOptions.Compiled | RegexOptions.ECMAScript);

        #region IJSGenerator Members

        public string GenerateJS(Type modelType, string host, List<string> readOnlyProperties, List<string> properties, List<string> viewIgnoreProperties, bool hasUpdate, bool hasAdd, bool hasDelete)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("//Org.Reddragonit.BackBoneDotNet.JSGenerators.RouterGenerator");
            Dictionary<string, List<BackboneHashRoute>> routes = new Dictionary<string, List<BackboneHashRoute>>();
            foreach (BackboneHashRoute bhr in modelType.GetCustomAttributes(typeof(BackboneHashRoute), false))
            {
                List<BackboneHashRoute> rts = new List<BackboneHashRoute>();
                if (routes.ContainsKey(bhr.RouterName))
                {
                    rts = routes[bhr.RouterName];
                    routes.Remove(bhr.RouterName);
                }
                rts.Add(bhr);
                routes.Add(bhr.RouterName, rts);
            }
            if (routes.Count > 0)
            {
                foreach (string routerName in routes.Keys)
                {
                    if (routerName.Contains("."))
                    {
                        string tmp = "";
                        foreach (string str in ModelNamespace.GetFullNameForModel(modelType, host).Split('.'))
                        {
                            sb.AppendLine(string.Format("{1}{0} = {2}{0} || {{}};",
                                str,
                                (tmp.Length == 0 ? "var " : tmp+"."),
                                (tmp.Length == 0 ? "" : tmp + ".")));
                            tmp += (tmp.Length == 0 ? "" : ".") + str;
                        }
                    }
                    else
                        sb.AppendLine(string.Format("var {0} = {0} || {{}};", routerName));
                    sb.AppendLine(string.Format(
@"if (!Backbone.History.started){{
    Backbone.history.start({{ pushState: false }});
}}
if ({0}.navigate == undefined){{
    {0} = new Backbone.Router();
}}", routerName));
                    foreach (BackboneHashRoute bhr in routes[routerName])
                    {
                        sb.AppendFormat(@"{0}.route('{1}','{2}',function(",routerName,bhr.Path,bhr.FunctionName);
                        if (bhr.Path.Contains(":"))
                        {
                            foreach (Match m in _REG_PARS.Matches(bhr.Path))
                                sb.Append(m.Groups[1].Value + ",");
                            sb.Length = sb.Length - 1;
                        }
                        sb.AppendLine(string.Format("){{ {0} }});", bhr.Code));
                    }
                }
            }
            return sb.ToString();
        }

        #endregion
    }
}
