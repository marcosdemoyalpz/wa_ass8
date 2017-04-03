using HandlebarsDotNet;
using PHttp;
using System;
using System.Configuration;
using System.IO;
using System.Web.Http;

namespace Mvc.Controllers
{
    internal class HomeController
    {
        private MimeTypes mimeTypes = new MimeTypes();
        private string name = "Home";

        // Explicit interface members implementation:
        [HttpGet]
        public void Index(HttpRequestEventArgs e = null)
        {
            string replacePath = ConfigurationManager.AppSettings["ReplacePath"]; ;
            string userprofile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string path = ConfigurationManager.AppSettings["Virtual"];
            path = path.Replace(replacePath, userprofile);
            HttpResponse res = e.Response;
            string filePath = path + "\\Views\\layout.hbs";
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
            }
            else
            {
                var source = File.ReadAllText(path + "\\Views\\error.hbs");
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