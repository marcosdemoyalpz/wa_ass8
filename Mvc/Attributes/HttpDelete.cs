using System;

namespace Mvc.Attributes
{
    ////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>   HTTP DELETE Attributes Tag. </summary>
    /// <remarks>   Marcos De Moya, 4/20/2017. </remarks>
    ////////////////////////////////////////////////////////////////////////////////////////////////////
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
