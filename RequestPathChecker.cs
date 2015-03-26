using System;
using System.Collections.Generic;
using System.Text;

namespace Org.Reddragonit.BackBoneDotNet
{
    internal class RequestPathChecker
    {
        private struct sPathPortion:IComparable
        {
            private string _path;
            public string Path
            {
                get{return _path;}
            }
            private bool _isEnd;
            private List<object> subPortions;

            public sPathPortion(string[] path,int index){
                _path = path[index];
                subPortions = new List<object>();
                if (path.Length>index+1)
                {
                    _isEnd = false;
                    subPortions.Add(new sPathPortion(path,index+1));
                }
                else
                    _isEnd = true;
            }

            public void MergeInPath(string[] path, int index)
            {
                if (path.Length > index + 1)
                {
                    bool add = true;
                    foreach (sPathPortion spp in subPortions)
                    {
                        if (spp.Path == path[index + 1])
                        {
                            spp.MergeInPath(path, index + 1);
                            add = false;
                            break;
                        }
                    }
                    if (add)
                    {
                        subPortions.Add(new sPathPortion(path, index + 1));
                        subPortions.Sort();
                    }
                }
                else
                    _isEnd = true;
            }

            public bool IsMatch(string[] path, int index)
            {
                if (path[index] == _path || (_path.StartsWith("{") && _path.EndsWith("}")))
                {
                    if (path[index] == _path && _isEnd && index==path.Length-1)
                        return true;
                    else if (path.Length>index+1)
                    {
                        foreach (sPathPortion por in subPortions)
                        {
                            if (por.IsMatch(path, index + 1))
                                return true;
                        }
                    }
                    return _isEnd;
                }
                return false;
            }

            public int CompareTo(object obj)
            {
                sPathPortion por = (sPathPortion)obj;
                if (Path.StartsWith("{") && Path.EndsWith("}"))
                {
                    if (por.Path.StartsWith("{") && por.Path.EndsWith("}"))
                    {
                        if (_isEnd)
                            return 1;
                        else if (por._isEnd)
                            return -1;
                        else
                            return -0;
                    }
                    else
                        return 1;
                }
                else if (por.Path.StartsWith("{") && por.Path.EndsWith("}"))
                    return -1;
                else
                    return Path.CompareTo(por.Path);
            }
        }

        private struct sHost
        {
            private string _host;
            public string Host
            {
                get { return _host; }
            }
            private List<sPathPortion> _paths;

            public sHost(string host, string[] path)
            {
                _host = host;
                _paths = new List<sPathPortion>();
                _paths.Add(new sPathPortion(path, 0));
                _paths.Sort();
            }

            public void MergeInPath(string[] path)
            {
                bool add = true;
                foreach (sPathPortion por in _paths)
                {
                    if (por.Path == path[0])
                    {
                        por.MergeInPath(path, 0);
                        add = false;
                        break;
                    }
                }
                if (add)
                {
                    _paths.Add(new sPathPortion(path, 0));
                    _paths.Sort();
                }
            }

            public bool IsMatch(string[] url)
            {
                bool ret = false;
                foreach (sPathPortion por in _paths)
                {
                    if (por.IsMatch(url, 0))
                    {
                        ret = true;
                        break;
                    }
                }
                return ret;
            }
        }

        private struct sMethod
        {
            private string _method;
            public string Method
            {
                get { return _method; }
            }
            private List<sHost> _hosts;

            public sMethod(string method,string host, string[] path)
            {
                _method = method;
                _hosts = new List<sHost>();
                _hosts.Add(new sHost(host, path));
            }

            public void MergeInPath(string host,string[] path)
            {
                bool add = true;
                foreach (sHost sh in _hosts)
                {
                    if (sh.Host == host)
                    {
                        sh.MergeInPath(path);
                        add = false;
                        break;
                    }
                }
                if (add)
                    _hosts.Add(new sHost(host, path));
            }

            public bool IsMatch(string host,string[] url)
            {
                bool ret = false;
                foreach (sHost sh in _hosts)
                {
                    if (sh.Host == host || sh.Host == "*")
                    {
                        ret = sh.IsMatch(url);
                        if (ret)
                            break;
                    }
                }
                return ret;
            }
        }

        private List<sMethod> _methods;

        public RequestPathChecker() {
            _methods = new List<sMethod>();
        }


        public void AddMethod(string method,string host, string url)
        {
            bool add = true;
            foreach (sMethod smt in _methods)
            {
                if (smt.Method == method)
                {
                    smt.MergeInPath(host, URLUtility.SplitUrl(url));
                    add = false;
                }
            }
            if (add)
                _methods.Add(new sMethod(method, host, URLUtility.SplitUrl(url)));
        }

        public bool IsMatch(string method,string host, string url)
        {
            bool ret = false;
            foreach (sMethod smt in _methods)
            {
                if (smt.Method == method)
                {
                    ret = smt.IsMatch(host,URLUtility.SplitUrl(url));
                    break;
                }
            }
            return ret;
        }
    }
}
