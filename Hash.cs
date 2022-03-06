using System.Security.Cryptography;
using System.Text;
using Microsoft.VisualBasic;

namespace door;

public class HashTools
{
    public static string HashString(string content, string salt)
    {
        //add salt
        content = Strings.StrReverse(salt) + Strings.StrReverse(content) + content + "KISS";
        
        StringBuilder hashedcontentbuilder = new StringBuilder();
        using (SHA256 hash = SHA256.Create()) {
            Encoding enc = Encoding.UTF8;
            Byte[] result = hash.ComputeHash(enc.GetBytes(content));
            foreach (Byte b in result)
                hashedcontentbuilder.Append(b.ToString("x2"));
        }
        string hashedcontent =  hashedcontentbuilder.ToString();
        return hashedcontent;
    }
}