using SimpleHttp;
using System.Threading;

namespace SimpleHttpDemo
{
    class Program
    {
        static void Main(string[] args)
        {
            Route.Add("/", (req, res, props) =>
            {
                res.AsText("Welcome to the Simple Http Server");
            });

            Route.Add("/users/{id}", (req, res, props) =>
            {
                res.AsText($"You have requested user #{props["id"]}");
            }, "POST");

            Route.Add("/header", (req, res, props) =>
            {
                res.AsText($"Value of my-header is: {req.Headers["my-header"]}");
            });

            HttpServer.ListenAsync(
                    1337,
                    CancellationToken.None,
                    Route.OnHttpRequestAsync
                )
                .Wait();
        }
    }
}
