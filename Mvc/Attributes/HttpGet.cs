using System;

namespace Mvc.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class HttpGet : Attribute
    {
        public readonly string MethodName;

        public HttpGet()
        {
            MethodName = "GET";
        }
    }
}
