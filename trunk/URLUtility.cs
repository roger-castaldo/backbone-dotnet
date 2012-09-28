using System;
using System.Collections.Generic;
using System.Text;
using Org.Reddragonit.BackBoneDotNet.Attributes;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Org.Reddragonit.BackBoneDotNet
{
    internal static class URLUtility
    {
        //DateTime format yyyymmddHHMMss

        private static Regex _regListPars = new Regex("\\{(\\d+)\\}", RegexOptions.Compiled | RegexOptions.ECMAScript);
        private static readonly DateTime _UTC = new DateTime(1970, 1, 1, 00, 00, 00);

        internal static string GenerateRegexForURL(ModelListMethod mlm, MethodInfo mi)
        {
            string ret = "^(GET\t";
            if (mlm.Host == "*")
                ret += ".+";
            else
                ret+=mlm.Host;
            if (mi.GetParameters().Length > 0)
            {
                ParameterInfo[] pars = mi.GetParameters();
                string[] regexs = new string[pars.Length];
                for (int x = 0; x < pars.Length; x++)
                    regexs[x] = _GetRegexStringForParameter(pars[x]);
                string path = string.Format(mlm.Path, regexs);
                ret += (path.StartsWith("/") ? path : "/" + path).TrimEnd('/');
            }
            else
                ret += (mlm.Path.StartsWith("/") ? mlm.Path : "/" + mlm.Path).TrimEnd('/');
            return ret+")$";
        }

        private static string _GetRegexStringForParameter(ParameterInfo parameterInfo)
        {
            string ret = ".+";
            Type ptype = parameterInfo.ParameterType;
            bool nullable = false;
            if (ptype.FullName.StartsWith("System.Nullable"))
            {
                nullable = true;
                if (ptype.IsGenericType)
                    ptype = ptype.GetGenericArguments()[0];
                else
                    ptype = ptype.GetElementType();
            }
            if (ptype == typeof(DateTime))
                ret = "(\\d+"+(nullable ? "|NULL" : "")+")";
            else if (ptype == typeof(int) ||
                ptype == typeof(long) ||
                ptype == typeof(short) ||
                ptype == typeof(byte))
                ret = "(-?\\d+" + (nullable ? "|NULL" : "") + ")";
            else if (ptype == typeof(uint) ||
                ptype == typeof(ulong) ||
                ptype == typeof(ushort))
                ret = "(-?\\d+" + (nullable ? "|NULL" : "") + ")";
            else if (ptype == typeof(double) ||
                ptype == typeof(decimal) ||
                ptype == typeof(float))
                ret = "(-?\\d+(.\\d+)?" + (nullable ? "|NULL" : "") + ")";
            else if (ptype == typeof(bool))
                ret = "(true|false)";
            return ret;
        }

        internal static object[] ExtractParametersForUrl(MethodInfo mi, Uri url, string path,bool isPaged)
        {
            object[] ret = new object[0];
            ParameterInfo[] pars = mi.GetParameters();
            if (pars.Length > 0)
            {
                List<string> spars = new List<string>();
                List<int> indexes = new List<int>();
                ret = new object[pars.Length];
                string surl = url.AbsolutePath;
                while (path.Contains("{"))
                {
                    if (path[0] == '{')
                    {
                        indexes.Add(int.Parse(path.Substring(1, path.IndexOf('}')-1)));
                        path = path.Substring(path.IndexOf('}') + 1);
                        if (path.Contains("{"))
                        {
                            spars.Add(surl.Substring(0, surl.IndexOf(path.Substring(0, path.IndexOf("{")))));
                            surl = surl.Substring(0, surl.IndexOf(path.Substring(0, path.IndexOf("{"))));
                        }
                        else if (path == "")
                        {
                            spars.Add(surl);
                            surl = "";
                        }
                        else
                        {
                            spars.Add(surl.Substring(0, surl.IndexOf(path)));
                            surl = surl.Substring(0, surl.IndexOf(path));
                        }
                    }
                    else
                    {
                        path = path.Substring(1);
                        surl = surl.Substring(1);
                    }
                }
                for(int x=0;x<indexes.Count;x++){
                    ret[indexes[x]] = _ConvertParameterValue(spars[x],pars[x].ParameterType);
                }
                if (isPaged)
                {
                    string[] qpars = url.Query.Split('&');
                    ret[ret.Length - 3] = _ConvertParameterValue(qpars[0].Substring(qpars[0].IndexOf("=") + 1), pars[ret.Length-3].ParameterType);
                    ret[ret.Length - 2] = _ConvertParameterValue(qpars[1].Substring(qpars[1].IndexOf("=") + 1), pars[ret.Length - 2].ParameterType);
                    ret[ret.Length - 1] = null;
                }
            }
            return ret;
        }

        private static object _ConvertParameterValue(string p, Type type)
        {
            if (p == "NULL")
                return null;
            else if (type == typeof(DateTime))
                return _UTC.AddMilliseconds(long.Parse(p));
            else if (type == typeof(int))
                return int.Parse(p);
            else if (type == typeof(long))
                return long.Parse(p);
            else if (type == typeof(short))
                return short.Parse(p);
            else if (type == typeof(byte))
                return byte.Parse(p);
            else if (type == typeof(uint))
                return uint.Parse(p);
            else if (type == typeof(ulong))
                return ulong.Parse(p);
            else if (type == typeof(ushort))
                return ushort.Parse(p);
            else if (type == typeof(double))
                return double.Parse(p);
            else if (type == typeof(decimal))
                return decimal.Parse(p);
            else if (type == typeof(float))
                return float.Parse(p);
            else if (type == typeof(bool))
                return bool.Parse(p);
            else
                return p;
        }

        internal static string CreateJavacriptUrlCode(ModelListMethod mlm,MethodInfo mi, Type modelType)
        {
            ParameterInfo[] pars = mi.GetParameters();
            if (pars.Length > 0)
            {
                string[] pNames = new string[pars.Length];
                StringBuilder sb = new StringBuilder();
                for (int x = 0; x < (mlm.Paged ? pars.Length -3 : pars.Length); x++)
                {
                    sb.AppendLine("if (" + pars[x].Name + " == undefined){");
                    sb.AppendLine("\t" + pars[x].Name + " = null;");
                    sb.AppendLine("}");
                    sb.AppendLine("if (" + pars[x].Name + " == null){");
                    sb.AppendLine("\t" + pars[x].Name + " = 'NULL';");
                    sb.AppendLine("}");
                    if (pars[x].ParameterType == typeof(bool))
                        sb.AppendLine(pars[x].Name + " = ((" + pars[x].Name + " == null ? 'false' : ("+pars[x].Name+" ? 'true' : 'false'));");
                    else if (pars[x].ParameterType == typeof(DateTime)){
                        sb.AppendLine("if (" + pars[x].Name + " != 'NULL'){");
                        sb.AppendLine("\tif (!(" + pars[x].Name + " instanceof Date)){");
                        sb.AppendLine("\t\t" + pars[x].Name + " = new Date(" + pars[x].Name + ");");
                        sb.AppendLine("\t}");
                        sb.AppendLine("\t"+pars[x].Name + " = " + pars[x].Name + ".UTC();");
                        sb.AppendLine("}");
                    }
                    pNames[x] = "'+"+pars[x].Name+"+'";
                }
                sb.AppendLine("var url='" + string.Format((mlm.Path.StartsWith("/") ? mlm.Path : "/" + mlm.Path).TrimEnd('/'), pNames) + "';");
                return sb.ToString();
            }
            else
                return "var url='" + (mlm.Path.StartsWith("/") ? mlm.Path : "/" + mlm.Path).TrimEnd('/') + "';";
        }

    }
}
