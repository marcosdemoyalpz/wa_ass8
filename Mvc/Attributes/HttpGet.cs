using System;

namespace Mvc.Attributes
{
    ////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>   HTTP GET Attributes Tag. </summary>
    /// <remarks>   Marcos De Moya, 4/20/2017. </remarks>
    ////////////////////////////////////////////////////////////////////////////////////////////////////
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
