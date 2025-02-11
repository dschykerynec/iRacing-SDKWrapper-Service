using System.Text.Json;


namespace iRacing_SDKWrapper_Service.Services
{
    public class UserPreferences
    {
        public int TelemetryUpdateFrequency { get; set; } = 20; // times per second (Hz)
        public int PortNumber { get; set; } = 7125;
    }
    public interface IUserPreferencesService
    {
        UserPreferences Load();
        void Save(UserPreferences newPreferences);
    }
}
