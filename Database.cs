using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace door;

public class Database
{
    public static IMongoCollection<User> Collection;
    public Database(string connectionString)
    {
        // Connect to the MongoDB Database
        MongoClientSettings settings = MongoClientSettings.FromConnectionString(connectionString);
        MongoClient client = new MongoClient(settings);
        IMongoDatabase database = client.GetDatabase("UsersDB");
        BsonClassMap.RegisterClassMap<User>();
        Collection = database.GetCollection<User>("users");
        Console.WriteLine("Database connected");    
    }

    public bool UserExists(string username)
    {
        FilterDefinition<User> userfilter = Builders<User>.Filter.Eq("Username", username);
        User userdocument = Collection.Find(userfilter).FirstOrDefault();
        
        if (userdocument != null)
        {
            return true;
        }
        return false;
    }

    public bool TicketIsValid(string ticket)
    {
        FilterDefinition<User> ticketfilter = Builders<User>.Filter.Eq("Ticket", ticket);
        User ticketdocument = Collection.Find(ticketfilter).FirstOrDefault();

        if (ticketdocument != null && ticketdocument.TicketCount >= 1)
        {
            return true;
        }
        return false;
    }

    public string? AuthenticateUser(string username, string password)
    {
        FilterDefinition<User> filter = Builders<User>.Filter.Eq("Username", username);
        User userdocument = Collection.Find(filter).FirstOrDefault();
        if (userdocument != null)
        {
            string hashedpassword = HashTools.HashString(password);
            
            if (userdocument.Password == hashedpassword)
            {
                string? token = WebToken.GenerateToken(userdocument._id.ToString());
                return token;
            }
            return null;
        }
        return null;
    }

    public bool RegisterUser(string username, string password, string ticket)
    {
        var updateTicketFilter = Builders<User>.Filter.Eq("Ticket", ticket);
        var updateTicketCount = Builders<User>.Update.Inc("TicketCount", -1);
        var updateResult = Collection.UpdateOne(updateTicketFilter, updateTicketCount);
        if (updateResult.MatchedCount == 1 && updateResult.ModifiedCount == 1)
        {
            string hashedpassword = HashTools.HashString(password);
            string newticket = Ticket.GenTicket(username);
            User newuser = new User(username, hashedpassword, newticket);
            Collection.InsertOne(newuser);
            return true;
        }

        return false;
    }
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
}
            