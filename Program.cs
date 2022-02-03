using System.Collections;
using System.Net;
using System.Security.Cryptography;
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
        public ObjectId _id { get; }
        public String Username { get; set; }
        public String Password { get; set; }
        public String Avatar { get; set; }
        public String Bio { get; set; }
        public String Banner { get; set; }
        public String Color { get; set; }
        public List<User> Friends { get; set; }
        public List<String> Circles { get; set; }

        [BsonConstructor]
        public User(string username, string password)
        {
            this.Username = username;
            this.Password = password;
            this.Avatar = "https://i.imgur.com/k7eDNwW.jpg";
            this.Bio = "Hey, I'm using Versine!";
            this.Banner = "https://images7.alphacoders.com/421/thumb-1920-421957.jpg";
            this.Color = "28DBB7";
            this.Friends = new List<User>();
            this.Circles = new List<string>();
        }
    }
    class HttpServer
    {
        
        public static HttpListener? Listener;

        public static async Task HandleIncomingConnections(IConfigurationRoot config)
        {

            // Connect to the MongoDB Database
            
            string connectionString = config.GetValue<String>("MongoDB");
            MongoClientSettings settings = MongoClientSettings.FromConnectionString(connectionString);
            MongoClient client = new MongoClient(settings);
            IMongoDatabase database = client.GetDatabase("UsersDB");
            BsonClassMap.RegisterClassMap<User>();
            IMongoCollection<User> collection = database.GetCollection<User>("users");
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
                    StreamReader reader = new StreamReader(req.InputStream);
                    string bodyString = await reader.ReadToEndAsync();
                    dynamic body = JsonConvert.DeserializeObject(bodyString)!;
                    
                    // Get the username and the password from the body
                    string username = body.username;
                    string password = body.password;
                    
                    // Check if the Body contains the required fields
                    if (!String.IsNullOrEmpty(username) && !String.IsNullOrEmpty(password) &&
                        !String.IsNullOrWhiteSpace(username) && !String.IsNullOrWhiteSpace(password))
                    {
                        
                        // Look in the Database for a potential match with the requested username
                        FilterDefinition<User> filter = Builders<User>.Filter.Eq("Username", username);
                        User documents = collection.Find(filter).FirstOrDefault();

                        // if no user exists with this username, then register the user in the database, and send a success response
                        if (documents == null)
                        {
                            // hash the password
                            StringBuilder hashedpasswordbuilder = new StringBuilder();
                            using (SHA256 hash = SHA256.Create()) {
                                Encoding enc = Encoding.UTF8;
                                Byte[] result = hash.ComputeHash(enc.GetBytes(password));
                                foreach (Byte b in result)
                                    hashedpasswordbuilder.Append(b.ToString("x2"));
                            }
                            string hashedpassword =  hashedpasswordbuilder.ToString();
                            
                            // Insert a new user in the Database with the hashed password and username
                            User newuser = new User(username, hashedpassword);
                            await collection.InsertOneAsync(newuser);                       
                            
                            Response response = new Response
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
                            Response response = new Response
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
                        Response response = new Response
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
                    Response response = new Response
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
            IConfigurationRoot config =
                new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", true)
                    .AddEnvironmentVariables()
                    .Build();
            
            // Create a Http server and start listening for incoming connections
            string url = "http://*:" + config.GetValue<String>("Port") + "/";
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
