using System;
using Mvc;
using System.Collections.Generic;
using PHttp;

namespace App1
{
    public class Application : IPHttpApplication
    {
        private string name = "App1";
        Mvc.ErrorHandler errorHandler = new Mvc.ErrorHandler();

        // Explicit interface members implementation:
        void IPHttpApplication.Start()
        {
            try
            {
                Console.WriteLine("\tStarting " + name + "!");
            }
            catch (Exception)
            {

            }
        }

        void IPHttpApplication.ExecuteAction(HttpRequestEventArgs e, string applicationsDir)
        {
            Console.WriteLine("\tExecute Action");
            try
            {
                Router router = new Router(name);
                router.CallAction(e, applicationsDir);
            }
            catch
            {
                errorHandler.RenderErrorPage(404, e);
            }
        }

        string IPHttpApplication.Name
        {
            set
            {
                name = value;
            }
            get
            {
                return name;
            }
        }

        public event PreApplicationStartMethod PreApplicationStart;

        public event ApplicationStartMethod ApplicationStart;

    }
}
