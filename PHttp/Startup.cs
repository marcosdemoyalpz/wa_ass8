using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SQLite;
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
                _jArray = ReadJSON();
                UpdateAppConfig(_jArray);
                string database = _jArray[0].SelectToken("database").ToString();
                string connectionString = _jArray[0].SelectToken("connectionString").ToString();
                Console.WriteLine("\n\tconfig.json read successfully!\n");

                SQLiteConnection m_dbConnection;
                Console.WriteLine("\tAttempting to load Database " + database + " ...");

                if (File.Exists(database))
                {
                    Console.WriteLine("\tDatabase " + database + " already exists!");
                }
                else
                {
                    // http://blog.tigrangasparian.com/2012/02/09/getting-started-with-sqlite-in-c-part-one/
                    // 
                    //### Create the database
                    SQLiteConnection.CreateFile(database);

                    // ### Connect to the database
                    m_dbConnection = new SQLiteConnection(connectionString);
                    m_dbConnection.Open();

                    // ### Create a table
                    string sql = "CREATE TABLE users (username VARCHAR(128) PRIMARY KEY UNIQUE, password VARCHAR(128), name VARCHAR(128), lastname VARCHAR(128), token VARCHAR(256) NULL)";
                    SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
                    command.ExecuteNonQuery();

                    // ### Add some data to the table
                    sql = "insert into users (username, password, name, lastname) values ('admin', '1234', 'Marcos', 'De Moya')";
                    command = new SQLiteCommand(sql, m_dbConnection);
                    command.ExecuteNonQuery();

                    // ### select the data
                    sql = "select * from users order by username desc";
                    command = new SQLiteCommand(sql, m_dbConnection);
                    SQLiteDataReader reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        Console.WriteLine("\tUsername: " + reader["username"] + "\tPassword: " + reader["password"]);
                    }
                    Console.WriteLine("\tDatabase " + database + " has been created!");
                }
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

        #region Read package.json
        JArray ReadJSON()
        {
            try
            {
                string config = "config.json";
                Console.WriteLine("\t" + config);
                string path = ConfigurationManager.AppSettings["Virtual"];
                Console.WriteLine("\t" + path);
                string jsonPath = path + config;
                Console.WriteLine("\t" + jsonPath);
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
                    JArray jArray = JArray.Parse(jsonString);
                    //foreach (JObject obj in jArray.Children<JObject>())
                    //{
                    //    Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(obj));
                    //}
                    return jArray;
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
        void UpdateAppConfig(JArray jArray)
        {
            _apps = new List<AppInfo>();
            try
            {
                System.Configuration.Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                var errorTemplate = jArray[0].SelectToken("errorTemplate").ToString();
                var connectionString = jArray[0].SelectToken("connectionString").ToString();
                var layout = jArray[0].SelectToken("layout").ToString();
                var database = jArray[0].SelectToken("database").ToString();
                var applicationsDir = jArray[0].SelectToken("defaultDir").ToString();

                var sites = jArray
                .Descendants()
                .Where(x => x is JObject)
                .Where(x => x["name"] != null && x["applicationsDir"] != null)
                .Select(x => new { Name = (string)x["name"], ApplicationsDir = (string)x["applicationsDir"] })
                .ToList();

                foreach (var a in sites)
                {
                    _apps.Add(new AppInfo(a.Name, a.ApplicationsDir));
                }

                if (errorTemplate != null)
                {
                    config.AppSettings.Settings["ErrorTemplate"].Value = errorTemplate;
                    Console.WriteLine("\tErrorTemplate = " + errorTemplate);
                }
                config.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection("appSettings");

            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        #endregion Read package.json
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