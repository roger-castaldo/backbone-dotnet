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
using Org.Reddragonit.BackBoneDotNet.Properties;

namespace Org.Reddragonit.BackBoneDotNet
{
    public static class RequestHandler
    {
        //structure to be used for model list call checks
        private struct sModelListCall
        {
            private string _path;
            private Regex _reg;
            private MethodInfo _method;
            private bool _isPaged;

            public string RegString
            {
                get { return _reg.ToString(); }
            }

            public bool HandlesRequest(IHttpRequest request)
            {
                return _reg.IsMatch(request.Method.ToUpper() + "\t" + request.URL + request.URL.AbsolutePath);
            }

            public object HandleRequest(IHttpRequest request)
            {
                object ret = null;
                try
                {
                    object[] pars = URLUtility.ExtractParametersForUrl(_method, request.URL, _path,_isPaged);
                    ret = _method.Invoke(null, pars);
                    if (_isPaged)
                    {
                        Hashtable tmp = new Hashtable();
                        tmp.Add("response", ret);
                        Hashtable pager = new Hashtable();
                        tmp.Add("Pager", pager);
                        pager.Add("TotalPages", pars[pars.Length - 1]);
                        ret = tmp;
                    }
                }
                catch (Exception e)
                {
                    ret = null;
                }
                return ret;
            }

            public sModelListCall(ModelListMethod mlm, MethodInfo method)
            {
                _isPaged = mlm.Paged;
                _path = mlm.Path;
                _reg = new Regex(URLUtility.GenerateRegexForURL(mlm, method),RegexOptions.ECMAScript|RegexOptions.Compiled);
                _method = method;
            }
        }

        //period interval between checks for cleaning up the cache (ms)
        private const int _CACHE_TIMER_PERIOD = 60000;
        //maximum time to cache a javascript file without being accessed (minutes)
        private const int _CACHE_TIMEOUT_MINUTES = 60;

        //how to startup the system as per their names, either disable invalid models or throw 
        //and exception about them
        public enum StartTypes{
            DisableInvalidModels,
            ThrowInvalidExceptions
        }

        //houses the regex used to check and see if a request can be handled by this system
        private static Regex _REG_URL;
        //houses a master regex for model list select calls
        private static Regex _REG_MODELS;
        //flag used to indicate if the handler is running
        private static bool _running;
        //houses all load all calls available, key is the url
        private static Dictionary<string, MethodInfo> _LoadAlls;
        //houses all load calls available, key is the url
        private static Dictionary<string, MethodInfo> _Loads;
        //houses all the select list calls, key is the url
        private static Dictionary<string, MethodInfo> _SelectLists;
        //houses all update methods, key is model type
        private static Dictionary<Type, MethodInfo> _UpdateMethods;
        //houses all delete methods, key is model type
        private static Dictionary<Type, MethodInfo> _DeleteMethods;
        //houses all add methods, key is model type
        private static Dictionary<Type, MethodInfo> _SaveMethods;
        //houses all the model list calls
        private static List<sModelListCall> _ModelListCalls;
        //houses all the cached javascript, key is the url
        private static Dictionary<string, CachedItemContainer> _CachedJScript;
        //houses a mapping from urls to model type definitions
        private static Dictionary<string, Type> _TypeMaps;
        //houses the url for jquery js file
        private static string _jqueryURL;
        //houses the url for json js file
        private static string _jsonURL;
        //houses the url for the backbone js file
        private static string _backboneURL;
        //houses the timer used to clean up cache
        private static Timer _cacheTimer;

        //houses a list of invalid models if StartTypes.DisableInvalidModels is passed for a startup parameter
        private static List<Type> _invalidModels;
        public static List<Type> InvalidModels
        {
            get { return _invalidModels; }
        }

        //called by the cache timer to clean up cached javascript
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

        /*
         * This function will start the request handler, it does this by first validating 
         * all located models, and will react according to the starttype specified.
         * It will also assign jquery,json and backbone urls as specified.
         * It will then create the url check regex, locat all load,loadalls, and select list methods
         * as well as specify types to urls, once complete it flags running to allow for handling requests.
         */
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
            _ModelListCalls = new List<sModelListCall>();
            _DeleteMethods = new Dictionary<Type, MethodInfo>();
            _SaveMethods = new Dictionary<Type, MethodInfo>();
            _UpdateMethods = new Dictionary<Type, MethodInfo>();
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
            reg = "^(";
            foreach (sModelListCall mlc in _ModelListCalls)
                reg += (reg.EndsWith(")") ? "|" : "")+ mlc.RegString.Substring(1).TrimEnd('$');
            reg += ")$";
            _REG_MODELS = new Regex(reg, RegexOptions.Compiled | RegexOptions.ECMAScript);
            _running = true;
        }

