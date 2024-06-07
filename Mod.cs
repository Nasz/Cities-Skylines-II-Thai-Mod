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
using Hash128 = Colossal.Hash128;

namespace ThaiLocale
{
    public class Mod : IMod
    {
        const string LOC_FOLDER = "Data~";
        const string CURRENT_LOCALIZATION = "th-TH";
        public static ILog log = LogManager.GetLogger($"{nameof(ThaiLocale)}.{nameof(Mod)}").SetShowsErrorsInUI(false);
        private LocalizationManager _localizationManager;
        public void OnLoad(UpdateSystem updateSystem)
        {
            _localizationManager = GameManager.instance.localizationManager;
            log.Info(nameof(OnLoad) + " called in phase " + updateSystem.currentPhase + " at " + DateTime.Now);
            log.Info("Localization version: " + Colossal.Localization.Version.current.fullVersion);
            if (GameManager.instance.modManager.TryGetExecutableAsset(this, out var asset))
                log.Info($"Current mod asset at {asset.path}");
            log.Info($"Current active locale {_localizationManager.activeLocaleId}");
            LogManagerLocales();
            LogDbLocales();
            LoadLocAsset(asset);
            LogManagerLocales();
            LogDbLocales();
        }
        private void LoadLocAsset(ExecutableAsset asset)
        {
            var filePaths = OverrideLocFile(asset);
            var supportedLocales = _localizationManager.GetSupportedLocales();
            if (supportedLocales.Contains(CURRENT_LOCALIZATION))
            {
                // Reload in case the last version was replaced
                _localizationManager.ReloadActiveLocale();
            }
            else
            {
                var thaiLocAsset = new LocaleAsset();
                FirstLoad(thaiLocAsset, filePaths.NewLocalizationPath);
                log.Info($"thaiLocAsset data - localeId: {thaiLocAsset.localeId}, systemLanguage: {thaiLocAsset.systemLanguage}, localizedName: {thaiLocAsset.localizedName}");
                MakeReserveDBCopy(filePaths.StreamingAssetPath);
                var hash = AddFileToDB(filePaths.NewLocalizationPath);
                thaiLocAsset.guid = hash;
                thaiLocAsset.Save();
                _localizationManager.AddLocale(thaiLocAsset);
                _localizationManager.AddSource(thaiLocAsset.localeId, thaiLocAsset);
                _localizationManager.SetActiveLocale(thaiLocAsset.localeId);
                _localizationManager.ReloadActiveLocale();
                log.Info($"Force set new locale {_localizationManager.activeLocaleId}");
            }
        }
        private void MakeReserveDBCopy(string streamingAssetsPath)
        {
            string currentDbPath = streamingAssetsPath + "cache.db";
            string backupDbPath = streamingAssetsPath + $"cache_backup_{DateTime.Now:yyyy-MM-dd-HH-mm-ss}.db";
            log.Info($"Created DB backup file: {backupDbPath}");
            File.Copy(currentDbPath, backupDbPath, true);
        }
        private FilePaths OverrideLocFile(ExecutableAsset asset)
        {
            string directoryPath = Path.GetDirectoryName(asset.path);
            string localizedPath = Path.Combine(directoryPath, "Sources\\Locale", CURRENT_LOCALIZATION + ".loc");
            var defaultLocAsset = AssetDatabase.global.GetAssets<LocaleAsset>().FirstOrDefault(f => f.localeId == _localizationManager.fallbackLocaleId);
            log.Info($"defaultLocAsset.path {defaultLocAsset.path}, defaultLocAsset.path.IndexOf(\"Data~\") {defaultLocAsset.path.IndexOf(LOC_FOLDER)}");
            var streamingAssetsPath = defaultLocAsset.path.Substring(0, defaultLocAsset.path.IndexOf(LOC_FOLDER));
            log.Info($"streamingAssetsPath {streamingAssetsPath}");
            string newLocalizedPath = streamingAssetsPath + LOC_FOLDER + "/" + CURRENT_LOCALIZATION + ".loc";
            log.Info($"newLocalizedPath {newLocalizedPath}");
            File.Copy(localizedPath, newLocalizedPath, true);
            return new FilePaths()
            {
                NewLocalizationPath = newLocalizedPath,
                StreamingAssetPath = streamingAssetsPath
            };
        }
        public void LogDbLocales()
        {
            log.Info("Existing locales in global db:");
            foreach (LocaleAsset localeAsset in AssetDatabase.global.GetAssets<LocaleAsset>())
            {
                log.Info($"{localeAsset.localeId} {localeAsset.state} {localeAsset.transient} {localeAsset.path} {localeAsset.subPath} " +
                         $"{localeAsset.guid} {localeAsset.identifier} isDirty:{localeAsset.isDirty} isDummy:{localeAsset.isDummy} isValid:{localeAsset.isValid} {localeAsset.systemLanguage}");
            }
        }
        private void LogManagerLocales()
        {
            var locs = _localizationManager.GetSupportedLocales();
            log.Info("Supported locales by localizationManager: " + string.Join(", ", locs));
        }
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
#if DEBUG
                log.Info($"SystemLang {m_SystemLanguage}");
                log.Info($"localizedName {localizedName}");
                log.Info($"num {num}");
#endif
                Dictionary<string, string> dictionary = new Dictionary<string, string>(num);
                for (int i = 0; i < num; i++)
                {
                    string key = binaryReader.ReadString();
                    string value = binaryReader.ReadString();
                    dictionary[key] = value;
                    //log.Info($"{key} {value}");
                }
                num = binaryReader.ReadInt32();
                //log.Info($"num {num}");
                Dictionary<string, int> dictionary2 = new Dictionary<string, int>(num);
                for (int j = 0; j < num; j++)
                {
                    string key2 = binaryReader.ReadString();
                    int value2 = binaryReader.ReadInt32();
                    dictionary2[key2] = value2;
                    //log.Info($"{key2} {value2}");
                }
                LocaleData data = new LocaleData(text, dictionary, dictionary2);
                localeAsset.SetData(data, m_SystemLanguage, localizedName);
                localeAsset.database = AssetDatabase.game;
            }
        }
        public void OnDispose()
        {
            log.Info(nameof(OnDispose));
        }
        private Hash128 AddFileToDB(string path)
        {
            log.Info("Adding file " + path);
            System.Type type;
            var assetFactory = DefaultAssetFactory.instance;
            if (!assetFactory.GetAssetType(Path.GetExtension(path), out type))
            {
                log.Info("Adding file not happens");
                return new Hash128();
            }
            log.Info($"Adding file happened! type: {type.Name}");
            var hash = AssetDatabase.game.dataSource.AddEntry(AssetDataPath.Create(path, EscapeStrategy.None), type, new Colossal.Hash128());
            assetFactory.CreateAndRegisterAsset<LocaleAsset>(type, hash, AssetDatabase.game);
            log.Info($"Saving DB with entry hash: {hash}");
            AssetDatabase.game.SaveCache();
            log.Info("Saved");
            return hash;
        }
    }
    internal class FilePaths
    {
        public string NewLocalizationPath { get; set; }
        public string StreamingAssetPath { get; set; }
    }
}
