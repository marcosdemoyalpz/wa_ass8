using PHttp;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Reflection;
using static PHttp.Startup;

namespace Mvc
{
    public class Router
    {
        private string _appName;
        public string AppName
        {
            get { return _appName; }
            set { _appName = value; }
        }
        public class Controllers
        {
            List<ControllerBase> _controllers = new List<ControllerBase>();
            public List<ControllerBase> GetControllers
            {
                get { return _controllers; }
                set { _controllers = value; }
            }
            public Controllers(List<ControllerBase> controllers)
            {
                _controllers = controllers;
            }
        }

        public static Controllers loadControllers(string path, string appName)
        {
            Console.WriteLine("\n\tLooking for apps in " + path + "\n");

            if (string.IsNullOrEmpty(path)) { throw new FileNotFoundException(); } //sanity check

            DirectoryInfo info = new DirectoryInfo(path);
            if (!info.Exists) { throw new FileNotFoundException(); } //make sure directory exists

            var impl = new List<ControllerBase>();

            foreach (FileInfo file in info.GetFiles("*.dll")) //loop through all dll files in directory
            {
                if (file.Name == (appName + ".dll"))
                {
                    Console.WriteLine("\tdll = " + file);
                    Assembly currentAssembly = null;
                    try
                    {
                        var name = AssemblyName.GetAssemblyName(file.FullName);
                        currentAssembly = Assembly.Load(name);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("\n\t" + ex);
                        continue;
                    }

                    var types = currentAssembly.GetTypes();
                    foreach (var type in types)
                    {
                        if (type != typeof(ControllerBase) && typeof(ControllerBase).IsAssignableFrom(type))
                        {
                            var temp = (ControllerBase)Activator.CreateInstance(type);
                            if (!impl.Contains(temp))
                            {
                                impl.Add(temp);
                            }
                        }
                    }
                }
            }
            Console.WriteLine();
            foreach (var el in impl)
            {
                Console.WriteLine("\tLoading " + el + "...\n");
            }
            return new Controllers(impl);
        }

        public Router(string appName)
        {
            _controller = new ControllerBase();
            _errorHandler = new ErrorHandler();
            _appName = appName;
        }

        private ControllerBase _controller;
        ErrorHandler _errorHandler;

        private static bool IsHttpAttributeAllowed(MemberInfo member, string requestType)
        {
            bool allowed = false;
            foreach (object attribute in member.GetCustomAttributes(true))
            {
                if (allowed == true)
                {
                    break;
                }
                else
                {
                    switch (requestType)
                    {
                        case "GET":
                            if (attribute is Attributes.HttpGet)
                            {
                                allowed = true;
                            }
                            break;
                        case "POST":
                            if (attribute is Attributes.HttpPost)
                            {
                                allowed = true;
                            }
                            break;
                        case "PUT":
                            if (attribute is Attributes.HttpPut)
                            {
                                allowed = true;
                            }
                            break;
                        case "DELETE":
                            if (attribute is Attributes.HttpDelete)
                            {
                                allowed = true;
                            }
                            break;
                        default:
                            allowed = false;
                            break;
                    }
                }
            }
            return allowed;
        }

        public void CallAction(HttpRequestEventArgs e, string directory)
        {
            string path = e.Request.Url.PathAndQuery;
            if (path == "" || path == "/")
            {
                _errorHandler.RenderErrorPage(404, e);
            }
            else
            {
                _controller.Context = e.Context;
                _controller.Request = e.Request;
                _controller.Route = e.Request.Path;
                _controller.ControllerName = path.Split('?')[0].Split('/')[2];
                _controller.ActionName = path.Split('?')[0].Split('/')[3];
                _controller.PrintControllerInfo();

                bool found = false;
                bool allowed = false;
                string className = _controller.ControllerName;
                foreach (var el in loadControllers(directory, _appName).GetControllers)
                {
                    var tempStr = path.Split('?')[0].Split('/')[1] + ".Controllers.";
                    var controllerName = el.ToString().Replace(tempStr, "");
                    controllerName = controllerName.Replace("Controller", "");
                    if (controllerName.ToUpper() == className.Replace("/", "").ToUpper())
                    {
                        Console.WriteLine("\tExecuting " + controllerName + "...\n");
                        Type type = el.GetType();

                        MethodInfo method = type.GetMethod(_controller.ActionName);
                        string requestType = e.Request.RequestType;

                        if (IsHttpAttributeAllowed(method, requestType))
                        {
                            Console.WriteLine("\tMethod " + method.Name + " allows " + requestType);
                            allowed = true;
                        }
                        else
                        {
                            Console.WriteLine("\tMethod " + method.Name + " does not allow " + requestType);
                        }

                        Console.WriteLine();

                        if (allowed == true)
                        {
                            e.Response.StatusCode = 200;
                            method.Invoke(el, new[] { e });
                        }
                        else
                        {
                            _errorHandler.RenderErrorPage(405, e);
                        }
                        found = true;
                        break;
                    }
                }
                if (found == false) _errorHandler.RenderErrorPage(404, e);
            }
        }
    }
}
