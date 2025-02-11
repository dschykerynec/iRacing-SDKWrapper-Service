using Serilog;

using iRacing_SDKWrapper_Service.Services;

using Velopack;
using Velopack.Sources;
using Microsoft.Extensions.Logging.Configuration;

#if RELEASE
string logFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "iRacing-SDKWrapper-Service", "logs", "main.log");
#else
string logFilePath = Path.Combine(Environment.CurrentDirectory, "logs", "main.log");
#endif

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File(logFilePath, rollingInterval: RollingInterval.Day)
    .CreateLogger();


var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog();

// Register services
builder.Services.AddSingleton<ISDKService, SDKService>();
builder.Services.AddSingleton<IWebSocketService, WebSocketService>();
builder.Services.AddHostedService<StartupService>();
builder.Services.AddSingleton<IUserPreferencesService, UserPreferencesService>();

var app = builder.Build();
var logger = app.Services.GetRequiredService<ILogger<Program>>();
var userPreferencesService = app.Services.GetRequiredService<IUserPreferencesService>();

var userPreferences = userPreferencesService.Load();

logger.LogInformation("--------------------------------------------------------------------------------------");
Log.Information("Loaded user preferences: {@UserPreferences}", userPreferences);

builder.WebHost.UseUrls("http://localhost:7125");

VelopackApp.Build().Run();
async Task UpdateMyApp()
{
    var mgr = new UpdateManager(new GithubSource("https://github.com/dschykerynec/iRacing-SDKWrapper-Service", "", false), logger:logger);

    if (!mgr.IsInstalled)
    {
        return;
    }

    logger.LogInformation($"app version: {mgr.CurrentVersion}");

    // check for new version
    var newVersion = await mgr.CheckForUpdatesAsync();
    if (newVersion == null)
        return; // no update available

    // download new version
    await mgr.DownloadUpdatesAsync(newVersion);

    // install new version and restart app
    try
    {
        mgr.ApplyUpdatesAndRestart(newVersion);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to apply updates");
        Environment.Exit(0); // Close the application
    }
}
await UpdateMyApp();

var webSocketOptions = new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromSeconds(15)
};
app.UseWebSockets(webSocketOptions);

app.Map("/sdk", async context =>
{
    logger.LogInformation("app.Map(/sdk)");
    if (context.WebSockets.IsWebSocketRequest)
    {
        using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        var socketFinishedTcs = new TaskCompletionSource<object>();

        var webSocketService = context.RequestServices.GetRequiredService<IWebSocketService>();
        webSocketService.AddSocket(webSocket, socketFinishedTcs);

        await socketFinishedTcs.Task;
    }
    else
    {
        logger.LogInformation("failed to connect");
        context.Response.StatusCode = 400;
    }
});

await app.RunAsync();