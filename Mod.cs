using Colossal.IO.AssetDatabase;
using Colossal.Localization;
using Colossal.Logging;
using Game;
using Game.Modding;
using Game.SceneFlow;
using System;
using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Object = UnityEngine.Object;

namespace ThaiLocale
{
    /// <summary>
    /// Thai Localization Mod for Cities: Skylines II
    /// Loads Thai language file from StreamingAssets and registers it with the game
    /// </summary>
    public class Mod : IMod
    {
        public static Mod Instance { get; private set; }
        const string LOC_FILE = "Locale.cok";
        const string DATA_FOLDER = "Cities2_Data";
        const string CURRENT_LOCALIZATION = "th-TH";
        internal const string ActiveLocaleId = CURRENT_LOCALIZATION;
        public static ILog log = LogManager.GetLogger($"{nameof(ThaiLocale)}.{nameof(Mod)}").SetShowsErrorsInUI(false);
        private const string WATCHER_OBJECT_NAME = "ThaiLocaleActivationWatcher";
        private LocalizationManager _localizationManager;

        /// <summary>
        /// Called when the mod is loaded by the game
        /// Workflow:
        /// 1. Detect StreamingAssets path
        /// 2. Copy locale file from mod cache to StreamingAssets (if needed)
        /// 3. Load locale data from StreamingAssets file
        /// 4. Register locale with LocalizationManager and set as active
        /// </summary>
        public void OnLoad(UpdateSystem updateSystem)
        {
            Instance = this;
            _localizationManager = GameManager.instance.localizationManager;
            
            if (GameManager.instance.modManager.TryGetExecutableAsset(this, out var asset))
            {
                log.Info(nameof(OnLoad) + " called in phase " + updateSystem.currentPhase + " at " + DateTime.Now);
                log.Info($"Current mod asset at: {asset.path}");
                log.Info($"Current active locale: {_localizationManager.activeLocaleId}");
                
                try
                {
                    EnsureThaiLocaleAvailable(asset.path);
                    ActivateThaiLocale();
                    EnsureActivationWatcher();
                }
                catch (Exception ex)
                {
                    log.Error($"Failed to load Thai locale: {ex}");
                }
            }
        }

        /// <summary>
        /// Downloads and updates locale file from GitHub
        /// Called by UI when user wants to update locale from remote source
        /// </summary>
        public bool CheckAndReplaceFromGitHub(string rawUrl)
        {
            try
            {
                var result = Services.GitHubLocaleFetcher.FetchToStreamingAssetsIfNew(rawUrl, CURRENT_LOCALIZATION + ".loc");
                if (result)
                {
                    log.Info("Downloaded new locale file from GitHub");
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                log.Error($"CheckAndReplaceFromGitHub failed: {ex}");
                return false;
            }
        }

        /// <summary>
        /// Activates Thai locale from UI
        /// Called by UI when user manually activates the locale
        /// </summary>
        public void ActivateFromUI()
        {
            try
            {
                EnsureThaiLocaleAvailable(null);
                ActivateThaiLocale();
                EnsureActivationWatcher();
                log.Info($"Activated Thai locale from UI: {_localizationManager.activeLocaleId}");
            }
            catch (Exception ex)
            {
                log.Error($"ActivateFromUI failed: {ex}");
            }
        }

        private void EnsureThaiLocaleAvailable(string executableAssetPath)
        {
            var existingLocale = AssetDatabase.global.GetAssets<LocaleAsset>()
                .FirstOrDefault(l => l.localeId == CURRENT_LOCALIZATION);
            if (existingLocale != null)
            {
                log.Info("Thai locale already registered in LocalizationManager");
                log.Info($"📍 Existing th-TH.loc from: {existingLocale.path}");
                log.Info($"   State: {existingLocale.state}, Transient: {existingLocale.transient}, Valid: {existingLocale.isValid}");
            }

            string streamingAssetsPath = GetStreamingAssetsPath();
            if (string.IsNullOrEmpty(streamingAssetsPath))
            {
                log.Error("Cannot determine StreamingAssets path");
                return;
            }

            string targetLocPath = Path.Combine(streamingAssetsPath, CURRENT_LOCALIZATION + ".loc");
            string modSourcePath = ResolveModSourcePath(executableAssetPath);
            log.Info($"Mod source: {modSourcePath ?? "(unresolved)"}");
            log.Info($"Target path: {targetLocPath}");

            if (!string.IsNullOrEmpty(modSourcePath) && File.Exists(modSourcePath))
            {
                bool needCopy = !File.Exists(targetLocPath) || !FilesAreEqual(modSourcePath, targetLocPath);
                if (needCopy)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(targetLocPath));
                    File.Copy(modSourcePath, targetLocPath, true);
                    log.Info("Copied locale file to StreamingAssets");
                }
                else
                {
                    log.Info("Locale file already up-to-date in StreamingAssets");
                }
            }

