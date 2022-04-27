using MongoDB.Bson;

namespace door;

public class User
{
    public String username;
    public String password;
    public String ticket;
    public Int32 ticketCount;
    public String avatar;
    public String bio;
    public String banner;
    public String color;
    public List<BsonObjectId> friends;
    public List<BsonObjectId> incomingFriendRequests;
    public List<BsonObjectId> outgoingFriendRequests;
    
    public User(string username, string password, string ticket)
    {
        this.username = username;
        this.password = password;
        this.ticket = ticket;
        ticketCount = 10;
        avatar = "https://i.imgur.com/k7eDNwW.jpg";
        bio = "Hey, I'm using Versine!";
        banner = "https://images7.alphacoders.com/421/thumb-1920-421957.jpg";
        color = "28DBB7";
        friends = new List<BsonObjectId>();
        incomingFriendRequests = new List<BsonObjectId>();
        outgoingFriendRequests = new List<BsonObjectId>();
    }

    public User(BsonDocument document)
    {
        username = document.GetElement("username").Value.AsString;
        password = document.GetElement("password").Value.AsString;;
        ticket = document.GetElement("ticket").Value.AsString;;
        ticketCount = 10;
        avatar = "https://i.imgur.com/k7eDNwW.jpg";
        bio = "Hey, I'm using Versine!";
        banner = "https://images7.alphacoders.com/421/thumb-1920-421957.jpg";
        color = "28DBB7";
        friends = new List<BsonObjectId>();
        incomingFriendRequests = new List<BsonObjectId>();
        outgoingFriendRequests = new List<BsonObjectId>();
    }

    public BsonDocument ToBson()
    {
        return new BsonDocument(
            new BsonElement("username",username),
            new BsonElement("password",password),
            new BsonElement("ticket",ticket),
            new BsonElement("ticketCount",ticketCount),
            new BsonElement("avatar",avatar),
            new BsonElement("bio",bio),
            new BsonElement("banner",banner),
            new BsonElement("color",color),
            new BsonElement("friends",new BsonArray(friends)),
            new BsonElement("incomingFriendRequests",new BsonArray(incomingFriendRequests)),
            new BsonElement("outgoingFriendRequests",new BsonArray(outgoingFriendRequests))
        );
    }
}
