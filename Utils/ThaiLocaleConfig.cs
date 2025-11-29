using System;
using System.IO;
using UnityEngine;

namespace Utils
{
    [Serializable]
    public class ThaiLocaleConfig
    {
        public bool readyToActivate = false;
        public string githubRawUrl = null;
        public string lastChecked = null;
        public string lastActivated = null;
        public string versionHash = null;
        // New fields for the install/activation workflow
        public string installPath = null; // full path to StreamingAssets th-TH.loc
        public bool hasAutoSetLanguage = false;

        private static string ConfigFolder => Path.Combine(Application.persistentDataPath, "ThaiLocale");
        private static string ConfigPath => Path.Combine(ConfigFolder, "locale_state.json");

        public static ThaiLocaleConfig Load()
        {
            try
            {
                if (!Directory.Exists(ConfigFolder)) Directory.CreateDirectory(ConfigFolder);
                if (!File.Exists(ConfigPath)) return null;
                var json = File.ReadAllText(ConfigPath);
                if (string.IsNullOrEmpty(json)) return null;
                // Use UnityEngine.JsonUtility directly
                return UnityEngine.JsonUtility.FromJson<ThaiLocaleConfig>(json);
            }
            catch (Exception ex)
            {
                Debug.Log($"ThaiLocaleConfig.Load error: {ex}");
                return null;
            }
        }

        public static void Save(ThaiLocaleConfig cfg)
        {
            try
            {
                if (cfg == null) return;
                if (!Directory.Exists(ConfigFolder)) Directory.CreateDirectory(ConfigFolder);
                var json = UnityEngine.JsonUtility.ToJson(cfg, true);
                File.WriteAllText(ConfigPath, json);
            }
            catch (Exception ex)
            {
                Debug.Log($"ThaiLocaleConfig.Save error: {ex}");
            }
        }
    }
}
