using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;

namespace PHttp
{
    public class Startup
    {
        LoadApps _loadApps;
        List<IPHttpApplication> _impl;
        List<AppInfo> _apps;
        JArray _jArray;
        private bool LoadApps()
        {
            _impl = new List<IPHttpApplication>();
            _apps = new List<AppInfo>();
            try
            {
                Console.WriteLine("\tStarting to load apps...");
                LoadConfig loadConfig = new LoadConfig();
                loadConfig.InitServer(ConfigurationManager.AppSettings["Virtual"], "config.json");
                _jArray = loadConfig.JsonArray;
                _apps = loadConfig.apps;
                var DbHelper = new DatabaseHelper(_apps);
                DbHelper.Init();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
            Console.WriteLine("\tDatabase read successfully!");
            try
            {
                foreach (var a in _apps)
                {
                    string path = a.applicationsDir;

                    Console.WriteLine("\n\tLooking for apps in " + path + "\n");

                    if (string.IsNullOrEmpty(path)) { return false; } //sanity check

                    DirectoryInfo info = new DirectoryInfo(path);
                    if (!info.Exists) { return false; } //make sure directory exists                

                    foreach (FileInfo file in info.GetFiles("*.dll")) //loop through all dll files in directory
                    {
                        Console.WriteLine("\tdll = " + file);
                        Assembly currentAssembly = null;
                        try
                        {
                            var name = AssemblyName.GetAssemblyName(file.FullName);
                            currentAssembly = Assembly.Load(name);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("\n\t" + ex);
                            continue;
                        }

                        var types = currentAssembly.GetTypes();
                        foreach (var type in types)
                        {
                            if (type != typeof(IPHttpApplication) && typeof(IPHttpApplication).IsAssignableFrom(type))
                            {
                                var temp = (IPHttpApplication)Activator.CreateInstance(type);
                                if (!_impl.Contains(temp))
                                {
                                    _impl.Add(temp);
                                }
                            }
                        }
                    }
                    Console.WriteLine();
                    foreach (var el in _impl)
                    {
                        el.Start();
                        Console.WriteLine("\tLoading " + el + "...");
                    }
                }
                _loadApps = new LoadApps(_impl, _apps);
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public LoadApps loadApps
        {
            get { return _loadApps; }
            private set { _loadApps = value; }
        }
        public Startup()
        {
            _impl = new List<IPHttpApplication>();
            _apps = new List<AppInfo>();
            LoadApps();
        }
    }
}