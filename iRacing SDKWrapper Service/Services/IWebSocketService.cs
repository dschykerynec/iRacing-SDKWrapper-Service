using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;

namespace iRacing_SDKWrapper_Service.Services
{
    public interface IWebSocketService
    {
        void AddSocket(WebSocket ws, TaskCompletionSource<object> tcs);
        Task<bool> SendMessage(string name, object value);
        //Task ConnectToUI();
        //Task ResetUIConnection();
        event EventHandler<WebSocketClosedEventArgs> WebSocketClosed;
        event EventHandler<WebSocketConnectedEventArgs> WebSocketOpened;

    }
}
