using PHttp.Application;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Reflection;
using System.Web.Http;
using System.Web.Routing;

namespace PHttp
{
    public class Startup
    {
        public class LoadDLLs
        {
            List<IPHttpApplication> _impl = new List<IPHttpApplication>();
            List<ControllerBase> _controllers = new List<ControllerBase>();
            public List<IPHttpApplication> Applications
            {
                get { return _impl; }
                set { _impl = value; }
            }
            public List<ControllerBase> Controllers
            {
                get { return _controllers; }
                set { _controllers = value; }
            }
            public LoadDLLs()
            {
                _impl = new List<IPHttpApplication>();
                _controllers = new List<ControllerBase>();
            }
            public LoadDLLs(List<IPHttpApplication> impl, List<ControllerBase> controllers)
            {
                _impl = impl;
                _controllers = controllers;
            }
        }

        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.Ignore("{resource}.axd/{*pathInfo}");
            routes.MapHttpRoute(
                "Default",                                              // Route name
                "{controller}/{action}/{id}",                           // URL with parameters
                new { controller = "Home", action = "Index", id = "" }  // Parameter defaults
            );
        }

        protected void Application_Start()
        {
            RegisterRoutes(RouteTable.Routes);
        }

        public static LoadDLLs LoadApps()
        {
            string replacePath = ConfigurationManager.AppSettings["ReplacePath"]; ;
            string userprofile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string path = ConfigurationManager.AppSettings["ApplicationsDir"];
            path = path.Replace(replacePath, userprofile);

            Console.WriteLine("\n\tLooking for apps in " + path + "\n");

            if (string.IsNullOrEmpty(path)) { return new LoadDLLs(); } //sanity check

            DirectoryInfo info = new DirectoryInfo(path);
            if (!info.Exists) { return new LoadDLLs(); } //make sure directory exists

            var impl = new List<IPHttpApplication>();
            var controllers = new List<ControllerBase>();

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
                        impl.Add((IPHttpApplication)Activator.CreateInstance(type));
                    }
                    if (type != typeof(ControllerBase) && typeof(ControllerBase).IsAssignableFrom(type))
                    {
                        controllers.Add((ControllerBase)Activator.CreateInstance(type));
                    }
                }
            }
            Console.WriteLine();
            foreach (var el in impl)
            {
                el.Start();
                Console.WriteLine("\tLoading " + el + "...\n");
            }
            foreach (var ctrl in controllers)
            {
                Console.WriteLine("\tLoading " + ctrl + "...\n");
            }
            LoadDLLs loadDLLs = new LoadDLLs(impl, controllers);
            return loadDLLs;
        }
    }
}