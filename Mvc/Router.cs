using JWT;
using JWT.Serializers;
using Newtonsoft.Json.Linq;
using PHttp;
using System;
using System.Collections.Generic;
using System.Configuration;
using Mono.Data.Sqlite;
using System.IO;
using System.Reflection;
using static PHttp.Startup;
using System.Data;

namespace Mvc
{
    public class Router
    {
        private string _appName;
        public string AppName
        {
            get { return _appName; }
            set { _appName = value; }
        }
        public class Controllers
        {
            List<ControllerBase> _controllers = new List<ControllerBase>();
            public List<ControllerBase> GetControllers
            {
                get { return _controllers; }
                set { _controllers = value; }
            }
            public Controllers(List<ControllerBase> controllers)
            {
                _controllers = controllers;
            }
        }

        bool _FailedAuth = false;

        JArray GetJArray(string jsonString)
        {
            if (jsonString[0] != '[')
            {
                jsonString = "[" + jsonString;
                if (jsonString[jsonString.Length - 1] != ']')
                {
                    jsonString = jsonString + "]";
                }
            }
            if (jsonString[jsonString.Length - 1] != ']')
            {
                jsonString = jsonString + "]";
            }
            return JArray.Parse(jsonString);
        }

        bool DbLogin(HttpRequestEventArgs e)
        {
            try
            {
                string resource = ConfigurationManager.AppSettings["Virtual"];
                LoadConfig loadConfig = new LoadConfig();
                AppInfo _app = loadConfig.InitApp(resource + _appName, "/config.json");
                JArray jArray = new JArray();
                string decoded;

                HttpCookie cookie = e.Request.Cookies.Get(_appName + "_JWT");
                decoded = DecodeToken(cookie.Value);
                jArray = GetJArray(decoded);

                string username = jArray[0].SelectToken("username").ToString();
                string password = jArray[0].SelectToken("password").ToString();

                bool success = false;

                // ### Connect to the database
                IDbConnection m_dbConnection;
                m_dbConnection = new SqliteConnection(_app.connectionString);
                m_dbConnection.Open();

                // ### select the data
                string sql = "select * from users order by username desc";
                IDbCommand command = m_dbConnection.CreateCommand(); command.CommandText = sql;
                IDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    if (reader["username"].ToString().ToLower() == username)
                    {
                        if (reader["password"].ToString() == password)
                        {
                            success = true;
                        }
                    }
                }
                reader.Close();
                return success;
            }
            catch
            {
                return false;
            }
        }

        private string DecodeToken(string token)
        {
            string json = "";
            try
            {
                string secret = ConfigurationManager.AppSettings["Secret"];
                IJsonSerializer serializer = new JsonNetSerializer();
                IDateTimeProvider provider = new UtcDateTimeProvider();
                IJwtValidator validator = new JwtValidator(serializer, provider);
                IBase64UrlEncoder urlEncoder = new JwtBase64UrlEncoder();
                IJwtDecoder decoder = new JwtDecoder(serializer, validator, urlEncoder);

                json = decoder.Decode(token, secret, verify: true);
                Console.WriteLine(json);
            }
            catch (TokenExpiredException)
            {
                Console.WriteLine("Token has expired");
            }
            catch (SignatureVerificationException)
            {
                Console.WriteLine("Token has invalid signature");
            }
            return json;
        }

