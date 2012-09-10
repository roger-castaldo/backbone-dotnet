using System;
using System.Collections.Generic;
using System.Text;
using Org.Reddragonit.BackBoneDotNet.Interfaces;
using System.Text.RegularExpressions;
using Org.Reddragonit.BackBoneDotNet.Attributes;
using System.Reflection;
using System.Threading;
using System.Collections;
using Org.Reddragonit.BackBoneDotNet.JSGenerators;

namespace Org.Reddragonit.BackBoneDotNet
{
    public static class RequestHandler
    {
        private const int _CACHE_TIMER_PERIOD = 60000;
        private const int _CACHE_TIMEOUT_MINUTES = 60;

        public enum StartTypes{
            DisableInvalidModels,
            ThrowInvalidExceptions
        }

        private static Regex _REG_URL;
        private static bool _running;
        private static Dictionary<string, MethodInfo> _LoadAlls;
        private static Dictionary<string, MethodInfo> _Loads;
        private static Dictionary<string, MethodInfo> _SelectLists;
        private static Dictionary<string, CachedItemContainer> _CachedJScript;
        private static Dictionary<string, Type> _TypeMaps;
        private static string _jqueryURL;
        private static string _jsonURL;
        private static string _backboneURL;
        private static Timer _cacheTimer;

        private static List<Type> _invalidModels;
        public static List<Type> InvalidModels
        {
            get { return _invalidModels; }
        }

        private static void _CleanJSCache(object obj)
        {
            lock (_CachedJScript)
            {
                DateTime now = DateTime.Now;
                string[] keys = new string[_CachedJScript.Count];
                _CachedJScript.Keys.CopyTo(keys, 0);
                foreach (string str in keys)
                {
                    if (now.Subtract(_CachedJScript[str].LastAccess).TotalMinutes > _CACHE_TIMEOUT_MINUTES)
                        _CachedJScript.Remove(str);
                }
            }
        }

        public static void Start(StartTypes startType,string jqueryURL,string jsonURL,string backboneURL)
        {
            List<Exception> errors = DefinitionValidator.Validate(out _invalidModels);
            if (startType == StartTypes.ThrowInvalidExceptions && errors.Count > 0)
                throw new ModelValidationException(errors);
            if (startType==StartTypes.DisableInvalidModels)
                _invalidModels = new List<Type>();
            _LoadAlls = new Dictionary<string, MethodInfo>();
            _Loads = new Dictionary<string, MethodInfo>();
            _SelectLists = new Dictionary<string, MethodInfo>();
            _CachedJScript = new Dictionary<string, CachedItemContainer>();
            _TypeMaps = new Dictionary<string, Type>();
            _cacheTimer = new Timer(new TimerCallback(_CleanJSCache), null, 0, _CACHE_TIMER_PERIOD);
            string reg = "^(";
            foreach (Type t in Utility.LocateTypeInstances(typeof(IModel)))
            {
                if (!_invalidModels.Contains(t))
                    AppendURLRegex(ref reg, t);
            }
            _jqueryURL = jqueryURL;
            _jsonURL = jsonURL;
            _backboneURL = backboneURL;
            if (_jqueryURL != null)
            {
                _jqueryURL = (!_jqueryURL.StartsWith("/") ? "/" + _jqueryURL : "");
                reg += (reg.EndsWith(")") ? "|" : "") + "(GET\t.+" + _jqueryURL + ")";
            }
            if (_jsonURL != null)
            {
                _jsonURL = (!_jsonURL.StartsWith("/") ? "/" + _jsonURL : "");
                reg += (reg.EndsWith(")") ? "|" : "") + "(GET\t.+" + _jsonURL + ")";
            }
            if (_backboneURL != null)
            {
                _backboneURL = (!_backboneURL.StartsWith("/") ? "/" + _backboneURL : "");
                reg += (reg.EndsWith(")") ? "|" : "") + "(GET\t.+" + _backboneURL + ")";
            }
            reg += ")$";
            _REG_URL = new Regex(reg, RegexOptions.ECMAScript | RegexOptions.Compiled);
            _running = true;
        }

