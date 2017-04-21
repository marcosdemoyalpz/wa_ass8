using System;

namespace Mvc.Attributes
{
    ////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>   HTTP AuthorizeAttribute Attributes Tag. </summary>
    /// <remarks>   Marcos De Moya, 4/20/2017. </remarks>
    ////////////////////////////////////////////////////////////////////////////////////////////////////
    [AttributeUsage(AttributeTargets.Method)]
    public class AuthorizeAttribute : Attribute
    {
        public AuthorizeAttribute()
        {

        }
    }
}
