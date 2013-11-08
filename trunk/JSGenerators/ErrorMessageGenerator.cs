using System;
using System.Collections.Generic;
using System.Text;
using Org.Reddragonit.BackBoneDotNet.Interfaces;
using Org.Reddragonit.BackBoneDotNet.Attributes;
using System.Collections;

namespace Org.Reddragonit.BackBoneDotNet.JSGenerators
{
    internal class ErrorMessageGenerator : IJSGenerator
    {
        private void _RecurAdd(string[] messageName, string message, int index, ref Hashtable ht)
        {
            if (index == messageName.Length-1)
            {
                ht.Remove(messageName[index]);
                ht.Add(messageName[index], message);
            }
            else
            {
                Hashtable tmp = new Hashtable();
                if (ht.ContainsKey(messageName[index]))
                {
                    tmp = (Hashtable)ht[messageName[index]];
                    ht.Remove(messageName[index]);
                }
                _RecurAdd(messageName, message, index + 1, ref tmp);
                ht.Add(messageName[index], tmp);
            }
        }

        private void _RecurWrite(StringBuilder sb, Hashtable msgs,string indent)
        {
            string[] keys = new string[msgs.Keys.Count];
            msgs.Keys.CopyTo(keys,0);
            for(int x=0;x<keys.Length;x++)
            {
                sb.Append(indent + "'" + keys[x] + "' : ");
                if (msgs[keys[x]] is string)
                    sb.Append("'" + msgs[keys[x]].ToString().Replace("'", "\'") + "'");
                else
                {
                    sb.AppendLine(indent+"\t{");
                    _RecurWrite(sb, (Hashtable)msgs[keys[x]], indent + "\t");
                    sb.Append(indent+"\t}");
                }
                sb.AppendLine((x == keys.Length - 1 ? "" : ","));
            }
        }

        #region IJSGenerator Members

        public string GenerateJS(Type modelType, string host, List<string> readOnlyProperties, List<string> properties, List<string> viewIgnoreProperties, bool hasUpdate, bool hasAdd, bool hasDelete)
        {
            Hashtable msgs = new Hashtable();
            foreach (ModelErrorMessage mem in modelType.GetCustomAttributes(typeof(ModelErrorMessage), false))
            {
                Hashtable ht = new Hashtable();
                if (msgs.Contains(mem.language))
                {
                    ht = (Hashtable)msgs[mem.language];
                    msgs.Remove(mem.language);
                }
                _RecurAdd(mem.MessageName.Split('.'), mem.Message, 0, ref ht);
                msgs.Add(mem.language, ht);
            }
            StringBuilder sb = new StringBuilder();
            if (msgs.Count > 0)
            {
                sb.AppendLine(
@"//Org.Reddragonit.BackBoneDotNet.JSGenerators.ErrorMessageGenerator
_.extend(true,Backbone.ErrorMessages,{");
                _RecurWrite(sb, msgs,"\t");
                sb.AppendLine("});");
            }
            return sb.ToString();
        }

        #endregion
    }
}
