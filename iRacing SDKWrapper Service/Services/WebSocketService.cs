using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Text.Json;
using System.Collections.Concurrent;

namespace iRacing_SDKWrapper_Service.Services
{
    public class WebSocketConnectedEventArgs : EventArgs
    {
        public WebSocketState WsState { get; }

        public WebSocketConnectedEventArgs(WebSocketState wsState)
        {
            WsState = wsState;
        }
    }
    public class WebSocketClosedEventArgs : EventArgs
    {
        public string CloseStatusDescription { get; }

        public WebSocketClosedEventArgs(string closeStatusDescription)
        {
            CloseStatusDescription = closeStatusDescription;
        }
    }
    public class WebSocketService: IWebSocketService
    {
        private WebSocket ws;
        CancellationToken cancellationToken;
        TaskCompletionSource<object> tcs;

        public event EventHandler<WebSocketConnectedEventArgs> WebSocketOpened;
        protected virtual void OnWebSocketOpenedEvent(WebSocketConnectedEventArgs e)
        {
            _ = SendMessage("sdk-connected", "You are conencted to the SDK Service. Waiting for iRacing to open.");
            EventHandler <WebSocketConnectedEventArgs> handler = WebSocketOpened;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        public event EventHandler<WebSocketClosedEventArgs> WebSocketClosed;
        protected virtual void OnWebSocketClosedEvent(WebSocketClosedEventArgs e)
        {
            _ = CloseSocket();
            EventHandler<WebSocketClosedEventArgs> handler = WebSocketClosed;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        private async Task CloseSocket()
        {
            if (ws != null)
            {
                if (ws.State == WebSocketState.Open)
                {
                    await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Connection closed", cancellationToken);
                }
                ws.Dispose();
                ws = null;
                tcs.SetResult(null);
            }
        }

        public async void AddSocket(WebSocket ws, TaskCompletionSource<object> tcs)
        {
            if (ws != null)
            {
                await CloseSocket();
            }

            this.ws = ws;
            this.tcs = tcs;

            OnWebSocketOpenedEvent(new WebSocketConnectedEventArgs(ws.State));
            ReceiveMessages();
        }

        public async Task<bool> SendMessage(string name, object value)
        {
            if (ws == null)
            {
                return false;
            }

            var messageObject = new { name = name, value = value };

            string message = JsonSerializer.Serialize(messageObject);
            var bytes = Encoding.UTF8.GetBytes(message);
            var arraySegment = new ArraySegment<byte>(bytes, 0, bytes.Length);

            if (ws.State == WebSocketState.Open)
            {
                await ws.SendAsync(arraySegment, WebSocketMessageType.Text, true, cancellationToken);
            }

            else if (ws.State == WebSocketState.Aborted)
            {
                Console.WriteLine("SendMessage() else if (ws.State == WebSocketState.Aborted)");
                OnWebSocketClosedEvent(new WebSocketClosedEventArgs("Connection aborted"));
            }

            return true;
        }

        private async Task ReceiveMessages()
        {
            var buffer = new byte[1024 * 4];
            while (ws.State == WebSocketState.Open)
            {
                var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    Console.WriteLine("ReceiveMessages()");
                    OnWebSocketClosedEvent(new WebSocketClosedEventArgs($"Connection closed: {result.CloseStatus}, {result.CloseStatusDescription}"));
                }
                else
                {
                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    Console.WriteLine($"Received message: {message}");
                    // Handle the received message as needed
                }
            }
        }
    }
}
