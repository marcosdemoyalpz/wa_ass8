using Newtonsoft.Json.Linq;
using PHttp;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using static PHttp.Startup;

namespace Mvc
{
    public class Application : IPHttpApplication
    {
        private string name = "MVC_APP";
        ErrorHandler errorHandler = new ErrorHandler();

        // Explicit interface members implementation:
        void IPHttpApplication.Start()
        {
            try
            {
                Console.WriteLine("\tStarting " + name + "!");
            }
            catch (Exception)
            {

            }
        }

        void IPHttpApplication.ExecuteAction(HttpRequestEventArgs e, string applicationsDir)
        {
            Console.WriteLine("\tExecute Action");
            try
            {
                Router router = new Router(name);
                router.CallAction(e, applicationsDir);
            }
            catch
            {
                errorHandler.RenderErrorPage(404, e);
            }
        }

        string IPHttpApplication.Name
        {
            set
            {
                name = value;
            }
            get
            {
                return name;
            }
        }

        //public event PreApplicationStartMethod PreApplicationStart;

        //public event ApplicationStartMethod ApplicationStart;

        //#region Read package.json
        //string ReadJSON()
        //{
        //    string config = name + ".json";

        //    string replacePath = ConfigurationManager.AppSettings["ReplacePath"]; ;
        //    string userprofile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        //    string path = ConfigurationManager.AppSettings["Virtual"];
        //    path = path.Replace(replacePath, userprofile);
        //    string jsonPath = path + config;
        //    object jsonObject = new object();
        //    if (File.Exists(jsonPath) == true)
        //    {
        //        Console.WriteLine("\tReading JSON object from: " + config);
        //        var jsonString = File.ReadAllText(jsonPath);
        //        if (jsonString[0] == '[')
        //        {
        //            try
        //            {
        //                List<JObject> jsonObjectList = (List<JObject>)Newtonsoft.Json.JsonConvert.DeserializeObject(jsonString, typeof(List<JObject>));
        //                foreach (var elem in jsonObjectList)
        //                {
        //                    //Console.WriteLine(elem.SelectToken("Name"));
        //                    jsonObject = elem;
        //                    Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(elem));
        //                }
        //            }
        //            catch (Exception ex)
        //            {
        //                if (ex.GetType().IsSubclassOf(typeof(Exception)))
        //                    throw;

        //                //Handle the case when e is the base Exception
        //                Console.WriteLine("Unable to parse jsonObject.");
        //            }
        //        }
        //        else
        //        {
        //            jsonObject = JObject.Parse(jsonString);
        //            //Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(jsonObject));
        //        }
        //    }
        //    return Newtonsoft.Json.JsonConvert.SerializeObject(jsonObject);
        //}
        //void UpdateAppConfig(string jsonString)
        //{
        //    System.Configuration.Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
        //    var jsonObject = JObject.Parse(jsonString);
        //    var errorTemplate = jsonObject.SelectToken("errorTemplate").ToString();
        //    var connectionString = jsonObject.SelectToken("connectionString").ToString();
        //    var layout = jsonObject.SelectToken("layout").ToString();
        //    if (errorTemplate != null)
        //    {
        //        config.AppSettings.Settings["ErrorTemplate"].Value = errorTemplate;
        //    }
        //    if (connectionString != null)
        //    {
        //        config.AppSettings.Settings["connectionString"].Value = connectionString;
        //    }
        //    if (layout != null)
        //    {
        //        config.AppSettings.Settings["Layout"].Value = layout;
        //    }
        //    config.Save(ConfigurationSaveMode.Modified);
        //    ConfigurationManager.RefreshSection("appSettings");
        //    Console.WriteLine("\tLayout = " + layout);
        //}
        //#endregion Read package.json
    }
}