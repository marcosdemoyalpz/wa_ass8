using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PHttp
{
    public class DatabaseHelper
    {
        private List<AppInfo> _apps;
        public DatabaseHelper(List<AppInfo> apps)
        {
            _apps = apps;
        }

        public void Init()
        {
            foreach (var app in _apps)
            {
                SQLiteConnection m_dbConnection;
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
                    SQLiteConnection.CreateFile(app.virtualPath + app.database);

                    // ### Connect to the database
                    m_dbConnection = new SQLiteConnection(app.connectionString);
                    m_dbConnection.Open();

                    // ### Create users table
                    string sql = "CREATE TABLE users (username VARCHAR(128) PRIMARY KEY UNIQUE, password VARCHAR(128), name VARCHAR(128), lastname VARCHAR(128), token VARCHAR(256) NULL)";
                    SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
                    command.ExecuteNonQuery();

                    // ### Add some data to the table
                    sql = "insert into users (username, password, name, lastname) values ('admin', '1234', 'Marcos', 'De Moya')";
                    command = new SQLiteCommand(sql, m_dbConnection);
                    command.ExecuteNonQuery();

                    if (app.database == "URL_Shortener_App_DB.sqlite")
                    {
                        // ### Create users table
                        sql = "CREATE TABLE urls (shortURL VARCHAR(128) PRIMARY KEY UNIQUE, longURL VARCHAR(128), username VARCHAR(128), FOREIGN KEY(username) REFERENCES users(username))";
                        command = new SQLiteCommand(sql, m_dbConnection);
                        command.ExecuteNonQuery();

                        // ### Add some data to the table
                        sql = "insert into urls (shortURL, longURL, username) values ('hello', 'https://www.google.com', 'admin')";
                        command = new SQLiteCommand(sql, m_dbConnection);
                        command.ExecuteNonQuery();
                    }

                    // ### select the data
                    sql = "select * from users order by username desc";
                    command = new SQLiteCommand(sql, m_dbConnection);
                    SQLiteDataReader reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        Console.WriteLine("\tUsername: " + reader["username"] + "\tPassword: " + reader["password"]);
                    }
                    Console.WriteLine("\tDatabase " + app.database + " has been created!");
                }
            }
        }
    }
}
