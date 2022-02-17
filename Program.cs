using System.Net;
using Newtonsoft.Json;


namespace door;

class HttpServer
{
        
    public static HttpListener? Listener;

    public static async Task HandleIncomingConnections(Database database)
    {
        while (true)
        {
            HttpListenerContext ctx = await Listener?.GetContextAsync()!;

            HttpListenerRequest req = ctx.Request;
            HttpListenerResponse resp = ctx.Response;
                
            Console.WriteLine(req.HttpMethod);
            Console.WriteLine(req.Url?.ToString());
            Console.WriteLine(req.UserHostName);
            Console.WriteLine(req.UserAgent);

            if (req.HttpMethod == "POST" && req.Url?.AbsolutePath == "/register")
            {
                StreamReader reader = new StreamReader(req.InputStream);
                string bodyString = await reader.ReadToEndAsync();
                dynamic body = JsonConvert.DeserializeObject(bodyString)!;
                    
                string username = (string) body.username;
                string password = (string) body.password;
                string ticket = (string) body.ticket;
                    
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

            if (req.HttpMethod == "POST" && req.Url?.AbsolutePath == "/login")
            {
                StreamReader reader = new StreamReader(req.InputStream);
                string bodyString = await reader.ReadToEndAsync();
                dynamic body = JsonConvert.DeserializeObject(bodyString)!;
                    
                string username = body.username;
                string password = body.password;
                    
                if (!String.IsNullOrEmpty(username) && !String.IsNullOrEmpty(password) &&
                    !String.IsNullOrWhiteSpace(username) && !String.IsNullOrWhiteSpace(password))
                {
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