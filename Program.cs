using System.Diagnostics;
using System.Net;
using System.Text;
using Newtonsoft.Json;



namespace door
{
    
    
    class HttpServer
    {
        
        public static HttpListener? Listener;

        public static async Task HandleIncomingConnections(Database database)
        {
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
                        if (!database.UserExists(username))
                        {
                            if (database.TicketIsValid(ticket))
                            {
                                if (database.RegisterUser(username, password, ticket))
                                {
                                    Response.Success(resp, "user created", "");
                                }
                                else
                                {
                                    Response.Fail(resp,"cannot register user");
                                }
                            }
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
                        string? token = database.AuthenticateUser(username, password);
                        if (token != null)
                        {
                            Response.Success(resp, "user logged", token);
                        }
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
            
            string connectionString = config.GetValue<String>("MongoDB");
            Database database = new Database(connectionString);
            // Create a Http server and start listening for incoming connections
            string url = "http://*:" + config.GetValue<String>("Port") + "/";
            Listener = new HttpListener();
            Listener.Prefixes.Add(url);
            Listener.Start();
            Console.WriteLine("Listening for connections on {0}", url);

            // Handle requests
            Task listenTask = HandleIncomingConnections(database);
            listenTask.GetAwaiter().GetResult();

            // Close the listener
            Listener.Close();
        }
    }
}
