using Microsoft.Extensions.Hosting;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.Collections.Concurrent;

namespace iRacing_SDKWrapper_Service.Services
{
    public class StartupService: IHostedService
    {
        private readonly IWebSocketService _webSocketService;
        private readonly ISDKService _sdkService;

        // will be used to queue messages that need to be sent to the client
        // that have a chance to be sent before the client is connected
        // e.g. SDKConnected or CarIsOnTrack will usually fire before the client
        // is connected to the web socket service
        private readonly ConcurrentQueue<(string, object)> _messageQueue = new();

        public StartupService(IWebSocketService webSocketService, ISDKService sdkService)
        {
            _webSocketService = webSocketService;
            _sdkService = sdkService;

        }
        public async Task StartAsync(CancellationToken cancellationToken)
        {
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

            Console.WriteLine($"QUEUEING game-opened MESSAGE");
            _messageQueue.Enqueue(("game-opened", "game opened"));
        }
        private async void ProcessSDKDisconnection(object sender, SDKConnectionEventArgs e)
        {
            Console.WriteLine("SDK disconnected");

            _sdkService.TelemetryUpdated -= ProcessTelemetry;
            _webSocketService.SendMessage("game-closed", "game closed");
        }
        private void ProcessTelemetry(object sender, TelemetryUpdatedEventArgs e)
        {
            // queue the message for special processing
            if (e.QueueMessage)
            {
                Console.WriteLine($"QUEUEING {e.Name} {e.Value} MESSAGE");
                _messageQueue.Enqueue((e.Name, e.Value));
            }
            else
            {
                bool sentSuccessfully = _webSocketService.SendMessage(e.Name, e.Value).Result;
            }
        }

        private void ProcessWebSocketConnection(object sender, WebSocketConnectedEventArgs e)
        {
            //_sdkService.TelemetryUpdated += ProcessTelemetry;
            Console.WriteLine("ProcessWebSocketConnection event in StartupService. WebSocket State: " + e.WsState);
            // periodically attempt to dequeue all queued messages. This covers the edge case for when
            // 
            if (e.WsState == WebSocketState.Open)
            {
                Task.Run(async () =>
                {
                    while (true)
                    {
                        Console.WriteLine("ATTEMPTING TO DEQUEUE MESSAGES");
                        if (_messageQueue.TryDequeue(out var message))
                        {
                            Console.WriteLine($"Dequeued message: {message}");
                            _webSocketService.SendMessage(message.Item1, message.Item2);

                            if (message.Item1 == "is-on-track" && (bool)message.Item2 == true)
                            {
                                _sdkService.ChangeTelemetryUpdateFrequency(20);
                            }
                            else if (message.Item1 == "is-on-track" && (bool)message.Item2 == false)
                            {
                                _sdkService.ChangeTelemetryUpdateFrequency(1);
                            }
                        }
                        await Task.Delay(5000); // Wait for 1 second before trying to dequeue again
                    }
                });
            }
        }

        private void ProcessWebSocketDisconnection(object sender, WebSocketClosedEventArgs e)
        {
            Console.WriteLine("ProcessWebSocketDisconnection event in StartupService. CloseStatusDescription: " + e.CloseStatusDescription);
            //_sdkService.TelemetryUpdated -= ProcessTelemetry;
        }
    }
}
