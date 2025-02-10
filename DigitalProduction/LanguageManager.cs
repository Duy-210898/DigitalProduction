using System.Collections.Generic;
using System.IO;
using DigitalProduction.Models;
using Newtonsoft.Json;

public static class Global
{
    private static UserRole currentUser;
    public static UserRole CurrentUser
    {
        get { return currentUser; }
        set { currentUser = value; }
    }
    public static string App = "CuttingProject";
    public static string Language { get; set; }
    public static void SetUser(string employeeName, int positionID, int departmentID, string role)
    {
        currentUser = new UserRole(employeeName, positionID, departmentID, role);
    }

    // Method to reset the current user (e.g., for logout)
    public static void ResetUser()
    {
        currentUser = null;
    }

}

public class LanguageManager
{
    private Dictionary<string, string> _translations;

    public LanguageManager(string language)
    {
        LoadLanguage(language);
    }

    private void LoadLanguage(string language)
    {
        var filePath = $"{language}.json";
        if (File.Exists(filePath))
        {
            var json = File.ReadAllText(filePath);
            _translations = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
        }
        else
        {
            _translations = new Dictionary<string, string>();
        }
    }

    public string GetString(string key, params object[] args)
    {
        if (_translations.TryGetValue(key, out var value))
        {
            return string.Format(value, args);
        }
        return key;
    }
}
