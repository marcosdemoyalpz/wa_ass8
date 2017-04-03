using System;

namespace PHttp
{
    public delegate void PreApplicationStartMethod(Type type, string method);

    public delegate void ApplicationStartMethod(Type type, string method);

    public interface IPHttpApplication
    {
        #region Methods

        void Start(string path, HttpRequestEventArgs e);

        void ExecuteAction(HttpRequestEventArgs e);

        event PreApplicationStartMethod PreApplicationStart;

        event ApplicationStartMethod ApplicationStart;

        #endregion Methods

        #region Properties

        string Name { get; set; }

        #endregion Properties
    }
}