        private static void AppendURLRegex(ref string reg, Type t)
        {
            if (reg.EndsWith(")"))
                reg += "|(";
            else
                reg += "(";
            foreach (ModelRoute mr in t.GetCustomAttributes(typeof(ModelRoute), false))
            {
                _TypeMaps.Add(mr.Host + (mr.Path.StartsWith("/") ? mr.Path : "/" + mr.Path).TrimEnd('/'), t);
                bool hasAdd = true;
                bool hasUpdate = true;
                bool hasDelete = true;
                MethodInfo LoadAll = null;
                MethodInfo Load = null;
                MethodInfo SelectList = null;
                if (t.GetCustomAttributes(typeof(ModelBlockActions),false).Length > 0)
                {
                    ModelBlockActions mba = (ModelBlockActions)t.GetCustomAttributes(typeof(ModelBlockActions),false)[0];
                    hasAdd = ((int)mba.Type & (int)ModelActionTypes.Add) == 0;
                    hasUpdate = ((int)mba.Type & (int)ModelActionTypes.Edit) == 0;
                    hasDelete = ((int)mba.Type & (int)ModelActionTypes.Delete) == 0;
                }
                foreach (MethodInfo mi in t.GetMethods(BindingFlags.Static | BindingFlags.Public))
                {
                    if (mi.GetCustomAttributes(typeof(ModelLoadAllMethod), false).Length > 0)
                        LoadAll = mi;
                    else if (mi.GetCustomAttributes(typeof(ModelLoadMethod), false).Length > 0)
                        Load = mi;
                    else if (mi.GetCustomAttributes(typeof(ModelSelectListMethod), false).Length > 0)
                        SelectList = mi;
                }
                _Loads.Add(mr.Host + (mr.Path.StartsWith("/") ? mr.Path : "/" + mr.Path).TrimEnd('/'), Load);
                string methods = "((";
                if (LoadAll != null)
                {
                    methods += "GET";
                    _LoadAlls.Add(mr.Host + (mr.Path.StartsWith("/") ? mr.Path : "/" + mr.Path).TrimEnd('/'), LoadAll);
                }
                if (hasAdd)
                    methods += (methods.EndsWith("(") ? "" : "|") + "POST";
                if (SelectList != null)
                {
                    methods += (methods.EndsWith("(") ? "" : "|") + "SELECT";
                    _SelectLists.Add(mr.Host + (mr.Path.StartsWith("/") ? mr.Path : "/" + mr.Path).TrimEnd('/'), SelectList);
                }
                methods += ")";
                if (methods!="(()")
                    reg += (reg.EndsWith(")") ? "|" : "") + methods + "\t" + (mr.Host == "*" ? ".+" : mr.Host) + (mr.Path.StartsWith("/") ? mr.Path : "/" + mr.Path).TrimEnd('/') + ")";
                string adds = "(GET";
                if (hasUpdate)
                    adds += "|PUT";
                if (hasDelete)
                    adds += "|DELETE";
                adds += ")";
                reg += (reg.EndsWith(")") ? "|" : "") + "("+adds+"\t" + (mr.Host == "*" ? ".+" : mr.Host) + (mr.Path.StartsWith("/") ? mr.Path : "/" + mr.Path) + (mr.Path.EndsWith("/") ? "" : "/") + ".+)";
            }
            foreach (ModelJSFilePath mj in t.GetCustomAttributes(typeof(ModelJSFilePath), false))
            {
                if (!reg.Contains("(" + (mj.Host == "*" ? ".+" : mj.Host) + (mj.Path.StartsWith("/") ? mj.Path : "/" + mj.Path) + ")"))
                    reg += (reg.EndsWith(")") ? "|" : "") + "(GET\t" + (mj.Host == "*" ? ".+" : mj.Host) + (mj.Path.StartsWith("/") ? mj.Path : "/" + mj.Path) + ")";
            }
            reg += ")";
        }

        public static void Stop()
        {
            _running = false;
            _REG_URL = null;
            _invalidModels = null;
        }

        public static bool HandlesURL(Uri url,string method)
        {
            if (_running)
                return _REG_URL.IsMatch(method.ToUpper()+"\t"+url.Host+url.AbsolutePath);
            return false;
        }

