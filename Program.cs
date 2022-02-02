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
    
    // Default Schema for a Http Response
    public class Response
    {
        public String success { get; set; }
        public String message { get; set; }
    }
    
    // Default Schema for a User
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

            // Connect to the MongoDB Database
            var connectionString = config.GetValue<String>("MongoDB");
            var client = new MongoClient(connectionString);
            BsonClassMap.RegisterClassMap<User>();
            var database = client.GetDatabase("UsersDB");
            var collection = database.GetCollection<User>("users");
            Console.WriteLine("Database connected");
            
            
            
            
            while (true)
            {
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

                
                // If `register` url requested w/ POST, then register the user if possible
                if ((req.HttpMethod == "POST") && (req.Url?.AbsolutePath == "/register"))
                {
                    // Parse the request's Body
                    var reader = new StreamReader(req.InputStream);
                    string bodyString = await reader.ReadToEndAsync();
                    dynamic body = JsonConvert.DeserializeObject(bodyString);
                    
                    // Get the username and the password from the body
                    string username = body.username;
                    string password = body.password;
                    
                    // Check if the Body contains the required fields
                    if (!String.IsNullOrEmpty(username) && !String.IsNullOrEmpty(password) &&
                        !String.IsNullOrWhiteSpace(username) && !String.IsNullOrWhiteSpace(password))
                    {
                        
                        // Look in the Database for a potential match with the requested username
                        var filter = Builders<User>.Filter.Eq("username", username);
                        var documents = collection.Find(filter);
    
                        // if no user exists with this username, then register the user in the database, and send a success response
                        if (await documents.CountDocumentsAsync() < 1)
                        {
                            // Insert a new user in the Database with the chosen password and username
                            // TODO: Hash the password before inserting the model
                            var newuser = new User(username, password);
                            await collection.InsertOneAsync(newuser);                       
                            
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
                        
                        // TODO: If `register` url requested w/ POST, then register the user if possible
                        
                        // If a user already exists with this username, the user can't be registered
                        else
                        {
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
            // Build the configuration for the env variables
            var config =
                new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", true)
                    .AddEnvironmentVariables()
                    .Build();
            
            // Create a Http server and start listening for incoming connections
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
