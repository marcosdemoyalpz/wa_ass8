using System;

namespace Mvc.Attributes
{
    ////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>   HTTP PUT Attributes Tag. </summary>
    /// <remarks>   Marcos De Moya, 4/20/2017. </remarks>
    ////////////////////////////////////////////////////////////////////////////////////////////////////
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