        public static void HandleRequest(IHttpRequest request)
        {
            if (request.URL.AbsolutePath.EndsWith(".js") && request.Method.ToUpper() == "GET")
            {
                request.SetResponseContentType("text/javascript");
                if (request.URL.AbsolutePath == (_jqueryURL == null ? "" : _jqueryURL))
                {
                    request.SetResponseStatus(200);
                    request.WriteContent(Utility.ReadEmbeddedResource("Org.Reddragonit.BackBoneDotNet.resources.jquery.min.js"));
                    request.SendResponse();
                }
                else if (request.URL.AbsolutePath == (_jsonURL == null ? "" : _jsonURL))
                {
                    request.SetResponseStatus(200);
                    request.WriteContent(Utility.ReadEmbeddedResource("Org.Reddragonit.BackBoneDotNet.resources.json2.min.js"));
                    request.SendResponse();
                }
                else if (request.URL.AbsolutePath == (_backboneURL == null ? "" : _backboneURL))
                {
                    request.SetResponseStatus(200);
                    request.WriteContent(Utility.ReadEmbeddedResource("Org.Reddragonit.BackBoneDotNet.resources.underscore-min.js"));
                    request.WriteContent(Utility.ReadEmbeddedResource("Org.Reddragonit.BackBoneDotNet.resources.backbone.min.js"));
                    request.SendResponse();
                }
                else
                {
                    bool found = false;
                    lock (_CachedJScript)
                    {
                        if (_CachedJScript.ContainsKey(request.URL.Host+request.URL.AbsolutePath))
                        {
                            found = true;
                            request.SetResponseStatus(200);
                            request.WriteContent((string)_CachedJScript[request.URL.Host+request.URL.AbsolutePath].Value);
                            request.SendResponse();
                        }
                    }
                    if (!found)
                    {
                        StringBuilder sb = new StringBuilder();
                        foreach (Type t in Utility.LocateTypeInstances(typeof(IModel)))
                        {
                            foreach (ModelJSFilePath mj in t.GetCustomAttributes(typeof(ModelJSFilePath), false))
                            {
                                if ((mj.Host == "*" || mj.Host == request.URL.Host) && mj.Path == request.URL.AbsolutePath)
                                    sb.Append(_GenerateModelJSFile(t, request.URL.Host));
                            }
                        }
                        request.SetResponseStatus(200);
                        request.WriteContent(sb.ToString());
                        request.SendResponse();
                        lock (_CachedJScript)
                        {
                            if (!_CachedJScript.ContainsKey(request.URL.Host + request.URL.AbsolutePath))
                                _CachedJScript.Add(request.URL.Host + request.URL.AbsolutePath, new CachedItemContainer(sb.ToString()));
                        }
                    }
                }
            }
            else
            {
                Object ret = null;
                switch (request.Method.ToUpper())
                {
                    case "GET":
                        if (_LoadAlls.ContainsKey(request.URL.Host + request.URL.AbsolutePath))
                            ret = _LoadAlls[request.URL.Host + request.URL.AbsolutePath].Invoke(null, new object[0]);
                        else if (_LoadAlls.ContainsKey("*" + request.URL.AbsolutePath))
                            ret = _LoadAlls["*" + request.URL.AbsolutePath].Invoke(null, new object[0]);
                        else if (_Loads.ContainsKey(request.URL.Host + request.URL.AbsolutePath.Substring(0, request.URL.AbsolutePath.LastIndexOf("/"))))
                            ret = _Loads[request.URL.Host + request.URL.AbsolutePath.Substring(0, request.URL.AbsolutePath.LastIndexOf("/"))].Invoke(null, new object[] { request.URL.AbsolutePath.Substring(request.URL.AbsolutePath.LastIndexOf("/") + 1) });
                        else if (_Loads.ContainsKey("*" + request.URL.AbsolutePath.Substring(0, request.URL.AbsolutePath.LastIndexOf("/"))))
                            ret = _Loads["*" + request.URL.AbsolutePath.Substring(0, request.URL.AbsolutePath.LastIndexOf("/"))].Invoke(null, new object[] { request.URL.AbsolutePath.Substring(request.URL.AbsolutePath.LastIndexOf("/") + 1) });
                        break;
                    case "SELECT":
                        if (_SelectLists.ContainsKey(request.URL.Host + request.URL.AbsolutePath))
                            ret = _SelectLists[request.URL.Host + request.URL.AbsolutePath].Invoke(null, new object[0]);
                        else if (_SelectLists.ContainsKey("*" + request.URL.AbsolutePath))
                            ret = _SelectLists["*" + request.URL.AbsolutePath].Invoke(null, new object[0]);
                        break;
                    case "PUT":
                        if (_Loads.ContainsKey(request.URL.Host + request.URL.AbsolutePath.Substring(0, request.URL.AbsolutePath.LastIndexOf("/"))))
                            ret = _Loads[request.URL.Host + request.URL.AbsolutePath.Substring(0, request.URL.AbsolutePath.LastIndexOf("/"))].Invoke(null, new object[] { request.URL.AbsolutePath.Substring(request.URL.AbsolutePath.LastIndexOf("/") + 1) });
                        else if (_Loads.ContainsKey("*" + request.URL.AbsolutePath.Substring(0, request.URL.AbsolutePath.LastIndexOf("/"))))
                            ret = _Loads["*" + request.URL.AbsolutePath.Substring(0, request.URL.AbsolutePath.LastIndexOf("/"))].Invoke(null, new object[] { request.URL.AbsolutePath.Substring(request.URL.AbsolutePath.LastIndexOf("/") + 1) });
                        if (ret != null)
                        {
                            Hashtable ht = (Hashtable)JSON.JsonDecode(request.ParameterContent);
                            foreach (string str in ht.Keys)
                                ret.GetType().GetProperty(str).SetValue(ret, _ConvertObjectToType(ht[str], ret.GetType().GetProperty(str).PropertyType), new object[0]);
                            ret = ((IModel)ret).Update();
                        }
                        break;
                    case "DELETE":
                        if (_Loads.ContainsKey(request.URL.Host + request.URL.AbsolutePath.Substring(0, request.URL.AbsolutePath.LastIndexOf("/"))))
                            ret = _Loads[request.URL.Host + request.URL.AbsolutePath.Substring(0, request.URL.AbsolutePath.LastIndexOf("/"))].Invoke(null, new object[] { request.URL.AbsolutePath.Substring(request.URL.AbsolutePath.LastIndexOf("/") + 1) });
                        else if (_Loads.ContainsKey("*" + request.URL.AbsolutePath.Substring(0, request.URL.AbsolutePath.LastIndexOf("/"))))
                            ret = _Loads["*" + request.URL.AbsolutePath.Substring(0, request.URL.AbsolutePath.LastIndexOf("/"))].Invoke(null, new object[] { request.URL.AbsolutePath.Substring(request.URL.AbsolutePath.LastIndexOf("/") + 1) });
                        if (ret != null)
                            ret = ((IModel)ret).Delete();
                        break;
                    case "POST":
                        Type t = null;
                        if (_TypeMaps.ContainsKey(request.URL.Host + request.URL.AbsolutePath))
                            t = _TypeMaps[request.URL.Host + request.URL.AbsolutePath];
                        else if (_TypeMaps.ContainsKey("*"+request.URL.AbsolutePath))
                            t = _TypeMaps["*"+request.URL.AbsolutePath];
                        if (t!=null){
                            IModel mod = (IModel)t.GetConstructor(Type.EmptyTypes).Invoke(new object[0]);
                            Hashtable mht = (Hashtable)JSON.JsonDecode(request.ParameterContent);
                            foreach (string str in mht.Keys)
                            {
                                if (t.GetProperty(str).GetCustomAttributes(typeof(ReadOnlyModelProperty),true).Length==0)
                                    t.GetProperty(str).SetValue(mod, _ConvertObjectToType(mht[str], t.GetProperty(str).PropertyType), new object[0]);
                            }
                            if (mod.Save())
                            {
                                ret = new Hashtable();
                                ((Hashtable)ret).Add("id",mod.id);
                            }else
                                ret=null;
                        }
                        break;
                }
                if (ret != null)
                {
                    request.SetResponseStatus(200);
                    request.SetResponseContentType("application/json");
                    request.WriteContent(JSON.JsonEncode(ret));
                    request.SendResponse();
                }
                else
                {
                    request.SetResponseStatus(404);
                    request.SendResponse();
                }
            }
        }

