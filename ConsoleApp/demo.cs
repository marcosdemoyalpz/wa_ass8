using System;
using System.Diagnostics;
using System.IO;
using PHttp;
using System.Drawing;
using System.Configuration;

namespace Demo
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var server = new HttpServer(8080))
            {
                // New requests are signaled through the RequestReceived
                // event.

                server.RequestReceived += (s, e) =>
                {
                    //// The response must be written to e.Response.OutputStream.
                    //// When writing text, a StreamWriter can be used.
                    //using (var writer = new StreamWriter(e.Response.OutputStream))
                    //{
                    //    writer.Write(query);
                    //}                                
                    server.ProcessRequest(e);
                };

                // Start the server on a random port. Use server.EndPoint
                // to specify a specific port, e.g.:
                //
                //     server.EndPoint = new IPEndPoint(IPAddress.Loopback, 80);
                //

                server.Start();

                // Start the default web browser.

                Process.Start(String.Format("http://{0}/", server.EndPoint));

                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();

                // When the HttpServer is disposed, all opened connections
                // are automatically closed.
            }
        }
    }
}