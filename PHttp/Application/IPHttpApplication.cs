﻿using System;

namespace PHttp
{
    //public delegate void PreApplicationStartMethod(Type type, string method);

    //public delegate void ApplicationStartMethod(Type type, string method);

    ////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>   Interface for IPHttpApplication. </summary>
    /// <remarks>   Marcos De Moya, 4/20/2017. </remarks>
    ////////////////////////////////////////////////////////////////////////////////////////////////////
    public interface IPHttpApplication
    {
        #region Methods

        void Start();

        void ExecuteAction(HttpRequestEventArgs e, string applicationsDir);

        //event PreApplicationStartMethod PreApplicationStart;

        //event ApplicationStartMethod ApplicationStart;

        #endregion Methods

        #region Properties

        string Name { get; set; }

        #endregion Properties
    }
}