using HandlebarsDotNet;
using System;
using System.Configuration;
using System.IO;
using Mvc.Attributes;
using PHttp;
using Mono.Data.Sqlite;
using System.Collections.Generic;
using JWT;
using JWT.Serializers;
using Newtonsoft.Json.Linq;
using NReco.PhantomJS;
using System.Data;

namespace URL_Shortener_App.Controllers
{
    internal class HomeController : Mvc.ControllerBase
    {
        string secret = ConfigurationManager.AppSettings["Secret"];
        string resource = ConfigurationManager.AppSettings["Virtual"];
        string layout = ConfigurationManager.AppSettings["Layout"];

        AppInfo _app;

        IDbConnection m_dbConnection;

        LoadConfig loadConfig = new LoadConfig();

        string _shortURL;
        string _longURL;
        float loginTimeout = 1.25f;
        int expiration = 7200;

        string cookieName1 = "URL_Shortener_App_JWT";
        string AnonToken = "";

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

        string GetAppURL(HttpRequestEventArgs e, string controllerName = "")
        {
            if (controllerName == "")
            {
                controllerName = _controllerName;
            }

            string protocol = (e.Request.Url.Scheme.ToString() == "https") ? "https://" : "http://";
            string url = protocol + e.Request.Url.Host + ":"
            + e.Request.Url.Port + "/" + _appName
            + "/" + controllerName + "/";
            return url;
        }

