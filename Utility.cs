using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Reflection;
using System.IO;

namespace Org.Reddragonit.BackBoneDotNet
{
    internal static class Utility
    {
        private const int _CACHE_TIMEOUT_MINUTES = 60;
        private const int _CACHE_TIMER_INTERVAL = 300;

        private static Dictionary<string, CachedItemContainer> _TYPE_CACHE;
        private static Dictionary<string, CachedItemContainer> _INSTANCES_CACHE;
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
                            if (!ass.GetName().Name.Contains("mscorlib") && !ass.GetName().Name.StartsWith("System") && !ass.GetName().Name.StartsWith("Microsoft"))
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
                        if (!ass.GetName().Name.Contains("mscorlib") && !ass.GetName().Name.StartsWith("System") && !ass.GetName().Name.StartsWith("Microsoft"))
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
                        if (e.Message != "The invoked member is not supported in a dynamic assembly.")
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
            Stream ret = typeof(Utility).Assembly.GetManifestResourceStream(name);
            if (ret == null)
            {
                foreach (Assembly ass in AppDomain.CurrentDomain.GetAssemblies())
                {
                    try
                    {
                        if (!ass.GetName().Name.Contains("mscorlib") && !ass.GetName().Name.StartsWith("System") && !ass.GetName().Name.StartsWith("Microsoft"))
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

        //returns a string containing the contents of an embedded resource
        public static string ReadEmbeddedResource(string name)
        {
            Stream s = LocateEmbededResource(name);
            string ret = "";
            if (s != null)
            {
                TextReader tr = new StreamReader(s);
                ret = tr.ReadToEnd();
                tr.Close();
            }
            return ret;
        }
    }
}