        private static object _ConvertObjectToType(object obj, Type expectedType)
        {
            if (expectedType.Equals(typeof(bool)) && (obj == null))
                return false;
            if (obj == null)
                return null;
            if (obj.GetType().Equals(expectedType))
                return obj;
            if (expectedType.Equals(typeof(string)))
                return obj.ToString();
            if (expectedType.IsEnum)
                return Enum.Parse(expectedType, obj.ToString());
            try
            {
                object ret = Convert.ChangeType(obj, expectedType);
                return ret;
            }
            catch (Exception e)
            {
            }
            if (expectedType.IsArray || (obj is ArrayList))
            {
                int count = 1;
                Type underlyingType = null;
                if (expectedType.IsGenericType)
                    underlyingType = expectedType.GetGenericArguments()[0];
                else
                    underlyingType = expectedType.GetElementType();
                if (obj is ArrayList)
                    count = ((ArrayList)obj).Count;
                Array ret = Array.CreateInstance(underlyingType, count);
                ArrayList tmp = new ArrayList();
                if (!(obj is ArrayList))
                {
                    tmp.Add(_ConvertObjectToType(obj, underlyingType));
                }
                else
                {
                    for (int x = 0; x < ret.Length; x++)
                    {
                        tmp.Add(_ConvertObjectToType(((ArrayList)obj)[x], underlyingType));
                    }
                }
                tmp.CopyTo(ret);
                return ret;
            }
            else if (expectedType.FullName.StartsWith("System.Collections.Generic.Dictionary"))
            {
                object ret = expectedType.GetConstructor(Type.EmptyTypes).Invoke(new object[0]);
                Type keyType = expectedType.GetGenericArguments()[0];
                Type valType = expectedType.GetGenericArguments()[1];
                foreach (string str in ((Hashtable)obj).Keys)
                {
                    ((IDictionary)ret).Add(_ConvertObjectToType(str, keyType), _ConvertObjectToType(((Hashtable)obj)[str], valType));
                }
                return ret;
            }
            else if (expectedType.FullName.StartsWith("System.Nullable"))
            {
                Type underlyingType = null;
                if (expectedType.IsGenericType)
                    underlyingType = expectedType.GetGenericArguments()[0];
                else
                    underlyingType = expectedType.GetElementType();
                if (obj == null)
                    return null;
                return _ConvertObjectToType(obj, underlyingType);
            }
            else
            {
                object ret=null;
                MethodInfo loadMethod = null;
                if (new List<Type>(expectedType.GetInterfaces()).Contains(typeof(IModel)))
                {
                    foreach (MethodInfo mi in expectedType.GetMethods(BindingFlags.Public | BindingFlags.Static))
                    {
                        if (mi.GetCustomAttributes(typeof(ModelLoadMethod), false).Length > 0)
                        {
                            loadMethod = mi;
                            break;
                        }
                    }
                }
                if (loadMethod == null)
                {
                    ret = expectedType.GetConstructor(Type.EmptyTypes).Invoke(new object[0]);
                    foreach (string str in ((Hashtable)obj).Keys)
                    {
                        PropertyInfo pi = expectedType.GetProperty(str);
                        pi.SetValue(ret, _ConvertObjectToType(((Hashtable)obj)[str], pi.PropertyType), new object[0]);
                    }
                }
                else
                    ret = loadMethod.Invoke(null,new object[]{((Hashtable)obj)["id"]});
                return ret;
            }
        }

        private static readonly IJSGenerator[] _generators = new IJSGenerator[]{
            new NamespaceGenerator(),
            new ModelDefinitionGenerator(),
            new CollectionGenerator(),
            new ViewGenerator(),
            new SelectListCallGenerator(),
            new EditAddFormGenerator()
        };

        private static string _GenerateModelJSFile(Type t,string host)
        {
            string ret = "";
            List<string> properties = new List<string>();
            List<string> readOnlyProperties = new List<string>();
            foreach (PropertyInfo pi in t.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (pi.GetCustomAttributes(typeof(ModelIgnoreProperty), false).Length == 0)
                {
                    if (pi.GetCustomAttributes(typeof(ReadOnlyModelProperty), false).Length > 0)
                        readOnlyProperties.Add(pi.Name);
                    properties.Add(pi.Name);
                }
            }
            foreach (IJSGenerator gen in _generators)
                ret += gen.GenerateJS(t, host, readOnlyProperties, properties);
            return ret;
        }
    }
}
