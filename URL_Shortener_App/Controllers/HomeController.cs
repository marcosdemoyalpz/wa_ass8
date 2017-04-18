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
    internal class HomeController : Mvc.ControllerBase
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

        string CreateTable(HttpRequestEventArgs e)
        {
            string table = "";
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

                    table = "<table class=\"table\">";
                    table = table + "<thead><tr><th>Short URL</th><th>Long URL</th></tr></thead><tbody>";

                    var trOpen = "<tr>";
                    var trClose = "</tr>";
                    var tdOpen = "<td>";
                    var tdClose = "</td>";

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
                            var link1 = "<a href=\"" + shortUrl + "\">" + shortUrl + "</a>";
                            var link2 = "<a href=\"" + longURL + "\">" + longURL + "</a>";

                            table = table + trOpen + tdOpen + link1 + tdClose + tdOpen + link2 + tdClose + trClose;
                        }
                    }

                    table = table + "</tbody></table>";
                    return table;
                }
            }
            catch
            {
                table = "";
            }
            table = "<table class=\"table\">";
            table = table + "<thead><tr><th>Short URL</th><th>Long URL</th></tr></thead><tbody>";
            table = table + "</tbody></table>";
            return table;
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

        [HttpGet, HttpPost]
        public void Index(HttpRequestEventArgs e)
        {
            string protocol = (e.Request.Url.Scheme.ToString() == "https") ? "https://" : "http://";
            string url = protocol + e.Request.Url.Host + ":"
                    + e.Request.Url.Port + "/" + _appName
                    + "/" + _controllerName + "/";

            if (e.Request.RequestType == "GET")
            {
                string views = resource + _appName + "/Views/";
                HttpResponse res = e.Response;
                string filePath = views + "Main.hbs";
                Console.WriteLine("\tStarting " + _appName + "!");
                Console.WriteLine("\tLoading file on " + filePath + "!");
                object data;

                if (DbLogin(e, true))
                {
                    var table = CreateTable(e);
                    data = new
                    {
                        showNavButtons = true,
                        login = true,
                        btn1 = "Home",
                        btn2 = "About",
                        btn3 = "Sign Out",
                        link1 = url + "Index",
                        link2 = url + "About",
                        link3 = url + "Login",
                        title = "Marcos URL Shortener",
                        mainH1 = "Marcos's URL Shortener",
                        body = table
                    };
                }
                else
                {
                    data = new
                    {
                        showNavButtons = true,
                        login = false,
                        btn1 = "Home",
                        btn2 = "About",
                        btn3 = "Sign In",
                        link1 = url + "Index",
                        link2 = url + "About",
                        link3 = url + "Login",
                        title = "Marcos URL Shortener",
                        mainH1 = "Marcos's URL Shortener",
                        body = File.ReadAllText(views + "/partials/captcha.hbs")
                    };
                }
                if (File.Exists(filePath) == true)
                {
                    var source = File.ReadAllText(filePath);
                    var template = Handlebars.Compile(source);

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
            else if (e.Request.RequestType == "POST")
            {

            }
        }

        [HttpGet]
        public void About(HttpRequestEventArgs e)
        {
            string protocol = (e.Request.Url.Scheme.ToString() == "https") ? "https://" : "http://";
            string url = protocol + e.Request.Url.Host + ":"
                    + e.Request.Url.Port + "/" + _appName
                    + "/" + _controllerName + "/";

            if (DbLogin(e, true))
            {
                string views = resource + _appName + "/Views/";
                HttpResponse res = e.Response;
                string filePath = views + layout;
                Console.WriteLine("\tStarting " + _appName + "!");
                Console.WriteLine("\tLoading file on " + filePath + "!");

                if (File.Exists(filePath) == true)
                {
                    var source = File.ReadAllText(filePath);
                    var template = Handlebars.Compile(source);
                    var data = new
                    {
                        noTitle = true,
                        showNavButtons = true,
                        btn1 = "Home",
                        btn2 = "About",
                        btn3 = "Sign Out",
                        link1 = url + "Index",
                        link2 = url + "About",
                        link3 = url + "Login",
                        title = "Marcos URL Shortener",
                        mainH1 = "About",
                        body = File.ReadAllText(views + "/partials/about.hbs")
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
            else
            {
                e.Response.Headers.Add("REFRESH", loginTimeout.ToString() + ";URL=" + url + "Index");
                errorHandler.RenderErrorPage(401, e);
            }
        }

        [HttpGet, HttpPost]
        public void Login(HttpRequestEventArgs e)
        {
            string url = "http://" + e.Request.Url.Host + ":"
                            + e.Request.Url.Port + "/" + _appName
                            + "/" + _controllerName + "/";
            if (e.Request.RequestType == "GET")
            {
                if (DbLogin(e, true))
                {
                    e.Response.Cookies.Get(cookieName1).Expires = DateTime.Now;
                    e.Response.Redirect(url + "Login");
                }
                else
                {
                    string views = resource + _appName + "/Views/";
                    HttpResponse res = e.Response;
                    string filePath = views + layout;
                    Console.WriteLine("\tStarting " + _appName + "!");
                    Console.WriteLine("\tLoading file on " + filePath + "!");

                    if (File.Exists(filePath) == true)
                    {
                        var source = File.ReadAllText(filePath);
                        var template = Handlebars.Compile(source);
                        var data = new
                        {
                            showNavButtons = false,
                            btn1 = "Home",
                            btn2 = "About",
                            btn3 = "Sign Out",
                            link1 = url + "Index",
                            link2 = url + "About",
                            link3 = url + "Login",
                            title = "Marcos URL Shortener",
                            mainH1 = "Login",
                            body = File.ReadAllText(views + "/partials/login.hbs")
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
            }
            else if (e.Request.RequestType == "POST")
            {
                string username = e.Request.Form.Get("username");
                string password = e.Request.Form.Get("password");
                if (DbLogin(e))
                {
                    IDateTimeProvider provider = new UtcDateTimeProvider();
                    var now = provider.GetNow();

                    var unixEpoch = JwtValidator.UnixEpoch; // or use JwtValidator.UnixEpoch
                    var secondsSinceEpoch = Math.Round((now - unixEpoch).TotalSeconds);

                    var exp = secondsSinceEpoch + expiration;

                    // Creating and Encodin JWT token
                    var payload = new Dictionary<string, object>
                        {
                            { "username", username },
                            { "password", password },
                            { "exp", exp }
                        };

                    IJwtAlgorithm algorithm = new JWT.Algorithms.HMACSHA256Algorithm();
                    IJsonSerializer serializer = new JsonNetSerializer();
                    IBase64UrlEncoder urlEncoder = new JwtBase64UrlEncoder();
                    IJwtEncoder encoder = new JwtEncoder(algorithm, serializer, urlEncoder);

                    string token = encoder.Encode(payload, secret);

                    Console.WriteLine(token);

                    HttpCookie cookie = new HttpCookie(cookieName1, token);
                    e.Response.Cookies.Add(cookie);
                    e.Response.Cookies.Get(cookieName1).Expires = DateTime.Now.AddDays(30);

                    e.Response.Headers.Add("REFRESH", loginTimeout.ToString() + ";URL=" + url + "Index");
                    e.Response.StatusCode = 200;
                    RenderMessage(e, "Login Successful!");
                }
                else
                {
                    e.Response.Headers.Add("REFRESH", loginTimeout.ToString() + ";URL=" + url + "Index");
                    e.Response.StatusCode = 401;
                    RenderMessage(e, "Login Failed!");
                }
            }
        }
    }
}