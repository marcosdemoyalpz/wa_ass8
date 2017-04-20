using HandlebarsDotNet;
using PHttp;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;

namespace PHttp
{
    public class ErrorHandler
    {
        #region Properties
        string _resources = ConfigurationManager.AppSettings["Virtual"];
        string errorTemplate = ConfigurationManager.AppSettings["ErrorTemplate"];
        List<ErrorPage> _errorPages;

        class ErrorPage
        {
            public int StatusCode { get; set; }
            public string Title { get; set; }
            public string MainH1 { get; set; }
            public string MainH2 { get; set; }
            public string ErrorDetails { get; set; }
        }
        #endregion

        #region Constructor
        public ErrorHandler()
        {
            _errorPages = new List<ErrorPage>();
        }
        public ErrorHandler(string resources)
        {
            _resources = resources;
            _errorPages = new List<ErrorPage>();
        }
        #endregion

        #region Methods
        public void RenderErrorPage(int errorCode, HttpRequestEventArgs e, string message = "")
        {
            string resources = ConfigurationManager.AppSettings["Virtual"];
            object data = new object();
            switch (errorCode)
            {
                case 401:
                    data = new
                    {
                        title = "401 Unauthorized",
                        mainH1 = "Oops!",
                        mainH2 = "401 Unauthorized",
                        errorDetails = (message != "") ? message : "The user does not have the necessary credentials.",
                        homeAddress = "/"
                    };
                    Console.WriteLine("\tError 401 - Unauthorized!");
                    break;

                case 403:
                    data = new
                    {
                        title = "403 Forbidden",
                        mainH1 = "Oops!",
                        mainH2 = "403 Forbidden",
                        errorDetails = (message != "") ? message : "The user might not have the necessary permissions for a resource.",
                        homeAddress = "/"
                    };
                    Console.WriteLine("\tError 403 - Forbidden!");
                    break;

                case 404:
                    data = new
                    {
                        title = "404 Not Found",
                        mainH1 = "Oops!",
                        mainH2 = "404 Not Found",
                        errorDetails = (message != "") ? message : "Sorry, an error has occured, Requested page not found!",
                        homeAddress = "/"
                    };
                    Console.WriteLine("\tError 404 - Not Found!");
                    break;

                case 405:
                    data = new
                    {
                        title = "405 Method Not Allowed",
                        mainH1 = "Oops!",
                        mainH2 = "405 Method Not Allowed",
                        errorDetails = (message != "") ? message : "Request method is not supported for the requested resource!",
                        homeAddress = "/"
                    };
                    Console.WriteLine("\tError 405 - Method Not Allowed!");
                    break;

                case 406:
                    data = new
                    {
                        title = "406 Not Acceptable",
                        mainH1 = "Oops!",
                        mainH2 = "406 Not Acceptable",
                        errorDetails = (message != "") ? message : "Requested resource is not acceptable,\naccording to the Accept headers sent in the request",
                        homeAddress = "/"
                    };
                    Console.WriteLine("\tError 406 - Not Acceptable!");
                    break;

                case 412:
                    data = new
                    {
                        title = "412 Precondition Failed",
                        mainH1 = "Oops!",
                        mainH2 = "412 Precondition Failed",
                        errorDetails = (message != "") ? message : "The server does not meet one of the preconditions that the requester put on the request.",
                        homeAddress = "/"
                    };
                    Console.WriteLine("\tError 412 - Precondition Failed!");
                    break;

                case 500:
                    data = new
                    {
                        title = "500 Internal Server Error",
                        mainH1 = "Oops!",
                        mainH2 = "500 Internal Server Error",
                        errorDetails = (message != "") ? message : "An internal server error occurred while processing the request.",
                        homeAddress = "/"
                    };
                    Console.WriteLine("\tError 500 - Internal Server Error!");
                    break;

                case 501:
                    data = new
                    {
                        title = "501 Not Implemented",
                        mainH1 = "Oops!",
                        mainH2 = "501 Not Implemented",
                        errorDetails = (message != "") ? message : "The server either does not recognize the request method,\nor it lacks the ability to fulfill the request..",
                        homeAddress = "/"
                    };
                    Console.WriteLine("\tError 501 - Not Implemented!");
                    break;

                case 502:
                    data = new
                    {
                        title = "502 Bad Gateway",
                        mainH1 = "Oops!",
                        mainH2 = "502 Bad Gateway",
                        errorDetails = (message != "") ? message : "The server was acting as a gateway or proxy and received an invalid response from the upstream server.",
                        homeAddress = "/"
                    };
                    Console.WriteLine("\tError 502 - Bad Gateway!");
                    break;

                default:
                    data = new
                    {
                        title = "500 Internal Server Error",
                        mainH1 = "Oops!",
                        mainH2 = "500 Internal Server Error",
                        errorDetails = (message != "") ? message : "An internal server error occurred while processing the request.",
                        homeAddress = "/"
                    };
                    Console.WriteLine("\tError 500 - Internal Server Error!");
                    break;
            }
            var source = File.ReadAllText(resources + "Views/" + errorTemplate);
            var template = Handlebars.Compile(source);
            var result = template(data);
            using (var writer = new StreamWriter(e.Response.OutputStream))
            {
                writer.Write(result);
            }
            e.Response.StatusCode = errorCode;
            e.Response.Status = errorCode.ToString();
        }

