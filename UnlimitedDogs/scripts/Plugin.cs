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

namespace Phedg1Studios
{
    namespace UnlimitedDogs
    {
        [BepInPlugin(PluginGUID, "UnlimitedDogs", "0.0.1")]

        public class Plugin : BaseUnityPlugin
        {
            public const string PluginGUID = "com.Phedg1Studios.UnlimitedDogs";
            public static Plugin plugin;

            const string dogShelterTitleLocalizationKey = "UI-Challenge-Title-Shop-ManyPuppies";
            const string dogShelterDescriptionLocalizationKey = "UI-Challenge-Description-Shop-ManyPuppies";
            private List<string> languages = new List<string>();
            public string localizationKey = "";
            public Dictionary<string, Dictionary<string, Dictionary<string, string>>> localizationChanges = new Dictionary<string, Dictionary<string, Dictionary<string, string>>>() {
                {"English", new Dictionary<string, Dictionary<string, string>>() {
                    { "UI-Challenge-Description-Shop-ManyPuppies", new Dictionary<string, string>() {
                        { "up to 3 dogs!", "unlimited dogs!" },
                    } },
                } },
            };

            private static FieldInfo constructableShopDataLimit;
            private static FieldInfo dictionaryInfo;
            public FieldInfo firstOpenInfo;

            public int dogCost = 60;
            public int freeDogLimit = 3;
            public int bonusDogs = 0;
            public int saleIndex = 0;
            public bool firstOpen = false;


            private void Awake() {
                plugin = this;
                var harmony = new Harmony(PluginGUID);
                harmony.PatchAll();
                Logger.LogInfo($"Plugin {PluginGUID} is loaded!");
                constructableShopDataLimit = typeof(ConstructableShopData).GetField("limit", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                firstOpenInfo = typeof(ShopManager).GetField("firstOpen", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                dictionaryInfo = typeof(LocalizationManager).GetField("Dictionary", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            }

            [HarmonyPatch(typeof(ChallengeManager))]
            [HarmonyPatch("Load")]
            public static class ChallengeManagerLoad
            {
                static void Postfix(ChallengeManager __instance) {
                    foreach (Challenge challenge in __instance.challenges) {
                        switch(challenge.TitleLocalizationKey) {
                            case dogShelterTitleLocalizationKey:
                                ChallengeShopPrices challengeShopPrices = (ChallengeShopPrices) challenge;
                                ShopData customShop = challengeShopPrices.customShop;
                                List<ConstructableShopData> items = customShop.Items;
                                foreach (ConstructableShopData item in items) {
                                    if (item.ItemID.Contains("Dog")) {
                                        constructableShopDataLimit.SetValue(item, (object) -1);
                                        if (item.saleCategory == SaleCategoryType.None) {
                                            item.saleCategory = SaleCategoryType.Safety;
                                        }
                                    }
                                }
                                continue;
                            default:
                                continue;
                        }
                    }
                }
            }

            [HarmonyPatch(typeof(ChallengeShopPrices))]
            [HarmonyPatch("HandleShopData")]
            public static class ChallengeShopPricesHandleShopData
            {
                static void Postfix(ChallengeShopPrices __instance, List<ConstructableShopData> shopData) {
                    foreach (ConstructableShopData item in shopData) {
                        if (item.ItemID.Contains("Dog")) {
                            int dogPurchases = 0;
                            foreach (ConstructableShopData purchaseHistory in ShopManager.Instance.purchasesHistory.Keys) {
                                if (purchaseHistory.ItemID == item.ItemID) {
                                    dogPurchases = ShopManager.Instance.purchasesHistory[purchaseHistory];
                                }
                            }
                            if (item.Limit == -1 && dogPurchases + plugin.bonusDogs >= plugin.freeDogLimit) {
                                item.Cost = plugin.dogCost;
                            }
                        }
                    }
                }
            }

           

            [HarmonyPatch(typeof(LocalizationManager))]
            [HarmonyPatch("Localize")]
            [HarmonyPatch(new System.Type[] { typeof(string), typeof(bool) })]
            public static class LocalizationGet
            {
                static string Postfix(string localizationKey, bool addColorTag, ref string __result) {
                    string language = LocalizationManager.Language;
                    if (!plugin.languages.Contains(LocalizationManager.Language)) {
                        language = "English";
                    }
                    if (language != "English") {
                        return __result;
                    }
                    if (plugin.localizationKey != dogShelterDescriptionLocalizationKey) {
                        return __result;
                    }
                    if (!plugin.localizationChanges["English"].ContainsKey(plugin.localizationKey)) {
                        return __result;
                    }
                    foreach (string substr in plugin.localizationChanges["English"][plugin.localizationKey].Keys) {
                        __result = __result.Replace(substr, plugin.localizationChanges["English"][plugin.localizationKey][substr]);
                    }
                    return __result;
                }
                
                static void Prefix(string localizationKey, bool addColorTag) {
                    plugin.localizationKey = localizationKey;
                }
            }

            [HarmonyPatch(typeof(LocalizationManager))]
            [HarmonyPatch("Read")]
            public static class LocalizationManagerRead
            {
                static void Postfix(string path) {
                    plugin.languages.Clear();
                    IDictionary collection = dictionaryInfo.GetValue(null) as IDictionary;
                    foreach (object key in collection.Keys) {
                        plugin.languages.Add((string)key);
                    }
                }
            }

            [HarmonyPatch(typeof(ShopManager))]
            [HarmonyPatch("NewPurchase")]
            public static class ShopManagerNewPurchase
            {
                static void Prefix(ShopManager __instance, ShopPurchase shopPurchase) {
                    if (shopPurchase == null) {
                        return;
                    }
                    if (shopPurchase.itemID != "Furniture-Dog") {
                        return;
                    }
                    plugin.bonusDogs += 1;
                }
            }

            [HarmonyPatch(typeof(SpaceshipGenerator))]
            [HarmonyPatch("RandomizeRooms")]
            public static class SpaceshipGenerateRandomizeRooms
            {
                static void Postfix(SpaceshipGenerator __instance) {
                    plugin.bonusDogs = 0;
                }
            }

            [HarmonyPatch(typeof(ShopManager))]
            [HarmonyPatch("ShowRecipeHUD")]
            public static class ShopManagerShowRecipeHUD
            {
                static void Prefix(ShopManager __instance) {
                    plugin.firstOpen = (bool) plugin.firstOpenInfo.GetValue(__instance);
                }
                
                static void Postfix(ShopManager __instance) {
                    if (!plugin.firstOpen) {
                        return;
                    }
                    bool firstOpen = (bool)plugin.firstOpenInfo.GetValue(__instance);
                    if (firstOpen) {
                        return;
                    }
                    foreach (ConstructableShopData item in __instance.shopData) {
                        if (!item.ItemID.Contains("Dog")) {
                            continue;
                        }
                        if (!(item.Limit <= 0)) {
                            continue;
                        }
                        __instance.UnlockedShopData.Add(item);
                    }
                }
            }
        }
    }
}

 