            if (_localizationManager.GetSupportedLocales().Any(l => l == CURRENT_LOCALIZATION))
            {
                return;
            }

            if (!File.Exists(targetLocPath))
            {
                log.Error($"Locale file not found: {targetLocPath}");
                return;
            }

            var locale = new LocaleAsset();
            locale.database = AssetDatabase.game;
            FirstLoad(locale, targetLocPath);
            log.Info($"Loaded locale - ID: {locale.localeId}, Language: {locale.systemLanguage}, Name: {locale.localizedName}");
            log.Info($"📍 Registered new th-TH.loc from: {targetLocPath}");

            _localizationManager.AddLocale(locale);
            _localizationManager.AddSource(locale.localeId, locale);
        }

        private string ResolveModSourcePath(string executableAssetPath)
        {
            if (!string.IsNullOrEmpty(executableAssetPath))
            {
                var sourcePath = Path.Combine(Path.GetDirectoryName(executableAssetPath), "Content", CURRENT_LOCALIZATION + ".loc");
                if (File.Exists(sourcePath))
                {
                    return sourcePath;
                }
            }

            var knownLocale = AssetDatabase.global.GetAssets<LocaleAsset>()
                .FirstOrDefault(l => l.localeId == CURRENT_LOCALIZATION && !string.IsNullOrEmpty(l.path));
            if (knownLocale != null)
            {
                var candidate = knownLocale.path;
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }

            return null;
        }

        internal void ActivateThaiLocale()
        {
            if (_localizationManager == null)
            {
                _localizationManager = GameManager.instance.localizationManager;
            }

            if (_localizationManager == null)
            {
                log.Error("LocalizationManager is not available");
                return;
            }

            if (!_localizationManager.GetSupportedLocales().Any(l => l == CURRENT_LOCALIZATION))
            {
                log.Error("Thai locale is not registered yet");
                return;
            }

            if (_localizationManager.activeLocaleId != CURRENT_LOCALIZATION)
            {
                log.Info($"Switching active locale from {_localizationManager.activeLocaleId} to {CURRENT_LOCALIZATION}");
            }

            _localizationManager.SetActiveLocale(CURRENT_LOCALIZATION);
            _localizationManager.ReloadActiveLocale();
            log.Info($"🎯 Active Locale: {_localizationManager.activeLocaleId}");
        }

        private void EnsureActivationWatcher()
        {
            var existing = Object.FindObjectOfType<ThaiLocaleActivationWatcher>();
            if (existing != null)
            {
                existing.Initialize(this);
                return;
            }

            var watcherObject = new GameObject(WATCHER_OBJECT_NAME);
            Object.DontDestroyOnLoad(watcherObject);
            watcherObject.hideFlags = HideFlags.HideAndDontSave;
            watcherObject.AddComponent<ThaiLocaleActivationWatcher>().Initialize(this);
        }

