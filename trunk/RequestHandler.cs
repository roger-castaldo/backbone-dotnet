﻿using System;
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

            public bool HandlesRequest(IHttpRequest request)
            {
                Logger.Trace("Checking if url " + request.URL.AbsolutePath + " is handled in path " + _path);
                return _reg.IsMatch(request.Method.ToUpper() + "\t" + request.URL.Host +request.URL.AbsolutePath+(_path.Contains("?") ? request.URL.Query : ""));
            }

            public object HandleRequest(IHttpRequest request)
            {
                Logger.Debug("Attempting to handle model list call for path "+_path);
                object ret = null;
                try
                {
                    Logger.Trace("Parsing parameters from url for path " + _path);
                    object[] pars = URLUtility.ExtractParametersForUrl(_method, request.URL, _path,_isPaged);
                    Logger.Trace("Invoking method for path " + _path);
                    ret = _method.Invoke(null, pars);
                    Logger.Trace("Model list method invoked successfully");
                    if (_isPaged)
                    {
                        Logger.Trace("Adding required paging values for the pager collection javascript");
                        Hashtable tmp = new Hashtable();
                        tmp.Add("response", ret);
                        Hashtable pager = new Hashtable();
                        tmp.Add("Pager", pager);
                        Logger.Trace("Total pages for path " + _path + " " + pars[pars.Length - 1].ToString());
                        pager.Add("TotalPages", pars[pars.Length - 1]);
                        ret = tmp;
                    }
                }
                catch (Exception e)
                {
                    Logger.LogError(e);
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
        private static RequestPathChecker _RPC_URL;
        //houses the regex used to check and see if a request can be handled by this system
        private static RequestPathChecker _RPC_SELECT;
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
            Logger.Trace("Cleaning cached javascript");
            lock (_CachedJScript)
            {
                DateTime now = DateTime.Now;
                string[] keys = new string[_CachedJScript.Count];
                _CachedJScript.Keys.CopyTo(keys, 0);
                foreach (string str in keys)
                {
                    if (now.Subtract(_CachedJScript[str].LastAccess).TotalMinutes > _CACHE_TIMEOUT_MINUTES)
                    {
                        Logger.Trace("Removing cached javascript for path " + str);
                        _CachedJScript.Remove(str);
                    }
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
        public static void Start(StartTypes startType,string jqueryURL,string jsonURL,string backboneURL,ILogWriter logWriter)
        {
            Logger.Setup(logWriter);
            Logger.Debug("Starting up BackBone request handler");
            List<Exception> errors = DefinitionValidator.Validate(out _invalidModels);
            if (errors.Count > 0)
            {
                Logger.Error("Backbone validation errors:");
                foreach (Exception e in errors)
                    Logger.LogError(e);
                Logger.Error("Invalid IModels:");
                foreach (Type t in _invalidModels)
                    Logger.Error(t.FullName);
            }
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
            _RPC_URL = new RequestPathChecker();
            _RPC_SELECT = new RequestPathChecker();
            foreach (Type t in Utility.LocateTypeInstances(typeof(IModel)))
            {
                if (!_invalidModels.Contains(t))
                {
                    Logger.Trace("Adding URL path checks for type " + t.FullName);
                    AppendURLRegex(t);
                }
            }
            _jqueryURL = jqueryURL;
            _jsonURL = jsonURL;
            _backboneURL = backboneURL;
            if (_jqueryURL != null)
            {
                _jqueryURL = (!_jqueryURL.StartsWith("/") ? "/" + _jqueryURL : "");
                _RPC_URL.AddMethod("GET", "*", _jqueryURL);
            }
            if (_jsonURL != null)
            {
                _jsonURL = (!_jsonURL.StartsWith("/") ? "/" + _jsonURL : "");
                _RPC_URL.AddMethod("GET", "*", _jsonURL);
            }
            if (_backboneURL != null)
            {
                _backboneURL = (!_backboneURL.StartsWith("/") ? "/" + _backboneURL : "");
                _RPC_URL.AddMethod("GET", "*", _backboneURL);
            }
            Logger.Debug("Backbone request handler successfully started");
            _running = true;
        }

        /*
         * Method called to append the regular expression code chunk to the existing regulare expression
         * to handle the supplied model type.  it does this through checking the different specified 
         * attributes to specify hosts, available commands, etc.
         */
        private static void AppendURLRegex(Type t)
        {
            bool hasAdd = false;
            bool hasUpdate = false;
            bool hasDelete = false;
            MethodInfo LoadAll = null;
            MethodInfo Load = null;
            MethodInfo SelectList = null;
            foreach (MethodInfo mi in t.GetMethods(Constants.LOAD_METHOD_FLAGS))
            {
                if (mi.GetCustomAttributes(typeof(ModelLoadAllMethod), false).Length > 0)
                {
                    Logger.Trace("Found load all method for type " + t.FullName);
                    LoadAll = mi;
                }
                else if (mi.GetCustomAttributes(typeof(ModelLoadMethod), false).Length > 0)
                {
                    Logger.Trace("Found load method for type " + t.FullName);
                    Load = mi;
                }
                else if (mi.GetCustomAttributes(typeof(ModelSelectListMethod), false).Length > 0)
                {
                    Logger.Trace("Found select list method for type " + t.FullName);
                    SelectList = mi;
                }
                else if (mi.GetCustomAttributes(typeof(ModelListMethod), false).Length > 0)
                {
                    foreach (ModelListMethod mlm in mi.GetCustomAttributes(typeof(ModelListMethod), false))
                    {
                        _RPC_SELECT.AddMethod("GET", mlm.Host, mlm.Path+(mlm.Paged ? (mlm.Path.Contains("?") ? "&" : "?")+"PageStartIndex={"+(mi.GetParameters().Length-3).ToString()+"}&PageSize={"+(mi.GetParameters().Length-2).ToString()+"}" : ""));
                        _ModelListCalls.Add(new sModelListCall(mlm, mi));
                    }
                }
            }
            foreach (MethodInfo mi in t.GetMethods(Constants.STORE_DATA_METHOD_FLAGS))
            {
                if (mi.GetCustomAttributes(typeof(ModelSaveMethod), false).Length > 0)
                {
                    Logger.Trace("Save method located for type " + t.FullName);
                    if (!_SaveMethods.ContainsKey(t))
                        _SaveMethods.Add(t, mi);
                    hasAdd = true;
                }
                else if (mi.GetCustomAttributes(typeof(ModelDeleteMethod), false).Length > 0)
                {
                    Logger.Trace("Delete method located for type " + t.FullName);
                    if (!_DeleteMethods.ContainsKey(t))
                        _DeleteMethods.Add(t, mi);
                    hasDelete = true;
                }
                else if (mi.GetCustomAttributes(typeof(ModelUpdateMethod), false).Length > 0)
                {
                    Logger.Trace("Update method located for type " + t.FullName);
                    if (!_UpdateMethods.ContainsKey(t))
                        _UpdateMethods.Add(t, mi);
                    hasUpdate = true;
                }
            }
            foreach (ModelRoute mr in t.GetCustomAttributes(typeof(ModelRoute), false))
            {
                Logger.Trace("Adding route " + mr.Host + (mr.Path.StartsWith("/") ? mr.Path : "/" + mr.Path).TrimEnd('/') + " for type " + t.FullName);
                _TypeMaps.Add(mr.Host + (mr.Path.StartsWith("/") ? mr.Path : "/" + mr.Path).TrimEnd('/'), t);
                _Loads.Add(mr.Host + (mr.Path.StartsWith("/") ? mr.Path : "/" + mr.Path).TrimEnd('/'), Load);
                if (LoadAll != null)
                {
                    _RPC_URL.AddMethod("GET", mr.Host, (mr.Path.StartsWith("/") ? mr.Path : "/" + mr.Path).TrimEnd('/'));
                    _LoadAlls.Add(mr.Host + (mr.Path.StartsWith("/") ? mr.Path : "/" + mr.Path).TrimEnd('/'), LoadAll);
                }
                if (hasAdd)
                    _RPC_URL.AddMethod("POST", mr.Host, (mr.Path.StartsWith("/") ? mr.Path : "/" + mr.Path).TrimEnd('/'));
                if (SelectList != null)
                {
                    _RPC_URL.AddMethod("SELECT", mr.Host, (mr.Path.StartsWith("/") ? mr.Path : "/" + mr.Path).TrimEnd('/'));
                    _SelectLists.Add(mr.Host + (mr.Path.StartsWith("/") ? mr.Path : "/" + mr.Path).TrimEnd('/'), SelectList);
                }
                _RPC_URL.AddMethod("GET", mr.Host, (mr.Path.StartsWith("/") ? mr.Path : "/" + mr.Path) + (mr.Path.EndsWith("/") ? "" : "/") + "{0}");
                if (hasUpdate)
                    _RPC_URL.AddMethod("PUT", mr.Host, (mr.Path.StartsWith("/") ? mr.Path : "/" + mr.Path) + (mr.Path.EndsWith("/") ? "" : "/") + "{0}");
                if (hasDelete)
                    _RPC_URL.AddMethod("DELETE", mr.Host, (mr.Path.StartsWith("/") ? mr.Path : "/" + mr.Path) + (mr.Path.EndsWith("/") ? "" : "/") + "{0}");
            }
            foreach (ModelJSFilePath mj in t.GetCustomAttributes(typeof(ModelJSFilePath), false))
            {
                Logger.Trace("Adding js path " + mj.Host + (mj.Path.StartsWith("/") ? mj.Path : "/" + mj.Path) + " for type " + t.FullName);
                _RPC_URL.AddMethod("GET", mj.Host, (mj.Path.StartsWith("/") ? mj.Path : "/" + mj.Path));
            }
        }

        // called to stop the handler and clear up resources
        public static void Stop()
        {
            Logger.Debug("Stopping model handler by clearing out all held resources");
            Logger.Destroy();
            _running = false;
            _RPC_URL = null;
            _RPC_SELECT = null;
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
                Logger.Debug("Checking if url " + url.ToString()+ " is handled by the Backbone request handler");
                if (!_RPC_URL.IsMatch(method.ToUpper(), url.Host, url.AbsolutePath))
                    return _RPC_SELECT.IsMatch(method.ToUpper(), url.Host, url.AbsolutePath+(url.Query!="" ? url.Query : ""));
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
            Logger.Debug("Handling backbone request for path " + request.URL.ToString());
            if (request.URL.AbsolutePath.EndsWith(".js") && request.Method.ToUpper() == "GET")
            {
                request.SetResponseContentType("text/javascript");
                if (request.URL.AbsolutePath == (_jqueryURL == null ? "" : _jqueryURL))
                {
                    Logger.Trace("Sending jquery javascript response through backbone handler");
                    request.SetResponseStatus(200);
                    request.WriteContent(Utility.ReadEmbeddedResource("Org.Reddragonit.BackBoneDotNet.resources.jquery.min.js"));
                    request.SendResponse();
                }
                else if (request.URL.AbsolutePath == (_jsonURL == null ? "" : _jsonURL))
                {
                    Logger.Trace("Sending json javascript response through backbone handler");
                    request.SetResponseStatus(200);
                    request.WriteContent(Utility.ReadEmbeddedResource("Org.Reddragonit.BackBoneDotNet.resources.json2.min.js"));
                    request.SendResponse();
                }
                else if (request.URL.AbsolutePath == (_backboneURL == null ? "" : _backboneURL))
                {
                    Logger.Trace("Sending modified backbone javascript response through backbone handler");
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
                            Logger.Trace("Sending cached javascript response for path " + request.URL.Host + request.URL.AbsolutePath);
                            found = true;
                            request.SetResponseStatus(200);
                            request.WriteContent((string)_CachedJScript[request.URL.Host+request.URL.AbsolutePath].Value);
                            request.SendResponse();
                        }
                    }
                    if (!found)
                    {
                        Logger.Trace("Buidling model javascript for path " + request.URL.Host + request.URL.AbsolutePath);
                        StringBuilder sb = new StringBuilder();
                        foreach (Type t in Utility.LocateTypeInstances(typeof(IModel)))
                        {
                            foreach (ModelJSFilePath mj in t.GetCustomAttributes(typeof(ModelJSFilePath), false))
                            {
                                if ((mj.Host == "*" || mj.Host == request.URL.Host) && mj.Path == request.URL.AbsolutePath)
                                {
                                    Logger.Trace("Appending model " + t.FullName + " to path " + request.URL.Host + request.URL.AbsolutePath);
                                    sb.Append(_GenerateModelJSFile(t, request.URL.Host));
                                }
                            }
                        }
                        request.SetResponseStatus(200);
                        if (request.URL.AbsolutePath.EndsWith(".min.js") || Settings.Default.CompressAllJS)
                        {
                            Logger.Trace("Compressing javascript for path "+request.URL.Host+request.URL.AbsolutePath);
                            string comp = JSMinifier.Minify(sb.ToString());
                            Logger.Trace("Caching compressed javascript for path " + request.URL.Host + request.URL.AbsolutePath);
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
                        Logger.Trace("Attempting to handle GET request");
                        if (_RPC_SELECT.IsMatch(request.Method.ToUpper(), request.URL.Host, request.URL.AbsolutePath + (request.URL.Query != "" ? request.URL.Query : "")))
                        {
                            Logger.Trace("Handling request as a model list method");
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
                            {
                                Logger.Trace("Handling Load All request");
                                ret = _LoadAlls[request.URL.Host + request.URL.AbsolutePath].Invoke(null, new object[0]);
                            }
                            else if (_LoadAlls.ContainsKey("*" + request.URL.AbsolutePath))
                            {
                                Logger.Trace("Handling Load All request");
                                ret = _LoadAlls["*" + request.URL.AbsolutePath].Invoke(null, new object[0]);
                            }
                            else if (_Loads.ContainsKey(request.URL.Host + request.URL.AbsolutePath.Substring(0, request.URL.AbsolutePath.LastIndexOf("/"))))
                            {
                                Logger.Trace("Handling Load request");
                                ret = _Loads[request.URL.Host + request.URL.AbsolutePath.Substring(0, request.URL.AbsolutePath.LastIndexOf("/"))].Invoke(null, new object[] { request.URL.AbsolutePath.Substring(request.URL.AbsolutePath.LastIndexOf("/") + 1) });
                            }
                            else if (_Loads.ContainsKey("*" + request.URL.AbsolutePath.Substring(0, request.URL.AbsolutePath.LastIndexOf("/"))))
                            {
                                Logger.Trace("Handling Load request");
                                ret = _Loads["*" + request.URL.AbsolutePath.Substring(0, request.URL.AbsolutePath.LastIndexOf("/"))].Invoke(null, new object[] { request.URL.AbsolutePath.Substring(request.URL.AbsolutePath.LastIndexOf("/") + 1) });
                            }
                        }
                        break;
                    case "SELECT":
                        Logger.Trace("Handling Select List request");
                        if (_SelectLists.ContainsKey(request.URL.Host + request.URL.AbsolutePath))
                            ret = _SelectLists[request.URL.Host + request.URL.AbsolutePath].Invoke(null, new object[0]);
                        else if (_SelectLists.ContainsKey("*" + request.URL.AbsolutePath))
                            ret = _SelectLists["*" + request.URL.AbsolutePath].Invoke(null, new object[0]);
                        break;
                    case "PUT":
                        Logger.Trace("Handling Put request");
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
                                if (str != "id")
                                {
                                    if (ret.GetType().GetProperty(str).GetCustomAttributes(typeof(ReadOnlyModelProperty), true).Length == 0)
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
                                }
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
                        Logger.Trace("Handling Delete request");
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
                        Logger.Trace("Handling Post request");
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
                                if (str != "id")
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
                    Logger.Trace("Request successfully handled, sending response");
                    request.SetResponseStatus(200);
                    request.SetResponseContentType("application/json");
                    request.WriteContent(JSON.JsonEncode(_SetupAdditionalBackbonehash(ret,request)));
                    request.SendResponse();
                }
                else
                {
                    Logger.Trace("Handling of request failed, sending 404 response");
                    request.SetResponseStatus(404);
                    request.SendResponse();
                }
            }
        }

        private static Hashtable _SetupAdditionalBackbonehash(object response,IHttpRequest request)
        {
            Logger.Trace("Adding additional Backbone variables");
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
            Logger.Debug("Attempting to convert object of type " + (obj == null ? "NULL" : obj.GetType().FullName) + " to " + expectedType.FullName);
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
                    foreach (MethodInfo mi in expectedType.GetMethods(Constants.LOAD_METHOD_FLAGS))
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
            Logger.Debug("Generating js file for type " + t.FullName);
            string ret = "";
            List<string> properties = new List<string>();
            List<string> readOnlyProperties = new List<string>();
            List<string> viewIgnoreProperties = new List<string>();
            bool hasAdd = false;
            bool hasUpdate = false;
            bool hasDelete = false;
            foreach (MethodInfo mi in t.GetMethods(Constants.STORE_DATA_METHOD_FLAGS))
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
            {
                Logger.Trace("Running js generator " + gen.GetType().FullName);
                ret += gen.GenerateJS(t, host, readOnlyProperties, properties, viewIgnoreProperties, hasUpdate, hasAdd, hasDelete);
            }
            return ret;
        }
    }
}