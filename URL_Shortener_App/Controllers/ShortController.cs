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
    ////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>   A controller for handling shortURLs. </summary>
    /// <remarks>   Marcos De Moya, 4/20/2017. </remarks>
    ////////////////////////////////////////////////////////////////////////////////////////////////////
    internal class ShortController : Mvc.ControllerBase
    {
        #region Properties
        /// <summary> The application. </summary>
        AppInfo _app;

        /// <summary>   The database connection. </summary>
        IDbConnection m_dbConnection;

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets the application settings[" secret"]. </summary>
        /// <value> The application settings[" secret"]. </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        string secret = ConfigurationManager.AppSettings["Secret"];

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets the application settings[" virtual"]. </summary>
        /// <value> The application settings[" virtual"]. </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        string resource = ConfigurationManager.AppSettings["Virtual"];

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets the application settings[" layout"]. </summary>
        /// <value> The application settings[" layout"]. </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        string layout = ConfigurationManager.AppSettings["Layout"];

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Loads the configuration. </summary>
        /// <remarks>   Marcos De Moya, 4/20/2017. </remarks>
        /// <returns>   The configuration. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        LoadConfig loadConfig = new LoadConfig();

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets the Login Timeout. </summary>
        /// <value> The loginTimeout. </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        float loginTimeout = 1.25f;

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets the Cookie expiration. </summary>
        /// <value> expiration </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        int expiration = 7200;

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets the "url shortener application jwt cookie name". </summary>
        /// <value> The "url shortener application jwt cookie name". </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        string cookieName1 = "URL_Shortener_App_JWT";

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets the anon temp cookie name". </summary>
        /// <value> The " anon temp". </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        string cookieName2 = "AnonTemp";

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets the App name. </summary>
        /// <value> _appName </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        private string _appName = "URL_Shortener_App";

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets the Controller Name". </summary>
        /// <value> The " short". </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        private string _controllerName = "Short";

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Handler, called when the error. </summary>
        /// <remarks>   Marcos De Moya, 4/20/2017. </remarks>
        /// <returns>   An errorHandler. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        ErrorHandler errorHandler = new ErrorHandler();
        #endregion

        #region Private Methods

        #region Misc. Methods
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets JArray. </summary>
        /// <remarks>   Marcos De Moya, 4/20/2017. </remarks>
        /// <param name="jsonString">   The JSON string. </param>
        /// <returns>   The JArray. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
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

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Decode token. </summary>
        /// <remarks>   Marcos De Moya, 4/20/2017. </remarks>
        /// <param name="token">    The token. </param>
        /// <returns>   A string. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
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

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets application URL. </summary>
        /// <remarks>   Marcos De Moya, 4/20/2017. </remarks>
        /// <param name="e">                HTTP request event information. </param>
        /// <param name="controllerName">   (Optional) Sets the Controller Name". </param>
        /// <returns>   The application URL. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
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

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Redirect short. </summary>
        /// <remarks>   Marcos De Moya, 4/20/2017. </remarks>
        /// <exception cref="ArgumentNullException">    Thrown when one or more required arguments are
        ///                                             null. </exception>
        /// <param name="e">    HTTP request event information. </param>
        /// <returns>   True if it succeeds, false if it fails. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
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
                    string shortURL = "";

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
                        Console.WriteLine("\n"
                        + "\n\t username = " + reader["username"].ToString()
                        + "\n\t shortURL = " + reader["shortURL"].ToString()
                        + "\n\t longURL = " + reader["longURL"].ToString()
                        + "\n");
                        if (reader["username"].ToString().ToLower() == username.ToLower())
                        {
                            shortURL = reader["shortURL"].ToString();

                            if (e.Request.Params["go"] == shortURL)
                            {
                                longURL = reader["longURL"].ToString();
                                shortURL = reader["shortURL"].ToString();
                                clicksCount = (int.Parse(reader["clicks"].ToString()) + 1);
                                break;
                            }
                        }
                    }
                    reader.Close();
                    #endregion

                    #region Referers
                    try
                    {
                        UpdateReferers(e, _app, username);
                    }
                    catch
                    {
                        Console.WriteLine("\n\t Referer not found!");
                    }
                    #endregion

                    #region Agents
                    try
                    {
                        UpdateAgents(e, _app, username);
                    }
                    catch
                    {
                        Console.WriteLine("\n\t Agent not found!");
                    }
                    #endregion

                    #region Platforms
                    try
                    {
                        UpdatePlatforms(e, _app, username);
                    }
                    catch
                    {
                        Console.WriteLine("\n\t Platform not found!");
                    }
                    #endregion

                    #region Locations
                    try
                    {
                        UpdateLocations(e, _app, username);
                    }
                    catch
                    {
                        Console.WriteLine("\n\t Location not found!");
                    }
                    #endregion

                    Console.WriteLine("\n\t Redirecting " + shortURL + " ...");
                    if (longURL != "")
                    {
                        // ### Update clicks on table
                        sql = "UPDATE urls SET "
                            + "clicks = " + clicksCount
                            + ", lastClicked = DATETIME('NOW') "
                            + "WHERE shortURL = '" + shortURL + "'";
                        command.CommandText = sql;
                        command.ExecuteNonQuery();
                        e.Response.Redirect(longURL);
                        Console.WriteLine("\n\t Redirected to " + longURL + "\n");
                        return true;
                    }
                    Console.WriteLine("\n\t Failed to redirect " + shortURL + "\n");
                    return false;
                }
                else
                {
                    if (e.Request.Params["go"] != null)
                    {
                        string decoded = DecodeToken(e.Request.Params["go"]);
                        JArray jArray = GetJArray(decoded);

                        if (jArray == null)
                        {
                            throw new ArgumentNullException();
                        }

                        string shortURL = jArray[0].SelectToken("shortURL").ToString();
                        string longURL = jArray[0].SelectToken("longURL").ToString();
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
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Database login. </summary>
        /// <remarks>   Marcos De Moya, 4/20/2017. </remarks>
        /// <param name="e">            HTTP request event information. </param>
        /// <param name="fromCookie">   (Optional) True to from cookie. </param>
        /// <returns>   True if it succeeds, false if it fails. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
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

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Renders a message. </summary>
        /// <remarks>   Marcos De Moya, 4/20/2017. </remarks>
        /// <param name="e">        HTTP request event information. </param>
        /// <param name="message">  The message. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
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

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Database check URL details. </summary>
        /// <remarks>   Marcos De Moya, 4/20/2017. </remarks>
        /// <param name="e">    HTTP request event information. </param>
        /// <returns>   True if it succeeds, false if it fails. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
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

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets longURL. </summary>
        /// <remarks>   Marcos De Moya, 4/20/2017. </remarks>
        /// <param name="e">    HTTP request event information. </param>
        /// <returns>   The long URL. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        string GetLongURL(HttpRequestEventArgs e)
        {
            try
            {
                string longURL = "";
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
                            }
                        }
                    }
                }
                return longURL;
            }
            catch
            {
                return "";
            }
        }
        #endregion

        #region Update/Create

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Updates the referers. </summary>
        ///
        /// <remarks>   Marcos De Moya, 4/20/2017. </remarks>
        ///
        /// <param name="e">        HTTP request event information. </param>
        /// <param name="_app">     The application. </param>
        /// <param name="username"> The username. </param>
        ///
        /// <returns>   True if it succeeds, false if it fails. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        bool UpdateReferers(HttpRequestEventArgs e, AppInfo _app, string username)
        {
            int referersCount = 0;
            bool refererFound = false;

            string referer = "";
            string shortURL = "";
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
                Console.WriteLine("\n"
                       + "\n\t username = " + reader["username"].ToString()
                       + "\n\t shortURL = " + reader["shortURL"].ToString()
                       + "\n\t referer = " + reader["referer"].ToString()
                       + "\n");
                if (reader["username"].ToString().ToLower() == username.ToLower())
                {
                    shortURL = reader["shortURL"].ToString();
                    if (e.Request.Params["go"] == shortURL)
                    {
                        if (referer == reader["referer"].ToString())
                        {
                            refererFound = true;
                            referersCount = (int.Parse(reader["count"].ToString()) + 1);
                            Console.WriteLine("\n\t Referer Found!");
                            break;
                        }
                    }
                }
            }
            reader.Close();
            if (refererFound == true && shortURL != "")
            {
                // ### Update clicks on table
                sql = "UPDATE referers SET "
                    + "count = " + referersCount
                    + " WHERE shortURL = '" + shortURL + "' AND "
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
            Console.WriteLine("\n\t Failed to insert referer!");
            return false;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Updates the agents. </summary>
        ///
        /// <remarks>   Marcos De Moya, 4/20/2017. </remarks>
        ///
        /// <param name="e">        HTTP request event information. </param>
        /// <param name="_app">     The application. </param>
        /// <param name="username"> The username. </param>
        ///
        /// <returns>   True if it succeeds, false if it fails. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        bool UpdateAgents(HttpRequestEventArgs e, AppInfo _app, string username)
        {
            int agentsCount = 0;
            bool agentFound = false;

            // ### Connect to the database
            m_dbConnection = new SqliteConnection(_app.connectionString);
            m_dbConnection.Open();

            string agent = "";
            string shortURL = "";
            UserAgentHelper agentHelper = new UserAgentHelper(e);
            if (agentHelper.agent_name != null)
            {
                agent = agentHelper.agent_name;
            }
            string readUser = "";
            // ### select the data from agents to load Agents Info
            string sql = "SELECT * FROM agents";
            IDbCommand command = m_dbConnection.CreateCommand(); command.CommandText = sql;
            IDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                Console.WriteLine("\n"
                        + "\n\t username = " + reader["username"].ToString()
                        + "\n\t shortURL = " + reader["shortURL"].ToString()
                        + "\n\t agent = " + reader["agent"].ToString()
                        + "\n");
                readUser = reader["username"].ToString();
                if (readUser.ToLower() == username.ToLower())
                {
                    shortURL = reader["shortURL"].ToString();
                    if (shortURL == e.Request.Params["go"])
                    {
                        if (agent == reader["agent"].ToString())
                        {
                            agentFound = true;
                            agentsCount = (int.Parse(reader["count"].ToString()) + 1);
                            Console.WriteLine("\n\t Agent Found!");
                            break;
                        }
                    }
                }
            }
            reader.Close();
            if (agentFound == true && shortURL != "")
            {
                // ### Update clicks on table
                sql = "UPDATE agents SET "
                    + "count = " + agentsCount
                    + " WHERE shortURL = '" + shortURL + "' AND "
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
            Console.WriteLine("\n\t Failed to insert agent!");
            return false;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Updates the locations. </summary>
        ///
        /// <remarks>   Marcos De Moya, 4/20/2017. </remarks>
        ///
        /// <param name="e">        HTTP request event information. </param>
        /// <param name="_app">     The application. </param>
        /// <param name="username"> The username. </param>
        ///
        /// <returns>   True if it succeeds, false if it fails. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        bool UpdateLocations(HttpRequestEventArgs e, AppInfo _app, string username)
        {
            int locationsCount = 0;
            bool locationFound = false;

            // ### Connect to the database
            m_dbConnection = new SqliteConnection(_app.connectionString);
            m_dbConnection.Open();

            string location = "";
            string shortURL = "";
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
                Console.WriteLine("\n"
                        + "\n\t username = " + reader["username"].ToString()
                        + "\n\t shortURL = " + reader["shortURL"].ToString()
                        + "\n\t location = " + reader["location"].ToString()
                        + "\n");
                readUser = reader["username"].ToString();
                if (readUser.ToLower() == username.ToLower())
                {
                    shortURL = reader["shortURL"].ToString();
                    if (shortURL == e.Request.Params["go"])
                    {
                        if (location == reader["location"].ToString())
                        {
                            locationFound = true;
                            locationsCount = (int.Parse(reader["count"].ToString()) + 1);
                            Console.WriteLine("\n\t Location Found!");
                            break;
                        }
                    }
                }
            }
            reader.Close();
            if (locationFound == true && shortURL != "")
            {
                // ### Update clicks on table
                sql = "UPDATE locations SET "
                    + "count = " + locationsCount
                    + " WHERE shortURL = '" + shortURL + "' AND "
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
            Console.WriteLine("\n\t Failed to insert location!");
            return false;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Updates the platforms. </summary>
        ///
        /// <remarks>   Marcos De Moya, 4/20/2017. </remarks>
        ///
        /// <param name="e">        HTTP request event information. </param>
        /// <param name="_app">     The application. </param>
        /// <param name="username"> The username. </param>
        ///
        /// <returns>   True if it succeeds, false if it fails. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        bool UpdatePlatforms(HttpRequestEventArgs e, AppInfo _app, string username)
        {
            int platformsCount = 0;
            bool platformFound = false;

            // ### Connect to the database
            m_dbConnection = new SqliteConnection(_app.connectionString);
            m_dbConnection.Open();

            string platform = "Others";
            string shortURL = "";

            UserAgentHelper agentHelper = new UserAgentHelper(e);
            if (agentHelper.os_name != null)
            {
                platform = agentHelper.os_name;
            }
            string readUser = "";
            // ### select the data from platforms to load Platform Info
            string sql = "SELECT * FROM platforms";
            IDbCommand command = m_dbConnection.CreateCommand(); command.CommandText = sql;
            IDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                Console.WriteLine("\n"
                         + "\n\t username = " + reader["username"].ToString()
                         + "\n\t shortURL = " + reader["shortURL"].ToString()
                         + "\n\t platform = " + reader["platform"].ToString()
                         + "\n");
                readUser = reader["username"].ToString();
                if (readUser.ToLower() == username.ToLower())
                {
                    shortURL = reader["shortURL"].ToString();
                    if (shortURL == e.Request.Params["go"])
                    {
                        if (platform == reader["platform"].ToString())
                        {
                            platformFound = true;
                            platformsCount = (int.Parse(reader["count"].ToString()) + 1);
                            Console.WriteLine("\n\t Platform Found!");
                            break;
                        }
                    }
                }
            }
            reader.Close();
            if (platformFound && shortURL != "")
            {
                // ### Update clicks on table
                sql = "UPDATE platforms SET "
                    + "count = " + platformsCount
                    + " WHERE shortURL = '" + shortURL + "' AND "
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
            Console.WriteLine("\n\t Failed to insert platform!");
            return false;
        }
        #endregion

        #region Delete
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Deletes the shortURL described by e. </summary>
        ///
        /// <remarks>   Marcos De Moya, 4/20/2017. </remarks>
        ///
        /// <param name="e">    HTTP request event information. </param>
        ///
        /// <returns>   True if it succeeds, false if it fails. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
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

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Deletes the referers. </summary>
        ///
        /// <remarks>   Marcos De Moya, 4/20/2017. </remarks>
        ///
        /// <param name="e">        HTTP request event information. </param>
        /// <param name="_app">     The application. </param>
        /// <param name="username"> The username. </param>
        ///
        /// <returns>   True if it succeeds, false if it fails. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
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

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Deletes the agents. </summary>
        ///
        /// <remarks>   Marcos De Moya, 4/20/2017. </remarks>
        ///
        /// <param name="e">        HTTP request event information. </param>
        /// <param name="_app">     The application. </param>
        /// <param name="username"> The username. </param>
        ///
        /// <returns>   True if it succeeds, false if it fails. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
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

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Deletes the locations. </summary>
        ///
        /// <remarks>   Marcos De Moya, 4/20/2017. </remarks>
        ///
        /// <param name="e">        HTTP request event information. </param>
        /// <param name="_app">     The application. </param>
        /// <param name="username"> The username. </param>
        ///
        /// <returns>   True if it succeeds, false if it fails. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
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

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Deletes the platforms. </summary>
        ///
        /// <remarks>   Marcos De Moya, 4/20/2017. </remarks>
        ///
        /// <param name="e">        HTTP request event information. </param>
        /// <param name="_app">     The application. </param>
        /// <param name="username"> The username. </param>
        ///
        /// <returns>   True if it succeeds, false if it fails. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
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

        #region Charts
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Creates anon table. </summary>
        ///
        /// <remarks>   Marcos De Moya, 4/20/2017. </remarks>
        ///
        /// <exception cref="ArgumentNullException">    Thrown when one or more required arguments are
        ///                                             null. </exception>
        ///
        /// <param name="e">    HTTP request event information. </param>
        ///
        /// <returns>   The new anon table. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        string CreateAnonTable(HttpRequestEventArgs e)
        {
            string url = GetAppURL(e, "Short") + "Path";
            string table = "";
            try
            {
                if (e.Request.Params["token"] != null)
                {
                    var token = e.Request.Params["token"];
                    string decoded = DecodeToken(token);
                    JArray jArray = GetJArray(decoded);

                    if (jArray == null)
                    {
                        throw new ArgumentNullException();
                    }

                    string shortURL = jArray[0].SelectToken("shortURL").ToString();
                    string longURL = jArray[0].SelectToken("longURL").ToString();

                    table = "<br><div class=\"table-responsive\">"
                        + "<table class=\"table table-bordered\" style=\"vertical-align: middle;\">";
                    table = table + "<thead>" + "<tr>"
                        + "<th style=\"text-align: center;\">" + "Image" + "</th>"
                        + "<th style=\"text-align: center;\">" + "Short URL" + "</th>"
                        + "<th style=\"text-align: center;\">" + "Long URL" + "</th>"
                        + "</tr>" + "</thead>"
                        + "<tbody>";

                    var trOpen = "<tr style=\"vertical-align: middle;\" align=\"center\">";
                    var trClose = "</tr>";
                    var tdOpen = "<td style=\"vertical-align: middle;\" align=\"center\">";
                    var tdClose = "</td>";

                    string img = "<img src=\"";
                    var shortUrl_link = url + "?go=" + e.Request.Params["token"];
                    var link1 = "<a href=\"" + shortUrl_link + "\">" + "go=" + shortURL + "</a>";
                    var link2 = "<a href=\"" + longURL + "\">" + longURL + "</a>";

                    string imgURL = "http://api.screenshotmachine.com/?key=35f0a9&url=" + longURL;
                    img = img + imgURL + "\" class=\"portrait\"" + " alt=\"URL Image\" + "
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
                        trClose;

                    table = table + "</tbody></table></div>";
                    return table;
                }
            }
            catch (Exception ex)
            {
                table = "";
                return ex.ToString();
            }
            table = "<br><div class=\"table-responsive\">"
                + "<table class=\"table table-bordered\" style=\"vertical-align: middle;\">";
            table = table + "<thead><tr><th>Short URL</th><th>Long URL</th></tr></thead><tbody>";
            table = table + "</tbody></table></div>";
            return table;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets the clicks. </summary>
        ///
        /// <remarks>   Marcos De Moya, 4/20/2017. </remarks>
        ///
        /// <param name="e">    HTTP request event information. </param>
        ///
        /// <returns>   The clicks. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
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

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets the referers. </summary>
        ///
        /// <remarks>   Marcos De Moya, 4/20/2017. </remarks>
        ///
        /// <param name="e">    HTTP request event information. </param>
        ///
        /// <returns>   The referers. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
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

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets referers count. </summary>
        ///
        /// <remarks>   Marcos De Moya, 4/20/2017. </remarks>
        ///
        /// <param name="e">    HTTP request event information. </param>
        ///
        /// <returns>   The referers count. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
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

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets the agents. </summary>
        ///
        /// <remarks>   Marcos De Moya, 4/20/2017. </remarks>
        ///
        /// <param name="e">    HTTP request event information. </param>
        ///
        /// <returns>   The agents. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
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

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets agents count. </summary>
        ///
        /// <remarks>   Marcos De Moya, 4/20/2017. </remarks>
        ///
        /// <param name="e">    HTTP request event information. </param>
        ///
        /// <returns>   The agents count. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
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

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets the locations. </summary>
        ///
        /// <remarks>   Marcos De Moya, 4/20/2017. </remarks>
        ///
        /// <param name="e">    HTTP request event information. </param>
        ///
        /// <returns>   The locations. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
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

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets locations count. </summary>
        ///
        /// <remarks>   Marcos De Moya, 4/20/2017. </remarks>
        ///
        /// <param name="e">    HTTP request event information. </param>
        ///
        /// <returns>   The locations count. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
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

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets the platforms. </summary>
        ///
        /// <remarks>   Marcos De Moya, 4/20/2017. </remarks>
        ///
        /// <param name="e">    HTTP request event information. </param>
        ///
        /// <returns>   The platforms. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
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

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets platforms count. </summary>
        ///
        /// <remarks>   Marcos De Moya, 4/20/2017. </remarks>
        ///
        /// <param name="e">    HTTP request event information. </param>
        ///
        /// <returns>   The platforms count. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
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

        #endregion

        #region Controller Methods
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   (An Action that handles HTTP GET requests)
        ///             Redirects to the longURL of the the given e. </summary>
        ///
        /// <remarks>   Marcos De Moya, 4/20/2017. </remarks>
        ///
        /// <param name="e">    HTTP request event information. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
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

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   (An Action that handles HTTP GET requests) deletes the given shortURL. </summary>
        ///
        /// <remarks>   Marcos De Moya, 4/20/2017. </remarks>
        ///
        /// <param name="e">    The e to delete. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        [HttpGet]
        public void Delete(HttpRequestEventArgs e)
        {
            if (DeleteShort(e)) { }
            else
            {
                errorHandler.RenderErrorPage(500, e);
            }
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   (An Action that handles HTTP GET requests) details the given shortURL. </summary>
        ///
        /// <remarks>   Marcos De Moya, 4/20/2017. </remarks>
        ///
        /// <param name="e">    HTTP request event information. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
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
                    //string imgURL = GetAppURL(e, "img") + e.Request.Params["go"] + ".png";
                    string imgURL = "http://api.screenshotmachine.com/?key=35f0a9&dimension=640x480&url=" + GetLongURL(e);
                    var source = File.ReadAllText(filePath);
                    var template = Handlebars.Compile(source);
                    var data = new
                    {
                        showNavButtons = true,
                        image = imgURL,
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
                        mainH1 = "Marcos' App",
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

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   (An Action that handles HTTP GET requests)
        ///             Created Anonymous shortURL page. </summary>
        ///
        /// <remarks>   Marcos De Moya, 4/20/2017. </remarks>
        ///
        /// <param name="e">    HTTP request event information. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        [HttpGet]
        public void Anonymous(HttpRequestEventArgs e)
        {
            string url = GetAppURL(e, "Home");
            if (DbLogin(e, true) == false)
            {
                string views = resource + _appName + "/Views/";
                HttpResponse res = e.Response;
                string filePath = views + "Main.hbs";
                Console.WriteLine("\tStarting " + _appName + "!");
                Console.WriteLine("\tLoading file on " + filePath + "!");

                if (File.Exists(filePath) == true)
                {
                    var source = File.ReadAllText(filePath);
                    var template = Handlebars.Compile(source);
                    var table = CreateAnonTable(e);
                    var data = new
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
                        mainH1 = "Marcos' App",
                        mainH2 = "Home",
                        body = File.ReadAllText(views + "/partials/captcha.hbs") + table
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