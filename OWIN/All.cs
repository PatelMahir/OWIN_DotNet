using Microsoft.AspNetCore.Owin;
using System.Globalization;
using System.Net.WebSockets;
using System.Text;
namespace OWIN
{
    public class All
    {
        public Task OwinHello(IDictionary<string, object> enviroment)
        {
            string responseText = "hello world via owin";
            byte[] responseBytes = Encoding.UTF8.GetBytes(responseText);
            var responseStream = (Stream)enviroment["owin.ResponseBody"];
            var responseHeaders = (IDictionary<string, string[]>)
                enviroment["owin.ResponseHeader"];
            responseHeaders["Content-length"] = new string[]
            {
                responseBytes.Length.ToString(
                    CultureInfo.InvariantCulture)};
            responseHeaders["content-type"] = new string[]
                {"text/plain"};
            return responseStream.WriteAsync(
                responseBytes, 0, responseBytes.Length);
        }
        public void Configure(IApplicationBuilder app)
        {
            app.UseOwin(pipeline =>
            {
                pipeline(next =>
                {
                    return async environment =>
                    {
                        await next(environment);
                    };
                });
            });
        }
    }
    public class Startup
    {
        private HttpContext HttpContext;
        public void Configure(IApplicationBuilder app)
        {
            app.Use(async (context, next) =>
            {
                if (context.WebSockets.IsWebSocketRequest)
                {
                    WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();
                    await EchoWebSocket(webSocket);
                }
                else
                {
                    await next();
                }
            });
            app.Run(context =>
            {
                return context.Response.WriteAsync("hello");
            });
        }
        private async Task EchoWebSocket(WebSocket webSocket)
        {
            byte[] buffer= new byte[1024];
            WebSocketReceiveResult received=await
                webSocket.ReceiveAsync(new ArraySegment
                <byte>(buffer),CancellationToken.None);
            while(!webSocket.CloseStatus.HasValue)
            {
                await webSocket.SendAsync(new ArraySegment<byte>
                    (buffer,0,received.Count),
                    received.MessageType
                    ,received.EndOfMessage,
                    CancellationToken.None);
                received = await webSocket.ReceiveAsync(new
                    ArraySegment<byte>(buffer),
                    CancellationToken.None);
            }
            await webSocket.CloseAsync(
                webSocket.CloseStatus.Value,
            webSocket.CloseStatusDescription, 
            CancellationToken.None);
            var environment = new OwinEnvironment(HttpContext);
            var features=new OwinFeatureCollection(environment);
        }
    }
}