using System.Text.Json;

namespace iRacing_SDKWrapper_Service.Services
{
    public class UserPreferencesService : IUserPreferencesService
    {
        #if RELEASE     
        public string FilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "iRacing-SDKWrapper-Service", "userPreferences.json");
        #else       
        public string FilePath = Path.Combine(Environment.CurrentDirectory, "userPreferences.json");
        #endif      

        private readonly ILogger<IUserPreferencesService> _logger;

        public UserPreferencesService(ILogger<IUserPreferencesService> logger)
        {
            _logger = logger;
        }

        public UserPreferences Load()
        {
            if (!File.Exists(FilePath))
            {
                var defaultPreferences = new UserPreferences();
                Save(defaultPreferences);
                return defaultPreferences;
            }

            var json = File.ReadAllText(FilePath);
            return JsonSerializer.Deserialize<UserPreferences>(json) ?? new UserPreferences();
        }

        public void Save(UserPreferences newPreferences)
        {
            var json = JsonSerializer.Serialize(newPreferences, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(FilePath, json);
        }
    }
}
