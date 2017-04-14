using System;

namespace Mvc.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class HttpPost : Attribute
    {
        public readonly string MethodName;

        public HttpPost()
        {
            MethodName = "POST";
        }
    }
}
