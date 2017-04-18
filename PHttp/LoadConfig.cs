using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PHttp
{
    public class LoadConfig
    {
        private JArray _jArray;
        private List<AppInfo> _apps;
        public JArray JsonArray
        {
            get { return _jArray; }
            private set { _jArray = value; }
        }
        public List<AppInfo> apps
        {
            get { return _apps; }
            private set { _apps = value; }
        }

        public void InitServer(string path, string config)
        {
            UpdateServerConfig(ReadJSON(path, config));
        }

        public AppInfo InitApp(string path, string config)
        {
            return GetAppInfo(ReadJSON(path, config));
        }

        #region Read package.json
        JArray ReadJSON(string path, string config)
        {
            try
            {
                //Console.WriteLine("\t" + config);
                //Console.WriteLine("\t" + path);
                string jsonPath = path + config;
                //Console.WriteLine("\t" + jsonPath);
                if (File.Exists(jsonPath) == true)
                {
                    Console.WriteLine("\tReading JSON object from: " + config);
                    var jsonString = File.ReadAllText(jsonPath);
                    if (jsonString[0] != '[')
                    {
                        try
                        {
                            jsonString = "[" + jsonString;
                            if (jsonString[jsonString.Length - 1] != ']')
                            {
                                jsonString = jsonString + "]";
                            }
                        }
                        catch (Exception ex)
                        {
                            throw new Exception(ex.Message);
                        }
                    }
                    return JArray.Parse(jsonString);
                }
                else
                {
                    throw new FileNotFoundException(config + " not found!");
                }
            }
            catch (Newtonsoft.Json.JsonReaderException ex)
            {
                throw new Newtonsoft.Json.JsonReaderException(ex.Message);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        void UpdateServerConfig(JArray jArray)
        {
            _apps = new List<AppInfo>();
            try
            {
                System.Configuration.Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                var layout = jArray[0].SelectToken("layout").ToString();
                var errorTemplate = jArray[0].SelectToken("errorTemplate").ToString();
                var applicationsDir = jArray[0].SelectToken("defaultDir").ToString();

                var sites = jArray
                .Descendants()
                .Where(x => x is JObject)
                .Where(x => x["name"] != null && x["applicationsDir"] != null)
                .Select(x => new
                {
                    Name = (string)x["name"],
                    ApplicationsDir = (string)x["applicationsDir"],
                    Database = (string)x["database"],
                    ConnectionString = (string)x["connectionString"],
                    VirtualPath = (string)x["virtualPath"],
                    DefaultDocument = (string)x["defaultDocument"]
                })
                .ToList();

                foreach (var a in sites)
                {
                    string connectionString = "Data Source=" + a.VirtualPath + a.Database + ";Version=3;";
                    _apps.Add(new AppInfo(a.Name, a.ApplicationsDir, a.Database, connectionString,
                        a.VirtualPath, layout, a.DefaultDocument));
                }

                //if (errorTemplate != null)
                //{
                //    config.AppSettings.Settings["ErrorTemplate"].Value = errorTemplate;
                //    Console.WriteLine("\tErrorTemplate = " + errorTemplate);
                //}
                config.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection("appSettings");

            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        AppInfo GetAppInfo(JArray jArray)
        {
            try
            {
                System.Configuration.Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                var layout = jArray[0].SelectToken("layout").ToString();
                var database = jArray[0].SelectToken("database").ToString();
                var applicationsDir = jArray[0].SelectToken("applicationsDir").ToString();
                var name = jArray[0].SelectToken("applicationsDir").ToString();
                var virtualPath = jArray[0].SelectToken("virtualPath").ToString();
                var defaultDocument = jArray[0].SelectToken("defaultDocument").ToString();

                var connectionString = "Data Source=" + virtualPath + database + ";Version=3;";

                return new AppInfo(name, applicationsDir, database, connectionString,
                        virtualPath, layout, defaultDocument);

            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        #endregion Read package.json
        public LoadConfig()
        {
            _jArray = new JArray();
            _apps = new List<AppInfo>();
        }
    }
}
