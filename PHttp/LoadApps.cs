using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace PHttp
{
    public class LoadApps
    {
        private List<AppInfo> _apps;
        private List<IPHttpApplication> _impl;
        public List<IPHttpApplication> Applications
        {
            get { return _impl; }
            set { _impl = value; }
        }
        public List<AppInfo> AppInfoList
        {
            get { return _apps; }
            set { _apps = value; }
        }
        public LoadApps()
        {
            _apps = new List<AppInfo>();
            _impl = new List<IPHttpApplication>();
        }
        public LoadApps(List<IPHttpApplication> impl, List<AppInfo> apps)
        {
            _impl = new List<IPHttpApplication>();
            _apps = new List<AppInfo>();
            _impl = impl;
            _apps = apps;
            Console.WriteLine("\tFinished loading apps!\n");
        }
    }
}
