using System.Globalization;
using System.Resources;
using System.Threading;

public static class LocalizationManager
{
    private static ResourceManager _resourceManager;
    private static CultureInfo _currentCulture;

    static LocalizationManager()
    {
        // Set default culture (English) at startup
        SetLanguage("en");
    }

    // Set the application language dynamically
    public static void SetLanguage(string languageCode)
    {
        _currentCulture = new CultureInfo(languageCode);
        Thread.CurrentThread.CurrentCulture = _currentCulture;
        Thread.CurrentThread.CurrentUICulture = _currentCulture;

        // Initialize the resource manager with the appropriate resource file
        _resourceManager = new ResourceManager($"DigitalProduction.Resources.{languageCode}", typeof(LocalizationManager).Assembly);
    }

    // Get a translated string from the resource file
    public static string GetString(string key)
    {
        return _resourceManager.GetString(key, _currentCulture) ?? key;
    }
}