        public static Controllers loadControllers(string path, string appName)
        {
            Console.WriteLine("\n\tLooking for apps in " + path + "\n");

            if (string.IsNullOrEmpty(path)) { throw new FileNotFoundException(); } //sanity check

            DirectoryInfo info = new DirectoryInfo(path);
            if (!info.Exists) { throw new FileNotFoundException(); } //make sure directory exists

            var impl = new List<ControllerBase>();

            foreach (FileInfo file in info.GetFiles("*.dll")) //loop through all dll files in directory
            {
                if (file.Name == (appName + ".dll"))
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
                        if (type != typeof(ControllerBase) && typeof(ControllerBase).IsAssignableFrom(type))
                        {
                            var temp = (ControllerBase)Activator.CreateInstance(type);
                            if (!impl.Contains(temp))
                            {
                                impl.Add(temp);
                            }
                        }
                    }
                }
            }
            Console.WriteLine();
            foreach (var el in impl)
            {
                Console.WriteLine("\tLoading " + el + "...\n");
            }
            return new Controllers(impl);
        }

        public Router(string appName)
        {
            _controller = new ControllerBase();
            _errorHandler = new ErrorHandler();
            _appName = appName;
        }

        private ControllerBase _controller;
        ErrorHandler _errorHandler;

        private bool IsHttpAttributeAllowed(MemberInfo member, string requestType, HttpRequestEventArgs e)
        {
            bool allowed = false;
            foreach (object attribute in member.GetCustomAttributes(true))
            {
                if (attribute is Attributes.AuthorizeAttribute)
                {
                    _FailedAuth = !DbLogin(e);
                }
                else
                {
                    if (allowed == true)
                    {
                        break;
                    }
                    else
                    {
                        switch (requestType)
                        {
                            case "GET":
                                if (attribute is Attributes.HttpGet)
                                {
                                    allowed = true;
                                }
                                break;
                            case "POST":
                                if (attribute is Attributes.HttpPost)
                                {
                                    allowed = true;
                                }
                                break;
                            case "PUT":
                                if (attribute is Attributes.HttpPut)
                                {
                                    allowed = true;
                                }
                                break;
                            case "DELETE":
                                if (attribute is Attributes.HttpDelete)
                                {
                                    allowed = true;
                                }
                                break;
                            default:
                                allowed = false;
                                break;
                        }
                    }
                }
            }
            if (_FailedAuth)
            {
                return false;
            }
            else
            {
                return allowed;
            }
        }

        public void CallAction(HttpRequestEventArgs e, string directory)
        {
            string path = e.Request.Url.PathAndQuery;
            if (path == "" || path == "/")
            {
                _errorHandler.RenderErrorPage(404, e);
            }
            else
            {
                _controller.Context = e.Context;
                _controller.Request = e.Request;
                _controller.Route = e.Request.Path;
                _controller.ControllerName = path.Split('?')[0].Split('/')[2];
                _controller.ActionName = path.Split('?')[0].Split('/')[3];
                _controller.PrintControllerInfo();

                bool found = false;
                bool allowed = false;
                string className = _controller.ControllerName;
                foreach (var el in loadControllers(directory, _appName).GetControllers)
                {
                    var tempStr = path.Split('?')[0].Split('/')[1] + ".Controllers.";
                    var controllerName = el.ToString().Replace(tempStr, "");
                    controllerName = controllerName.Replace("Controller", "");
                    if (controllerName.ToUpper() == className.Replace("/", "").ToUpper())
                    {
                        Console.WriteLine("\tExecuting " + controllerName + "...\n");
                        Type type = el.GetType();

                        MethodInfo method = type.GetMethod(_controller.ActionName);
                        string requestType = e.Request.RequestType;

                        if (IsHttpAttributeAllowed(method, requestType, e))
                        {
                            Console.WriteLine("\tMethod " + method.Name + " allows " + requestType);
                            allowed = true;
                        }
                        else
                        {
                            if (_FailedAuth)
                            {
                                Console.WriteLine("\tMethod requires authentication!");
                            }
                            else
                            {
                                Console.WriteLine("\tMethod " + method.Name + " does not allow " + requestType);
                            }
                        }

                        Console.WriteLine();

                        if (allowed == true)
                        {
                            e.Response.StatusCode = 200;
                            method.Invoke(el, new[] { e });
                        }
                        else
                        {
                            if (_FailedAuth)
                            {
                                _errorHandler.RenderErrorPage(401, e);
                            }
                            else
                            {
                                _errorHandler.RenderErrorPage(405, e);
                            }
                        }
                        found = true;
                        break;
                    }
                }
                if (found == false) _errorHandler.RenderErrorPage(404, e);
            }
        }
    }
}
