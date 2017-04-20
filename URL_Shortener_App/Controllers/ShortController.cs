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
    internal class ShortController : Mvc.ControllerBase
    {
        string secret = ConfigurationManager.AppSettings["Secret"];
        string resource = ConfigurationManager.AppSettings["Virtual"];
        string layout = ConfigurationManager.AppSettings["Layout"];

        AppInfo _app;

        IDbConnection m_dbConnection;

        LoadConfig loadConfig = new LoadConfig();

        float loginTimeout = 1.25f;
        int expiration = 7200;

        string cookieName1 = "URL_Shortener_App_JWT";
        string cookieName2 = "AnonTemp";

        private string _appName = "URL_Shortener_App";
        private string _controllerName = "Short";
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
                    string longURL = "";

                    int clicksCount = 0;

                    // ### Connect to the database
                    m_dbConnection = new SqliteConnection(_app.connectionString);
                    m_dbConnection.Open();

                    #region Clicks
                    // ### select the data from urls to load Click Info
                    string sql = "SELECT * FROM urls";
                    IDbCommand command = m_dbConnection.CreateCommand(); command.CommandText = sql;
                    IDataReader reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        if (reader["username"].ToString().ToLower() == username.ToLower())
                        {
                            var shortUrl = reader["shortURL"].ToString();

                            if (e.Request.Params["go"] == shortUrl)
                            {
                                longURL = reader["longURL"].ToString();
                                clicksCount = (int.Parse(reader["clicks"].ToString()) + 1);
                            }
                        }
                    }
                    reader.Close();
                    #endregion

                    #region Referers
                    UpdateReferers(e, _app, username);
                    #endregion

                    #region Agents
                    UpdateAgents(e, _app, username);
                    #endregion

                    #region Platforms
                    UpdatePlatforms(e, _app, username);
                    #endregion

                    #region Locations
                    UpdateLocations(e, _app, username);
                    #endregion

                    if (longURL != "")
                    {
                        // ### Update clicks on table
                        sql = "UPDATE urls SET "
                            + "clicks = " + clicksCount
                            + ", lastClicked = DATETIME('NOW')"
                            + "WHERE shortURL = '" + e.Request.Params["go"] + "'";
                        command.CommandText = sql;
                        command.ExecuteNonQuery();
                        e.Response.Redirect(longURL);
                        return true;
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

        #region Update/Create
        bool UpdateReferers(HttpRequestEventArgs e, AppInfo _app, string username)
        {
            int referersCount = 0;
            bool refererFound = false;

            string referer = "";
            if (e.Request.ServerVariables["http_referer"] != null)
            {
                referer = e.Request.ServerVariables["http_referer"];
            }
            referer = referer.Replace(",", " ");
            // ### select the data from referers to load Referers Info
            string sql = "SELECT * FROM referers";
            IDbCommand command = m_dbConnection.CreateCommand(); command.CommandText = sql;
            IDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                if (reader["username"].ToString().ToLower() == username.ToLower())
                {
                    var shortUrl = reader["shortURL"].ToString();
                    if (e.Request.Params["go"] == shortUrl)
                    {
                        if (referer == reader["referer"].ToString())
                        {
                            refererFound = true;
                            referersCount = (int.Parse(reader["count"].ToString()) + 1);
                        }
                    }
                }
            }
            reader.Close();
            if (refererFound)
            {
                // ### Update clicks on table
                sql = "UPDATE referers SET "
                    + "count = " + referersCount
                    + " WHERE shortURL = '" + e.Request.Params["go"] + "' AND "
                    + "referer = '" + referer + "' AND "
                    + "username = '" + username + "'";
                command.CommandText = sql;
                command.ExecuteNonQuery();
                return true;
            }
            else
            {
                if (referer != "" && referer != null)
                {
                    // ### Add some data to the table
                    sql = "insert into referers "
                        + "(referer, username, shortURL, count)"
                        + " values "
                        + "('" + referer + "',"
                        + "'" + username + "',"
                        + "'" + e.Request.Params["go"] + "',"
                        + " 1 )";
                    command.CommandText = sql;
                    command.ExecuteNonQuery();
                    return true;
                }
            }
            return false;
        }
        bool UpdateAgents(HttpRequestEventArgs e, AppInfo _app, string username)
        {
            int agentsCount = 0;
            bool agentFound = false;

            // ### Connect to the database
            m_dbConnection = new SqliteConnection(_app.connectionString);
            m_dbConnection.Open();

            string agent = "";
            if (e.Request.UserAgent != null)
            {
                agent = e.Request.UserAgent;
            }
            string readUser = "";
            // ### select the data from agents to load Agents Info
            string sql = "SELECT * FROM agents";
            IDbCommand command = m_dbConnection.CreateCommand(); command.CommandText = sql;
            IDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                readUser = reader["username"].ToString();
                if (readUser.ToLower() == username.ToLower())
                {
                    var shortUrl = reader["shortURL"].ToString();
                    if (shortUrl == e.Request.Params["go"])
                    {
                        if (agent == reader["agent"].ToString())
                        {
                            agentFound = true;
                            agentsCount = (int.Parse(reader["count"].ToString()) + 1);
                        }
                    }
                }
            }
            reader.Close();
            if (agentFound)
            {
                // ### Update clicks on table
                sql = "UPDATE agents SET "
                    + "count = " + agentsCount
                    + " WHERE shortURL = '" + e.Request.Params["go"] + "' AND "
                    + "agent = '" + agent + "' AND "
                    + "username = '" + username + "'";
                command.CommandText = sql;
                command.ExecuteNonQuery();
                return true;
            }
            else
            {
                if (agent != "" && agent != null)
                {
                    var shortUrl = e.Request.Params["go"];
                    // ### Add some data to the table
                    sql = "insert into agents "
                        + "(agent, username, shortURL, count)"
                        + " values "
                        + "('" + agent + "',"
                        + "'" + username + "',"
                        + "'" + shortUrl + "',"
                        + " 1 )";
                    command.CommandText = sql;
                    command.ExecuteNonQuery();
                    return true;
                }
            }
            return false;
        }
        bool UpdateLocations(HttpRequestEventArgs e, AppInfo _app, string username)
        {
            int locationsCount = 0;
            bool locationFound = false;

            // ### Connect to the database
            m_dbConnection = new SqliteConnection(_app.connectionString);
            m_dbConnection.Open();

            string location = "";
            GeoLocation geo = new GeoLocation(e);
            if (geo.countryn != null)
            {
                location = geo.countryn;
            }
            string readUser = "";
            // ### select the data from locations to load Locations Info
            string sql = "SELECT * FROM locations";
            IDbCommand command = m_dbConnection.CreateCommand(); command.CommandText = sql;
            IDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                readUser = reader["username"].ToString();
                if (readUser.ToLower() == username.ToLower())
                {
                    var shortUrl = reader["shortURL"].ToString();
                    if (shortUrl == e.Request.Params["go"])
                    {
                        if (location == reader["location"].ToString())
                        {
                            locationFound = true;
                            locationsCount = (int.Parse(reader["count"].ToString()) + 1);
                        }
                    }
                }
            }
            reader.Close();
            if (locationFound)
            {
                // ### Update clicks on table
                sql = "UPDATE locations SET "
                    + "count = " + locationsCount
                    + " WHERE shortURL = '" + e.Request.Params["go"] + "' AND "
                    + "location = '" + location + "' AND "
                    + "username = '" + username + "'";
                command.CommandText = sql;
                command.ExecuteNonQuery();
                return true;
            }
            else
            {
                if (location != "" && location != null)
                {
                    var shortUrl = e.Request.Params["go"];
                    // ### Add some data to the table
                    sql = "insert into locations "
                        + "(location, username, shortURL, count)"
                        + " values "
                        + "('" + location + "',"
                        + "'" + username + "',"
                        + "'" + shortUrl + "',"
                        + " 1 )";
                    command.CommandText = sql;
                    command.ExecuteNonQuery();
                    return true;
                }
            }
            return false;
        }
        bool UpdatePlatforms(HttpRequestEventArgs e, AppInfo _app, string username)
        {
            int platformsCount = 0;
            bool platformFound = false;

            // ### Connect to the database
            m_dbConnection = new SqliteConnection(_app.connectionString);
            m_dbConnection.Open();

            string platform = "Others";
            string userAgent = e.Request.UserAgent;

            if (userAgent.Contains("Windows NT 10.0"))
            {
                //Windows 8.1
                platform = "Windows NT 10.0";
            }
            else if (userAgent.Contains("Windows NT 6.3"))
            {
                //Windows 8.1
                platform = "Windows NT 6.3";
            }
            else if (userAgent.Contains("Windows NT 6.2"))
            {
                //Windows 8
                platform = "Windows NT 6.2";
            }
            else if (userAgent.Contains("Windows NT 6.1"))
            {
                //Windows 7
                platform = "Windows NT 6.1";
            }
            else if (userAgent.Contains("Windows NT 6.0"))
            {
                //Windows Vista
                platform = "Windows NT 6.0";
            }
            else if (userAgent.Contains("Windows NT 5.2"))
            {
                //Windows Server 2003; Windows XP x64 Edition
                platform = "Windows NT 5.2";
            }
            else if (userAgent.Contains("Windows NT 5.1"))
            {
                //Windows XP
                platform = "Windows NT 5.1";
            }
            else if (userAgent.Contains("Windows NT 5.01"))
            {
                //Windows 2000, Service Pack 1 (SP1)
                platform = "Windows NT 5.01";
            }
            else if (userAgent.Contains("Windows NT 5.0"))
            {
                //Windows 2000
                platform = "Windows NT 5.0";
            }
            else if (userAgent.Contains("Windows NT 4.0"))
            {
                //Microsoft Windows NT 4.0
                platform = "Windows NT 4.0";
            }
            else if (userAgent.Contains("Win 9x 4.90"))
            {
                //Windows Millennium Edition (Windows Me)
                platform = "Win 9x 4.90";
            }
            else if (userAgent.Contains("Windows 98"))
            {
                //Windows 98
                platform = "Windows 98";
            }
            else if (userAgent.Contains("Windows 95"))
            {
                //Windows 95
                platform = "Windows 95";
            }
            else if (userAgent.Contains("Windows CE"))
            {
                //Windows CE
                platform = "Windows CE";
            }
            else if (userAgent.Contains("Android"))
            {
                platform = "Android";
            }
            else if (userAgent.Contains("iPad"))
            {
                platform = "iPad OS";
            }
            else if (userAgent.Contains("iPhone"))
            {
                platform = "iPhone OS";
            }
            else if (userAgent.Contains("Linux") && userAgent.Contains("KFAPWI"))
            {
                platform = "Kindle Fire";
            }
            else if (userAgent.Contains("RIM Tablet") || (userAgent.Contains("BB") && userAgent.Contains("Mobile")))
            {
                platform = "Black Berry";
            }
            else if (userAgent.Contains("Windows Phone"))
            {
                platform = "Windows Phone";
            }
            else if (userAgent.Contains("Mac OS"))
            {
                platform = "Mac OS";
            }
            string readUser = "";
            // ### select the data from platforms to load Platform Info
            string sql = "SELECT * FROM platforms";
            IDbCommand command = m_dbConnection.CreateCommand(); command.CommandText = sql;
            IDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                readUser = reader["username"].ToString();
                if (readUser.ToLower() == username.ToLower())
                {
                    var shortUrl = reader["shortURL"].ToString();
                    if (shortUrl == e.Request.Params["go"])
                    {
                        if (platform == reader["platform"].ToString())
                        {
                            platformFound = true;
                            platformsCount = (int.Parse(reader["count"].ToString()) + 1);
                        }
                    }
                }
            }
            reader.Close();
            if (platformFound)
            {
                // ### Update clicks on table
                sql = "UPDATE platforms SET "
                    + "count = " + platformsCount
                    + " WHERE shortURL = '" + e.Request.Params["go"] + "' AND "
                    + "agent = '" + platform + "' AND "
                    + "username = '" + username + "'";
                command.CommandText = sql;
                command.ExecuteNonQuery();
                return true;
            }
            else
            {
                if (platform != "" && platform != null)
                {
                    var shortUrl = e.Request.Params["go"];
                    // ### Add some data to the table
                    sql = "insert into platforms "
                        + "(platform, username, shortURL, count)"
                        + " values "
                        + "('" + platform + "',"
                        + "'" + username + "',"
                        + "'" + shortUrl + "',"
                        + " 1 )";
                    command.CommandText = sql;
                    command.ExecuteNonQuery();
                    return true;
                }
            }
            return false;
        }
        #endregion

        #region Delete
        bool DeleteShort(HttpRequestEventArgs e)
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
                    bool found = false;

                    // ### Connect to the database
                    m_dbConnection = new SqliteConnection(_app.connectionString);
                    m_dbConnection.Open();

                    // ### select the data from urls to load Click Info
                    string sql = "SELECT * FROM urls";
                    IDbCommand command = m_dbConnection.CreateCommand(); command.CommandText = sql;
                    IDataReader reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        if (reader["username"].ToString().ToLower() == username.ToLower())
                        {
                            var shortUrl = reader["shortURL"].ToString();

                            if (e.Request.Params["go"] == shortUrl)
                            {
                                found = true;
                            }
                        }
                    }
                    reader.Close();

                    if (found == true)
                    {
                        #region Referers
                        DeleteReferers(e, _app, username);
                        #endregion

                        #region Agents
                        DeleteAgents(e, _app, username);
                        #endregion

                        #region Platforms
                        DeletePlatforms(e, _app, username);
                        #endregion

                        #region Locations
                        DeleteLocations(e, _app, username);
                        #endregion

                        // ### Update clicks on table
                        sql = "DELETE FROM urls"
                            + " WHERE shortURL = '" + e.Request.Params["go"] + "'";
                        command.CommandText = sql;
                        command.ExecuteNonQuery();
                        e.Response.Redirect(GetAppURL(e, "Home") + "Index");
                        return true;
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
        bool DeleteReferers(HttpRequestEventArgs e, AppInfo _app, string username)
        {
            // ### Connect to the database
            m_dbConnection = new SqliteConnection(_app.connectionString);
            m_dbConnection.Open();
            try
            {
                // ### Delete row
                string sql = "DELETE FROM referers"
                    + " WHERE shortURL = '" + e.Request.Params["go"] + "' AND "
                    + "username = '" + username + "'";
                IDbCommand command = m_dbConnection.CreateCommand();
                command.CommandText = sql;
                command.ExecuteNonQuery();
                return true;
            }
            catch
            {
                return false;
            }
        }
        bool DeleteAgents(HttpRequestEventArgs e, AppInfo _app, string username)
        {
            // ### Connect to the database
            m_dbConnection = new SqliteConnection(_app.connectionString);
            m_dbConnection.Open();
            try
            {
                // ### Update clicks on table
                string sql = "DELETE FROM agents"
                    + " WHERE shortURL = '" + e.Request.Params["go"] + "' AND "
                    + "username = '" + username + "'";
                IDbCommand command = m_dbConnection.CreateCommand();
                command.CommandText = sql;
                command.ExecuteNonQuery();
                return true;
            }
            catch
            {
                return false;
            }
        }
        bool DeleteLocations(HttpRequestEventArgs e, AppInfo _app, string username)
        {
            // ### Connect to the database
            m_dbConnection = new SqliteConnection(_app.connectionString);
            m_dbConnection.Open();
            try
            {
                string sql = "DELETE FROM locations"
                    + " WHERE shortURL = '" + e.Request.Params["go"] + "' AND "
                    + "username = '" + username + "'";
                IDbCommand command = m_dbConnection.CreateCommand();
                command.CommandText = sql;
                command.ExecuteNonQuery();
                return true;
            }
            catch
            {
                return false;
            }
        }
        bool DeletePlatforms(HttpRequestEventArgs e, AppInfo _app, string username)
        {
            // ### Connect to the database
            m_dbConnection = new SqliteConnection(_app.connectionString);
            m_dbConnection.Open();

            try
            {
                string sql = "DELETE FROM platforms"
                    + " WHERE shortURL = '" + e.Request.Params["go"] + "' AND "
                    + "username = '" + username + "'";
                IDbCommand command = m_dbConnection.CreateCommand();
                command.CommandText = sql;
                command.ExecuteNonQuery();
                return true;
            }
            catch
            {
                return false;
            }
        }
        #endregion

        bool DbLogin(HttpRequestEventArgs e, bool fromCookie = false)
        {
            try
            {
                _app = loadConfig.InitApp(resource + _appName, "/config.json");
                HttpCookie cookie = e.Request.Cookies.Get(cookieName1);
                JArray jArray = new JArray();
                if (cookie != null && cookie.Value != "")
                {
                    string decoded = DecodeToken(cookie.Value);
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

        bool DbCheckURLDetails(HttpRequestEventArgs e)
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
                    m_dbConnection = new SqliteConnection(_app.connectionString);
                    m_dbConnection.Open();

                    // ### select the data
                    string sql = "SELECT * FROM urls";
                    IDbCommand command = m_dbConnection.CreateCommand(); command.CommandText = sql;
                    IDataReader reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        var go = e.Request.Params["go"];
                        var shortUrl = reader["shortURL"].ToString();
                        var rowUsername = reader["username"].ToString();
                        if (rowUsername.ToLower() == username.ToLower())
                        {
                            if (go == shortUrl)
                            {
                                return true;
                            }
                        }
                    }
                    reader.Close();
                    return false;
                }
            }
            catch
            {
                return false;
            }
            return false;
        }

        List<int> GetClicks(HttpRequestEventArgs e)
        {
            List<int> results = new List<int>();
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
                            var shortUrl = reader["shortURL"].ToString();
                            var longURL = reader["longURL"].ToString();

                            if (e.Request.Params["go"] == shortUrl)
                            {
                                results.Add(int.Parse(reader["clicks"].ToString()));
                            }
                        }
                    }
                    reader.Close();
                }
            }
            catch
            {
                return new List<int>();
            }
            return results;
        }

        List<string> GetReferers(HttpRequestEventArgs e)
        {
            List<string> results = new List<string>();
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
                    m_dbConnection = new SqliteConnection(_app.connectionString);
                    m_dbConnection.Open();

                    // ### select the data
                    string sql = "SELECT * FROM referers";
                    IDbCommand command = m_dbConnection.CreateCommand(); command.CommandText = sql;
                    IDataReader reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        if (reader["username"].ToString().ToLower() == username.ToLower())
                        {
                            var shortUrl = reader["shortURL"].ToString();
                            if (e.Request.Params["go"] == shortUrl)
                            {
                                string referer = reader["referer"].ToString();
                                results.Add(referer);
                            }
                        }
                    }
                    reader.Close();
                }
            }
            catch
            {
                return new List<string>();
            }
            return results;
        }
        List<int> GetReferersCount(HttpRequestEventArgs e)
        {
            List<int> results = new List<int>();
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
                    m_dbConnection = new SqliteConnection(_app.connectionString);
                    m_dbConnection.Open();

                    // ### select the data
                    string sql = "SELECT * FROM referers";
                    IDbCommand command = m_dbConnection.CreateCommand(); command.CommandText = sql;
                    IDataReader reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        if (reader["username"].ToString().ToLower() == username.ToLower())
                        {
                            var shortUrl = reader["shortURL"].ToString();

                            if (e.Request.Params["go"] == shortUrl)
                            {
                                int count = int.Parse(reader["count"].ToString());
                                results.Add(count);
                            }
                        }
                    }
                    reader.Close();
                }
            }
            catch
            {
                return new List<int>();
            }
            return results;
        }

        List<string> GetAgents(HttpRequestEventArgs e)
        {
            List<string> results = new List<string>();
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
                    m_dbConnection = new SqliteConnection(_app.connectionString);
                    m_dbConnection.Open();

                    // ### select the data
                    string sql = "SELECT * FROM agents";
                    IDbCommand command = m_dbConnection.CreateCommand(); command.CommandText = sql;
                    IDataReader reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        if (reader["username"].ToString().ToLower() == username.ToLower())
                        {
                            var shortUrl = reader["shortURL"].ToString();
                            if (e.Request.Params["go"] == shortUrl)
                            {
                                string agent = reader["agent"].ToString();
                                results.Add(agent);
                            }
                        }
                    }
                    reader.Close();
                }
            }
            catch
            {
                return new List<string>();
            }
            return results;
        }
        List<int> GetAgentsCount(HttpRequestEventArgs e)
        {
            List<int> results = new List<int>();
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
                    m_dbConnection = new SqliteConnection(_app.connectionString);
                    m_dbConnection.Open();

                    // ### select the data
                    string sql = "SELECT * FROM agents";
                    IDbCommand command = m_dbConnection.CreateCommand(); command.CommandText = sql;
                    IDataReader reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        if (reader["username"].ToString().ToLower() == username.ToLower())
                        {
                            var shortUrl = reader["shortURL"].ToString();

                            if (e.Request.Params["go"] == shortUrl)
                            {
                                int count = int.Parse(reader["count"].ToString());
                                results.Add(count);
                            }
                        }
                    }
                    reader.Close();
                }
            }
            catch
            {
                return new List<int>();
            }
            return results;
        }

        List<string> GetLocations(HttpRequestEventArgs e)
        {
            List<string> results = new List<string>();
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
                    m_dbConnection = new SqliteConnection(_app.connectionString);
                    m_dbConnection.Open();

                    // ### select the data
                    string sql = "SELECT * FROM locations";
                    IDbCommand command = m_dbConnection.CreateCommand(); command.CommandText = sql;
                    IDataReader reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        if (reader["username"].ToString().ToLower() == username.ToLower())
                        {
                            var shortUrl = reader["shortURL"].ToString();
                            if (e.Request.Params["go"] == shortUrl)
                            {
                                string location = reader["location"].ToString();
                                results.Add(location);
                            }
                        }
                    }
                    reader.Close();
                }
            }
            catch
            {
                return new List<string>();
            }
            return results;
        }
        List<int> GetLocationsCount(HttpRequestEventArgs e)
        {
            List<int> results = new List<int>();
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
                    m_dbConnection = new SqliteConnection(_app.connectionString);
                    m_dbConnection.Open();

                    // ### select the data
                    string sql = "SELECT * FROM locations";
                    IDbCommand command = m_dbConnection.CreateCommand(); command.CommandText = sql;
                    IDataReader reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        if (reader["username"].ToString().ToLower() == username.ToLower())
                        {
                            var shortUrl = reader["shortURL"].ToString();

                            if (e.Request.Params["go"] == shortUrl)
                            {
                                int count = int.Parse(reader["count"].ToString());
                                results.Add(count);
                            }
                        }
                    }
                    reader.Close();
                }
            }
            catch
            {
                return new List<int>();
            }
            return results;
        }

        List<string> GetPlatforms(HttpRequestEventArgs e)
        {
            List<string> results = new List<string>();
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
                    m_dbConnection = new SqliteConnection(_app.connectionString);
                    m_dbConnection.Open();

                    // ### select the data
                    string sql = "SELECT * FROM platforms";
                    IDbCommand command = m_dbConnection.CreateCommand(); command.CommandText = sql;
                    IDataReader reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        if (reader["username"].ToString().ToLower() == username.ToLower())
                        {
                            var shortUrl = reader["shortURL"].ToString();
                            if (shortUrl == e.Request.Params["go"])
                            {
                                string platform = reader["platform"].ToString();
                                results.Add(platform);
                            }
                        }
                    }
                    reader.Close();
                }
            }
            catch
            {
                return new List<string>();
            }
            return results;
        }
        List<int> GetPlatformsCount(HttpRequestEventArgs e)
        {
            List<int> results = new List<int>();
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
                    m_dbConnection = new SqliteConnection(_app.connectionString);
                    m_dbConnection.Open();

                    // ### select the data
                    string sql = "SELECT * FROM platforms";
                    IDbCommand command = m_dbConnection.CreateCommand(); command.CommandText = sql;
                    IDataReader reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        if (reader["username"].ToString().ToLower() == username.ToLower())
                        {
                            var shortUrl = reader["shortURL"].ToString();

                            if (e.Request.Params["go"] == shortUrl)
                            {
                                int count = int.Parse(reader["count"].ToString());
                                results.Add(count);
                            }
                        }
                    }
                    reader.Close();
                }
            }
            catch
            {
                return new List<int>();
            }
            return results;
        }

        #endregion

        #region Controller Methods
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
        [HttpGet]
        public void Delete(HttpRequestEventArgs e)
        {
            if (DeleteShort(e)) { }
            else
            {
                errorHandler.RenderErrorPage(500, e);
            }
        }
        [HttpGet]
        public void Details(HttpRequestEventArgs e)
        {
            string url = GetAppURL(e, "Home");
            if (DbCheckURLDetails(e))
            {
                string views = resource + _appName + "/Views/";
                HttpResponse res = e.Response;
                string filePath = views + "detallesURL.hbs";
                Console.WriteLine("\tStarting " + _appName + "!");
                Console.WriteLine("\tLoading file on " + filePath + "!");

                if (File.Exists(filePath) == true)
                {
                    var source = File.ReadAllText(filePath);
                    var template = Handlebars.Compile(source);
                    var data = new
                    {
                        showNavButtons = true,
                        image = GetAppURL(e, "img") + e.Request.Params["go"] + ".png",
                        delete = GetAppURL(e) + "Delete?go=" + e.Request.Params["go"],
                        clicks = String.Join(",", GetClicks(e)),
                        clicksLabels = e.Request.Params["go"],
                        referers = String.Join(",", GetReferersCount(e)),
                        referersLabels = String.Join(",", GetReferers(e)),
                        agents = String.Join(",", GetAgentsCount(e)),
                        agentsLabels = String.Join(",", GetAgents(e)),
                        locations = String.Join(",", GetLocationsCount(e)),
                        locationsLabels = String.Join(",", GetLocations(e)),
                        platforms = String.Join(",", GetPlatformsCount(e)),
                        platformsLabels = String.Join(",", GetPlatforms(e)),
                        btn1 = "Home",
                        btn2 = "About",
                        btn3 = "Sign Out",
                        link1 = url + "Index",
                        link2 = url + "About",
                        link3 = url + "Login",
                        title = "Marcos URL Shortener",
                        mainH1 = "Marcos's App",
                        mainH2 = "URL Details",
                        body = ""
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
                errorHandler.RenderErrorPage(404, e);
                Console.WriteLine("\tFile not found!");
            }
        }

        [HttpGet]
        public void Anonymous(HttpRequestEventArgs e)
        {
            string url = GetAppURL(e, "Home");
            if (DbCheckURLDetails(e))
            {
                string views = resource + _appName + "/Views/";
                HttpResponse res = e.Response;
                string filePath = views + "detallesURL.hbs";
                Console.WriteLine("\tStarting " + _appName + "!");
                Console.WriteLine("\tLoading file on " + filePath + "!");

                if (File.Exists(filePath) == true)
                {
                    var source = File.ReadAllText(filePath);
                    var template = Handlebars.Compile(source);
                    var data = new
                    {
                        showNavButtons = true,
                        clicks = String.Join(",", GetClicks(e)),
                        clicksLabels = e.Request.Params["go"],
                        referers = String.Join(",", GetReferersCount(e)),
                        referersLabels = String.Join(",", GetReferers(e)),
                        agents = String.Join(",", GetAgentsCount(e)),
                        agentsLabels = String.Join(",", GetAgents(e)),
                        locations = String.Join(",", GetLocationsCount(e)),
                        locationsLabels = String.Join(",", GetLocations(e)),
                        platforms = String.Join(",", GetPlatformsCount(e)),
                        platformsLabels = String.Join(",", GetPlatforms(e)),
                        btn1 = "Home",
                        btn2 = "About",
                        btn3 = "Sign Out",
                        link1 = url + "Index",
                        link2 = url + "About",
                        link3 = url + "Login",
                        title = "Marcos URL Shortener",
                        mainH1 = "Marcos's App",
                        mainH2 = "URL Details",
                        body = ""
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
                errorHandler.RenderErrorPage(404, e);
                Console.WriteLine("\tFile not found!");
            }
        }
        #endregion
    }
}