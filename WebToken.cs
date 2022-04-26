using System.Text;
using System.Security.Cryptography;
using Newtonsoft.Json.Linq;

namespace door;
public static class WebToken
{
    private static readonly IConfigurationRoot config =
    new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json", true)
        .AddEnvironmentVariables()
        .Build();
                
    // secret key, stored in an environment variable
    private static readonly string secretKey = config.GetValue<String>("secretKey") ?? "";
    
    // number of seconds until the token expires, if 0 the token never expires
    // stored in an environment variable
    private static readonly uint expireDelay = UInt32.Parse(config.GetValue<String>("expireDelay") ?? "0");

    // the encryption algorithm
    private static readonly string algorithm = "HS256";
    
    
    // returns an empty string if token is invalid
    // otherwise returns the username in the token
    public static string GetIdFromToken(string token)
    {
        // splits the token into the header, the payload and the signature
        string[] tokenParts = token.Split('.');

        // verifies that the token has 3 parts
        if (tokenParts.Length != 3)
        {
            return "";
        }

        // decode token from base 64
        // the 3 parts of the token are
        // encoded for obfuscation
        
        tokenParts[2] = DecodeBase64(tokenParts[2]);

        // verifies that token hasn't been altered
        // with the signature field
        if (!(Encoding.ASCII.GetString(
                HMACSHA256.HashData(
                    Encoding.ASCII.GetBytes(secretKey), 
                    Encoding.ASCII.GetBytes(tokenParts[0] + tokenParts[1]))).Equals(tokenParts[2])))
        {
            return "";
        }

        tokenParts[0] = DecodeBase64(tokenParts[0]);
        tokenParts[1] = DecodeBase64(tokenParts[1]);
        
        // put the content of the payload in
        // the payload variable
        JObject payload = JObject.Parse(tokenParts[1]);

        // parse exp date, 0 if not present
        JToken expToken = payload.GetValue("exp");
        uint exp = 0;
        if (expToken != null)
        {
            exp = UInt32.Parse(expToken.ToString());
        }

        // verifies if exp date isn't reached
        if (exp != 0 && exp<GetCurrentUnixTime())
        {
            return "";
        }

        // parse username, empty string if not present
        JToken id = payload.GetValue("id");
        string StrId = "";
        if (id != null)
        {
            StrId = id.ToString();
        }

        return StrId;
    }

    private static string EncodeBase64(string s)
    {
        return Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes(s));
    }
    
    private static string DecodeBase64(string s)
    {
        return Encoding.UTF8.GetString(Convert.FromBase64String(s));
    }

    private static uint GetCurrentUnixTime()
    {
        return (uint) DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
    }
    
    public static string GenerateToken(string id)
    {
        // generate header json
        JObject headerJson = new JObject(
        new JProperty("algo",algorithm),
        new JProperty("type","JWT")
            );
        
        // generate payload json
        JObject payloadJson = new JObject(
            new JProperty("id",id),
            new JProperty("exp",expireDelay==0?"0":(GetCurrentUnixTime()+expireDelay).ToString())
        );

        string header = EncodeBase64(headerJson.ToString());
        string payload = EncodeBase64(payloadJson.ToString());
        string signature = EncodeBase64(
                Encoding.ASCII.GetString(
                    HMACSHA256.HashData(
                        Encoding.ASCII.GetBytes(secretKey), 
                        Encoding.ASCII.GetBytes(header + payload))));

        return (header+"."+payload+"."+signature);
    }
}
