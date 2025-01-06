using DevExpress.XtraPrinting.Native.WebClientUIControl;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;

public static class Global
{
    public static string Username { get; set; }
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
