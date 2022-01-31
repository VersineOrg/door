// Filename:  HttpServer.cs        
// Author:    Benjamin N. Summerton <define-private-public>        
// License:   Unlicense (http://unlicense.org/)

using System.Net;
using System.Text;
using System.Text.Json;

namespace door
{
    public class Response
    {
        public String success { get; set; }
        public String message { get; set; }
    }
    class HttpServer
    {
        public static HttpListener listener;
        public static string url = "http://localhost:8000/";

        public static async Task HandleIncomingConnections()
        {

            while (true)
            {
                // While a user hasn't visited the `shutdown` url, keep on handling requests
                // Will wait here until we hear from a connection
                HttpListenerContext ctx = await listener.GetContextAsync();

                // Peel out the requests and response objects
                HttpListenerRequest req = ctx.Request;
                HttpListenerResponse resp = ctx.Response;

                // Print out some info about the request
                Console.WriteLine(req.Url.ToString());
                Console.WriteLine(req.HttpMethod);
                Console.WriteLine(req.UserHostName);
                Console.WriteLine(req.UserAgent);
                Console.WriteLine();

                
                // If `shutdown` url requested w/ POST, then shutdown the server after serving the page
                if ((req.HttpMethod == "POST") && (req.Url.AbsolutePath == "/login"))
                {
                    var reader = new StreamReader(req.InputStream);
                    Console.WriteLine(reader.ReadToEnd());
                    Console.WriteLine("Login Requested");
                    var response = new Response
                    {
                        success = "true",
                        message = "login requested"
                    };
                    string jsonString = JsonSerializer.Serialize(response);
                    byte[] data = Encoding.UTF8.GetBytes(jsonString);
                    resp.ContentType = "application/json";
                    resp.ContentEncoding = Encoding.UTF8;
                    resp.ContentLength64 = data.LongLength;
                    // Write out to the response stream (asynchronously), then close it
                    await resp.OutputStream.WriteAsync(data, 0, data.Length);
                    resp.Close();
                }
                else
                {
                    var response = new Response
                    {
                        success = "false",
                        message = "404"
                    };
                    string jsonString = JsonSerializer.Serialize(response);
                    byte[] data = Encoding.UTF8.GetBytes(jsonString);
                    resp.ContentType = "application/json";
                    resp.ContentEncoding = Encoding.UTF8;
                    resp.ContentLength64 = data.LongLength;
                    // Write out to the response stream (asynchronously), then close it
                    await resp.OutputStream.WriteAsync(data, 0, data.Length);
                    resp.Close();    
                }
            }
        }


        public static void Main(string[] args)
        {
            // Create a Http server and start listening for incoming connections
            listener = new HttpListener();
            listener.Prefixes.Add(url);
            listener.Start();
            Console.WriteLine("Listening for connections on {0}", url);

            // Handle requests
            Task listenTask = HandleIncomingConnections();
            listenTask.GetAwaiter().GetResult();

            // Close the listener
            listener.Close();
        }
    }
}
