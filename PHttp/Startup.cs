using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web.Http;
using System.Web.Routing;

namespace PHttp
{
    public class Startup
    {
        public class LoadDLLs
        {
            List<AppInfo> _apps;
            List<IPHttpApplication> _impl;
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
            public LoadDLLs()
            {
                _apps = new List<AppInfo>();
                _impl = new List<IPHttpApplication>();
            }
            public LoadDLLs(List<IPHttpApplication> impl, List<AppInfo> apps)
            {
                _apps = apps;
                _impl = impl;
            }
        }

        public class AppInfo
        {
            string _name;
            string _applicationsDir;

            public AppInfo(string name, string applicationsDir)
            {
                _name = name;
                _applicationsDir = applicationsDir;
            }
            public string name
            {
                get { return _name; }
                set { _name = value; }
            }
            public string applicationsDir
            {
                get { return _applicationsDir; }
                set { _applicationsDir = value; }
            }
        }

        public static LoadDLLs LoadApps()
        {
            List<AppInfo> apps = new List<AppInfo>();
            try
            {
                string jsonString = ReadJSON();
                apps = UpdateAppConfig(jsonString);
            }
            catch
            {
                throw new Exception("Failed to load config.json!");
            }

            var impl = new List<IPHttpApplication>();

            foreach (var a in apps)
            {
                string replacePath = ConfigurationManager.AppSettings["ReplacePath"];
                a.applicationsDir = replacePath + a.applicationsDir;
                string userprofile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                string path = a.applicationsDir;
                path = path.Replace(replacePath, userprofile);

                Console.WriteLine("\n\tLooking for apps in " + path + "\n");

                if (string.IsNullOrEmpty(path)) { return new LoadDLLs(); } //sanity check

                DirectoryInfo info = new DirectoryInfo(path);
                if (!info.Exists) { return new LoadDLLs(); } //make sure directory exists                

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
                            if (!impl.Contains(temp))
                            {
                                impl.Add(temp);
                            }
                        }
                    }
                }
                Console.WriteLine();
                foreach (var el in impl)
                {
                    el.Start();
                    Console.WriteLine("\tLoading " + el + "...\n");
                }
            }
            LoadDLLs loadDLLs = new LoadDLLs(impl, apps);
            return loadDLLs;
        }

        #region Read package.json
        static string ReadJSON()
        {
            string config = "config.json";

            string replacePath = ConfigurationManager.AppSettings["ReplacePath"]; ;
            string userprofile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string path = ConfigurationManager.AppSettings["Virtual"];
            path = path.Replace(replacePath, userprofile);
            string jsonPath = path + config;
            object jsonObject = new object();
            if (File.Exists(jsonPath) == true)
            {
                Console.WriteLine("\tReading JSON object from: " + config);
                var jsonString = File.ReadAllText(jsonPath);
                if (jsonString[0] == '[')
                {
                    try
                    {
                        List<JObject> jsonObjectList = (List<JObject>)Newtonsoft.Json.JsonConvert.DeserializeObject(jsonString, typeof(List<JObject>));
                        foreach (var elem in jsonObjectList)
                        {
                            //Console.WriteLine(elem.SelectToken("Name"));
                            jsonObject = elem;
                            Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(elem));
                        }
                    }
                    catch (Exception ex)
                    {
                        if (ex.GetType().IsSubclassOf(typeof(Exception)))
                            throw;

                        //Handle the case when e is the base Exception
                        Console.WriteLine("Unable to parse jsonObject.");
                    }
                }
                else
                {
                    jsonObject = JObject.Parse(jsonString);
                    //Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(jsonObject));
                }
            }
            return Newtonsoft.Json.JsonConvert.SerializeObject(jsonObject);
        }
        static List<AppInfo> UpdateAppConfig(string jsonString)
        {
            System.Configuration.Configuration config =
                ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            var jsonObject = JObject.Parse(jsonString);
            var errorTemplate = jsonObject.SelectToken("errorTemplate").ToString();
            var connectionString = jsonObject.SelectToken("connectionString").ToString();
            var layout = jsonObject.SelectToken("layout").ToString();

            var sytes = jsonObject
            .Descendants()
            .Where(x => x is JObject)
            .Where(x => x["name"] != null && x["applicationsDir"] != null)
            .Select(x => new { Name = (string)x["name"], ApplicationsDir = (string)x["applicationsDir"] })
            .ToList();

            List<AppInfo> apps = new List<AppInfo>();
            foreach (var a in sytes)
            {
                apps.Add(new AppInfo(a.Name, a.ApplicationsDir.Replace(
                    ConfigurationManager.AppSettings["ReplacePath"], "")));
            }

            var applicationsDir = jsonObject.SelectToken("defaultDir").ToString();

            if (errorTemplate != null)
            {
                config.AppSettings.Settings["ErrorTemplate"].Value = errorTemplate;
            }
            if (connectionString != null)
            {
                config.AppSettings.Settings["connectionString"].Value = connectionString;
            }
            if (layout != null)
            {
                config.AppSettings.Settings["Layout"].Value = layout;
            }
            if (applicationsDir != null)
            {
                config.AppSettings.Settings["ApplicationsDir"].Value =
                    ConfigurationManager.AppSettings["ReplacePath"] + applicationsDir;
            }
            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings");
            Console.WriteLine("\tLayout = " + layout);
            return apps;
        }
        #endregion Read package.json
    }
}