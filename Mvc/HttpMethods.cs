﻿using System;
using System.Collections.Generic;
using Mvc.Attributes;

namespace Mvc
{
    ////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>   HTTP Attributes Tag. </summary>
    /// <remarks>   Marcos De Moya, 4/20/2017. </remarks>
    ////////////////////////////////////////////////////////////////////////////////////////////////////
    class HttpMethods : FilterAttribute
    {
        private readonly Dictionary<string, Type> _methodAttributes
            = new Dictionary<string, Type>
            {
                { "GET", typeof(HttpGet) },
                { "POST", typeof(HttpPost) },
                { "PUT", typeof(HttpPut) },
                { "DELETE", typeof(HttpDelete) }
            };
        public Dictionary<string, Type> MethodAttributes { get; }
    }
}
