using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PHttp
{
    public class AppInfo
    {
        string _name;
        string _applicationsDir;
        string _database;
        string _connectionString;
        string _virtualPath;
        string _layout;
        string _defaultDocument;

        public AppInfo(string name, string applicationsDir, string database, string connectionString,
            string virtualPath, string layout, string defaultDocument)
        {
            _name = name;
            _applicationsDir = applicationsDir;
            _database = database;
            _connectionString = connectionString;
            _virtualPath = virtualPath;
            _layout = layout;
            _defaultDocument = defaultDocument;

        }
        public string name
        {
            get { return _name; }
            set { _name = value; }
        }
        public string applicationsDir
        {
            get { return _applicationsDir; }
            set { _applicationsDir = value; }
        }
        public string database
        {
            get { return _database; }
            set { _database = value; }
        }
        public string connectionString
        {
            get { return _connectionString; }
            set { _connectionString = value; }
        }
        public string virtualPath
        {
            get { return _virtualPath; }
            set { _virtualPath = value; }
        }
        public string layout
        {
            get { return _layout; }
            set { _layout = value; }
        }
        public string defaultDocument
        {
            get { return _defaultDocument; }
            set { _defaultDocument = value; }
        }
    }
}
