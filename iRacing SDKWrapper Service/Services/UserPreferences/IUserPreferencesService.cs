namespace iRacing_SDKWrapper_Service.Services
{
    public interface IUserPreferencesService
    {
        UserPreferences Load();
        void Save(UserPreferences newPreferences);
    }
}
