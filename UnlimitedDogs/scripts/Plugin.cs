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
        [BepInPlugin(PluginGUID, "UnlimitedDogs", "0.0.2")]
        [BepInDependency("com.Phedg1Studios.CustomLocalisations", BepInDependency.DependencyFlags.HardDependency)]

        public class Plugin : BaseUnityPlugin
        {
            public const string PluginGUID = "com.Phedg1Studios.UnlimitedDogs";
            public static Plugin plugin;

            const string dogShelterTitleLocalizationKey = "UI-Challenge-Title-Shop-ManyPuppies";
            const string dogShelterDescriptionLocalizationKey = "UI-Challenge-Description-Shop-ManyPuppies";
            public Dictionary<string, Dictionary<string, string>> localisations = new Dictionary<string, Dictionary<string, string>>() {
                { dogShelterDescriptionLocalizationKey, new Dictionary<string, string>() {
                    { "English", "You can adopt unlimited dogs!" },
                } },
            };

            private static FieldInfo constructableShopDataLimit;
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
                foreach (string guidName in localisations.Keys) {
                    CustomLocalisations.Plugin.AddLocalisations(guidName, localisations[guidName]);
                }
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

 