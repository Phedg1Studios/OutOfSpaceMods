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
    namespace ChallengesOnOtherShipSizes
    {
        [BepInPlugin(PluginGUID, "ChallengesOnOtherShipSizes", "0.0.1")]

        public class Plugin : BaseUnityPlugin
        {
            public const string PluginGUID = "com.Phedg1Studios.ChallengesOnOtherShipSizes";
            public static Plugin plugin;

            public PlayerSelectUI playerSelectUI;
            public FieldInfo startGamePhotonEvent;
            private string shipSizeKey = "shipSize";

            private void Awake() {
                plugin = this;
                var harmony = new Harmony(PluginGUID);
                harmony.PatchAll();
                Logger.LogInfo($"Plugin {PluginGUID} is loaded!");
                startGamePhotonEvent = typeof(PlayerSelectUI).GetField("StartGamePhotonEvent", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            }


            [HarmonyPatch(typeof(PlayerSelectUI))]
            [HarmonyPatch("OnEvent")]
            public static class PlayerSelectUIOnEvent
            {
                static void Prefix(PlayerSelectUI __instance, ExitGames.Client.Photon.EventData photonEvent) {
                    if (photonEvent.Code == (byte) plugin.startGamePhotonEvent.GetValue(__instance)) {
                        plugin.playerSelectUI = __instance;
                    }
                }
            }

            [HarmonyPatch(typeof(Room))]
            [HarmonyPatch("SetCustomProperties")]
            public static class RoomSetCustomProperties
            {
                static void Prefix(Room __instance, ref ExitGames.Client.Photon.Hashtable propertiesToSet, ExitGames.Client.Photon.Hashtable expectedProperties, WebFlags webFlags) {
                    bool containedShipSize = false;
                    IDictionary propertiesDictionary = (IDictionary)propertiesToSet;
                    foreach (string propertyKey in propertiesDictionary.Keys) {
                        if (propertyKey == plugin.shipSizeKey) {
                            containedShipSize = true;
                        }
                    }
                    if (!containedShipSize) {
                        return;
                    }
                    propertiesDictionary[plugin.shipSizeKey] = plugin.playerSelectUI.shipSizeCarousel.GetCurrentValue();
                    propertiesToSet = (ExitGames.Client.Photon.Hashtable) propertiesDictionary;
                }
            }
        }
    }
}

 