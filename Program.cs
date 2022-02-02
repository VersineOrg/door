using System.Collections;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace door
{
    public class Response
    {
        public String success { get; set; }
        public String message { get; set; }
    }
    public class User
         {
             
             public String username { get; set; }
             public String password { get; set; }
             
             public String avatar { get; set; }
             
             public String bio { get; set; }
             
             public String banner { get; set; }
             
             public String color { get; set; }
             
             public List<User> friends { get; set; }
             
             public List<String> circles { get; set; }

             [BsonConstructor]
             public User(string username, string password)
             {
                 this.username = username;
                 this.password = password;
                 this.avatar = "https://i.imgur.com/k7eDNwW.jpg";
                 this.bio = "Hey, I'm using Versine!";
                 this.banner = "https://images7.alphacoders.com/421/thumb-1920-421957.jpg";
                 this.color = "28DBB7";
                 this.friends = new List<User>();
                 this.circles = new List<string>();
             }

         }
    class HttpServer
    {
        
        public static HttpListener? Listener;

        public static async Task HandleIncomingConnections(IConfigurationRoot config)
        {
            // Replace the uri string with your MongoDB deployment's connection string.

            var connectionString = config.GetValue<String>("MongoDB");
            var client = new MongoClient(
                connectionString
            );
            
            BsonClassMap.RegisterClassMap<User>();
            var database = client.GetDatabase("UsersDB");
            var collection = database.GetCollection<User>("users");
            Console.WriteLine("Database connected");
            
            
            
            
            while (true)
            {
                // While a user hasn't visited the `shutdown` url, keep on handling requests
                // Will wait here until we hear from a connection
                HttpListenerContext ctx = await Listener?.GetContextAsync()!;

                // Peel out the requests and response objects
                HttpListenerRequest req = ctx.Request;
                HttpListenerResponse resp = ctx.Response;

                // Print out some info about the request
                Console.WriteLine(req.HttpMethod);
                Console.WriteLine(req.Url?.ToString());
                Console.WriteLine(req.UserHostName);
                Console.WriteLine(req.UserAgent);

                
                // If `shutdown` url requested w/ POST, then shutdown the server after serving the page
                if ((req.HttpMethod == "POST") && (req.Url?.AbsolutePath == "/register"))
                {
                    var reader = new StreamReader(req.InputStream);
                    string bodyString = await reader.ReadToEndAsync();
                    dynamic body = JsonConvert.DeserializeObject(bodyString);
                    string username = body.username;
                    string password = body.password;
                    if (!String.IsNullOrEmpty(username) && !String.IsNullOrEmpty(password) &&
                        !String.IsNullOrWhiteSpace(username) && !String.IsNullOrWhiteSpace(password))
                    {
                        var filter = Builders<User>.Filter.Eq("username", username);
                        var documents = collection.Find(filter);
    
                        if (await documents.CountDocumentsAsync() < 1)
                        {
                            Console.WriteLine("No user found with username: " + username);
                            var newuser = new User(username, password);
                            await collection.InsertOneAsync(newuser);                       
                            Console.WriteLine("User created");
                            var response = new Response
                            {
                                success = "true",
                                message = "user created"
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
                            Console.WriteLine("A user already exists with this username");
                            var response = new Response
                            {
                                success = "false",
                                message = "username taken"
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
                    else
                    {
                        Console.WriteLine("Invalid body");
                        var response = new Response
                        {
                            success = "false",
                            message = "invalid body"
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
            var config =
                new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", true)
                    .AddEnvironmentVariables()
                    .Build();
            
            var url = "http://*:" + config.GetValue<String>("Port") + "/";
            Listener = new HttpListener();
            Listener.Prefixes.Add(url);
            Listener.Start();
            Console.WriteLine("Listening for connections on {0}", url);

            // Handle requests
            Task listenTask = HandleIncomingConnections(config);
            listenTask.GetAwaiter().GetResult();

            // Close the listener
            Listener.Close();
        }
    }
}
