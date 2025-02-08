using System.Net;
using System.Text;
using System.Net.WebSockets;

using iRacing_SDKWrapper_Service.Services;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://localhost:7125");

// Register services
builder.Services.AddSingleton<ISDKService, SDKService>();
builder.Services.AddSingleton<IWebSocketService, WebSocketService>();
builder.Services.AddHostedService<StartupService>();

var app = builder.Build();

var webSocketOptions = new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromSeconds(15)
};
app.UseWebSockets(webSocketOptions);

app.Map("/sdk", async context =>
{
    Console.WriteLine("app.Map(/sdk) called");
    if (context.WebSockets.IsWebSocketRequest)
    {
        Console.WriteLine("WEB SOCKET REQUEST");
        using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        var socketFinishedTcs = new TaskCompletionSource<object>();

        var webSocketService = context.RequestServices.GetRequiredService<IWebSocketService>();
        webSocketService.AddSocket(webSocket, socketFinishedTcs);

        await socketFinishedTcs.Task;
    }
    else
    {
        context.Response.StatusCode = 400;
    }
});

await app.RunAsync();