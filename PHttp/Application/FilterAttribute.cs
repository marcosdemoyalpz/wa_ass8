using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PHttp.Application
{
    abstract class FilterAttribute : Attribute
    {
        Attribute HttpMethod()
        {
            throw new NotImplementedException();
        }
        Attribute HttpGet()
        {
            throw new NotImplementedException();
        }
        Attribute HttpPost()
        {
            throw new NotImplementedException();
        }
        Attribute HttpPut()
        {
            throw new NotImplementedException();
        }
        Attribute HttpDelete()
        {
            throw new NotImplementedException();
        }
        Attribute AuthorizeAttribute()
        {
            throw new NotImplementedException();
        }
    }
}
