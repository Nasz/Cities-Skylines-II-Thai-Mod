using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem; // Add this line if using the new Input System
using Utils;
using ThaiLocale;
using Game.SceneFlow;

namespace Options
{
    // Very small overlay UI to check and activate the Thai locale.
    // Toggle the window with F10. This is intentionally lightweight so it works without the game's mod-options API.
    // This version attempts to register the same UI into the game's Mod Settings page (via reflection)
    // and falls back to the overlay when registration isn't available.
    public class ThaiLocaleOptions : MonoBehaviour
    {
        private bool visible = false;
        private Rect windowRect = new Rect(20, 60, 360, 160);
        private string githubRawUrl = "https://github.com/Nasz/Cities-Skylines-II-Thai-Mod/raw/refs/heads/main/Content/th-TH.loc";
        private string status = "";
        private bool registeredInModSettings = false;
        private ThaiLocaleConfig cfg;

        void Start()
        {
            // load persisted config
            cfg = ThaiLocaleConfig.Load() ?? new ThaiLocaleConfig();
            if (!string.IsNullOrEmpty(cfg.githubRawUrl)) githubRawUrl = cfg.githubRawUrl;

            // try to register immediately and retry a few seconds later if the options UI isn't ready yet
            TryRegisterToModSettings();
            InvokeRepeating(nameof(TryRegisterToModSettings), 5f, 5f);
        }

        void OnDestroy()
        {
            CancelInvoke(nameof(TryRegisterToModSettings));
        }

        void Update()
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.F10))
            {
                visible = !visible;
            }
#elif ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.f10Key.wasPressedThisFrame)
            {
                visible = !visible;
            }
