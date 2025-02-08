using System.Collections.Generic;

using iRacingSdkWrapper;
//using iRacing_SDKWrapper_Service.Events;

namespace iRacing_SDKWrapper_Service.Services
{
    public class TelemetryUpdatedEventArgs : EventArgs
    {
        public string Name { get; }
        public object Value { get; }

        // this should be used for important telemetry events that need to be sent to the client
        // these events may happen before the client is loaded and connected to the web socket
        // e.g. when the car enters the track or garage for the first time, that telemetry event
        // can be used to enable/disable the client UI
        public bool QueueMessage { get; }

        public TelemetryUpdatedEventArgs(string name, object value, bool queueMessage = false)
        {
            Name = name;
            Value = value;
            QueueMessage = queueMessage;
        }
    }

    public class SDKConnectionEventArgs : EventArgs
    {
        public string ConnectionStatus { get; }

        public SDKConnectionEventArgs(string connectionStatus)
        {
            ConnectionStatus = connectionStatus;
        }
    }

    public class SDKService: ISDKService
    {
        static SdkWrapper? wrapper;

        private int lapNum { get; set; }
        private bool isOnTrack { get; set; }

        public event EventHandler<TelemetryUpdatedEventArgs> TelemetryUpdated;
        protected virtual void OnTelemetryUpdatedEvent(TelemetryUpdatedEventArgs e)
        {
            EventHandler<TelemetryUpdatedEventArgs> handler = TelemetryUpdated;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        public event EventHandler<SDKConnectionEventArgs> SDKConnectedHandler;
        protected virtual void OnSDKConnectedEvent(SDKConnectionEventArgs e)
        {
            EventHandler<SDKConnectionEventArgs> handler = SDKConnectedHandler;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        public event EventHandler<SDKConnectionEventArgs> SDKDisconnectedHandler;
        protected virtual void OnSDKDisconnectedEvent(SDKConnectionEventArgs e)
        {
            EventHandler<SDKConnectionEventArgs> handler = SDKDisconnectedHandler;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        public async void StartSDK()
        {
            // Create a new instance of the SdkWrapper object
            wrapper = new SdkWrapper();

            // Tell it to raise events on the current thread (don't worry if you don't know what a thread is)
            wrapper.EventRaiseType = SdkWrapper.EventRaiseTypes.CurrentThread;
            // Only update telemetry 1 time(s) per second
            wrapper.TelemetryUpdateFrequency = 1;

            // Attach some useful events so you can respond when they get raised
            wrapper.Disconnected += SDKDisconnected;
            wrapper.Connected += SDKConnected;

            this.lapNum = 0;

            wrapper.Start();
        }

        public void SDKTelemetryUpdated(object sender, SdkWrapper.TelemetryUpdatedEventArgs e)
        {
            // if we just went on track, let client know that it can start receiving telemetry
            if (!isOnTrack && e.TelemetryInfo.IsOnTrack.Value)
            {
                isOnTrack = true;
                OnTelemetryUpdatedEvent(new TelemetryUpdatedEventArgs("is-on-track", true, true));
            }
            // if we just got off track, let client know that it can stop receiving telemetry
            else if (!e.TelemetryInfo.IsOnTrack.Value && isOnTrack)
            {
                isOnTrack = false;
                OnTelemetryUpdatedEvent(new TelemetryUpdatedEventArgs("is-on-track", false, true));
            }

            var telemetryDict= new Dictionary<string, object>();

            var newLapValue = e.TelemetryInfo.Lap.Value;

            // make sure we don't report the out lap, first lap, or the same lap twice
            if (lapNum < newLapValue && newLapValue > 1)
            {
                lapNum = newLapValue;

                var lapTimeObject = wrapper.GetTelemetryValue<float>("LapLastLapTime");
                var lastLapTimeValue = lapTimeObject.Value;
                telemetryDict.Add("LastLapTimeValue", lastLapTimeValue);
            }

            var throttleInputValue = e.TelemetryInfo.Throttle.Value;
            telemetryDict.Add("ThrottleInputValue", throttleInputValue);

            var brakeInputValue = e.TelemetryInfo.Brake.Value;
            telemetryDict.Add("BrakeInputValue", brakeInputValue);

            var steeringInputValue = e.TelemetryInfo.SteeringWheelAngle.Value;
            telemetryDict.Add("SteeringInputValue", steeringInputValue);

            var steeringInputUnit = e.TelemetryInfo.SteeringWheelAngle.Unit;
            telemetryDict.Add("SteeringInputUnit", steeringInputUnit);

            var speedValue = e.TelemetryInfo.Speed.Value;
            telemetryDict.Add("SpeedValue", speedValue);

            var speedUnit = e.TelemetryInfo.Speed.Unit;
            telemetryDict.Add("SpeedUnit", speedUnit);

            var gearValue = e.TelemetryInfo.Gear.Value;
            telemetryDict.Add("GearValue", gearValue);

            
            OnTelemetryUpdatedEvent(new TelemetryUpdatedEventArgs("TelemetryDictionary", telemetryDict));
        }

        public async void SDKConnected(object sender, EventArgs e)
        {
            Console.WriteLine("iRacing SDK Connected! :)");
            wrapper.TelemetryUpdated += SDKTelemetryUpdated;
            wrapper.SessionInfoUpdated += SessionInfoUpdated;
            OnSDKConnectedEvent(new SDKConnectionEventArgs("Connected"));
        }

        private void SessionInfoUpdated(object? sender, SdkWrapper.SessionInfoUpdatedEventArgs e)
        {
        }

        public async void SDKDisconnected(object sender, EventArgs e)
        {
            Console.WriteLine("iRacing SDK disconnected!");

            wrapper.TelemetryUpdated -= SDKTelemetryUpdated;
            wrapper.SessionInfoUpdated -= SessionInfoUpdated;

            OnSDKDisconnectedEvent(new SDKConnectionEventArgs("Disconnected"));
        }

        public void ChangeTelemetryUpdateFrequency(int frequency)
        {
            wrapper.TelemetryUpdateFrequency = frequency;
        }
    }
}
