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
    namespace ChangeColour
    {
        [BepInPlugin(PluginGUID, "ChangeColour", "0.0.3")]
        [BepInDependency("com.Phedg1Studios.SelectionInputs", BepInDependency.DependencyFlags.HardDependency)]

        public class Plugin : BaseUnityPlugin
        {
            public const string PluginGUID = "com.Phedg1Studios.ChangeColour";
            public static Plugin plugin;
            public static byte changeColourPhotonEvent = 16;
            public Dictionary<string, int> playerColours = new Dictionary<string, int>();
            public static bool addingLocalPlayer = false;
            public static bool removingLocalPlayer = false;
            public static bool addingOnlinePlayer = false;
            public static bool removingOnlinePlayer = false;
            public static string currentPlayerId = null;
            public static List<string> currentPlayerIds = new List<string>();

            public static string changeColourDescriptiveName = PluginGUID + "." + "ChangeColour";
            public Dictionary<string, Dictionary<string, string>> localisations = new Dictionary<string, Dictionary<string, string>>() {
                {changeColourDescriptiveName, new Dictionary<string, string>() {
                    { "English", "Change Color" },
                } },
            };


            private void Awake() {
                plugin = this;
                var harmony = new Harmony(PluginGUID);
                harmony.PatchAll();
                CustomLocalisations.Plugin.AddLocalisations(localisations);
                Logger.LogInfo($"Plugin {PluginGUID} is loaded!");
            }
            
            [HarmonyPatch(typeof(PlayerSelectUI))]
            [HarmonyPatch("Update")]
            public static class PlayerSelectUIUpdate
            {
                static void Postfix(PlayerSelectUI __instance) {
                    List<PlayerSelectUI.PlayertUI> playerUis = __instance.playerUIs;
                    foreach (PlayerData localPlayer in __instance.players) {
                        if (!IsLocalPlayer(playerUis, localPlayer.Id)) {
                            continue;
                        }
                        Rewired.Player player = Rewired.ReInput.players.GetPlayer(localPlayer.ControllerId);
                        
                        if (player.GetButtonDown(SelectionInputs.CustomInputs.nextSelection)) {
                            RaiseChangeColour(__instance, localPlayer.Id, 1);
                        } 
                        if (player.GetButtonDown(SelectionInputs.CustomInputs.prevSelection)) {
                            RaiseChangeColour(__instance, localPlayer.Id, -1);
                        }
                    }
                }
            }

            public static bool IsLocalPlayer(List<PlayerSelectUI.PlayertUI> playerUis, string id) {
                foreach (PlayerSelectUI.PlayertUI playerUi in playerUis) {
                    if (playerUi.PlayerId != id) {
                        continue;
                    }
                    return playerUi.isLocal;
                }
                return false;
            }

            public static void RaiseChangeColour(PlayerSelectUI __instance, string playerId, int direction) {
                if (__instance.networkStatus == NetworkStatus.Disconnected) {
                    ChangePlayerColour(__instance, playerId, direction);
                    return;
                }

                Photon.Pun.PhotonNetwork.RaiseEvent(changeColourPhotonEvent, (object)new object[2] {
                    (object) playerId,
                    (object) direction
                }, new RaiseEventOptions() {
                    Receivers = ReceiverGroup.All
                }, new SendOptions() {
                    Reliability = true
                });
            }

            [HarmonyPatch(typeof(PlayerSelectUI))]
            [HarmonyPatch("OnEvent")]
            public static class PlayerSelectUIOnEvent
            {
                static void Postfix(PlayerSelectUI __instance, EventData photonEvent) {
                    if (!PhotonNetwork.IsMasterClient) {
                        return;
                    }
                    byte code = photonEvent.Code;
                    if ((int) code == (int) changeColourPhotonEvent) {
                        ChangeColourEvent(__instance, photonEvent);
                    }
                }
            }

            public static void ChangeColourEvent(PlayerSelectUI __instance, EventData photonEvent) {
                object[] customData = (object[])photonEvent.CustomData;
                string playerId = (string) customData[0];
                int direction = (int) customData[1];
                ChangePlayerColour(__instance, playerId, direction);
            }

            public static void ChangePlayerColour(PlayerSelectUI __instance, string playerId, int direction) {
                int colourId = plugin.GetColour(__instance, playerId, direction);
                plugin.playerColours[playerId] = colourId;
                RaiseSetColour(__instance, playerId, colourId);
            }

            private int GetColour(PlayerSelectUI __instance, string playerId, int direction) {
                int startIndex = plugin.playerColours[playerId];
                for (int index = 0; index < __instance.playerMaterials.Count; index++) {
                    int adjustedIndex = (startIndex + (1 + index) * direction + __instance.playerMaterials.Count) % __instance.playerMaterials.Count;
                    if (!playerColours.ContainsValue(adjustedIndex)) {
                        return adjustedIndex;
                    }
                }
                if (startIndex == -1) {
                    startIndex += 1;
                }
                return startIndex;
            }

            public static void RaiseSetColour(PlayerSelectUI __instance, string playerId, int colourId) {
                if (__instance.networkStatus == NetworkStatus.Disconnected) {
                    SetPlayerColour(__instance, playerId, colourId);
                    return;
                }

                // Add reflection to get event code using __instance here
                Photon.Pun.PhotonNetwork.RaiseEvent(3, (object)new object[2] {
                    (object) playerId,
                    (object) colourId
                }, new RaiseEventOptions() {
                    Receivers = ReceiverGroup.All
                }, new SendOptions() {
                    Reliability = true
                });
            }

            public static void SetPlayerColour(PlayerSelectUI __instance, string playerId, int colourId) {
                foreach (PlayerSelectUI.PlayertUI playerUi in __instance.playerUIs) {
                    if (playerUi.PlayerId == playerId) {
                        playerUi.SetColor(colourId, __instance.playerMaterials);
                    }
                }
                foreach (PlayerData localPlayerData in MultiplayerManager.instance.LocalPlayerDatas) {
                    if (localPlayerData.Id == playerId) {
                        localPlayerData.ColorId = colourId;
                    }
                }
            }

            public static PlayerData GetPlayerData(PlayerSelectUI __instance, string playerId) {
                foreach (PlayerData playerData in __instance.players) {
                    if (playerData.Id == playerId) {
                        return playerData;
                    }
                }
                return null;
            }

            [HarmonyPatch(typeof(PlayerSelectUI))]
            [HarmonyPatch("OnEnable")]
            public static class PlayerSelectUIOnEnable
            {
                static void Prefix(PlayerSelectUI __instance) {
                    plugin.playerColours.Clear();
                }
            }

            [HarmonyPatch(typeof(PlayerSelectUI))]
            [HarmonyPatch("AddLocalPlayer")]
            public static class PlayerSelectUIAddLocalPlayer
            {
                static void Prefix(PlayerSelectUI __instance, Player rewiredPlayer) {
                    addingLocalPlayer = true;
                }

                static void Postfix(PlayerSelectUI __instance, Player rewiredPlayer) {
                    addingLocalPlayer = false;
                    plugin.playerColours.Add(currentPlayerId, -1);
                    if (!PhotonNetwork.IsMasterClient && __instance.networkStatus != NetworkStatus.Disconnected) {
                        return;
                    }
                    ChangePlayerColour(__instance, currentPlayerId, 1);
                }
            }
            
            [HarmonyPatch(typeof(PlayerSelectUI))]
            [HarmonyPatch("LookForLocalPlayers")]
            public static class PlayerSelectUILookForLocalPlayers
            {
                static void Prefix(PlayerSelectUI __instance) {
                    removingLocalPlayer = true;
                }

                static void Postfix(PlayerSelectUI __instance) {
                    removingLocalPlayer = false;
                    foreach (string playerId in currentPlayerIds) {
                        plugin.playerColours.Remove(playerId);
                    }
                    if (currentPlayerIds.Count != 0) {
                        currentPlayerIds.Clear();
                    }
                }
            }

            [HarmonyPatch(typeof(PlayerSelectUI))]
            [HarmonyPatch("AddOnlinePlayer")]
            public static class PlayerSelectUIAddOnlinePlayer
            {
                static void Prefix(PlayerSelectUI __instance, Player player) {
                    addingOnlinePlayer = true;
                }

                static void Postfix(PlayerSelectUI __instance, Player player) {
                    addingOnlinePlayer = false;
                    plugin.playerColours.Add(currentPlayerId, -1);
                    if (!PhotonNetwork.IsMasterClient) {
                        return;
                    }
                    ChangePlayerColour(__instance, currentPlayerId, 1);
                }
            }
            
            [HarmonyPatch(typeof(PlayerSelectUI))]
            [HarmonyPatch("RemoveOnlinePlayer")]
            public static class PlayerSelectUIRemoveOnlinePlayer
            {
                static void Prefix(PlayerSelectUI __instance, Player player) {
                    removingOnlinePlayer = true;
                }

                static void Postfix(PlayerSelectUI __instance, Player player) {
                    removingOnlinePlayer = false;
                    plugin.playerColours.Remove(currentPlayerId);
                }
            }

            [HarmonyPatch(typeof(PlayerSelectUI))]
            [HarmonyPatch("ShowPlayerUI")]
            public static class PlayerSelectUIShowPlayerUI
            {
                static void Prefix(PlayerSelectUI __instance, string playerId) {
                    if (!addingLocalPlayer && !addingOnlinePlayer && !removingOnlinePlayer) {
                        currentPlayerId = null;
                        if (removingLocalPlayer) {
                            currentPlayerIds.Add(playerId);
                        }
                        return;
                    }
                    currentPlayerId = playerId;
                }
            }

            [HarmonyPatch(typeof(PlayerSelectUI))]
            [HarmonyPatch("UpdatePlayerColors")]
            public static class PlayerSelectUIUpdatePlayerColors
            {
                static bool Prefix(PlayerSelectUI __instance) {
                    return false;
                }
            }

            [HarmonyPatch(typeof(PlayerSelectUI))]
            [HarmonyPatch("Awake")]
            public static class PlayerSelectUIAwake
            {
                static void Postfix(PlayerSelectUI __instance) {
                    GameObject container = GameObject.Find("[CTN] Change Skin");
                    GameObject newContainer = GameObject.Instantiate(container, container.transform.parent);
                    newContainer.transform.SetSiblingIndex(container.transform.GetSiblingIndex());
                    newContainer.transform.localScale = container.transform.localScale;
                    newContainer.transform.localPosition = container.transform.localPosition;
                    newContainer.transform.localPosition = new Vector3(newContainer.transform.localPosition.x + 15, newContainer.transform.localPosition.y - 30, newContainer.transform.localPosition.z);
                    for (int childIndex = 0; childIndex < newContainer.transform.childCount; childIndex++) {
                        Transform child = newContainer.transform.GetChild(childIndex);
                        if (child.name == "[TXT] Change Skin") {
                            Assets.SimpleLocalization.LocalizedText localizedText = child.GetComponent<Assets.SimpleLocalization.LocalizedText>();
                            localizedText.UpdateKey(changeColourDescriptiveName);
                        } else if (child.name == "[PNL] Back" || child.name == "[PNL] Next") {
                            ControllerGlyph controllerGlyph = child.GetComponent<ControllerGlyph>();
                            if (child.name == "[PNL] Back") {
                                controllerGlyph.actionName = SelectionInputs.CustomInputs.prevSelection;
                            } else if (child.name == "[PNL] Next") {
                                controllerGlyph.actionName = SelectionInputs.CustomInputs.nextSelection;
                            }
                            controllerGlyph.SetLastActiveControllerGlyph();
                        }
                    }
                }
            }
        }
    }
}

 