        string CreateTable(HttpRequestEventArgs e)
        {
            string url = GetAppURL(e, "Short") + "Path";
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

                    table = "<table class=\"table\" style=\"vertical-align: middle;\">";
                    table = table + "<thead>" + "<tr>"
                        + "<th style=\"text-align: center;\">" + "Image" + "</th>"
                        + "<th style=\"text-align: center;\">" + "Short URL" + "</th>"
                        + "<th style=\"text-align: center;\">" + "Long URL" + "</th>"
                        + "<th style=\"text-align: center;\">" + "Date Created" + "</th>"
                        + "<th style=\"text-align: center;\">" + "Clicks" + "</th>"
                        + "<th style=\"text-align: center;\">" + "Last Click" + "</th>"
                        + "</tr>" + "</thead>"
                        + "<tbody>";

                    var trOpen = "<tr style=\"vertical-align: middle;\" align=\"center\">";
                    var trClose = "</tr>";
                    var tdOpen = "<td style=\"vertical-align: middle;\" align=\"center\">";
                    var tdClose = "</td>";

                    // ### Connect to the database
                    m_dbConnection = new SqliteConnection(_app.connectionString);
                    m_dbConnection.Open();

                    // ### select the data
                    string sql = "SELECT * FROM urls";
                    IDbCommand command = m_dbConnection.CreateCommand(); command.CommandText = sql;
                    IDataReader reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        if (reader["username"].ToString().ToLower() == username.ToLower())
                        {
                            string img = "<img src=\"";
                            var shortUrl = url + "?go=" + reader["shortURL"].ToString();
                            var longURL = reader["longURL"].ToString();
                            var link1 = "<a href=\"" + shortUrl + "\">" + shortUrl.Replace(url + "?", "") + "</a>";
                            var link2 = "<a href=\"" + longURL + "\">" + longURL + "</a>";
                            var dateCreated = reader["dateCreated"].ToString();
                            var clicks = reader["clicks"].ToString();
                            var lastClicked = reader["lastClicked"].ToString();

                            var button = "<a href=\"" + GetAppURL(e, "Short") + "Details" + "?go=" + reader["shortURL"].ToString()
                                + "\" class=\"btn btn-lg btn-info btn-block\" role=\"button\">Analytics</a>";

                            img = img + GetAppURL(e, "img") + reader["shortURL"].ToString() + ".png\"" + "class=\"portrait\"" + " alt=\"URL Image\" + "
                                + "style=\"max-width:100px; max-height:100%; display:inline-block; overflow: hidden;\">";
                            Console.WriteLine(img);
                            table = table +
                                trOpen +
                                    "<td class=\"thumbnail\" align=\"center\">" +
                                        img +
                                    tdClose +
                                    tdOpen +
                                        link1 +
                                    tdClose +
                                    tdOpen +
                                        link2 +
                                    tdClose +
                                    tdOpen +
                                        dateCreated +
                                    tdClose +
                                    tdOpen +
                                        clicks +
                                    tdClose +
                                    tdOpen +
                                        lastClicked +
                                    tdClose +
                                    tdOpen +
                                        button +
                                    tdClose +
                                trClose;
                        }
                    }
                    reader.Close();
                    table = table + "</tbody></table>";
                    return table;
                }
            }
            catch (Exception ex)
            {
                table = "";
                return ex.ToString();
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
                JArray jArray = new JArray();
                string decoded;
                if (fromCookie)
                {
                    HttpCookie cookie = e.Request.Cookies.Get(cookieName1);
                    decoded = DecodeToken(cookie.Value);
                    jArray = GetJArray(decoded);
                }

                string username = (fromCookie == true) ? jArray[0].SelectToken("username").ToString() : e.Request.Form.Get("username");
                string password = (fromCookie == true) ? jArray[0].SelectToken("password").ToString() : e.Request.Form.Get("password");

                bool success = false;

                // ### Connect to the database
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

        bool DbRegister(HttpRequestEventArgs e)
        {
            try
            {
                _app = loadConfig.InitApp(resource + _appName, "/config.json");

                string username = e.Request.Form.Get("username");
                string password = e.Request.Form.Get("password");
                string confirm_password = e.Request.Form.Get("confirm_password");
                string name = e.Request.Form.Get("name");
                string lastname = e.Request.Form.Get("lastname");

                if (password != confirm_password)
                {
                    return false;
                }

                // ### Connect to the database
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
                        return false;
                    }
                }
                reader.Close();
                // ### Add some data to the table
                sql = "insert into users (username, password, name, lastname) values ('"
                    + username + "','"
                    + password + "','"
                    + name + "','"
                    + lastname + "')";
                command.CommandText = sql;
                command.ExecuteNonQuery();
                return true;
            }
            catch
            {
                return false;
            }
        }

        bool LoginProcess(HttpRequestEventArgs e)
        {
            string url = GetAppURL(e);
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
                return true;
            }
            else
            {
                e.Response.Headers.Add("REFRESH", loginTimeout.ToString() + ";URL=" + url + "Login");
                e.Response.StatusCode = 401;
                RenderMessage(e, "Login Failed!");
                return false;
            }
        }

        bool CreateShortURL(HttpRequestEventArgs e)
        {
            try
            {
                _app = loadConfig.InitApp(resource + _appName, "/config.json");

                string longURL = e.Request.Form.Get("longURL");

                Random random = new Random();
                string shortURL = "";
                int i;
                for (i = 1; i < 11; i++)
                {
                    shortURL += random.Next(0, 9).ToString();
                }

                if (DbLogin(e, true))
                {
                    string decoded;
                    HttpCookie cookie = e.Request.Cookies.Get(cookieName1);
                    decoded = DecodeToken(cookie.Value);
                    JArray jArray = GetJArray(decoded);

                    string username = jArray[0].SelectToken("username").ToString();

                    // ### Connect to the database
                    m_dbConnection = new SqliteConnection(_app.connectionString);
                    m_dbConnection.Open();

                    // ### select the data
                    string sql = "select * from urls order by username desc";
                    IDbCommand command = m_dbConnection.CreateCommand(); command.CommandText = sql;
                    IDataReader reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        if (reader["username"].ToString().ToLower() == username)
                        {
                            if (reader["shortURL"].ToString() == shortURL)
                            {
                                return false;
                            }
                        }
                    }
                    reader.Close();
                    // ### Add some data to the table
                    sql = "insert into urls (shortURL, longURL, username, dateCreated, clicks, lastClicked) values ('"
                        + shortURL + "','"
                        + longURL + "','"
                        + username + "',"
                        + "DATETIME('NOW'),"
                        + 0 + ","
                        + "DATETIME('NOW')"
                        + ")";
                    command.CommandText = sql;
                    command.ExecuteNonQuery();
                    _shortURL = shortURL;
                    _longURL = longURL;
                    return true;
                }
                else
                {
                    IDateTimeProvider provider = new UtcDateTimeProvider();
                    var now = provider.GetNow();

                    var unixEpoch = JwtValidator.UnixEpoch; // or use JwtValidator.UnixEpoch
                    var secondsSinceEpoch = Math.Round((now - unixEpoch).TotalSeconds);

                    var exp = secondsSinceEpoch + expiration;

                    // Creating and Encodin JWT token
                    var payload = new Dictionary<string, object>
                        {
                            { "shortURL", shortURL },
                            { "longURL", longURL }
                            //{ "exp", 7200 }
                        };

                    IJwtAlgorithm algorithm = new JWT.Algorithms.HMACSHA256Algorithm();
                    IJsonSerializer serializer = new JsonNetSerializer();
                    IBase64UrlEncoder urlEncoder = new JwtBase64UrlEncoder();
                    IJwtEncoder encoder = new JwtEncoder(algorithm, serializer, urlEncoder);

                    string token = encoder.Encode(payload, secret);

                    Console.WriteLine(token);
                    AnonToken = token;

                    _longURL = longURL;
                    _shortURL = shortURL;
                    return true;
                }
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
            string url = GetAppURL(e);

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
                        mainH1 = "Marcos's App",
                        mainH2 = "Home",
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
                        mainH1 = "Marcos's App",
                        mainH2 = "Home",
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
                if (CreateShortURL(e))
                {                    
                    var phantomJS = new PhantomJS();
                    var imgSource = resource + _appName + "/img/" + _shortURL + ".png";
                    phantomJS.Run(resource + _appName + "/" + "rasterize.js", new[] { _longURL, imgSource });
                    if (DbLogin(e, true))
                    {
                        e.Response.Headers.Add("REFRESH", loginTimeout.ToString() + ";URL=" + url + "Index");
                        e.Response.StatusCode = 200;
                        RenderMessage(e, "Success!");
                        e.Response.Redirect(url + "Index");
                    }
                    else
                    {
                        e.Response.Headers.Add("REFRESH", loginTimeout.ToString() + ";URL=" + GetAppURL(e, "Short") + "Anonymous?token=" + AnonToken);
                        e.Response.StatusCode = 200;
                        RenderMessage(e, "Success!");
                    }
                }
                else
                {
                    e.Response.Headers.Add("REFRESH", loginTimeout.ToString() + ";URL=" + url + "Index");
                    e.Response.StatusCode = 401;
                    RenderMessage(e, "Login Failed!");
                }
            }
        }

        [Authorize, HttpGet]
        public void About(HttpRequestEventArgs e)
        {
            string url = GetAppURL(e);

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
                    showNavButtons = true,
                    btn1 = "Home",
                    btn2 = "About",
                    btn3 = "Sign Out",
                    link1 = url + "Index",
                    link2 = url + "About",
                    link3 = url + "Login",
                    title = "Marcos URL Shortener",
                    mainH1 = "Marcos's App",
                    mainH2 = "About",
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

        [HttpGet, HttpPost]
        public void Login(HttpRequestEventArgs e)
        {
            if (e.Request.RequestType == "GET")
            {
                string url = GetAppURL(e);
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
                            btn3 = "Register",
                            link1 = url + "Index",
                            link2 = url + "About",
                            link3 = url + "Register",
                            title = "Marcos URL Shortener",
                            mainH1 = "Marcos's App",
                            mainH2 = "Login",
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
                LoginProcess(e);
            }
        }

        [HttpGet, HttpPost]
        public void Register(HttpRequestEventArgs e)
        {
            string url = GetAppURL(e);
            if (e.Request.RequestType == "GET")
            {
                if (DbLogin(e, true))
                {
                    e.Response.Cookies.Get(cookieName1).Expires = DateTime.Now;
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
                            btn3 = "Sign In",
                            link1 = url + "Index",
                            link2 = url + "About",
                            link3 = url + "Login",
                            title = "Marcos URL Shortener",
                            mainH1 = "Marcos's App",
                            mainH2 = "Register",
                            body = File.ReadAllText(views + "/partials/register.hbs")
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
                string confirm_password = e.Request.Form.Get("confirm_password");
                string name = e.Request.Form.Get("name");
                string lastname = e.Request.Form.Get("lastname");

                if (DbRegister(e))
                {
                    LoginProcess(e);
                }
                else
                {
                    if (password != confirm_password)
                    {
                        e.Response.Headers.Add("REFRESH", loginTimeout.ToString() + ";URL=" + url + "Register");
                        e.Response.StatusCode = 401;
                        RenderMessage(e, "Passwords do not Match!");
                    }
                    else
                    {
                        e.Response.Headers.Add("REFRESH", loginTimeout.ToString() + ";URL=" + url + "Register");
                        e.Response.StatusCode = 401;
                        RenderMessage(e, "Username exists!");
                    }
                }
            }
        }
    }
}