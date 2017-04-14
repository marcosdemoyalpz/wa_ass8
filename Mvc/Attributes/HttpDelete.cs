using System;

namespace Mvc.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class HttpDelete : Attribute
    {
        public readonly string MethodName;

        public HttpDelete()
        {
            MethodName = "DELETE";
        }
    }
}
