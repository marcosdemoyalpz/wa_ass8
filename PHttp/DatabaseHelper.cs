using System;
using System.Collections.Generic;
using System.IO;
using Mono.Data.Sqlite;
using System.Data;

namespace PHttp
{
    ////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>   A database helper. </summary>
    /// <remarks>   Marcos De Moya, 4/20/2017. </remarks>
    ////////////////////////////////////////////////////////////////////////////////////////////////////
    public class DatabaseHelper
    {
        #region Properties
        /// <summary>   List of apps. </summary>
        private List<AppInfo> _apps;
        #endregion

        #region Constructor
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Constructor. </summary>
        /// <remarks>   Marcos De Moya, 4/20/2017. </remarks>
        /// <param name="apps"> List of apps. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        public DatabaseHelper(List<AppInfo> apps)
        {
            _apps = apps;
        }
        #endregion

        #region Public Methods
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Initializes the Databases. </summary>
        /// <remarks>   Marcos De Moya, 4/20/2017. </remarks>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        public void Init()
        {
            foreach (var app in _apps)
            {
                IDbConnection m_dbConnection;
                Console.WriteLine("\tAttempting to load Database " + app.database + " ...");

                if (File.Exists(app.virtualPath + app.database))
                {
                    Console.WriteLine("\tDatabase " + app.database + " already exists!");
                }
                else
                {
                    // http://blog.tigrangasparian.com/2012/02/09/getting-started-with-sqlite-in-c-part-one/
                    // 
                    //### Create the database
                    //SQLiteConnection.CreateFile(app.virtualPath + app.database);

                    // ### Connect to the database
                    m_dbConnection = new SqliteConnection(app.connectionString);
                    m_dbConnection.Open();

                    // ### Create users table
                    string sql = "CREATE TABLE users (username VARCHAR(128) PRIMARY KEY UNIQUE, password VARCHAR(128), name VARCHAR(128), lastname VARCHAR(128), token VARCHAR(256) NULL)";
                    IDbCommand command = m_dbConnection.CreateCommand(); command.CommandText = sql;
                    command.ExecuteNonQuery();

                    // ### Add some data to the table
                    sql = "insert into users (username, password, name, lastname) values ('admin', '1234', 'Marcos', 'De Moya')";
                    command.CommandText = sql;
                    command.ExecuteNonQuery();

                    if (app.database == "URL_Shortener_App_DB.sqlite")
                    {
                        // ### Create urls table
                        sql = "CREATE TABLE urls (shortURL VARCHAR(256) PRIMARY KEY UNIQUE, longURL VARCHAR(256), username VARCHAR(128), dateCreated DATETIME, clicks INT, lastClicked DATETIME,"
                            + "FOREIGN KEY(username) REFERENCES users(username))";
                        command.CommandText = sql;
                        command.ExecuteNonQuery();

                        // ### Create referers table
                        sql = "CREATE TABLE referers (referer VARCHAR(256), username VARCHAR(128), shortURL VARCHAR(256), count INT,"
                            + "FOREIGN KEY(username) REFERENCES users(username), FOREIGN KEY(shortURL) REFERENCES urls(shortURL))";
                        command.CommandText = sql;
                        command.ExecuteNonQuery();

                        // ### Create agents table
                        sql = "CREATE TABLE agents (agent VARCHAR(256), username VARCHAR(128), shortURL VARCHAR(256), count INT,"
                            + "FOREIGN KEY(username) REFERENCES users(username), FOREIGN KEY(shortURL) REFERENCES urls(shortURL))";
                        command.CommandText = sql;
                        command.ExecuteNonQuery();

                        // ### Create locations table
                        sql = "CREATE TABLE locations (location VARCHAR(256), username VARCHAR(128), shortURL VARCHAR(256), count INT,"
                            + "FOREIGN KEY(username) REFERENCES users(username), FOREIGN KEY(shortURL) REFERENCES urls(shortURL))";
                        command.CommandText = sql;
                        command.ExecuteNonQuery();

                        // ### Create platforms table
                        sql = "CREATE TABLE platforms (platform VARCHAR(256), username VARCHAR(128), shortURL VARCHAR(256), count INT,"
                            + "FOREIGN KEY(username) REFERENCES users(username), FOREIGN KEY(shortURL) REFERENCES urls(shortURL))";
                        command.CommandText = sql;
                        command.ExecuteNonQuery();

                        // ### Add some data to the table
                        sql = "insert into urls (shortURL, longURL, username, dateCreated, clicks, lastClicked) values ('hello', 'https://www.google.com', 'admin', DATETIME('NOW'), 0, DATETIME('0') )";
                        command.CommandText = sql;
                        command.ExecuteNonQuery();

                        // ### select the data
                        sql = "select * from urls order by username desc";
                        command.CommandText = sql;
                        IDataReader reader = command.ExecuteReader();
                        while (reader.Read())
                        {
                            string shortURL = reader["shortURL"].ToString();
                            string longURL = reader["longURL"].ToString();
                            string username = reader["username"].ToString();
                            int clicks = int.Parse(reader["clicks"].ToString());
                            string lastClicked = reader["lastClicked"].ToString();
                            Console.WriteLine("\n\tUsername: " + username + "\n\tShort URL: " + shortURL + "\n\tLong URL: " + longURL + "\n\tClicks: " + clicks + "\n\tLast Clicked: " + lastClicked + "\n");
                        }
                        reader.Close();

                    }
                    Console.WriteLine("\tDatabase " + app.database + " has been created!");
                }
            }
        }
        #endregion
    }
}
