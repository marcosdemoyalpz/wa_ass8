﻿using System;

namespace Mvc.Attributes
{
    ////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>   HTTP POST Attributes Tag. </summary>
    /// <remarks>   Marcos De Moya, 4/20/2017. </remarks>
    ////////////////////////////////////////////////////////////////////////////////////////////////////
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
