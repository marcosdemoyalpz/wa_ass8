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

        public static List<IPHttpApplication> LoadApps()
        {
            string replacePath = ConfigurationManager.AppSettings["ReplacePath"]; ;
            string userprofile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string path = ConfigurationManager.AppSettings["ApplicationsDir"];
            path = path.Replace(replacePath, userprofile);

            Console.WriteLine("\n\tLooking for apps in " + path + "\n");

            if (string.IsNullOrEmpty(path)) { return new List<IPHttpApplication>(); } //sanity check

            DirectoryInfo info = new DirectoryInfo(path);
            if (!info.Exists) { return new List<IPHttpApplication>(); } //make sure directory exists

            var impl = new List<IPHttpApplication>();

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
                }
            }
            Console.WriteLine();

            //foreach (var el in impl)
            //{
            //    Console.WriteLine("\tExecuting " + el + "...\n");
            //    Console.WriteLine("\tName: " + el.Name);
            //    el.Start(virtualPath);
            //    Console.WriteLine();
            //}

            //Console.ReadKey();
            return impl;
        }
    }
}