using System;
using UnityEngine;
using Utils;
using ThaiLocale;

namespace Options
{
    // Static settings UI used by ModSetting.OnSettingsGUI (official Options UI).
    // This avoids creating any in-game overlay GameObjects and matches the Options_UI pattern.
    public static class ThaiLocaleSettings
    {
        private static string githubRawUrl = "https://github.com/Nasz/Cities-Skylines-II-Thai-Mod/raw/refs/heads/main/Content/th-TH.loc";
        private static string status = string.Empty;

        public static void DrawSettingsGUI()
        {
            GUILayout.BeginVertical();

            GUILayout.Label("Thai Locale — Update and Activate");

            GUILayout.Label("GitHub raw URL (optional):");
            githubRawUrl = GUILayout.TextField(githubRawUrl, GUILayout.ExpandWidth(true));

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Check language file on GitHub and update StreamingAssets", GUILayout.ExpandWidth(true)))
            {
                status = "Checking...";
                try
                {
                    if (string.IsNullOrEmpty(githubRawUrl))
                    {
                        status = "No URL provided. Enter raw GitHub URL to the .loc file.";
                    }
                    else if (ThaiLocale.Mod.Instance == null)
                    {
                        status = "Mod instance not available.";
                    }
                    else
                    {
                        var ok = ThaiLocale.Mod.Instance.CheckAndReplaceFromGitHub(githubRawUrl);
                        var cfg = ThaiLocaleConfig.Load() ?? new ThaiLocaleConfig();
                        cfg.githubRawUrl = githubRawUrl;
                        cfg.lastChecked = DateTime.UtcNow.ToString("o");
                        cfg.readyToActivate = ok;
                        ThaiLocaleConfig.Save(cfg);
                        status = ok ? "StreamingAssets updated (or already current). You can now Activate." : "No update or failed to fetch.";
                    }
                }
                catch (Exception ex)
                {
                    status = "Error: " + ex.Message;
                }
            }

            if (GUILayout.Button("Activate Thai now", GUILayout.ExpandWidth(true)))
            {
                if (ThaiLocale.Mod.Instance == null)
                {
                    status = "Mod instance not available.";
                }
                else
                {
                    ThaiLocale.Mod.Instance.ActivateFromUI();
                    var cfg = ThaiLocaleConfig.Load() ?? new ThaiLocaleConfig();
                    cfg.lastActivated = DateTime.UtcNow.ToString("o");
                    cfg.readyToActivate = false;
                    cfg.githubRawUrl = githubRawUrl;
                    ThaiLocaleConfig.Save(cfg);
                    status = "Activation requested. Check game UI or logs.";
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(6);
            GUILayout.Label("Status: " + status);
            GUILayout.EndVertical();
        }
    }
}
