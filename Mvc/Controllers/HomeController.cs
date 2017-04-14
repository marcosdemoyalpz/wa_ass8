using HandlebarsDotNet;
using PHttp;
using PHttp.Application;
using System;
using System.Configuration;
using System.IO;
using System.Web.Http;

namespace Mvc.Controllers
{
    internal class HomeController : ControllerBase
    {
        private MimeTypes mimeTypes = new MimeTypes();
        private string name = "Home";

        [HttpGet]
        public void Index(HttpRequestEventArgs e = null)
        {
            string replacePath = ConfigurationManager.AppSettings["ReplacePath"]; ;
            string userprofile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string views = ConfigurationManager.AppSettings["Views"];
            views = views.Replace(replacePath, userprofile);
            HttpResponse res = e.Response;
            string filePath = views + ConfigurationManager.AppSettings["Layout"];
            Console.WriteLine("\tStarting " + name + "!");
            Console.WriteLine("\tLoading file on " + filePath + "!");

            if (File.Exists(filePath) == true)
            {
                var source = File.ReadAllText(filePath);
                var template = Handlebars.Compile(source);
                var data = new
                {
                    title = "Default Web Site",
                    mainH1 = "Home",
                    body = "Here is some text!"
                };
                var result = template(data);
                using (var writer = new StreamWriter(e.Response.OutputStream))
                {
                    writer.Write(result);
                }
                Console.WriteLine("\n\tErrorTemplate = " + ConfigurationManager.AppSettings["ErrorTemplate"]);
                Console.WriteLine("\tconnectionString = " + ConfigurationManager.AppSettings["connectionString"]);
            }
            else
            {
                var source = File.ReadAllText(views + ConfigurationManager.AppSettings["ErrorTemplate"]);
                var template = Handlebars.Compile(source);
                var data = new
                {
                    title = "404 Not Found",
                    mainH1 = "Oops!",
                    mainH2 = "404 Not Found",
                    errorDetails = "Sorry, an error has occured, Requested page not found!",
                    homeAddress = "/"
                };
                var result = template(data);
                using (var writer = new StreamWriter(e.Response.OutputStream))
                {
                    writer.Write(result);
                }
                Console.WriteLine("\tFile not found!");
            }
        }
    }
}