using System.Diagnostics;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;


namespace door
{
    
    // Default Schema for a User
    public class User
    { 
        public ObjectId _id { get; set; }
        public String Username { get; set; }
        public String Password { get; set; }
        public String Ticket { get; set; }
        public Int32 TicketCount { get; set; }
        public String Avatar { get; set; }
        public String Bio { get; set; }
        public String Banner { get; set; }
        public String Color { get; set; }
        public List<User> Friends { get; set; }
        public List<String> Circles { get; set; }

        [BsonConstructor]
        public User(string username, string password, string ticket)
        {
            this.Username = username;
            this.Password = password;
            this.Ticket = ticket;
            this.TicketCount = 10;
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
                    string username = (string) body.username;
                    string password = (string) body.password;
                    string ticket = (string) body.ticket;
                    
                    // Check if the Body contains the required fields
                    if (!String.IsNullOrEmpty(username) && !String.IsNullOrEmpty(password) &&
                        !String.IsNullOrWhiteSpace(username) && !String.IsNullOrWhiteSpace(password) && !String.IsNullOrEmpty(ticket) && !String.IsNullOrWhiteSpace(ticket))
                    {
                        // Look in the Database for a potential match with the requested username
                        FilterDefinition<User> userfilter = Builders<User>.Filter.Eq("Username", username);
                        User userdocument = collection.Find(userfilter).FirstOrDefault();
                        
                        // if no user exists with this username, then register the user in the database, and send a success response
                        if (userdocument == null)
                        {
                            
                            // look in the database for a potential match with the requested ticket
                            FilterDefinition<User> ticketfilter = Builders<User>.Filter.Eq("Ticket", ticket);
                            User ticketdocument = collection.Find(ticketfilter).FirstOrDefault();                            // hash the password
                            
                            // if a ticket is found we register the user and we substract one to the used ticket count
                            if (ticketdocument != null && ticketdocument.TicketCount >= 1)
                            {
                                // the user's password is hashed
                                string hashedpassword = HashTools.HashString(password);
                                // generate a new ticket based of the username
                                string newticket = Ticket.GenTicket(username);
                                
                                // Insert a new user in the Database with the hashed password and username generated ticket
                                User newuser = new User(username, hashedpassword, newticket);
                                await collection.InsertOneAsync(newuser);                       
                                
                                // change the count of the inviter
                                var updateTicketFilter = Builders<User>.Filter.Eq("Ticket", ticket);
                                var updateTicketCount = Builders<User>.Update.Inc("TicketCount", -1);
                                var updateResult = collection.UpdateOne(updateTicketFilter, updateTicketCount);
                                if (updateResult.MatchedCount == 1 && updateResult.ModifiedCount == 1)
                                {
                                    Response.Success(resp, "user created", "");
                                }
                                else
                                {
                                    Response.Fail(resp,"cannot update inviter's ticket");
                                }
                            }
                            // no ticket found 
                            else
                            {
                                Response.Fail(resp,"invalid or expired ticket");
                            }
                            
                        }
                        
                        // If a user already exists with this username, the user can't be registered
                        else
                        {
                            Response.Fail(resp, "username taken");
                        }    
                    }
                    else
                    {
                        Response.Fail(resp,"invalid body");
                    }
                }

                if ((req.HttpMethod == "POST") && (req.Url?.AbsolutePath == "/login"))
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
                        User document = collection.Find(filter).FirstOrDefault();

                        // if a user is found, check for the password matching
                        if (document != null)
                        {
                            // hash the password
                            string hashedpassword = HashTools.HashString(password);
                            // check if the hashed password and the stored password are matching
                            if (document.Password == hashedpassword)
                            {
                                // generate the token with the id and send the response
                                string token = WebToken.GenerateToken(document._id.ToString()); 
                                
                                Response.Success(resp, "user logged", token);
                            }
                            else
                            {
                                // password is not matching
                                Response.Fail(resp, "wrong password or username");
                            }
                        }
                        
                        // no user found with this username
                        else
                        {
                            Response.Fail(resp, "wrong password or username");
                        }    
                    }
                    else
                    {
                        Response.Fail(resp, "invalid body");
                    }
                }
                else
                {
                    Response.Fail(resp, "404");
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
