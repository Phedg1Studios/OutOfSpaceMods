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
using BepInEx.Bootstrap;

namespace Phedg1Studios
{
    namespace AddShipSizes
    {
        [BepInPlugin(PluginGUID, "AddShipSizes", "0.0.2")]
        [BepInDependency("com.Phedg1Studios.ChallengesOnOtherShipSizes", BepInDependency.DependencyFlags.SoftDependency)]

        public class Plugin : BaseUnityPlugin
        {
            public const string PluginGUID = "com.Phedg1Studios.AddShipSizes";
            public static Plugin plugin;
            public GameValue maxAliensPerRoomOriginal;
            public GameValue initialMaxAliensPerRoomOriginal;
            public Dictionary<string, Dictionary<string, int>> shipSizes = new Dictionary<string, Dictionary<string, int>>() {
                {"GIANT", new Dictionary<string, int>() {
                    { "rooms", 16 },
                    { "roomCells", 20 },
                    { "maxAliensIncrease", 2 },
                    { "maxAliensIncreaseEZ", 1 },
                    { "initialAliensIncrease", 1 },
                    { "initialAliensIncreaseEZ", 1 },
                }},
                {"MEGA", new Dictionary<string, int>() {
                    { "rooms", 20 },
                    { "roomCells", 24 },
                    { "maxAliensIncrease", 4 },
                    { "maxAliensIncreaseEZ", 2 },
                    { "initialAliensIncrease", 2 },
                    { "initialAliensIncreaseEZ", 1 },
                }},
                {"GIGA", new Dictionary<string, int>() {
                    { "rooms", 30 },
                    { "roomCells", 28 },
                    { "maxAliensIncrease", 6 },
                    { "maxAliensIncreaseEZ", 3 },
                    { "initialAliensIncrease", 3 },
                    { "initialAliensIncreaseEZ", 1 },
                }},
            };
            public FieldInfo challengeLocalizedOptions;
            public MethodInfo setButtonState;

            private void Awake() {
                plugin = this;
                var harmony = new Harmony(PluginGUID);
                harmony.PatchAll();
                Logger.LogInfo($"Plugin {PluginGUID} is loaded!");
                challengeLocalizedOptions = typeof(PlayerSelectUI).GetField("challengeLocalizedOptions", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                setButtonState = typeof(PlayerSelectUI).GetMethod("SetButtonState", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            }

            [HarmonyPatch(typeof(SpaceshipGenerator))]
            [HarmonyPatch("GetShipSize")]
            public static class SpaceshipGeneratorGetShipSize
            {
                static void Postfix(SpaceshipGenerator __instance, ref int __result) {
                    if (PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom != null) {
                        string shipSize = __instance.sizeForDebugging;
                        if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey((object) "shipSize")) {
                            shipSize = (string) PhotonNetwork.CurrentRoom.CustomProperties[(object) "shipSize"];
                        }
                        if (plugin.shipSizes.ContainsKey(shipSize)) {
                            __result = plugin.shipSizes[shipSize]["rooms"];
                        }
                    }
                }
            }

            [HarmonyPatch(typeof(PlayerSelectUI))]
            [HarmonyPatch("InitializeCarrousel")]
            public static class PlayerSelectUIInitializeCarrousel
            {
                static void Postfix(PlayerSelectUI __instance) {
                    List<string> currentValues = __instance.shipSizeCarousel.values;
                    int largeIndex = currentValues.IndexOf("LARGE");
                    if (largeIndex == -1) {
                        return;
                    }
                    currentValues.InsertRange(largeIndex + 1, plugin.shipSizes.Keys );
                    __instance.shipSizeCarousel.Initialize("SMALL", currentValues);
                    __instance.shipSizeCarousel.SetValue(PlayerPrefs.GetString("shipSize", "SMALL"));
                }
            }

            [HarmonyPatch(typeof(PlayerSelectUI))]
            [HarmonyPatch("SetWeeklyChallenge")]
            public static class PlayerSelectUISetWeeklyChallenge
            {
                static void Postfix(PlayerSelectUI __instance) {
                    if (!BepInEx.Unity.Mono.Bootstrap.UnityChainloader.Instance.Plugins.ContainsKey("com.Phedg1Studios.ChallengesOnOtherShipSizes")) {
                        return;
                    }
                    IDictionary collection = plugin.challengeLocalizedOptions.GetValue(__instance) as IDictionary;
                    if ((string) collection[(object) __instance.challengeCarousel.GetCurrentValue()] == "No") {
                        return;
                    }
                    List<string> currentValues = __instance.shipSizeCarousel.values;
                    int largeIndex = currentValues.IndexOf("LARGE");
                    if (largeIndex == -1) {
                        return;
                    }
                    currentValues.InsertRange(largeIndex + 1, plugin.shipSizes.Keys);
                    string prefSize = PlayerPrefs.GetString("shipSize", "LARGE");
                    if (!currentValues.Contains(prefSize)) {
                        prefSize = "LARGE";
                    }
                    __instance.shipSizeCarousel.Initialize(prefSize, currentValues);
                    __instance.shipSizeCarousel.SetValue(prefSize);
                    plugin.setButtonState.Invoke(__instance, new object[] { __instance.buttonShipSize, 0, false });
                }
            }

            [HarmonyPatch(typeof(SpaceshipGenerator))]
            [HarmonyPatch("RandomizeRooms")]
            public static class SpaceshipGenerateRandomizeRooms
            {
                static void Prefix(SpaceshipGenerator __instance) {
                    if (!plugin.shipSizes.ContainsKey("DEFAULT")) {
                        plugin.shipSizes.Add("DEFAULT", new Dictionary<string, int>() {
                            { "roomCells", __instance.maxRoomCells },
                            { "radius", __instance.radius },
                        });
                        plugin.maxAliensPerRoomOriginal = CloneGameValue(GameValues.Instance.MaxAliensPerRoom);
                        plugin.initialMaxAliensPerRoomOriginal = CloneGameValue(GameValues.Instance.InitialMaxAliensPerRoom);
                    }
                    bool customShipSize = false;
                    string shipSize = "";
                    if (PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom != null) {
                        shipSize = __instance.sizeForDebugging;
                        if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey((object)"shipSize")) {
                            shipSize = (string)PhotonNetwork.CurrentRoom.CustomProperties[(object)"shipSize"];
                        }
                        if (plugin.shipSizes.ContainsKey(shipSize)) {
                            customShipSize = true;
                        }
                    }
                    if (customShipSize) {
                        __instance.maxRoomCells = plugin.shipSizes[shipSize]["roomCells"];
                        __instance.radius = 18;
                        GameValues.Instance.MaxAliensPerRoom = OffsetGameValue(GameValues.Instance.MaxAliensPerRoom, plugin.maxAliensPerRoomOriginal, plugin.shipSizes[shipSize]["maxAliensIncrease"], plugin.shipSizes[shipSize]["maxAliensIncreaseEZ"]);
                        GameValues.Instance.InitialMaxAliensPerRoom = OffsetGameValue(GameValues.Instance.InitialMaxAliensPerRoom, plugin.initialMaxAliensPerRoomOriginal, plugin.shipSizes[shipSize]["initialAliensIncrease"], plugin.shipSizes[shipSize]["initialAliensIncreaseEZ"]);
                    } else {
                        __instance.maxRoomCells = plugin.shipSizes["DEFAULT"]["roomCells"];
                        __instance.radius = plugin.shipSizes["DEFAULT"]["radius"];
                        GameValues.Instance.MaxAliensPerRoom = OffsetGameValue(GameValues.Instance.MaxAliensPerRoom, plugin.maxAliensPerRoomOriginal, 0, 0);
                        GameValues.Instance.InitialMaxAliensPerRoom = OffsetGameValue(GameValues.Instance.InitialMaxAliensPerRoom, plugin.initialMaxAliensPerRoomOriginal, 0, 0);
                    }
                }
            }