        /*
         * Method called to append the regular expression code chunk to the existing regulare expression
         * to handle the supplied model type.  it does this through checking the different specified 
         * attributes to specify hosts, available commands, etc.
         */
        private static void AppendURLRegex(ref string reg, Type t)
        {
            if (reg.EndsWith(")"))
                reg += "|(";
            else
                reg += "(";
            foreach (ModelRoute mr in t.GetCustomAttributes(typeof(ModelRoute), false))
            {
                _TypeMaps.Add(mr.Host + (mr.Path.StartsWith("/") ? mr.Path : "/" + mr.Path).TrimEnd('/'), t);
                bool hasAdd = false;
                bool hasUpdate = false;
                bool hasDelete = false;
                MethodInfo LoadAll = null;
                MethodInfo Load = null;
                MethodInfo SelectList = null;
                foreach (MethodInfo mi in t.GetMethods(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (mi.GetCustomAttributes(typeof(ModelSaveMethod), false).Length > 0)
                    {
                        _SaveMethods.Add(t, mi);
                        hasAdd = true;
                    }
                    else if (mi.GetCustomAttributes(typeof(ModelDeleteMethod), false).Length > 0)
                    {
                        _DeleteMethods.Add(t, mi);
                        hasDelete = true;
                    }
                    else if (mi.GetCustomAttributes(typeof(ModelUpdateMethod), false).Length > 0)
                    {
                        _UpdateMethods.Add(t, mi);
                        hasUpdate = true;
                    }
                }
                foreach (MethodInfo mi in t.GetMethods(BindingFlags.Static | BindingFlags.Public))
                {
                    if (mi.GetCustomAttributes(typeof(ModelLoadAllMethod), false).Length > 0)
                        LoadAll = mi;
                    else if (mi.GetCustomAttributes(typeof(ModelLoadMethod), false).Length > 0)
                        Load = mi;
                    else if (mi.GetCustomAttributes(typeof(ModelSelectListMethod), false).Length > 0)
                        SelectList = mi;
                    else if (mi.GetCustomAttributes(typeof(ModelListMethod), false).Length > 0)
                    {
                        foreach (ModelListMethod mlm in mi.GetCustomAttributes(typeof(ModelListMethod), false))
                            _ModelListCalls.Add(new sModelListCall(mlm, mi));
                    }
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

        // called to stop the handler and clear up resources
        public static void Stop()
        {
            _running = false;
            _REG_URL = null;
            _invalidModels = null;
            _cacheTimer.Dispose();
            _jsonURL = null;
            _jqueryURL = null;
            _backboneURL = null;
            _invalidModels = null;
            _LoadAlls = null;
            _Loads = null;
            _SelectLists = null;
            _TypeMaps = null;
        }

        //uses the compiled regular expression to determin if this handler handles the given url and request method
        public static bool HandlesURL(Uri url,string method)
        {
            if (_running)
            {
                if (!_REG_URL.IsMatch(method.ToUpper() + "\t" + url.Host + url.AbsolutePath))
                    return _REG_MODELS.IsMatch(method.ToUpper() + "\t" + url.Host + url.AbsolutePath);
                else
                    return true;
            }
            return false;
        }


        /*
         * Called to handle a given web request.  The system uses IHttpRequest to allow for different web server 
         * systems.  It checks to see if the url is for jquery,json or backbone, if none of those, it generates the required model 
         * javascript for the given url, as specified by an attribute or attributes in Models, that define the 
         * url that the model/view/collection javascript is available on.
         * If the call is not for javascript it processes the given action using the http method (GET = load,
         * DELETE = delete, POST = create, PUT = update, SELECT = SelectList)
         */
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
                    request.WriteContent(Utility.ReadEmbeddedResource("Org.Reddragonit.BackBoneDotNet.resources.backbone.validate.js"));
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
                        if (request.URL.AbsolutePath.EndsWith(".min.js") || Settings.Default.CompressAllJS)
                        {
                            string comp = JSMinifier.Minify(sb.ToString());
                            request.WriteContent(comp);
                            lock (_CachedJScript)
                            {
                                if (!_CachedJScript.ContainsKey(request.URL.Host + request.URL.AbsolutePath))
                                    _CachedJScript.Add(request.URL.Host + request.URL.AbsolutePath, new CachedItemContainer(comp));
                            }
                        }
                        else
                        {
                            request.WriteContent(sb.ToString());
                            lock (_CachedJScript)
                            {
                                if (!_CachedJScript.ContainsKey(request.URL.Host + request.URL.AbsolutePath))
                                    _CachedJScript.Add(request.URL.Host + request.URL.AbsolutePath, new CachedItemContainer(sb.ToString()));
                            }
                        }
                        request.SendResponse();
                    }
                }
            }
            else
            {
                Object ret = null;
                switch (request.Method.ToUpper())
                {
                    case "GET":
                        if (_REG_MODELS.IsMatch(request.Method.ToUpper() + "\t" + request.URL.Host + request.URL.AbsolutePath))
                        {
                            foreach (sModelListCall mlc in _ModelListCalls)
                            {
                                if (mlc.HandlesRequest(request))
                                {
                                    ret = mlc.HandleRequest(request);
                                    break;
                                }
                            }
                        }
                        else
                        {
                            if (_LoadAlls.ContainsKey(request.URL.Host + request.URL.AbsolutePath))
                                ret = _LoadAlls[request.URL.Host + request.URL.AbsolutePath].Invoke(null, new object[0]);
                            else if (_LoadAlls.ContainsKey("*" + request.URL.AbsolutePath))
                                ret = _LoadAlls["*" + request.URL.AbsolutePath].Invoke(null, new object[0]);
                            else if (_Loads.ContainsKey(request.URL.Host + request.URL.AbsolutePath.Substring(0, request.URL.AbsolutePath.LastIndexOf("/"))))
                                ret = _Loads[request.URL.Host + request.URL.AbsolutePath.Substring(0, request.URL.AbsolutePath.LastIndexOf("/"))].Invoke(null, new object[] { request.URL.AbsolutePath.Substring(request.URL.AbsolutePath.LastIndexOf("/") + 1) });
                            else if (_Loads.ContainsKey("*" + request.URL.AbsolutePath.Substring(0, request.URL.AbsolutePath.LastIndexOf("/"))))
                                ret = _Loads["*" + request.URL.AbsolutePath.Substring(0, request.URL.AbsolutePath.LastIndexOf("/"))].Invoke(null, new object[] { request.URL.AbsolutePath.Substring(request.URL.AbsolutePath.LastIndexOf("/") + 1) });
                        }
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
                            Hashtable IModelTypes = new Hashtable();
                            foreach (string str in ht.Keys)
                            {
                                Type propType = ret.GetType().GetProperty(str).PropertyType;
                                if (propType.IsArray)
                                    propType = propType.GetElementType();
                                else if (propType.IsGenericType)
                                {
                                    if (propType.GetGenericTypeDefinition() == typeof(List<>))
                                        propType = propType.GetGenericArguments()[0];
                                }
                                var obj = _ConvertObjectToType(ht[str], ret.GetType().GetProperty(str).PropertyType);
                                if (new List<Type>(propType.GetInterfaces()).Contains(typeof(IModel)))
                                    IModelTypes.Add(str, obj);
                                ret.GetType().GetProperty(str).SetValue(ret, obj, new object[0]);
                            }
                            if (_UpdateMethods.ContainsKey(ret.GetType()))
                                ret = _UpdateMethods[ret.GetType()].Invoke(ret, new object[0]);
                            else
                                ret = false;
                            if ((bool)ret && IModelTypes.Count > 0)
                                ret = IModelTypes;
                            else if (!(bool)ret)
                                ret = null;
                        }
                        break;
                    case "DELETE":
                        if (_Loads.ContainsKey(request.URL.Host + request.URL.AbsolutePath.Substring(0, request.URL.AbsolutePath.LastIndexOf("/"))))
                            ret = _Loads[request.URL.Host + request.URL.AbsolutePath.Substring(0, request.URL.AbsolutePath.LastIndexOf("/"))].Invoke(null, new object[] { request.URL.AbsolutePath.Substring(request.URL.AbsolutePath.LastIndexOf("/") + 1) });
                        else if (_Loads.ContainsKey("*" + request.URL.AbsolutePath.Substring(0, request.URL.AbsolutePath.LastIndexOf("/"))))
                            ret = _Loads["*" + request.URL.AbsolutePath.Substring(0, request.URL.AbsolutePath.LastIndexOf("/"))].Invoke(null, new object[] { request.URL.AbsolutePath.Substring(request.URL.AbsolutePath.LastIndexOf("/") + 1) });
                        if (ret != null)
                        {
                            if (_DeleteMethods.ContainsKey(ret.GetType()))
                                ret = _DeleteMethods[ret.GetType()].Invoke(ret, new object[0]);
                            else
                                ret = null;
                        }
                        break;
                    case "POST":
                        Type t = null;
                        if (_TypeMaps.ContainsKey(request.URL.Host + request.URL.AbsolutePath))
                            t = _TypeMaps[request.URL.Host + request.URL.AbsolutePath];
                        else if (_TypeMaps.ContainsKey("*"+request.URL.AbsolutePath))
                            t = _TypeMaps["*"+request.URL.AbsolutePath];
                        if (t != null)
                        {
                            IModel mod = (IModel)t.GetConstructor(Type.EmptyTypes).Invoke(new object[0]);
                            Hashtable mht = (Hashtable)JSON.JsonDecode(request.ParameterContent);
                            foreach (string str in mht.Keys)
                            {
                                if (t.GetProperty(str).GetCustomAttributes(typeof(ReadOnlyModelProperty), true).Length == 0)
                                    t.GetProperty(str).SetValue(mod, _ConvertObjectToType(mht[str], t.GetProperty(str).PropertyType), new object[0]);
                            }
                            if (_SaveMethods.ContainsKey(t))
                            {
                                if ((bool)_SaveMethods[t].Invoke(mod, new object[0]))
                                {
                                    ret = new Hashtable();
                                    ((Hashtable)ret).Add("id", mod.id);
                                }
                                else
                                    ret = null;
                            }
                            else
                                ret = null;
                        }
                        break;
                }
                if (ret != null)
                {
                    request.SetResponseStatus(200);
                    request.SetResponseContentType("application/json");
                    request.WriteContent(JSON.JsonEncode(_SetupAdditionalBackbonehash(ret,request)));
                    request.SendResponse();
                }
                else
                {
                    request.SetResponseStatus(404);
                    request.SendResponse();
                }
            }
        }

        private static Hashtable _SetupAdditionalBackbonehash(object response,IHttpRequest request)
        {
            Hashtable ret = new Hashtable();
            ret.Add("response", response);
            Hashtable Backbone = new Hashtable();
            if (request.AcceptLanguageHeaderValue == null)
                Backbone.Add("Language", "en");
            else
                Backbone.Add("Language", request.AcceptLanguageHeaderValue.Split(';')[0].Split(',')[1].Trim());
            ret.Add("Backbone", Backbone);
            return ret;
        }

        /*
         * Called to convert a given json object to the expected type.
         */
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

        //houses the list of Javascript generators, broken apart into generators
        //for cleaner code
        private static readonly IJSGenerator[] _generators = new IJSGenerator[]{
            new NamespaceGenerator(),
            new ErrorMessageGenerator(),
            new ModelDefinitionGenerator(),
            new CollectionGenerator(),
            new ViewGenerator(),
            new CollectionViewGenerator(),
            new SelectListCallGenerator(),
            new EditFormGenerator(),
            new ModelListCallGenerators()
        };

        //called to generate Javascript for the given model.  It uses all the specified IJSGenerators above
        //they were broken into components for easier code reading.
        private static string _GenerateModelJSFile(Type t,string host)
        {
            string ret = "";
            List<string> properties = new List<string>();
            List<string> readOnlyProperties = new List<string>();
            List<string> viewIgnoreProperties = new List<string>();
            bool hasAdd = false;
            bool hasUpdate = false;
            bool hasDelete = false;
            foreach (MethodInfo mi in t.GetMethods(BindingFlags.Public | BindingFlags.Instance))
            {
                if (mi.GetCustomAttributes(typeof(ModelSaveMethod), false).Length > 0)
                    hasAdd = true;
                else if (mi.GetCustomAttributes(typeof(ModelDeleteMethod), false).Length > 0)
                    hasDelete = true;
                else if (mi.GetCustomAttributes(typeof(ModelUpdateMethod), false).Length > 0)
                    hasUpdate = true;
            }
            foreach (PropertyInfo pi in t.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (pi.GetCustomAttributes(typeof(ModelIgnoreProperty), false).Length == 0)
                {
                    if (pi.GetCustomAttributes(typeof(ReadOnlyModelProperty), false).Length > 0)
                        readOnlyProperties.Add(pi.Name);
                    if (pi.GetCustomAttributes(typeof(ViewIgnoreField), false).Length > 0)
                        viewIgnoreProperties.Add(pi.Name);
                    properties.Add(pi.Name);
                }
            }
            foreach (IJSGenerator gen in _generators)
                ret += gen.GenerateJS(t, host, readOnlyProperties, properties,viewIgnoreProperties,hasUpdate,hasAdd,hasDelete);
            return ret;
        }
    }
}
