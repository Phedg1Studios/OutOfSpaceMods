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

namespace Phedg1Studios
{
    namespace ChallengesOnSmallerShips
    {
        [BepInPlugin(PluginGUID, "ChallengesOnSmallerShips", "0.0.1")]
        [BepInDependency("com.Phedg1Studios.ChallengesOnOtherShipSizes", BepInDependency.DependencyFlags.HardDependency)]

        public class Plugin : BaseUnityPlugin
        {
            public const string PluginGUID = "com.Phedg1Studios.ChallengesOnSmallerShips";
            public static Plugin plugin;
            public MethodInfo setButtonState;
            public FieldInfo challengeLocalizedOptions;
            public List<string> incompatibleChallengeOptions = new List<string>() { "No", "Weekly" };
            public List<string> shipSizes = new List<string>() {
                "SMALL",
                "MEDIUM",
            };

            private void Awake() {
                plugin = this;
                var harmony = new Harmony(PluginGUID);
                harmony.PatchAll();
                Logger.LogInfo($"Plugin {PluginGUID} is loaded!");
                challengeLocalizedOptions = typeof(PlayerSelectUI).GetField("challengeLocalizedOptions", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                setButtonState = typeof(PlayerSelectUI).GetMethod("SetButtonState", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            }


            [HarmonyPatch(typeof(PlayerSelectUI))]
            [HarmonyPatch("SetWeeklyChallenge")]
            public static class PlayerSelectUISetWeeklyChallenge
            {
                static void Postfix(PlayerSelectUI __instance) {
                    IDictionary collection = plugin.challengeLocalizedOptions.GetValue(__instance) as IDictionary;
                    if (plugin.incompatibleChallengeOptions.Contains((string) collection[(object) __instance.challengeCarousel.GetCurrentValue()])) {
                        return;
                    }
                    List<string> currentValues = __instance.shipSizeCarousel.values;
                    int largeIndex = currentValues.IndexOf("LARGE");
                    if (largeIndex == -1) {
                        return;
                    }
                    currentValues.InsertRange(largeIndex, plugin.shipSizes);
                    string prefSize = PlayerPrefs.GetString("shipSize", "LARGE");
                    if (!currentValues.Contains(prefSize)) {
                        prefSize = "LARGE";
                    }
                    __instance.shipSizeCarousel.Initialize("LARGE", currentValues);
                    __instance.shipSizeCarousel.SetValue(prefSize);
                    plugin.setButtonState.Invoke(__instance, new object[] { __instance.buttonShipSize, 0, false });
                }
            }
        }
    }
}

 