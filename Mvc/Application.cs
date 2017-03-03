using PHttp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mvc
{
    public class Application : IPHttpApplication
    {
        private string name = "App1";

        // Explicit interface members implementation: 
        void IPHttpApplication.Start()
        {
            Console.WriteLine("\tStarting " + name + "!");
        }

        void IPHttpApplication.ExecuteAction()
        {
            Console.WriteLine("\tExecute Action");
        }

        string IPHttpApplication.Name
        {
            set
            {
                name = value;
            }
            get
            {
                //Console.WriteLine($"name = " + name);
                return name;
            }
        }

        public event PreApplicationStartMethod PreApplicationStart;
        public event ApplicationStartMethod ApplicationStart;
    }
}
