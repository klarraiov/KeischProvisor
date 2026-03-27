using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Globalization;

namespace KeischProvisor.Utils
{
    internal class Settings
    {
        public Microsoft.UI.Xaml.ElementTheme AppTheme { get; set; } = Microsoft.UI.Xaml.ElementTheme.Default;
        public AppLanguages AppLanguage { get; set; } = AppLanguages.English;
    }

    internal enum AppLanguages
    {
        English,
        Korean,
    }

    internal class SettingsManager
    {
        const string SETTINGS_FILE = @"\settings.json";
        static string SETTINGS_FOLDER_PATH = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) , "Settings");
        static string SETTINGS_PATH = Path.Combine(SETTINGS_FOLDER_PATH + SETTINGS_FILE);

        internal static Settings LoadSettings()
        {
            if (!System.IO.Directory.Exists(SETTINGS_FOLDER_PATH))
            {
                Debug.WriteLine("[SM] Settings Folder unexists. Making a new one..");
                Directory.CreateDirectory(SETTINGS_FOLDER_PATH);
                Settings settings = new Settings();
                string jsondata = JsonSerializer.Serialize<Settings>(settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllBytes(SETTINGS_PATH, Encoding.UTF8.GetBytes(jsondata));
            }

            return JsonSerializer.Deserialize<Settings>(System.IO.File.ReadAllBytes(SETTINGS_PATH)) ?? new Settings();
        }

        internal static void SaveSettings(Settings settings)
        {
            if (!System.IO.File.Exists(SETTINGS_FOLDER_PATH))
            {
                Directory.CreateDirectory(SETTINGS_FOLDER_PATH);
            }

            string jsonString = JsonSerializer.Serialize<Settings>(settings, new JsonSerializerOptions { WriteIndented = true });
            System.IO.File.WriteAllText(SETTINGS_PATH, jsonString);
        }

        public static string AppLanguagesToTag(AppLanguages lang) => lang switch
        {
            AppLanguages.Korean => "ko-KR",
            AppLanguages.English => "en-US",
            _ => "" //Default
        };

    }
}
