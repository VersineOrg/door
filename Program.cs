using System.Net;
using MongoDB.Bson;
using Newtonsoft.Json;
using VersineUser;

namespace door;

class HttpServer
{
    public static HttpListener? listener;

    public static async Task HandleIncomingConnections(EasyMango.EasyMango database, WebToken.WebToken jwt)
    {
        while (true)
        {
            HttpListenerContext ctx = await listener?.GetContextAsync()!;

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
                    password = ((string) body.password).Trim();
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
                                string userTicket = Ticket.GenTicket(username);
                                User newUser = new User(username, "", userTicket);

                                // Register new user
                                if (database.AddSingleDatabaseEntry(newUser.ToBson()))
                                {
                                    if (database.GetSingleDatabaseEntry("username", username,out BsonDocument newUserBson))
                                    {
                                        
                                        // id of the registered user
                                        string userId = newUserBson.GetElement("_id").Value.AsObjectId.ToString();
                                        
                                        // change ticket count of the ticket owner
                                        ticketOwnerBson.SetElement(new BsonElement("ticketCount", ticketCount - 1));
                                        database.ReplaceSingleDatabaseEntry("_id",
                                            ticketOwnerBson.GetElement("_id").Value.AsObjectId,
                                            ticketOwnerBson);
                                        
                                        // Hash password and add salt
                                        password = HashTools.HashString(userId, username);
                                        
                                        // set new user password
                                        newUser.password = password;
                                        database.ReplaceSingleDatabaseEntry("_id",
                                            userId,
                                            newUser.ToBson());

                                        // response
                                        Response.Success(resp, "user created", jwt.GenerateToken(userId));
                                    }
                                    else
                                    {
                                        database.RemoveSingleDatabaseEntry("username", username);
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
                    password = ((string) body.password).Trim();
                }
                catch
                {
                    username = "";
                    password = "";
                }

                if (!(String.IsNullOrEmpty(username) || String.IsNullOrEmpty(password)))
                {
                    
                    // search user by username
                    if (database.GetSingleDatabaseEntry("username", username, out BsonDocument userDocument))
                    {

                        string userId = userDocument.GetElement("_id").Value.AsObjectId.ToString();
                        password = HashTools.HashString(password, userId);
                        
                        if (string.Equals(userDocument.GetElement("password").Value.AsString,password))
                        {
                            Response.Success(resp, "logged in", jwt.GenerateToken(userId));
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
                
                string token;
                
                try
                {
                    token = ((string) body.token).Trim();
                }
                catch
                {
                    token = "";
                }

                if (!String.IsNullOrEmpty(token))
                {
                    string id = jwt.GetIdFromToken(token);
                    if (id=="")
                    {
                        Response.Fail(resp, "invalid token");
                    }
                    else
                    {
                        if (database.GetSingleDatabaseEntry("_id", new BsonObjectId(new ObjectId(id)),
                                out BsonDocument userBsonDocument))
                        {
                            Response.Success(resp, "logged in",id);
                        }
                        else
                        {
                            Response.Fail(resp, "user no longer exists");
                        }
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
        
        // Load config file
        IConfigurationRoot config =
            new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", true)
                .AddEnvironmentVariables()
                .Build();
            
        // Get values from config file
        string connectionString = config.GetValue<String>("connectionString");
        string databaseNAme = config.GetValue<String>("databaseName");
        string collectionName = config.GetValue<String>("collectionName");
        string secretKey = config.GetValue<String>("secretKey");
        uint expireDelay = config.GetValue<uint>("expireDelay");

        // Json web token
        WebToken.WebToken jwt = new WebToken.WebToken(secretKey,expireDelay);
        
        // Create a new EasyMango database
        EasyMango.EasyMango database = new EasyMango.EasyMango(connectionString,databaseNAme,collectionName);

        // Create a Http server and start listening for incoming connections
        string url = "http://*:" + config.GetValue<String>("Port") + "/";
        listener = new HttpListener();
        listener.Prefixes.Add(url);
        listener.Start();
        Console.WriteLine("Listening for connections on {0}", url);

        // Handle requests
        Task listenTask = HandleIncomingConnections(database, jwt);
        listenTask.GetAwaiter().GetResult();
        
        // Close the listener
        listener.Close();
    }
}
