using System;

namespace Mvc.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class HttpPut : Attribute
    {
        public readonly string MethodName;

        public HttpPut()
        {
            MethodName = "PUT";
        }
    }
}
