using System;
using Mvc;
using PHttp;

namespace URL_Shortener_App
{
    ////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>   An application. </summary>
    /// <remarks>   Marcos De Moya, 4/20/2017. </remarks>
    ////////////////////////////////////////////////////////////////////////////////////////////////////
    public class Application : IPHttpApplication
    {
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets the application name". </summary>
        /// <value> The application name". </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        private string name = "URL_Shortener_App";

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Instance of ErrorHandler class. </summary>
        /// <remarks>   Marcos De Moya, 4/20/2017. </remarks>
        /// <returns>   An errorHandler. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        Mvc.ErrorHandler errorHandler = new Mvc.ErrorHandler();

        // Explicit interface members implementation:

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Starts this object. </summary>
        /// <remarks>   Marcos De Moya, 4/20/2017. </remarks>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        void IPHttpApplication.Start()
        {
            try
            {
                Console.WriteLine("\tStarting " + name + "!");
            }
            catch (Exception)
            {

            }
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Executes the action. </summary>
        /// <remarks>   Marcos De Moya, 4/20/2017. </remarks>
        /// <param name="e">                HTTP request event information. </param>
        /// <param name="applicationsDir">  The applications dir. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        void IPHttpApplication.ExecuteAction(HttpRequestEventArgs e, string applicationsDir)
        {
            Console.WriteLine("\tExecute Action");
            try
            {
                Router router = new Router(name);
                router.CallAction(e, applicationsDir);
            }
            catch
            {
                errorHandler.RenderErrorPage(404, e);
            }
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets or sets the name. </summary>
        /// <value> The name. </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        string IPHttpApplication.Name
        {
            set
            {
                name = value;
            }
            get
            {
                return name;
            }
        }
        //public event PreApplicationStartMethod PreApplicationStart;
        //public event ApplicationStartMethod ApplicationStart;
    }
}