        /// <summary>
        /// Detects the StreamingAssets path by analyzing existing locale assets
        /// Returns the full path to StreamingAssets folder
        /// </summary>
        private string GetStreamingAssetsPath()
        {
            try
            {
                // Method 1: Derive from existing locale asset path
                var defaultLocAsset = AssetDatabase.global.GetAssets<LocaleAsset>().FirstOrDefault(l => l.localeId == _localizationManager.fallbackLocaleId);
                if (defaultLocAsset != null)
                {
                    int idxLocale = defaultLocAsset.path.IndexOf(LOC_FILE, StringComparison.OrdinalIgnoreCase);
                    if (idxLocale >= 0)
                    {
                        var contentLocalePath = defaultLocAsset.path.Substring(0, idxLocale);
                        int idxData = contentLocalePath.IndexOf(DATA_FOLDER, StringComparison.OrdinalIgnoreCase);
                        if (idxData >= 0)
                        {
                            var streamingAssetsRoot = contentLocalePath.Substring(0, idxData + DATA_FOLDER.Length);
                            return Path.Combine(streamingAssetsRoot, "StreamingAssets");
                        }
                    }
                }

                // Fallback to Application.dataPath/StreamingAssets
                var fallback = Path.Combine(Application.dataPath, "StreamingAssets");
                if (Directory.Exists(fallback)) return fallback;

                // Try parent search for Cities2_Data in dataPath
                var candidate = Application.dataPath;
                var idx = candidate.IndexOf(DATA_FOLDER, StringComparison.OrdinalIgnoreCase);
                if (idx >= 0)
                {
                    var root = candidate.Substring(0, idx + DATA_FOLDER.Length);
                    return Path.Combine(root, "StreamingAssets");
                }

                return null;
            }
            catch (Exception ex)
            {
                log.Info($"GetStreamingAssetsPath failed: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Compares two files byte-by-byte to check if they are identical
        /// Used to determine if locale file needs to be updated
        /// </summary>
        private bool FilesAreEqual(string pathA, string pathB)
        {
            try
            {
                if (!File.Exists(pathA) || !File.Exists(pathB)) return false;
                var fa = new FileInfo(pathA);
                var fb = new FileInfo(pathB);
                if (fa.Length != fb.Length) return false;

                const int bufferSize = 8192;
                using (var sa = File.OpenRead(pathA))
                using (var sb = File.OpenRead(pathB))
                {
                    var bufferA = new byte[bufferSize];
                    var bufferB = new byte[bufferSize];
                    int read;
                    while ((read = sa.Read(bufferA, 0, bufferSize)) > 0)
                    {
                        int readB = sb.Read(bufferB, 0, read);
                        if (readB != read) return false;
                        for (int i = 0; i < read; i++)
                        {
                            if (bufferA[i] != bufferB[i]) return false;
                        }
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                log.Info($"FilesAreEqual failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Loads locale data from .loc file into LocaleAsset
        /// Reads binary format: version, language, localeId, localizedName, entries, and index counts
        /// </summary>
        private void FirstLoad(LocaleAsset localeAsset, string filePath)
        {
            using (var input = File.OpenRead(filePath))
            using (var binaryReader = new BinaryReader(input))
            {
                binaryReader.ReadUInt16();
                Enum.TryParse<SystemLanguage>(binaryReader.ReadString(), out var m_SystemLanguage);
                string text = binaryReader.ReadString();
                var localizedName = binaryReader.ReadString();
                int num = binaryReader.ReadInt32();

                Dictionary<string, string> dictionary = new Dictionary<string, string>(num);
                for (int i = 0; i < num; i++)
                {
                    string key = binaryReader.ReadString();
                    string value = binaryReader.ReadString();
                    dictionary[key] = value;
                }
                num = binaryReader.ReadInt32();
                Dictionary<string, int> dictionary2 = new Dictionary<string, int>(num);
                for (int j = 0; j < num; j++)
                {
                    string key2 = binaryReader.ReadString();
                    int value2 = binaryReader.ReadInt32();
                    dictionary2[key2] = value2;
                }
                LocaleData data = new LocaleData(text, dictionary, dictionary2);
                localeAsset.SetData(data, m_SystemLanguage, localizedName);
                localeAsset.database = AssetDatabase.game;
            }
        }

        /// <summary>
        /// Called when the mod is unloaded
        /// </summary>
        public void OnDispose()
        {
            log.Info(nameof(OnDispose));
        }
    }

    internal sealed class ThaiLocaleActivationWatcher : MonoBehaviour
    {
        private Mod _mod;
        private float _nextRetryAt;
        private float _stopRetryAt;

        internal void Initialize(Mod mod)
        {
            _mod = mod;
            _nextRetryAt = 0f;
            _stopRetryAt = Time.unscaledTime + 20f;
        }

        private void Update()
        {
            if (_mod == null)
            {
                return;
            }

            if (Time.unscaledTime > _stopRetryAt)
            {
                enabled = false;
                return;
            }

            if (Time.unscaledTime < _nextRetryAt)
            {
                return;
            }

            _nextRetryAt = Time.unscaledTime + 1.5f;
            if (GameManager.instance?.localizationManager?.activeLocaleId != Mod.ActiveLocaleId)
            {
                Mod.log.Info("Detected locale change away from th-TH during startup. Re-applying Thai locale.");
                _mod.ActivateThaiLocale();
            }
        }
    }
}
