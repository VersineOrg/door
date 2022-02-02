using System.Net;
using System.Text;
using Newtonsoft.Json;
using MongoDB.Bson;
using MongoDB.Driver;

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
            // Replace the uri string with your MongoDB deployment's connection string.
            
            
            /* var connectionString = "";
            var client = new MongoClient(
                connectionString
            );
            var database = client.GetDatabase("UserDB");
            var collection = database.GetCollection<BsonDocument>("users");
            */
            
            
            Console.WriteLine("Database connected");
            
            while (true)
            {
                // While a user hasn't visited the `shutdown` url, keep on handling requests
                // Will wait here until we hear from a connection
                HttpListenerContext ctx = await listener.GetContextAsync();

                // Peel out the requests and response objects
                HttpListenerRequest req = ctx.Request;
                HttpListenerResponse resp = ctx.Response;

                // Print out some info about the request
                Console.WriteLine(req.HttpMethod);
                Console.WriteLine(req.Url.ToString());
                Console.WriteLine(req.UserHostName);
                Console.WriteLine(req.UserAgent);

                
                // If `shutdown` url requested w/ POST, then shutdown the server after serving the page
                if ((req.HttpMethod == "POST") && (req.Url.AbsolutePath == "/login"))
                {
                    var reader = new StreamReader(req.InputStream);
                    string bodyString= reader.ReadToEnd();
                    
                    dynamic body = JsonConvert.DeserializeObject(bodyString);
                    Console.WriteLine(body);
                    
                    var response = new Response
                    {
                        success = "true",
                        message = "login requested"
                    };
                    
                    string jsonString = JsonConvert.SerializeObject(response);
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
                    
                    string jsonString = JsonConvert.SerializeObject(response);
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
