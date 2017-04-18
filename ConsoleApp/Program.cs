using PHttp;
using System;
using System.Diagnostics;

namespace ConsoleApp
{
    internal class Program
    {
        private static int Main()
        {
            Console.WriteLine("\n\tServer is starting...");
            try
            {
                Startup startup = new Startup();
                LoadApps loadDLLs = startup.loadApps;
                Console.WriteLine("\tFinished Startup!");

                //using (var server = new HttpServer(8080))
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
                        throw new Exception(ex.Message);
                    }

                    // Start the default web browser.
                    try
                    {
                        Process.Start("http://" + server.EndPoint + "/");
                    }
                    catch (Exception ex)
                    {
                        throw new Exception(ex.Message);
                    }
                    //Console.WriteLine("Press any key to continue...");
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