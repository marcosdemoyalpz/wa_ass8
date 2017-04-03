using Mvc.Controllers;
using PHttp;
using PHttp.Application;
using System;
using System.Reflection;

namespace Mvc
{
    public class Application : IPHttpApplication
    {
        private string name = "App1";
        private ControllerBase controller = new ControllerBase();

        // Explicit interface members implementation:
        void IPHttpApplication.Start(string path = null, HttpRequestEventArgs e = null)
        {
            Console.WriteLine("\tStarting " + name + "!");
            controller.Context = e.Context;
            controller.Request = e.Request;
            controller.Route = e.Request.Path;
            controller.ControllerName = path.Split('?')[0].Split('/')[1];
            controller.ActionName = path.Split('?')[0].Split('/')[2];
            controller.PrintControllerInfo();
        }

        void IPHttpApplication.ExecuteAction(HttpRequestEventArgs e = null)
        {
            Console.WriteLine("\tExecute Action");
            try
            {
                HomeController homeController = new HomeController();
                typeof(HomeController).GetMethod(controller.ActionName).Invoke(homeController, new[] { e });
            }
            catch
            {
                ErrorHandler errorHandler = new ErrorHandler();
                errorHandler.RenderErrorPage(404, e);
            }
            //homeController.Index(e);
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