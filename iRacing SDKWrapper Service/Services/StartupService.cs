using System.Net.WebSockets;
using System.Collections.Concurrent;


namespace iRacing_SDKWrapper_Service.Services
{
    public class StartupService: IHostedService
    {
        private readonly IWebSocketService _webSocketService;
        private readonly ISDKService _sdkService;
        private readonly IUserPreferencesService _userPreferencesService;
        private readonly ILogger _logger;

        private readonly UserPreferences _userPreferences;

        // will be used to queue messages that need to be sent to the client
        // that have a chance to be sent before the client is connected
        // e.g. SDKConnected or CarIsOnTrack will usually fire before the client
        // is connected to the web socket service
        private readonly ConcurrentQueue<(string, object)> _messageQueue = new();

        public StartupService(IWebSocketService webSocketService, ISDKService sdkService, IUserPreferencesService userPreferencesService, ILogger<StartupService> logger)
        {
            _webSocketService = webSocketService;
            _sdkService = sdkService;
            _userPreferencesService = userPreferencesService;
            _logger = logger;

            _userPreferences = _userPreferencesService.Load();
        }
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("StartupService.cs");
            _sdkService.SDKConnectedHandler += ProcessSDKConnection;
            _sdkService.SDKDisconnectedHandler += ProcessSDKDisconnection;
            _sdkService.StartSDK();

            _webSocketService.WebSocketOpened += ProcessWebSocketConnection;
            _webSocketService.WebSocketClosed += ProcessWebSocketDisconnection;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            // Implement any cleanup logic if necessary
            return Task.CompletedTask;
        }

        private void ProcessSDKConnection(object sender, SDKConnectionEventArgs e)
        {
            _sdkService.TelemetryUpdated += ProcessTelemetry;

            _logger.LogInformation($"QUEUEING game-opened MESSAGE");
            _messageQueue.Enqueue(("game-opened", "game opened"));
        }
        private async void ProcessSDKDisconnection(object sender, SDKConnectionEventArgs e)
        {
            _logger.LogInformation("SDK disconnected");

            _sdkService.TelemetryUpdated -= ProcessTelemetry;
            _webSocketService.SendMessage("game-closed", "game closed");
        }
        private void ProcessTelemetry(object sender, TelemetryUpdatedEventArgs e)
        {
            // queue the message for special processing
            if (e.QueueMessage)
            {
                _logger.LogInformation($"QUEUEING {e.Name} {e.Value} MESSAGE");
                _messageQueue.Enqueue((e.Name, e.Value));
            }
            else
            {
                bool sentSuccessfully = _webSocketService.SendMessage(e.Name, e.Value).Result;
            }
        }

        private void ProcessWebSocketConnection(object sender, WebSocketConnectedEventArgs e)
        {
            _logger.LogInformation("ProcessWebSocketConnection event in StartupService. WebSocket State: " + e.WsState);

            if (e.WsState == WebSocketState.Open)
            {
                // loop to continuously process queued messages. These messages are important events that UIs can build on to trigger
                // events such as enabling/disabling a window or some other event
                // queued messages will have a delay before processing to reduce CPU load, so time sensitive messages such as inputs should
                // not be queued. Good candidates for queueing are on-track/off-track events since these are rare but important for the UIs
                // to toggle themselves on or off so that they don't cover the settings menu or other important UI elements
                // also should significantly reduce CPU usage since the UIs will not be constantly polling the SDK for updates
                Task.Run(async () =>
                {
                    while (true)
                    {
                        if (_messageQueue.TryDequeue(out var message))
                        {
                            _logger.LogInformation($"Dequeued message: {message}");
                            _webSocketService.SendMessage(message.Item1, message.Item2);

                            // reduces polling update frequency to 1hz when user's car is not on track to reduce CPU usage
                            // this logic is not sufficient to handle the case where the user is using replay mode
                            // TODO: add a check for replay mode
                            if (message.Item1 == "is-on-track" && (bool)message.Item2 == true)
                            {
                                _logger.LogInformation($"Car is on track. Setting telemetry update frequency from user preference. {_userPreferences.TelemetryUpdateFrequency}");
                                _sdkService.ChangeTelemetryUpdateFrequency(_userPreferences.TelemetryUpdateFrequency);
                            }
                            else if (message.Item1 == "is-on-track" && (bool)message.Item2 == false)
                            {
                                _sdkService.ChangeTelemetryUpdateFrequency(1);
                            }
                        }
                        // Wait some time before trying to dequeue again.
                        await Task.Delay(1500);
                    }
                });
            }
        }

        private void ProcessWebSocketDisconnection(object sender, WebSocketClosedEventArgs e)
        {
            _logger.LogInformation("ProcessWebSocketDisconnection event in StartupService. CloseStatusDescription: " + e.CloseStatusDescription);
        }
    }
}
