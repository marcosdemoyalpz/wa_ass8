using System;

namespace Mvc
{
    public abstract class FilterAttribute : Attribute
    {       
        Attribute AuthorizeAttribute()
        {
            throw new NotImplementedException();
        }
    }
}
