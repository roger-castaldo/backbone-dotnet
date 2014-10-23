using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Reflection;
using System.IO;
using Org.Reddragonit.BackBoneDotNet.Attributes;
using Org.Reddragonit.BackBoneDotNet.Interfaces;

namespace Org.Reddragonit.BackBoneDotNet
{
    /*
     * This class houses some basic utility functions used by other classes to access embedded resources, search
     * for types, etc.
     */
    internal static class Utility
    {
        //max time to hold an item in cache (minutes)
        private const int _CACHE_TIMEOUT_MINUTES = 60;
        //internval between checks to clean out cache (ms)
        private const int _CACHE_TIMER_INTERVAL = 300;

        //houses a cache of Types found through locate type, this is used to increase performance
        private static Dictionary<string, CachedItemContainer> _TYPE_CACHE;
        //houses a cache of Type instances through locate type instances, this is used to increate preformance
        private static Dictionary<string, CachedItemContainer> _INSTANCES_CACHE;
        //timer used to cleanup expired cache items
        private static Timer _CLEANUP_TIMER;

        static Utility()
        {
            _TYPE_CACHE = new Dictionary<string, CachedItemContainer>();
            _INSTANCES_CACHE = new Dictionary<string, CachedItemContainer>();
            _CLEANUP_TIMER = new Timer(new TimerCallback(_CleanupCache), null, 0, _CACHE_TIMER_INTERVAL);
        }

        //Called by the cleanup timer to clean up stale cached types to keep memory usage low
        private static void _CleanupCache(object pars)
        {
            string[] keys;
            DateTime now = DateTime.Now;
            lock (_TYPE_CACHE)
            {
                keys = new string[_TYPE_CACHE.Count];
                _TYPE_CACHE.Keys.CopyTo(keys, 0);
                foreach (string str in keys)
                {
                    if (now.Subtract(_TYPE_CACHE[str].LastAccess).TotalMinutes > _CACHE_TIMEOUT_MINUTES)
                        _TYPE_CACHE.Remove(str);
                }
            }
            lock (_INSTANCES_CACHE)
            {
                keys = new string[_INSTANCES_CACHE.Count];
                _INSTANCES_CACHE.Keys.CopyTo(keys, 0);
                foreach (string str in keys)
                {
                    if (now.Subtract(_INSTANCES_CACHE[str].LastAccess).TotalMinutes > _CACHE_TIMEOUT_MINUTES)
                        _INSTANCES_CACHE.Remove(str);
                }
            }
        }

        //Called to locate a type by its name, this scans through all assemblies 
        //which by default Type.Load does not perform.
        public static Type LocateType(string typeName)
        {
            Logger.Debug("Attempting to locate type " + typeName);
            Type t = null;
            lock (_TYPE_CACHE)
            {
                if (_TYPE_CACHE.ContainsKey(typeName))
                    t = (Type)_TYPE_CACHE[typeName].Value;
            }
            if (t == null)
            {
                t = Type.GetType(typeName, false, true);
                if (t == null)
                {
                    foreach (Assembly ass in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        try
                        {
                            if (ass.GetName().Name != "mscorlib" && !ass.GetName().Name.StartsWith("System.") && ass.GetName().Name != "System" && !ass.GetName().Name.StartsWith("Microsoft"))
                            {
                                t = ass.GetType(typeName, false, true);
                                if (t != null)
                                    break;
                            }
                        }
                        catch (Exception e)
                        {
                            if (e.Message != "The invoked member is not supported in a dynamic assembly.")
                            {
                                throw e;
                            }
                        }
                    }
                }
                lock (_TYPE_CACHE)
                {
                    if (!_TYPE_CACHE.ContainsKey(typeName))
                        _TYPE_CACHE.Add(typeName, new CachedItemContainer(t));
                }
            }
            return t;
        }

