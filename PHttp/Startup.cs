using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace PHttp
{
    public class Startup
    {
        public static void LoadApps()
        {
            string path = ConfigurationManager.AppSettings["ApplicationsDir"];

            Console.WriteLine("\n\tLooking for apps in " + path + "\n");

            if (string.IsNullOrEmpty(path)) { return; } //sanity check

            DirectoryInfo info = new DirectoryInfo(path);
            if (!info.Exists) { return; } //make sure directory exists

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

            foreach (var el in impl)
            {
                Console.WriteLine("\tExecuting " + el + "...\n");
                Console.WriteLine("\tName: " + el.Name);
                el.Start();
            }

            Console.ReadKey();
        }
    }
}
