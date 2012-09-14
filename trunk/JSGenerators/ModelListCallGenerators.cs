using System;
using System.Collections.Generic;
using System.Text;
using Org.Reddragonit.BackBoneDotNet.Interfaces;
using System.Reflection;
using Org.Reddragonit.BackBoneDotNet.Attributes;

namespace Org.Reddragonit.BackBoneDotNet.JSGenerators
{
    internal class ModelListCallGenerators : IJSGenerator
    {
        #region IJSGenerator Members

        public string GenerateJS(Type modelType, string host, List<string> readOnlyProperties, List<string> properties)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("//Org.Reddragonit.BackBoneDotNet.JSGenerators.ModelListCallGenerators");
            foreach (MethodInfo mi in modelType.GetMethods(BindingFlags.Public | BindingFlags.Static))
            {
                if (mi.GetCustomAttributes(typeof(ModelListMethod), false).Length > 0)
                {
                    foreach (ModelListMethod mlm in mi.GetCustomAttributes(typeof(ModelListMethod), false))
                    {
                        if (mlm.Host == host || mlm.Host == "*")
                        {
                            sb.Append(modelType.FullName + "." + mi.Name + " = function(");
                            for (int x = 0; x < mi.GetParameters().Length; x++)
                            {
                                sb.Append((x == 0 ? "" : ",") + mi.GetParameters()[x].Name);
                            }
                            sb.AppendLine("){");
                            sb.AppendLine(URLUtility.CreateJavacriptUrlCode(mlm, mi, modelType));
                            sb.AppendLine("var ret = Backbone.Collection.extend({url:url,model:" + modelType.FullName + ".Model});");
                            sb.AppendLine("ret = new ret();");
                            sb.AppendLine("ret.fetch();");
                            sb.AppendLine("return ret;");
                            sb.AppendLine("}");
                            break;
                        }
                    }
                }
            }
            return sb.ToString();
        }

        #endregion
    }
}