        //Called to locate all child classes of a given parent type
        public static List<Type> LocateTypeInstances(Type parent)
        {
            Logger.Debug("Attempting to locate instances of type " + parent.FullName);
            List<Type> ret = null;
            lock (_INSTANCES_CACHE)
            {
                if (_INSTANCES_CACHE.ContainsKey(parent.FullName))
                    ret = (List<Type>)_INSTANCES_CACHE[parent.FullName].Value;
            }
            if (ret == null)
            {
                ret = new List<Type>();
                foreach (Assembly ass in AppDomain.CurrentDomain.GetAssemblies())
                {
                    try
                    {
                        if (ass.GetName().Name != "mscorlib" && !ass.GetName().Name.StartsWith("System.") && ass.GetName().Name != "System" && !ass.GetName().Name.StartsWith("Microsoft"))
                        {
                            foreach (Type t in ass.GetTypes())
                            {
                                if (t.IsSubclassOf(parent) || (parent.IsInterface && new List<Type>(t.GetInterfaces()).Contains(parent)))
                                    ret.Add(t);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        if (e.Message != "The invoked member is not supported in a dynamic assembly."
                            &&e.Message!="Unable to load one or more of the requested types. Retrieve the LoaderExceptions property for more information.")
                        {
                            throw e;
                        }
                    }
                }
                lock (_INSTANCES_CACHE)
                {
                    if (!_INSTANCES_CACHE.ContainsKey(parent.FullName))
                        _INSTANCES_CACHE.Add(parent.FullName, new CachedItemContainer(ret));
                }
            }
            return ret;
        }

        //called to open a stream of a given embedded resource, again searches through all assemblies
        public static Stream LocateEmbededResource(string name)
        {
            Logger.Debug("Locating embedded resource " + name);
            Stream ret = typeof(Utility).Assembly.GetManifestResourceStream(name);
            if (ret == null)
            {
                foreach (Assembly ass in AppDomain.CurrentDomain.GetAssemblies())
                {
                    try
                    {
                        if (ass.GetName().Name != "mscorlib" && !ass.GetName().Name.StartsWith("System.") && ass.GetName().Name != "System" && !ass.GetName().Name.StartsWith("Microsoft"))
                        {
                            ret = ass.GetManifestResourceStream(name);
                            if (ret != null)
                                break;
                        }
                    }
                    catch (Exception e)
                    {
                        if (e.Message != "The invoked member is not supported in a dynamic assembly.")
                        {
                            throw e;
                        }
                    }
                }
            }
            return ret;
        }

        private static Dictionary<string, string> _compressedJS = new Dictionary<string, string>();

        //returns a string containing the contents of an embedded resource
        public static string ReadEmbeddedResource(string name,bool minimize)
        {
            string ret = null;
            if (minimize)
            {
                lock (_compressedJS)
                {
                    ret = (_compressedJS.ContainsKey(name) ? _compressedJS[name] : null);
                }
            }
            if (ret == null)
            {
                Logger.Debug("Reading embedded resource " + name);
                Stream s = LocateEmbededResource(name);
                if (s != null)
                {
                    TextReader tr = new StreamReader(s);
                    ret = tr.ReadToEnd();
                    tr.Close();
                    if (minimize)
                    {
                        ret = JSMinifier.Minify(ret).Trim();
                        lock (_compressedJS)
                        {
                            if (!_compressedJS.ContainsKey(name))
                                _compressedJS.Add(name, ret);
                        }
                    }
                }
            }
            return (ret==null ? "" : ret);
        }

        internal static bool IsBlockedModel(Type t)
        {
            if (t.GetCustomAttributes(typeof(ModelBlockInheritance), false).Length > 0)
                return true;
            else if (t.BaseType == null)
                return false;
            else if (!new List<Type>(t.BaseType.GetInterfaces()).Contains(typeof(IModel)))
                return false;
            return IsBlockedModel(t.BaseType);
        }

        internal static void ClearCaches()
        {
            lock (_INSTANCES_CACHE)
            {
                _INSTANCES_CACHE.Clear();
            }
            lock (_TYPE_CACHE)
            {
                _TYPE_CACHE.Clear();
            }
        }
    }
}
