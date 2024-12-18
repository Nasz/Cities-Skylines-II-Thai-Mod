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

        const string LOC_FILE = "Locale.cok";
        const string DATA_FOLDER = "Cities2_Data";
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
            var filePaths = AddLocFile(asset);

            var supportedLocales = _localizationManager.GetSupportedLocales();
            if (supportedLocales.Contains(CURRENT_LOCALIZATION))
            {
                log.Info($"Reload in case the last version was replaced");
                _localizationManager.ReloadActiveLocale();
            }
            else
            {
                var thaiLocaleAsset = new LocaleAsset();
                FirstLoad(thaiLocaleAsset, filePaths.NewLocalizationPath);

                log.Info($"thaiLocaleAsset data - localeId: {thaiLocaleAsset.localeId}, systemLanguage: {thaiLocaleAsset.systemLanguage}, localizedName: {thaiLocaleAsset.localizedName}");

                //MakeReserveDBCopy(filePaths.ContentGamePath, filePaths.StreamingAssetPath);

                var hash = AddFileToDB(filePaths.NewLocalizationPath);
                thaiLocaleAsset.guid = hash;

                thaiLocaleAsset.Save();

                _localizationManager.AddLocale(thaiLocaleAsset);
                _localizationManager.AddSource(thaiLocaleAsset.localeId,thaiLocaleAsset);

                _localizationManager.SetActiveLocale(thaiLocaleAsset.localeId);
                _localizationManager.ReloadActiveLocale();

                log.Info($"Force set new locale {_localizationManager.activeLocaleId}");
            }
        }
        private void MakeReserveDBCopy(string ContentGamePath, string backupFolderPath)
        {
            string currentDbPath = ContentGamePath + "/cache.db";
            log.Info($"Backup DB folder: {backupFolderPath}");
            string backupDbPath = backupFolderPath + $"cache_backup_{DateTime.Now:yyyy-MM-dd-HH-mm-ss}.db";
            log.Info($"Created DB backup file: {backupDbPath}");
            File.Copy(currentDbPath, backupDbPath, true);
        }
        private FilePaths AddLocFile(ExecutableAsset asset)
        {
            string directoryPath = Path.GetDirectoryName(asset.path);
            string localizedPath = Path.Combine(directoryPath, "Content", CURRENT_LOCALIZATION + ".loc");

            var defaultLocAsset = AssetDatabase.global.GetAssets<LocaleAsset>().FirstOrDefault(f => f.localeId == _localizationManager.fallbackLocaleId);

            log.Info($"defaultLocAsset.path {defaultLocAsset.path}, defaultLocAsset.path.IndexOf({LOC_FILE}) {defaultLocAsset.path.IndexOf(LOC_FILE)}");

            var contentLocalePath = defaultLocAsset.path.Substring(0, defaultLocAsset.path.IndexOf(LOC_FILE));
            log.Info($"contentLocalePath {contentLocalePath}");

            var streamingAssetsPath = contentLocalePath.Substring(0, contentLocalePath.IndexOf(DATA_FOLDER) + DATA_FOLDER.Length) + "/StreamingAssets/";
            log.Info($"streamingAssetsPath {streamingAssetsPath}");
            Directory.CreateDirectory(streamingAssetsPath);


            string newLocalizedPath = streamingAssetsPath + CURRENT_LOCALIZATION + ".loc";
            log.Info($"newLocalizedPath {newLocalizedPath}");

            File.Copy(localizedPath, newLocalizedPath, true);
            return new FilePaths()
            {
                NewLocalizationPath = newLocalizedPath,
                ContentGamePath = localizedPath,
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

                log.Info($"SystemLang {m_SystemLanguage}");
                log.Info($"localizedName {localizedName}");
                log.Info($"num {num}");

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
            //AssetDatabase.game.SaveCache();
            //log.Info("Saved");
            return hash;
        }
    }
    internal class FilePaths
    {
        public string NewLocalizationPath { get; set; }
        public string StreamingAssetPath { get; set; }
        public string ContentGamePath { get; set; }
    }
}
