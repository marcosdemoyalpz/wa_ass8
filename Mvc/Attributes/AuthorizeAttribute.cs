using System;

namespace Mvc.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class AuthorizeAttribute : Attribute
    {
        public AuthorizeAttribute()
        {

        }
    }
}
