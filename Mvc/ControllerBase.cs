using PHttp;
using System;
using System.Collections.Generic;

namespace Mvc
{
    ////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>   Controller base class. </summary>
    /// <remarks>   Marcos De Moya, 4/20/2017. </remarks>
    ////////////////////////////////////////////////////////////////////////////////////////////////////
    public class ControllerBase
    {
        HttpContext _context;
        HttpRequest _request;
        string _route;
        string _controllerName;
        string _actionName;
        string _username;

        public HttpContext Context
        {
            get { return _context; }
            set { _context = value; }
        }
        public HttpRequest Request
        {
            get { return _request; }
            set { _request = value; }
        }
        public string Route
        {
            get { return _route; }
            set { _route = value; }
        }
        public string ControllerName
        {
            get { return _controllerName; }
            set { _controllerName = value; }
        }
        public string ActionName
        {
            get { return _actionName; }
            set { _actionName = value; }
        }
        public string User
        {
            get { return _username; }
            set { _username = value; }
        }
        Dictionary<string, string> _session = new Dictionary<string, string>();
        Dictionary<string, string> _urlParams = new Dictionary<string, string>();

        public void PrintControllerInfo()
        {
            Console.WriteLine("\tRoute = " + _route);
            Console.WriteLine("\tControllerName = " + _controllerName);
            Console.WriteLine("\tActionName = " + _actionName);
            //Console.WriteLine("\tUser = " + _username);
        }
    }
}