        public void RenderErrorPage(int errorCode, HttpContext e, string message = "")
        {
            string resources = ConfigurationManager.AppSettings["Virtual"];
            object data = new object();
            switch (errorCode)
            {
                case 401:
                    data = new
                    {
                        title = "401 Unauthorized",
                        mainH1 = "Oops!",
                        mainH2 = "401 Unauthorized",
                        errorDetails = (message != "") ? message : "The user does not have the necessary credentials.",
                        homeAddress = "/"
                    };
                    Console.WriteLine("\tError 401 - Unauthorized!");
                    break;

                case 403:
                    data = new
                    {
                        title = "403 Forbidden",
                        mainH1 = "Oops!",
                        mainH2 = "403 Forbidden",
                        errorDetails = (message != "") ? message : "The user might not have the necessary permissions for a resource.",
                        homeAddress = "/"
                    };
                    Console.WriteLine("\tError 403 - Forbidden!");
                    break;

                case 404:
                    data = new
                    {
                        title = "404 Not Found",
                        mainH1 = "Oops!",
                        mainH2 = "404 Not Found",
                        errorDetails = (message != "") ? message : "Sorry, an error has occured, Requested page not found!",
                        homeAddress = "/"
                    };
                    Console.WriteLine("\tError 404 - Not Found!");
                    break;

                case 405:
                    data = new
                    {
                        title = "405 Method Not Allowed",
                        mainH1 = "Oops!",
                        mainH2 = "405 Method Not Allowed",
                        errorDetails = (message != "") ? message : "Request method is not supported for the requested resource!",
                        homeAddress = "/"
                    };
                    Console.WriteLine("\tError 405 - Method Not Allowed!");
                    break;

                case 406:
                    data = new
                    {
                        title = "406 Not Acceptable",
                        mainH1 = "Oops!",
                        mainH2 = "406 Not Acceptable",
                        errorDetails = (message != "") ? message : "Requested resource is not acceptable,\naccording to the Accept headers sent in the request",
                        homeAddress = "/"
                    };
                    Console.WriteLine("\tError 406 - Not Acceptable!");
                    break;

                case 412:
                    data = new
                    {
                        title = "412 Precondition Failed",
                        mainH1 = "Oops!",
                        mainH2 = "412 Precondition Failed",
                        errorDetails = (message != "") ? message : "The server does not meet one of the preconditions that the requester put on the request.",
                        homeAddress = "/"
                    };
                    Console.WriteLine("\tError 412 - Precondition Failed!");
                    break;

                case 500:
                    data = new
                    {
                        title = "500 Internal Server Error",
                        mainH1 = "Oops!",
                        mainH2 = "500 Internal Server Error",
                        errorDetails = (message != "") ? message : "An internal server error occurred while processing the request.",
                        homeAddress = "/"
                    };
                    Console.WriteLine("\tError 500 - Internal Server Error!");
                    break;

                case 501:
                    data = new
                    {
                        title = "501 Not Implemented",
                        mainH1 = "Oops!",
                        mainH2 = "501 Not Implemented",
                        errorDetails = (message != "") ? message : "The server either does not recognize the request method,\nor it lacks the ability to fulfill the request..",
                        homeAddress = "/"
                    };
                    Console.WriteLine("\tError 501 - Not Implemented!");
                    break;

                case 502:
                    data = new
                    {
                        title = "502 Bad Gateway",
                        mainH1 = "Oops!",
                        mainH2 = "502 Bad Gateway",
                        errorDetails = (message != "") ? message : "The server was acting as a gateway or proxy and received an invalid response from the upstream server.",
                        homeAddress = "/"
                    };
                    Console.WriteLine("\tError 502 - Bad Gateway!");
                    break;

                default:
                    data = new
                    {
                        title = "500 Internal Server Error",
                        mainH1 = "Oops!",
                        mainH2 = "500 Internal Server Error",
                        errorDetails = (message != "") ? message : "An internal server error occurred while processing the request.",
                        homeAddress = "/"
                    };
                    Console.WriteLine("\tError 500 - Internal Server Error!");
                    break;
            }
            var source = File.ReadAllText(resources + "Views/" + errorTemplate);
            var template = Handlebars.Compile(source);
            var result = template(data);
            using (var writer = new StreamWriter(e.Response.OutputStream))
            {
                writer.Write(result);
            }
            e.Response.StatusCode = errorCode;
            e.Response.Status = errorCode.ToString();
        }
        #endregion
    }
}