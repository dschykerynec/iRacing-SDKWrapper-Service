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
        private UserPreferences UserPreferences = new UserPreferences();

        public UserPreferencesService(ILogger<IUserPreferencesService> logger)
        {
            _logger = logger;

            if (!File.Exists(FilePath))
            {
                _logger.LogInformation("User preferences file not found. Creating new file with default preferences.");
                SetDefaultPreferences();
            }
            try
            {
                _logger.LogInformation($"Loading user preferences from {FilePath}");
                this.UserPreferences = JsonSerializer.Deserialize<UserPreferences>(File.ReadAllText(FilePath)) ?? new UserPreferences();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading user preferences. Using default preferences.");
                SetDefaultPreferences();
            }
        }

        private void SetDefaultPreferences()
        {
            var defaultPreferences = new UserPreferences();
            Save(defaultPreferences);
            this.UserPreferences = new UserPreferences();
        }

        public UserPreferences Load()
        {
            return this.UserPreferences;
        }

        public void Save(UserPreferences newPreferences)
        {
            File.WriteAllText(FilePath, JsonSerializer.Serialize(newPreferences, new JsonSerializerOptions { WriteIndented = true }));
        }
    }
}
