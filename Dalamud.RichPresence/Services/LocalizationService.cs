using Dalamud.Game;
using Dalamud.RichPresence.Models;
using Dalamud.Utility;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace Dalamud.RichPresence.Services
{
    internal class LocalizationService : IDisposable
    {
        private const string FilePrefix = "dalamud_richpresence_";
        private const string DefaultLangCode = "en";

        private static readonly HashSet<string> SupportedLangCodes = new(StringComparer.OrdinalIgnoreCase)
        {
            "en", "de", "fr", "ja", "zh", "ur", "tw", "si", "ru", "pt", "no", "ko", "it", "es"
        };

        private CultureInfo clientCultureInfo = CultureInfo.GetCultureInfo(DefaultLangCode);
        private Dictionary<string, LocalizationEntry> clientLocalizationDictionary = [];
        private Dictionary<string, LocalizationEntry> pluginLocalizationDictionary = [];
        private readonly Dictionary<string, LocalizationEntry> defaultLocalizationDictionary;

        private string currentClientLangCode = DefaultLangCode;
        private string currentPluginLangCode = DefaultLangCode;

        public LocalizationService()
        {
            defaultLocalizationDictionary = ReadFileWithLangCode(DefaultLangCode);

            currentClientLangCode = ClientLanguageToLangCode(Plugin.ClientState.ClientLanguage);
            clientLocalizationDictionary = ReadFileWithLangCode(currentClientLangCode);
            clientCultureInfo = CultureInfo.GetCultureInfo(currentClientLangCode);

            currentPluginLangCode = NormalizeLanguageCode(Plugin.PluginInterface.UiLanguage);
            pluginLocalizationDictionary = ReadFileWithLangCode(currentPluginLangCode);

            Plugin.PluginInterface.LanguageChanged += OnLanguageChanged;
        }
        public string TitleCase(string input) => clientCultureInfo.TextInfo.ToTitleCase(input);
        private void OnLanguageChanged(string langCode)
        {
            var normalized = NormalizeLanguageCode(langCode);
            if (string.Equals(currentClientLangCode, normalized, StringComparison.Ordinal)) return;

            Plugin.Log.Debug($"UI language changed to {normalized}. Updating localization data.");
            currentPluginLangCode = normalized;
            pluginLocalizationDictionary = ReadFileWithLangCode(currentPluginLangCode);
        }

        private static Dictionary<string, LocalizationEntry> ReadFileWithLangCode(string langCode)
        {
            var normalized = NormalizeLanguageCode(langCode);
            var filePath = GetLocalizationFilePath(normalized);

            try
            {
                Plugin.Log.Debug($"Attempting to read localization file at: {filePath}");
                var json = File.ReadAllText(filePath);
                return JsonConvert.DeserializeObject<Dictionary<string, LocalizationEntry>>(json) ??
                    new Dictionary<string, LocalizationEntry>(StringComparer.Ordinal);
            }
            catch (Exception ex)
            {
                Plugin.Log.Error(ex, $"Failed to read localization file for lang code '{normalized}'. Falling back to {DefaultLangCode}.");

                if (!string.Equals(normalized, DefaultLangCode, StringComparison.Ordinal))
                    return ReadFileWithLangCode(DefaultLangCode);

                return new Dictionary<string, LocalizationEntry>(StringComparer.Ordinal);
            }
        }

        private static string NormalizeLanguageCode(string langCode)
        {
            if (langCode.IsNullOrWhitespace())
                return DefaultLangCode;

            var normalized = langCode.Trim().ToLowerInvariant();
            return SupportedLangCodes.Contains(normalized) ? normalized : DefaultLangCode;
        }
        private static string GetLocalizationFilePath(string langCode)
        {
            var assemblyDir = Plugin.PluginInterface.AssemblyLocation.DirectoryName ?? AppContext.BaseDirectory;

            return Path.Combine(
                assemblyDir,
                "Resources",
                "loc",
                $"{FilePrefix}{langCode}.json");

        }
        private void RefreshClientLanguageIfChanged()
        {
            var normalized = ClientLanguageToLangCode(Plugin.ClientState.ClientLanguage);
            if (string.Equals(currentClientLangCode, normalized, StringComparison.Ordinal)) return;

            Plugin.Log.Debug($"Client language changed to {normalized}. Updating client localization data.");
            currentClientLangCode = normalized;
            clientLocalizationDictionary = ReadFileWithLangCode(currentClientLangCode);
            clientCultureInfo = CultureInfo.GetCultureInfo(currentClientLangCode);
        }
        private static string ClientLanguageToLangCode(ClientLanguage clientLanguage) => clientLanguage switch
        {
            ClientLanguage.German => "de",
            ClientLanguage.French => "fr",
            ClientLanguage.Japanese => "ja",
            _ => DefaultLangCode,
        };

        private static bool TryGetMessage(Dictionary<string, LocalizationEntry> dict, string key, out string message)
        {
            if (dict.TryGetValue(key, out var entry) && !string.IsNullOrWhiteSpace(entry.Message))
            {
                message = entry.Message;
                return true;
            }
            Plugin.Log.Debug("Failed to find localization message for key: " + key);
            message = string.Empty;
            return false;
        }
        public string Localize(string localizationStringKey, LocalizationLanguage localizationSource)
        {
            if (localizationStringKey.IsNullOrWhitespace()) return string.Empty;
            if (localizationSource == LocalizationLanguage.Client)
                RefreshClientLanguageIfChanged();

            var sourceDict = localizationSource == LocalizationLanguage.Client
                ? clientLocalizationDictionary
                : pluginLocalizationDictionary;
            
            Plugin.Log.Debug("All passed");
            if (TryGetMessage(sourceDict, localizationStringKey, out var message))
                return message;

            if (TryGetMessage(defaultLocalizationDictionary, localizationStringKey, out var fallbackMessage))
                return fallbackMessage;

            Plugin.Log.Warning($"Missing localization entry: {localizationStringKey} in both {localizationSource} and default dictionaries.");
            return localizationStringKey;

        }

        public void Dispose()
        {
            Plugin.PluginInterface.LanguageChanged -= OnLanguageChanged;
        }
    }
}
