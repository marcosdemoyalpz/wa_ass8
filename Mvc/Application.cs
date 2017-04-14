using Mvc.Controllers;
using Newtonsoft.Json.Linq;
using PHttp;
using PHttp.Application;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Reflection;
using static PHttp.Startup;

namespace Mvc
{
    public class Application : IPHttpApplication
    {
        private string name = "App1";
        private ControllerBase controller = new ControllerBase();
        ErrorHandler errorHandler = new ErrorHandler();

        string defaultPath = "App1/Home/Index";

        // Explicit interface members implementation:
        void IPHttpApplication.Start()
        {
            try
            {
                string jsonString = ReadJSON();
                UpdateAppConfig(jsonString);
                Console.WriteLine("\tStarting " + name + "!");
            }
            catch (Exception)
            {

            }
        }

        void IPHttpApplication.ExecuteAction(LoadDLLs loadDLLs, HttpRequestEventArgs e = null)
        {
            Console.WriteLine("\tExecute Action");
            try
            {
                string path = e.Request.Url.PathAndQuery;
                if (path == "" || path == "/")
                {
                    path = defaultPath;
                    e.Response.Redirect(e.Request.Url.ToString() + path);
                }
                controller.Context = e.Context;
                controller.Request = e.Request;
                controller.Route = e.Request.Path;
                controller.ControllerName = path.Split('?')[0].Split('/')[2];
                controller.ActionName = path.Split('?')[0].Split('/')[3];
                controller.PrintControllerInfo();

                bool found = false;
                string className = controller.ControllerName;
                foreach (var el in loadDLLs.Controllers)
                {
                    var controllerName = el.ToString().Replace("Mvc.Controllers.", "");
                    controllerName = controllerName.Replace("Controller", "");
                    if (controllerName.ToUpper() == className.Replace("/", "").ToUpper())
                    {
                        Console.WriteLine("\n\tExecuting " + controllerName + "...\n");
                        Type type = el.GetType();
                        type.GetMethod(controller.ActionName).Invoke(el, new[] { e });
                        found = true;
                        break;
                    }
                }
                if (found == false) errorHandler.RenderErrorPage(404, e);
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

        public event PreApplicationStartMethod PreApplicationStart;

        public event ApplicationStartMethod ApplicationStart;

        #region Read package.json
        string ReadJSON(string config = "config.json")
        {
            string replacePath = ConfigurationManager.AppSettings["ReplacePath"]; ;
            string userprofile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string path = ConfigurationManager.AppSettings["Virtual"];
            path = path.Replace(replacePath, userprofile);
            string jsonPath = path + config;
            object jsonObject = new object();
            if (File.Exists(jsonPath) == true)
            {
                Console.WriteLine("\n\tReading JSON object from: " + config);
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
        void UpdateAppConfig(string jsonString)
        {
            System.Configuration.Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            var jsonObject = JObject.Parse(jsonString);
            var errorTemplate = jsonObject.SelectToken("errorTemplate").ToString();
            var connectionString = jsonObject.SelectToken("connectionString").ToString();
            var layout = jsonObject.SelectToken("layout").ToString();
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
            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings");
            Console.WriteLine("Layout = " + layout);
        }
        #endregion Read package.json
    }
}