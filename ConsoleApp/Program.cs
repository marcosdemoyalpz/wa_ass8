using PHttp;
using System;
using System.Diagnostics;

namespace ConsoleApp
{
    ////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>   A Console program. </summary>
    /// <remarks>   Marcos De Moya, 4/20/2017. </remarks>
    ////////////////////////////////////////////////////////////////////////////////////////////////////
    internal class Program
    {
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Main entry-point for this application. </summary>
        ///
        /// <remarks>   Marcos De Moya, 4/20/2017. </remarks>
        ///
        /// <exception cref="Exception">    Thrown when an exception error condition occurs. </exception>
        ///
        /// <returns>   Exit-code for the process - 0 for success, else an error code. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        private static int Main()
        {
            Console.WriteLine("\n\tServer is starting...");
            try
            {
                Startup startup = new Startup();
                LoadApps loadDLLs = startup.loadApps;
                Console.WriteLine("\tFinished Startup!");

                using (var server = new HttpServer("0.0.0.0", 8080))
                {
                    try
                    {
                        // New requests are signaled through the RequestReceived
                        // event.
                        server.RequestReceived += (sender, e) =>
                        {
                            server.ProcessRequest(e, loadDLLs);
                        };
                        server.Start();
                    }
                    catch (Exception ex)
                    {
                        throw new Exception(ex.ToString());
                    }
                    Console.ReadKey();

                    // When the HttpServer is disposed, all opened connections
                    // are automatically closed.
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
            return 0;
        }
    }
}