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
        private static readonly DateTime _UTC = new DateTime(1970, 1, 1, 00, 00, 00,DateTimeKind.Utc);

        internal static string[] SplitUrl(string url)
        {
            List<string> tret = new List<string>();
            if (url.Contains("?"))
            {
                string[] tmp = url.Trim('/').Split('?');
                foreach (string s in tmp[0].Split('/'))
                {
                    tret.Add(s);
                    tret.Add("/");
                }
                tret.RemoveAt(tret.Count - 1);
                tret.Add("?");
                tmp[1] = Uri.UnescapeDataString(tmp[1]);
                foreach (string str in tmp[1].Split('&'))
                {
                    if (str.Length > 0)
                    {
                        if (str.Contains("="))
                        {
                            foreach (string s in str.Split('='))
                            {
                                if (s != "")
                                {
                                    tret.Add(s);
                                    tret.Add("=");
                                }
                            }
                            if (tret[tret.Count - 1] == "=")
                                tret.RemoveAt(tret.Count - 1);
                        }
                        else if (str.Length > 0)
                            tret.Add(str);
                        tret.Add("&");
                    }
                }
                if (tret[tret.Count - 1] == "&")
                    tret.RemoveAt(tret.Count - 1);
            }
            else
            {
                foreach (string s in url.Trim('/').Split('/'))
                {
                    tret.Add(s);
                    tret.Add("/");
                }
                tret.RemoveAt(tret.Count - 1);
            }
            return tret.ToArray();

        }

        internal static string GenerateRegexForURL(ModelListMethod mlm, MethodInfo mi)
        {
            Logger.Debug("Generating regular expression for model list method at path " + mlm.Path);
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
                {
                    Logger.Trace("Adding parameter " + pars[x].Name+"["+pars[x].ParameterType.FullName+"]");
                    regexs[x] = _GetRegexStringForParameter(pars[x]);
                }
                string path = string.Format((mlm.Path+(mlm.Paged ? (mlm.Path.Contains("?") ? "&" : "?")+"PageStartIndex={"+(regexs.Length-3).ToString()+"}&PageSize={"+(regexs.Length-2).ToString()+"}" : "")).Replace("?","\\?"), regexs);
                ret += (path.StartsWith("/") ? path : "/" + path).TrimEnd('/');
            }
            else
                ret += (mlm.Path.StartsWith("/") ? mlm.Path : "/" + mlm.Path).Replace("?", "\\?").TrimEnd('/');
            Logger.Trace("Regular expression constructed: " + ret + ")$");
            return ret+")$";
        }

        internal static string GenerateRegexForSelectListQueryString(string path,MethodInfo mi)
        {
            Logger.Debug("Generating regular expression for select list method " + mi.Name+" in type "+mi.DeclaringType.FullName);
            if (mi.GetParameters().Length == 0)
                return "^(SELECT\t"+path+")$";
            else
            {
                string ret = "^(SELECT\t"+path+"\\?";
                foreach (ParameterInfo pi in mi.GetParameters())
                    ret += pi.Name + "=" + _GetRegexStringForParameter(pi) + "&";
                return ret.Substring(0,ret.Length-1)+")$";
            }
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
            else if (ptype.IsEnum)
            {
                ret = "(";
                foreach (string str in Enum.GetNames(ptype))
                    ret += str + "|";
                ret = ret.Substring(0, ret.Length - 1) +(nullable ? "|NULL" : "")+ ")";
            }
            return ret;
        }

        internal static object[] ExtractParametersForUrl(MethodInfo mi, Uri url, string path,bool isPaged)
        {
            Logger.Debug("Extracting parameters (for model list method) from url " + url.AbsolutePath + (path.Contains("?") ? "?" + url.Query : ""));
            object[] ret = new object[0];
            ParameterInfo[] pars = mi.GetParameters();
            if (pars.Length > 0)
            {
                ret = new object[pars.Length];
                path += (isPaged ? (path.Contains("?") ? "&" : "?")+"PageStartIndex={"+(ret.Length-3).ToString()+"}&PageSize={"+(ret.Length-2).ToString()+"}" : "");
                string[] surl = SplitUrl(url.AbsolutePath + (path.Contains("?") || isPaged ? url.Query : ""));
                string[] spath = SplitUrl(path);
                int x = 0;
                int y = 0;
                while (x < spath.Length)
                {
                    if (spath[x] == surl[y])
                    {
                        x++;
                        y++;
                    }
                    else if (spath[x].StartsWith("{") && spath[x].EndsWith("}"))
                    {
                        int index = int.Parse(spath[x].TrimStart('{').TrimEnd('}'));
                        string par = "";
                        if (x == spath.Length - 1)
                        {
                            while (y < surl.Length)
                            {
                                par += surl[y];
                                y++;
                                if (y < surl.Length)
                                {
                                    if (surl[y] == "&" && isPaged)
                                        break;
                                }
                            }
                        }
                        else
                        {
                            while (surl[x + 1] != spath[y])
                            {
                                par += surl[y];
                                y++;
                            }
                        }
                        ret[index] = _ConvertParameterValue(par, pars[index].ParameterType);
                        x++;
                    }
                }
                if (isPaged)
                    ret[ret.Length - 1] = null;
            }
            return ret;
        }

        private static object _ConvertParameterValue(string p, Type type)
        {
            p = Uri.UnescapeDataString(p);
            Logger.Trace("Converting \"" + p + "\" to " + type.FullName);
            if (type.IsGenericType)
                type = type.GetGenericArguments()[0];
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
            else if (type.IsEnum)
                return Enum.Parse(type, p);
            else
                return p;
        }

        internal static string CreateJavacriptUrlCode(ModelListMethod mlm,MethodInfo mi, Type modelType)
        {
            Logger.Debug("Creating the javascript url call for the model list method at path " + mlm.Path);
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
                        sb.AppendLine(pars[x].Name + " = (" + pars[x].Name + " == null ? 'false' : ("+pars[x].Name+" ? 'true' : 'false'));");
                    else if (pars[x].ParameterType == typeof(DateTime)){
                        sb.AppendLine("if (" + pars[x].Name + " != 'NULL'){");
                        sb.AppendLine("\tif (!(" + pars[x].Name + " instanceof Date)){");
                        sb.AppendLine("\t\t" + pars[x].Name + " = new Date(" + pars[x].Name + ");");
                        sb.AppendLine("\t}");
                        sb.AppendLine("\t" + pars[x].Name + " = Date.UTC(" + pars[x].Name + ".getUTCFullYear(), " + pars[x].Name + ".getUTCMonth(), " + pars[x].Name + ".getUTCDate(),  " + pars[x].Name + ".getUTCHours(), " + pars[x].Name + ".getUTCMinutes(), " + pars[x].Name + ".getUTCSeconds());");
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
