using BepInEx;
using BepInEx.Unity.Mono;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using HarmonyLib;
using Photon.Realtime;
using Photon.Pun;
using System.Reflection;
using System.Reflection.Emit;
using System.Linq;
using Assets.SimpleLocalization;
using ExitGames.Client.Photon;

namespace Phedg1Studios
{
    namespace CustomLocalisations
    {
        [BepInPlugin(PluginGUID, "CustomLocalisations", "0.0.2")]

        public class Plugin : BaseUnityPlugin
        {
            public const string PluginGUID = "com.Phedg1Studios.CustomLocalisations";
            public static Plugin plugin;

            private List<string> languages = new List<string>();
            public string localizationKey = "";
            public Dictionary<string, Dictionary<string, string>> localizations = new Dictionary<string, Dictionary<string, string>>();

            public FieldInfo dictionaryInfo = typeof(LocalizationManager).GetField("Dictionary", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);


            private void Awake() {
                plugin = this;
                var harmony = new Harmony(PluginGUID);
                harmony.PatchAll();
                Logger.LogInfo($"Plugin {PluginGUID} is loaded!");
            }

            public static void AddLocalisations(Dictionary<string, Dictionary<string, string>> localisations) {
                foreach (string descriptiveName in localisations.Keys) {
                    AddLocalisations(descriptiveName, localisations[descriptiveName]);
                }
            }

            public static void AddLocalisations(string descriptiveName, Dictionary<string, string> localisations) {
                foreach (string language in localisations.Keys) {
                    AddLocalisation(descriptiveName, language, localisations[language]);
                }
            }

            public static void AddLocalisation(string descriptiveName, string language, string localisation) {
                if (!plugin.localizations.ContainsKey(language)) {
                    plugin.localizations.Add(language, new Dictionary<string, string>());
                }
                if (!plugin.localizations[language].ContainsKey(descriptiveName)) {
                    plugin.localizations[language].Add(descriptiveName, "");
                }
                plugin.localizations[language][descriptiveName] = localisation;
            }

            [HarmonyPatch(typeof(LocalizationManager))]
            [HarmonyPatch("Read")]
            public static class LocalizationManagerRead
            {
                static void Postfix(string path) {
                    plugin.languages.Clear();
                    IDictionary collection = plugin.dictionaryInfo.GetValue(null) as IDictionary;
                    foreach (object key in collection.Keys) {
                        plugin.languages.Add((string)key);
                    }
                }
            }

            public static string GetLanguage() {
                string language = LocalizationManager.Language;
                if (!plugin.languages.Contains(LocalizationManager.Language)) {
                    language = "English";
                }
                return language;
            }

            [HarmonyPatch(typeof(LocalizationManager))]
            [HarmonyPatch("Localize")]
            [HarmonyPatch(new System.Type[] { typeof(string), typeof(bool) })]
            public static class LocalizationGet
            {
                static void Prefix(string localizationKey, bool addColorTag) {
                    plugin.localizationKey = localizationKey;
                }

                static string Postfix(string localizationKey, bool addColorTag, ref string __result) {
                    string language = GetLanguage();
                    if (!plugin.localizations.ContainsKey(language)) {
                        return __result;
                    }
                    if (!plugin.localizations[language].ContainsKey(plugin.localizationKey)) {
                        return __result;
                    }
                    __result = plugin.localizations[language][plugin.localizationKey];
                    return __result;
                }
            }
        }
    }
}

 