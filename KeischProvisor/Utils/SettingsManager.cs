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

namespace KeischProvisor.Utils
{
    public class Settings
    {
        public Microsoft.UI.Xaml.ElementTheme AppTheme { get; set; } = Microsoft.UI.Xaml.ElementTheme.Default;
        public int iValue { get; set; } = 30;
    }
    internal class SettingsManager
    {
        const string SETTINGS_FILE = @"\settings.json";
        static string SETTINGS_FOLDER_PATH = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) , "Settings");
        static string SETTINGS_PATH = Path.Combine(SETTINGS_FOLDER_PATH + SETTINGS_FILE);

        public static Settings LoadSettings()
        {

            if (!System.IO.Directory.Exists(SETTINGS_FOLDER_PATH))
            {
                Debug.Write("failed");
                Directory.CreateDirectory(SETTINGS_FOLDER_PATH);
                Settings settings = new Settings();
                string jsondata = JsonSerializer.Serialize<Settings>(settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllBytes(SETTINGS_PATH, Encoding.UTF8.GetBytes(jsondata));
            }

            return JsonSerializer.Deserialize<Settings>(System.IO.File.ReadAllBytes(SETTINGS_PATH)) ?? new Settings();
        }

        public static void SaveSettings(Settings settings)
        {
            if (!System.IO.File.Exists(SETTINGS_FOLDER_PATH))
            {
                Directory.CreateDirectory(SETTINGS_FOLDER_PATH);
            }

            string jsonString = JsonSerializer.Serialize<Settings>(settings, new JsonSerializerOptions { WriteIndented = true });
            System.IO.File.WriteAllText(SETTINGS_PATH, jsonString);
        }
    }
}
