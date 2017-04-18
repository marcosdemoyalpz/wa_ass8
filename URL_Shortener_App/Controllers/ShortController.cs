using HandlebarsDotNet;
using System;
using System.Configuration;
using System.IO;
using Mvc.Attributes;
using PHttp;
using System.Data.SQLite;
using System.Collections.Generic;
using JWT;
using JWT.Serializers;
using Newtonsoft.Json.Linq;

namespace URL_Shortener_App.Controllers
{
    internal class ShortController : Mvc.ControllerBase
    {
        string secret = ConfigurationManager.AppSettings["Secret"];
        string resource = ConfigurationManager.AppSettings["Virtual"];
        string layout = ConfigurationManager.AppSettings["Layout"];

        AppInfo _app;

        SQLiteConnection m_dbConnection;

        LoadConfig loadConfig = new LoadConfig();

        float loginTimeout = 1.25f;
        int expiration = 7200;

        string cookieName1 = "URL_Shortener_App_JWT";

        private string _appName = "URL_Shortener_App";
        private string _controllerName = "Home";
        ErrorHandler errorHandler = new ErrorHandler();

        #region Private Methods
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

        private string DecodeToken(string token)
        {
            string json = "";
            try
            {
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

        string GetAppURL(HttpRequestEventArgs e, string controllerName = null)
        {
            if (controllerName == null || controllerName == "")
            {
                controllerName = _controllerName;
            }

            string protocol = (e.Request.Url.Scheme.ToString() == "https") ? "https://" : "http://";
            string url = protocol + e.Request.Url.Host + ":"
            + e.Request.Url.Port + "/" + _appName
            + "/" + controllerName + "/";
            return url;
        }

        bool RedirectShort(HttpRequestEventArgs e)
        {
            try
            {
                if (DbLogin(e, true))
                {
                    _app = loadConfig.InitApp(resource + _appName, "/config.json");
                    HttpCookie cookie = e.Request.Cookies.Get(cookieName1);
                    JArray jArray = new JArray();
                    string decoded;
                    if (cookie != null && cookie.Value != "")
                    {
                        decoded = DecodeToken(cookie.Value);
                        jArray = GetJArray(decoded);
                    }

                    string username = jArray[0].SelectToken("username").ToString();

                    // ### Connect to the database
                    m_dbConnection = new SQLiteConnection(_app.connectionString);
                    m_dbConnection.Open();

                    // ### select the data
                    string sql = "SELECT * FROM urls";
                    SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
                    SQLiteDataReader reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        if (reader["username"].ToString().ToLower() == username.ToLower())
                        {
                            var shortUrl = reader["shortURL"].ToString();
                            var longURL = reader["longURL"].ToString();

                            if (e.Request.Params["go"] == shortUrl)
                            {
                                e.Response.Redirect(longURL);
                                return true;
                            }
                        }
                    }
                    return false;
                }
            }
            catch
            {
                return false;
            }
            return false;
        }

        bool DbLogin(HttpRequestEventArgs e, bool fromCookie = false)
        {
            try
            {
                _app = loadConfig.InitApp(resource + _appName, "/config.json");
                HttpCookie cookie = e.Request.Cookies.Get(cookieName1);
                JArray jArray = new JArray();
                string decoded;
                if (cookie != null && cookie.Value != "")
                {
                    decoded = DecodeToken(cookie.Value);
                    jArray = GetJArray(decoded);
                }

                string username = (fromCookie == true) ? jArray[0].SelectToken("username").ToString() : e.Request.Form.Get("username");
                string password = (fromCookie == true) ? jArray[0].SelectToken("password").ToString() : e.Request.Form.Get("password");

                bool success = false;

                // ### Connect to the database
                m_dbConnection = new SQLiteConnection(_app.connectionString);
                m_dbConnection.Open();

                // ### select the data
                string sql = "select * from users order by username desc";
                SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
                SQLiteDataReader reader = command.ExecuteReader();
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
                return success;
            }
            catch
            {
                return false;
            }
        }

        void RenderMessage(HttpRequestEventArgs e, string message)
        {
            string views = resource + _appName + "/Views/";
            HttpResponse res = e.Response;
            string filePath = views + "alert.hbs";

            if (File.Exists(filePath) == true)
            {
                var source = File.ReadAllText(filePath);
                var template = Handlebars.Compile(source);
                var data = new
                {
                    title = "Marcos URL Shortener",
                    mainH1 = message,
                    body = "Redirecting, please wait..."
                };
                var result = template(data);
                using (var writer = new StreamWriter(e.Response.OutputStream))
                {
                    writer.Write(result);
                }
            }
            else
            {
                errorHandler.RenderErrorPage(404, e);
                Console.WriteLine("\tFile not found!");
            }
        }
        #endregion

        [HttpGet]
        public void Path(HttpRequestEventArgs e)
        {
            if (RedirectShort(e)) { }
            else
            {
                errorHandler.RenderErrorPage(404, e);
                Console.WriteLine("\tFile not found!");
            }
        }
    }
}