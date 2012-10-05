using System;
using System.Collections.Generic;
using System.Text;

namespace Org.Reddragonit.BackBoneDotNet
{
    internal class RequestPathChecker
    {
        private struct sPathPortion
        {
            private string _path;
            public string Path
            {
                get{return _path;}
            }
            private bool _isEnd;
            private List<sPathPortion> subPortions;

            public sPathPortion(string[] path,int index){
                _path = path[index];
                subPortions = new List<sPathPortion>();
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
                    if (!add)
                        subPortions.Add(new sPathPortion(path, index + 1));
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
                            if (por.IsMatch(path, index+1))
                                return true;
                        }
                    }
                    else if (index == path.Length-1 && _path.StartsWith("{") && _path.EndsWith("}") && _isEnd)
                        return true;
                }
                return false;
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
                    _paths.Add(new sPathPortion(path, 0));
            }

            public bool IsMatch(string[] url)
            {
                bool ret = false;
                foreach (sPathPortion por in _paths)
                {
                    if (por.Path == url[0])
                    {
                        ret = por.IsMatch(url, 0);
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

            public sMethod(string method,string host, string path)
            {
                _method = method;
                _hosts = new List<sHost>();
                _hosts.Add(new sHost(host, path.Trim('/').Split('/')));
            }

            public void MergeInPath(string host,string path)
            {
                string portion = path;
                if (portion.Contains("/"))
                    portion = portion.Substring(0, portion.IndexOf("/"));
                bool add = true;
                foreach (sHost sh in _hosts)
                {
                    if (sh.Host == host)
                    {
                        sh.MergeInPath(path.Trim('/').Split('/'));
                        add = false;
                        break;
                    }
                }
                if (add)
                    _hosts.Add(new sHost(host, path.Trim('/').Split('/')));
            }

            public bool IsMatch(string host,string url)
            {
                bool ret = false;
                foreach (sHost sh in _hosts)
                {
                    if (sh.Host == host || sh.Host == "*")
                    {
                        ret = sh.IsMatch(url.Trim('/').Split('/'));
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
                    smt.MergeInPath(host,url);
                    add = false;
                }
            }
            if (add)
                _methods.Add(new sMethod(method,host, url.Trim('/')));
        }

        public bool IsMatch(string method,string host, string url)
        {
            bool ret = false;
            foreach (sMethod smt in _methods)
            {
                if (smt.Method == method)
                {
                    ret = smt.IsMatch(host, url.Trim('/'));
                    break;
                }
            }
            return ret;
        }
    }
}