#endif
        }

        void OnGUI()
        {
            // fallback overlay (only if not integrated into Mod Settings)
            if (!visible || registeredInModSettings) return;
            windowRect = GUI.Window(123456, windowRect, WindowFunc, "Thai Locale - Quick Tools (F10 to toggle)");
        }

        void WindowFunc(int id)
        {
            GUILayout.BeginVertical();

            GUILayout.Label("GitHub raw URL (optional):");
            githubRawUrl = GUILayout.TextField(githubRawUrl, GUILayout.ExpandWidth(true));

            if (GUILayout.Button("Check language file on GitHub and update StreamingAssets"))
            {
                status = "Checking...";
                bool ok = false;
                try
                {
                    var url = githubRawUrl;
                    if (string.IsNullOrEmpty(url))
                    {
                        status = "No URL provided. Enter raw GitHub URL to the .loc file.";
                    }
                    else if (Mod.Instance == null)
                    {
                        status = "Mod instance not available.";
                    }
                    else
                    {
                        ok = Mod.Instance.CheckAndReplaceFromGitHub(url);
                        cfg = cfg ?? new ThaiLocaleConfig();
                        cfg.githubRawUrl = githubRawUrl;
                        cfg.lastChecked = DateTime.UtcNow.ToString("o");
                        cfg.readyToActivate = ok;
                        ThaiLocaleConfig.Save(cfg);
                        status = ok ? "StreamingAssets updated (or already current). You can now Activate." : "No update or failed to fetch.";
                    }
                }
                catch (System.Exception ex)
                {
                    status = "Error: " + ex.Message;
                }
            }

            if (GUILayout.Button("Activate Thai now"))
            {
                if (Mod.Instance == null)
                {
                    status = "Mod instance not available.";
                }
                else
                {
                    Mod.Instance.ActivateFromUI();
                    cfg = cfg ?? new ThaiLocaleConfig();
                    cfg.lastActivated = DateTime.UtcNow.ToString("o");
                    cfg.readyToActivate = false;
                    cfg.githubRawUrl = githubRawUrl;
                    ThaiLocaleConfig.Save(cfg);
                    status = "Activation requested. Check game UI or logs.";
                }
            }

            GUILayout.Space(6);
            GUILayout.Label("Status: " + status);
            GUILayout.EndVertical();

            GUI.DragWindow(new Rect(0, 0, 9999, 20));
        }

        // This method will be registered into the game's Mod Settings UI (when possible).
        // Keep the same GUILayout-based UI so it can be invoked by a host that expects an Action/GUI callback.
        private void OnModSettingsGUI()
        {
            GUILayout.BeginVertical();

            GUILayout.Label("GitHub raw URL (optional):");
            githubRawUrl = GUILayout.TextField(githubRawUrl, GUILayout.ExpandWidth(true));

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Check language file on GitHub and update StreamingAssets"))
            {
                status = "Checking...";
                try
                {
                    if (string.IsNullOrEmpty(githubRawUrl))
                    {
                        status = "No URL provided. Enter raw GitHub URL to the .loc file.";
                    }
                    else if (Mod.Instance == null)
                    {
                        status = "Mod instance not available.";
                    }
                    else
                    {
                        var ok = Mod.Instance.CheckAndReplaceFromGitHub(githubRawUrl);
                        cfg = cfg ?? new ThaiLocaleConfig();
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

            if (GUILayout.Button("Activate Thai now"))
            {
                if (Mod.Instance == null)
                {
                    status = "Mod instance not available.";
                }
                else
                {
                    Mod.Instance.ActivateFromUI();
                    cfg = cfg ?? new ThaiLocaleConfig();
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

        // Public wrapper so external static ModSetting entrypoints can invoke the same settings UI.
        public void DrawModSettingsGUI()
        {
            OnModSettingsGUI();
        }

        // Try to find a place to register the settings UI inside the game's Mod Settings.
        // Uses reflection to be resilient across game/SDK versions. If registration succeeds
        // we stop the periodic retry and hide the overlay.
        private void TryRegisterToModSettings()
        {
            if (registeredInModSettings) return;

            try
            {
                if (Mod.Instance == null) return;
                var manager = GameManager.instance?.modManager;
                if (manager == null) return;

                // Try to get the asset object for this mod
                if (!manager.TryGetExecutableAsset(Mod.Instance, out var asset) || asset == null) return;
                var t = asset.GetType();

                // 1) Try methods that look like "Register", "Add", "Set" and accept a single delegate parameter
                var registerMethod = t.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                    .FirstOrDefault(m =>
                    {
                        var ps = m.GetParameters();
                        return ps.Length == 1 && typeof(Delegate).IsAssignableFrom(ps[0].ParameterType)
                               && (m.Name.IndexOf("Register", StringComparison.OrdinalIgnoreCase) >= 0
                                   || m.Name.IndexOf("Add", StringComparison.OrdinalIgnoreCase) >= 0
                                   || m.Name.IndexOf("Set", StringComparison.OrdinalIgnoreCase) >= 0);
                    });

                if (registerMethod != null)
                {
                    var paramType = registerMethod.GetParameters()[0].ParameterType;
                    var callbackMethod = GetType().GetMethod(nameof(OnModSettingsGUI), BindingFlags.Instance | BindingFlags.NonPublic);
                    Delegate dlg = null;

                    // Try to create a delegate matching the parameter type
                    try
                    {
                        dlg = Delegate.CreateDelegate(paramType, this, callbackMethod);
                    }
                    catch
                    {
                        // best-effort: if the host expects System.Action, wrap our method
                        if (paramType == typeof(Action))
                            dlg = new Action(OnModSettingsGUI);
                    }

                    if (dlg != null)
                    {
                        registerMethod.Invoke(asset, new object[] { dlg });
                        registeredInModSettings = true;
                        visible = false;
                        Debug.Log("[ThaiLocale] Registered settings UI via asset method: " + registerMethod.Name);
                        CancelInvoke(nameof(TryRegisterToModSettings));
                        return;
                    }
                }

                // 2) Try to attach to an event that looks like settings UI (Action-like)
                var ev = t.GetEvents(BindingFlags.Public | BindingFlags.Instance)
                          .FirstOrDefault(e => e.EventHandlerType == typeof(Action));
                if (ev != null)
                {
                    ev.AddEventHandler(asset, new Action(OnModSettingsGUI));
                    registeredInModSettings = true;
                    visible = false;
                    Debug.Log("[ThaiLocale] Registered settings UI via event: " + ev.Name);
                    CancelInvoke(nameof(TryRegisterToModSettings));
                    return;
                }

                // If nothing matched, we keep retrying periodically until the options system is ready
            }
            catch (Exception ex)
            {
                Debug.Log($"ThaiLocaleOptions.TryRegisterToModSettings failed: {ex}");
            }
        }
    }
}