            public static GameValue CloneGameValue(GameValue givenValue) {
                GameValue gameValue = new GameValue();
                gameValue.independentFromNumberOfPlayers = givenValue.independentFromNumberOfPlayers;
                gameValue.value = givenValue.value;
                gameValue._1P = givenValue._1P;
                gameValue._2P = givenValue._2P;
                gameValue._3P = givenValue._3P;
                gameValue._4P = givenValue._4P;
                gameValue.EZValue = givenValue.EZValue;
                gameValue.EZ1 = givenValue.EZ1;
                gameValue.EZ2 = givenValue.EZ2;
                gameValue.EZ3 = givenValue.EZ3;
                gameValue.EZ4 = givenValue.EZ4;
                return gameValue;
            }

            public static GameValue OffsetGameValue(GameValue givenValue, GameValue givenOriginal, float offset, float ezOffset) {
                givenValue.value = givenOriginal.value + offset;
                givenValue._1P = givenOriginal._1P + offset;
                givenValue._2P = givenOriginal._2P + offset;
                givenValue._3P = givenOriginal._3P + offset;
                givenValue._4P = givenOriginal._4P + offset;
                givenValue.EZValue = givenOriginal.EZValue + ezOffset;
                givenValue.EZ1 = givenOriginal.EZ1 + ezOffset;
                givenValue.EZ2 = givenOriginal.EZ2 + ezOffset;
                givenValue.EZ3 = givenOriginal.EZ3 + ezOffset;
                givenValue.EZ4 = givenOriginal.EZ4 + ezOffset;
                return givenValue;
            }


            [HarmonyPatch(typeof(SpaceshipGenerator))]
            [HarmonyPatch("RandomizeRooms")]
            public static class SpaceshipGenerateRandomizeRoomsIterations
            {
                static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
                    List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
                    int startIndexA = -1;
                    for (int codeIndex = 0; codeIndex < codes.Count - 3; codeIndex++) {
                        if (codes[codeIndex].opcode == OpCodes.Ldc_I4 && NullSafeToString(codes[codeIndex].operand) == "10000") {
                            if (codes[codeIndex + 1].opcode == OpCodes.Ble && NullSafeToString(codes[codeIndex + 1].operand) == "System.Reflection.Emit.Label") {
                                if (codes[codeIndex + 2].opcode == OpCodes.Br && NullSafeToString(codes[codeIndex + 2].operand) == "System.Reflection.Emit.Label") {
                                    startIndexA = codeIndex + 0;
                                    break;
                                }
                            }
                            
                        }
                        //plugin.Logger.LogInfo(codes[codeIndex].opcode.ToString() + " " + NullSafeToString(codes[codeIndex].operand));
                    }
                    if (startIndexA != -1) {
                        //codes.RemoveAt(startIndexA + 0);
                        //codes.Insert(startIndexA + 0, new CodeInstruction(OpCodes.Ldc_I4, 100000));
                    }
                    return codes.AsEnumerable();
                }
            }

            static public string NullSafeToString(object givenObject) {
                if (givenObject != null) {
                    return givenObject.ToString();
                } else {
                    return "null";
                }
            }
        }
    }
}

 