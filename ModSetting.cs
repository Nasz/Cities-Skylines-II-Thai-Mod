using System;
using UnityEngine;

namespace ThaiLocale
{
    // Per Options_UI guidance, provide a class named `ModSetting` that the host options UI can reflectively call.
    // We expose a single static method `OnSettingsGUI` which will render our options GUI by forwarding
    // to the persistent `ThaiLocaleOptions` MonoBehaviour (or creating a short-lived instance if needed).
    public static class ModSetting
    {
        // Called by the game's options system to render our mod's settings.
        public static void OnSettingsGUI()
        {
            try
            {
                // Call the static settings UI directly (no overlay instance required)
                Options.ThaiLocaleSettings.DrawSettingsGUI();
                return;
            }
            catch (Exception ex)
            {
                Debug.Log($"ModSetting.OnSettingsGUI error: {ex}");
            }
        }
    }
}
