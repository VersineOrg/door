using System.Net;
using System.Text;
using Newtonsoft.Json;

namespace door;

public class ResponseFormat
{
    public String status { get; set; }
    public String message { get; set; }
    public String data { get; set;}
}
public class Response
{
    public static void Success(HttpListenerResponse resp, string message, string data)
    {
        ResponseFormat response = new ResponseFormat
        {
            status = "success",
            message = message,
            data = data
        };
        string jsonString = JsonConvert.SerializeObject(response);
        byte[] buffer = Encoding.UTF8.GetBytes(jsonString);

        try
        {
            //resp.StatusCode = 200;
            resp.ContentLength64 = buffer.LongLength;
            resp.ContentType = "application/json";
            resp.ContentEncoding = Encoding.UTF8;


            // Write out to the response stream (asynchronously), then close it
            resp.OutputStream.Write(buffer, 0, buffer.Length);
            //resp.Close();        
        }
        catch
        {
            
        }
        
    }
    public static async void Fail(HttpListenerResponse resp, string message)
    {
        ResponseFormat response = new ResponseFormat
        {
            status = "fail",
            message = message
        };
        string jsonString = JsonConvert.SerializeObject(response);
        byte[] buffer = Encoding.UTF8.GetBytes(jsonString);



        try
        {
            resp.ContentLength64 = buffer.Length;
            System.IO.Stream output = resp.OutputStream;
            resp.ContentType = "application/json";
            await output.WriteAsync(buffer, 0, buffer.Length);

            // You must close the output stream.
            output.Close();
        }
        catch
        {
           
        }

        
        
    }
}