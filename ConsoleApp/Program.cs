using PHttp;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace ConsoleApp
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Startup.LoadDLLs loadDLLs = Startup.LoadApps();
            using (var server = new HttpServer(8080))
            {
                try
                {
                    // New requests are signaled through the RequestReceived
                    // event.
                    server.RequestReceived += (s, e) =>
                    {
                        server.ProcessRequest(e, loadDLLs);
                    };
                    server.Start();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }

                // Start the default web browser.

                Process.Start("http://" + server.EndPoint + "/");
                //Console.WriteLine("Press any key to continue...");
                Console.ReadKey();

                // When the HttpServer is disposed, all opened connections
                // are automatically closed.
            }
        }
    }
}