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

        public AppInfo(string name, string applicationsDir)
        {
            _name = name;
            _applicationsDir = applicationsDir;
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
    }
}
