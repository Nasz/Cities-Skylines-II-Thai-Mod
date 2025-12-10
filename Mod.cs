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
        public static ILog log = LogManager.GetLogger($"{nameof(ThaiLocale)}.{nameof(Mod)}").SetShowsErrorsInUI(false);
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
                    // Step 1: Check if Thai locale already exists in the system
                    var existingLocales = _localizationManager.GetSupportedLocales();
                    bool thaiLocaleExists = existingLocales.Any(l => l == CURRENT_LOCALIZATION);
                    
                    if (thaiLocaleExists)
                    {
                        log.Info($"Thai locale already registered in LocalizationManager");
                        
                        // Debug: Show where the existing locale is from
                        var existingLocale = AssetDatabase.global.GetAssets<LocaleAsset>()
                            .FirstOrDefault(l => l.localeId == CURRENT_LOCALIZATION);
                        if (existingLocale != null)
                        {
                            log.Info($"📍 Using existing th-TH.loc from: {existingLocale.path}");
                            log.Info($"   State: {existingLocale.state}, Transient: {existingLocale.transient}, Valid: {existingLocale.isValid}");
                        }
                        
                        _localizationManager.SetActiveLocale(CURRENT_LOCALIZATION);
                        log.Info($"🎯 Active Locale: {_localizationManager.activeLocaleId}");
                        return;
                    }

                    // Step 2: Detect StreamingAssets path
                    string streamingAssetsPath = GetStreamingAssetsPath();
                    if (string.IsNullOrEmpty(streamingAssetsPath))
                    {
                        log.Error("Cannot determine StreamingAssets path");
                        return;
                    }

                    string targetLocPath = Path.Combine(streamingAssetsPath, CURRENT_LOCALIZATION + ".loc");
                    string modSourcePath = Path.Combine(Path.GetDirectoryName(asset.path), "Content", CURRENT_LOCALIZATION + ".loc");
                    
                    log.Info($"Mod source: {modSourcePath}");
                    log.Info($"Target path: {targetLocPath}");

                    // Step 3: Copy locale file to StreamingAssets if needed
                    if (File.Exists(modSourcePath))
                    {
                        bool needCopy = !File.Exists(targetLocPath) || !FilesAreEqual(modSourcePath, targetLocPath);
                        if (needCopy)
                        {
                            Directory.CreateDirectory(Path.GetDirectoryName(targetLocPath));
                            File.Copy(modSourcePath, targetLocPath, true);
                            log.Info($"Copied locale file to StreamingAssets");
                        }
                        else
                        {
                            log.Info("Locale file already up-to-date in StreamingAssets");
                        }
                    }
                    else
                    {
                        log.Error($"Mod source file not found: {modSourcePath}");
                        return;
                    }

                    // Step 4: Load locale data from StreamingAssets (in-memory)
                    var locale = new LocaleAsset();
                    locale.database = AssetDatabase.game;
                    FirstLoad(locale, targetLocPath);
                    log.Info($"Loaded locale - ID: {locale.localeId}, Language: {locale.systemLanguage}, Name: {locale.localizedName}");
                    log.Info($"📍 Created new th-TH.loc from: {targetLocPath}");

                    // Step 5: Register locale and set as active
                    _localizationManager.AddLocale(locale);
                    _localizationManager.AddSource(locale.localeId, locale);
                    _localizationManager.SetActiveLocale(locale.localeId);
                    
                    log.Info($"🎯 Active Locale: {_localizationManager.activeLocaleId}");
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
                string streamingAssetsPath = GetStreamingAssetsPath();
                if (string.IsNullOrEmpty(streamingAssetsPath))
                {
                    log.Error("Cannot determine StreamingAssets path");
                    return;
                }

                string targetLocPath = Path.Combine(streamingAssetsPath, CURRENT_LOCALIZATION + ".loc");
                if (!File.Exists(targetLocPath))
                {
                    log.Error($"Locale file not found: {targetLocPath}");
                    return;
                }

                // Load and activate
                var locale = new LocaleAsset();
                locale.database = AssetDatabase.game;
                FirstLoad(locale, targetLocPath);
                
                _localizationManager.AddLocale(locale);
                _localizationManager.AddSource(locale.localeId, locale);
                _localizationManager.SetActiveLocale(locale.localeId);
                
                log.Info($"Activated Thai locale from UI: {_localizationManager.activeLocaleId}");
            }
            catch (Exception ex)
            {
                log.Error($"ActivateFromUI failed: {ex}");
            }
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
}
