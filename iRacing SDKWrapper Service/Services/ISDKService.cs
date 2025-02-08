using iRacingSdkWrapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;

namespace iRacing_SDKWrapper_Service.Services
{
    public interface ISDKService
    {
        void StartSDK();
        void SDKTelemetryUpdated(object sender, SdkWrapper.TelemetryUpdatedEventArgs e);
        event EventHandler<TelemetryUpdatedEventArgs> TelemetryUpdated;
        event EventHandler<SDKConnectionEventArgs> SDKConnectedHandler;
        event EventHandler<SDKConnectionEventArgs> SDKDisconnectedHandler;
        void ChangeTelemetryUpdateFrequency(int frequency);
    }
}
