using System.Net;
using MongoDB.Bson;
using Newtonsoft.Json;

namespace door;

class HttpServer
{
        
    public static HttpListener? Listener;

    public static async Task HandleIncomingConnections(EasyMango.EasyMango database)
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

                string username;
                string password;
                string ticket;
                try
                {
                    username = ((string) body.username).Trim();
                    password = (string) body.password;
                    ticket = ((string) body.ticket).Trim();
                }
                catch
                {
                    username = "";
                    password = "";
                    ticket = "";
                }

                if (!(String.IsNullOrEmpty(username) || String.IsNullOrEmpty(password) || String.IsNullOrEmpty(ticket)))
                {

                    // Verify username
                    if (!database.GetSingleDatabaseEntry("username", username, out BsonDocument nonExistentUser))
                    {

                        // Verify ticket
                        if (database.GetSingleDatabaseEntry("ticket", ticket, out BsonDocument ticketOwnerBson))
                        {
                            int ticketCount = ticketOwnerBson.GetElement("ticketCount").Value.AsInt32;
                            if ( ticketCount > 0)
                            {
                                // Hash password and add salt
                                password = HashTools.HashString(password, username);

                                string userTicket = Ticket.GenTicket(username);
                                User newUser = new User(username, password, userTicket);

                                // Register new user
                                if (database.AddSingleDatabaseEntry(newUser.ToBson()))
                                {
                                    if (database.GetSingleDatabaseEntry("username", username,out BsonDocument newUserBson))
                                    {
                                        // change ticket count
                                        ticketOwnerBson.SetElement(new BsonElement("ticketCount", ticketCount - 1));
                                        database.ReplaceSingleDatabaseEntry("_id",
                                            ticketOwnerBson.GetElement("_id").Value.AsObjectId,
                                            ticketOwnerBson);

                                        // response
                                        Response.Success(resp, "user created", WebToken.GenerateToken(newUserBson.GetElement("_id").Value.AsObjectId.ToString()));
                                    }
                                    else
                                    {
                                        Response.Fail(resp, "an error occured, please try again in a few minutes");
                                    }
                                }
                                else
                                {
                                    Response.Fail(resp, "an error occured, please try again in a few minutes");
                                }
                            }
                            else
                            {
                                Response.Fail(resp, "expired ticket");
                            }
                        }
                        else
                        {
                            Response.Fail(resp, "invalid ticket");
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
            else if (req.HttpMethod == "POST" && req.Url?.AbsolutePath == "/login")
            {
                StreamReader reader = new StreamReader(req.InputStream);
                string bodyString = await reader.ReadToEndAsync();
                dynamic body = JsonConvert.DeserializeObject(bodyString)!;


                string username;
                string password;
                try
                {
                    username = ((string) body.username).Trim();
                    password = (string) body.password;
                }
                catch
                {
                    username = "";
                    password = "";
                }

                if (!(String.IsNullOrEmpty(username) || String.IsNullOrEmpty(password)))
                {
                    password = HashTools.HashString(password, username);

                    // search user by username
                    if (database.GetSingleDatabaseEntry("username", username, out BsonDocument userDocument))
                    {
                        if (userDocument.GetElement("password").Value.AsString == password)
                        {
                            Response.Success(resp, "logged in", WebToken.GenerateToken(userDocument.GetElement("_id").Value.AsObjectId.ToString()));
                        }
                        else
                        {
                            Response.Fail(resp, "wrong username or password");
                        }
                    }
                    else
                    {
                        Response.Fail(resp, "user doesn't exist"); 
                    }
                }
                else
                {
                    Response.Fail(resp, "invalid body");
                }
            }
            else if (req.HttpMethod == "POST" && req.Url?.AbsolutePath == "/tokenlogin")
            {
                StreamReader reader = new StreamReader(req.InputStream);
                string bodyString = await reader.ReadToEndAsync();
                dynamic body = JsonConvert.DeserializeObject(bodyString)!;
                    
                string token = ((string) body.token).Trim();

                if (!String.IsNullOrEmpty(token))
                {
                    string id = WebToken.GetIdFromToken(token);
                    if (id=="")
                    {
                        Response.Fail(resp, "invalid token");
                    }
                    else
                    {
                        Response.Success(resp, "logged in","");
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
            // close response
            resp.Close();
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
            
        string connectionString = config.GetValue<String>("connectionString");
        string databaseNAme = config.GetValue<String>("databaseName");
        string collectionName = config.GetValue<String>("collectionName");

        
        // Create a new EasyMango database
        EasyMango.EasyMango database = new EasyMango.EasyMango(connectionString,databaseNAme,collectionName);
        
            
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
