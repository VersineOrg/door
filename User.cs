using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace door;

public class User
{
    public String Username { get; set; }
    public String Password { get; set; }
    public String Ticket { get; set; }
    public Int32 TicketCount { get; set; }
    public String Avatar { get; set; }
    public String Bio { get; set; }
    public String Banner { get; set; }
    public String Color { get; set; }
    public List<string> Friends { get; set; }
    public List<string> Circles { get; set; }
    
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
        this.Friends = new List<string>();
        this.Circles = new List<string>();
    }

    public User(BsonDocument document)
    {
        this.Username = document.GetElement("username").Value.AsString;
        this.Password = document.GetElement("password").Value.AsString;;
        this.Ticket = document.GetElement("ticket").Value.AsString;;
        this.TicketCount = 10;
        this.Avatar = "https://i.imgur.com/k7eDNwW.jpg";
        this.Bio = "Hey, I'm using Versine!";
        this.Banner = "https://images7.alphacoders.com/421/thumb-1920-421957.jpg";
        this.Color = "28DBB7";
        this.Friends = new List<string>();
        this.Circles = new List<string>();
    }

    public BsonDocument ToBson()
    {
        BsonDocument result = new BsonDocument(
            new BsonElement("username",Username),
            new BsonElement("password",Password),
            new BsonElement("ticket",Ticket),
            new BsonElement("ticketCount",TicketCount),
            new BsonElement("avatar",Avatar),
            new BsonElement("bio",Bio),
            new BsonElement("banner",Banner),
            new BsonElement("color",Color),
            new BsonElement("friends",new BsonArray(Friends.AsEnumerable())),
            new BsonElement("circles",new BsonArray(Circles.AsEnumerable()))
        );
        return result;
    }
}    