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
    namespace LogInput
    {
        [BepInPlugin(PluginGUID, "ChangeColour", "0.0.1")]
        [BepInDependency("com.Phedg1Studios.CustomInputMappings", BepInDependency.DependencyFlags.HardDependency)]

        public class Plugin : BaseUnityPlugin
        {
            public const string PluginGUID = "com.Phedg1Studios.LogInput";
            public static Plugin plugin;

            public FieldInfo userDataInfo = typeof(Rewired.ReInput).GetField("MFTggXcZfAYQOmqicqVlwKrEWkj", BindingFlags.NonPublic | BindingFlags.Static);

            public Dictionary<string, Dictionary<string, string>> localisations = new Dictionary<string, Dictionary<string, string>>() {
                {CustomInputs.logInput, new Dictionary<string, string>() {
                    { "English", "Log Input" },
                } },
            };

            public List<CustomInputMappings.CustomInput> customActions = new List<CustomInputMappings.CustomInput>() {
                new CustomInputMappings.CustomInput(
                    CustomInputs.logInput,
                    true
                ),
            };

            public static class CustomInputs {
                public static string logInput = PluginGUID + "." + "LogInput";
            }

            private void Awake() {
                plugin = this;
                var harmony = new Harmony(PluginGUID);
                harmony.PatchAll();
                Logger.LogInfo($"Plugin {PluginGUID} is loaded!");
                foreach (CustomInputMappings.CustomInput customAction in customActions) {
                    CustomInputMappings.Plugin.AddCustomInput(customAction);
                }
                foreach (string guidName in localisations.Keys) {
                    CustomInputMappings.Plugin.AddLocalisations(guidName, localisations[guidName]);
                }
                PatchSpecialMethods(harmony);
            }

            public static void PatchSpecialMethods(Harmony harmony) {
                Rewired.ElementAssignment elementAssignment;
                Rewired.ActionElementMap actionElementMap = null;
                MethodInfo replaceOrCreateElementMapInfo = typeof(Rewired.ControllerMap).GetMethod("ReplaceOrCreateElementMap", new System.Type[] { typeof(Rewired.ElementAssignment), typeof(Rewired.ActionElementMap).MakeByRefType() });
                MethodInfo controllerMapreplaceOrCreateElementMapInfoPostfixInfo = typeof(Plugin).GetMethod("ControllerMapreplaceOrCreateElementMapInfoPostfix", BindingFlags.Public | BindingFlags.Static);
                harmony.Patch(replaceOrCreateElementMapInfo, null, new HarmonyMethod(controllerMapreplaceOrCreateElementMapInfoPostfixInfo));
            }

            public static void ControllerMapreplaceOrCreateElementMapInfoPostfix(Rewired.ControllerMap __instance, Rewired.ElementAssignment elementAssignment, bool __result) {
                plugin.Logger.LogInfo("----------------");
                Rewired.Data.UserData userData = plugin.userDataInfo.GetValue(null) as Rewired.Data.UserData;
                Rewired.InputAction action = userData.GetActionById(elementAssignment.actionId);
                if (action.name != CustomInputs.logInput) {
                    return;
                }
                Rewired.Player player = Rewired.ReInput.players.GetPlayer(__instance.playerId);
                List<Rewired.ActionElementMap> maps = player.controllers.maps.ElementMapsWithAction(__instance.controllerType, __instance.controllerId, elementAssignment.actionId, false).ToList();
                foreach (Rewired.ActionElementMap map in maps) {
                    plugin.Logger.LogInfo("----");
                    plugin.Logger.LogInfo("Controller type: " + System.Enum.GetName(typeof(Rewired.ElementAssignmentType), map.controllerMap.controllerType));
                    plugin.Logger.LogInfo("Category id: " + map.controllerMap.categoryId);
                    plugin.Logger.LogInfo("Layout id: " + map.controllerMap.layoutId);
                    plugin.Logger.LogInfo("-");
                    plugin.Logger.LogInfo("Keyboard key code: " + System.Enum.GetName(typeof(UnityEngine.KeyCode), map.keyboardKeyCode));
                    plugin.Logger.LogInfo("Key code: " + System.Enum.GetName(typeof(UnityEngine.KeyCode), map.keyCode));
                    plugin.Logger.LogInfo("-");
                    plugin.Logger.LogInfo("Element type: " + System.Enum.GetName(typeof(Rewired.ControllerElementType), map.elementType));
                    plugin.Logger.LogInfo("Element id: " + map.elementIdentifierId);
                    plugin.Logger.LogInfo("Axis range: " + System.Enum.GetName(typeof(Rewired.AxisRange), map.axisRange));
                    plugin.Logger.LogInfo("Axis contribution: " + System.Enum.GetName(typeof(Rewired.Pole), map.axisContribution));
                    plugin.Logger.LogInfo("-");
                    plugin.Logger.LogInfo("Modifier key flags: " + System.Enum.GetName(typeof(Rewired.ModifierKeyFlags), map.modifierKeyFlags));
                    plugin.Logger.LogInfo("Modifier key 1: " + System.Enum.GetName(typeof(Rewired.ModifierKey), map.modifierKey1));
                    plugin.Logger.LogInfo("Modifier key 2: " + System.Enum.GetName(typeof(Rewired.ModifierKey), map.modifierKey2));
                    plugin.Logger.LogInfo("Modifier key 3: " + System.Enum.GetName(typeof(Rewired.ModifierKey), map.modifierKey3));
                    plugin.Logger.LogInfo("Invert" + map.invert.ToString());
                    plugin.Logger.LogInfo("----");
                }
                plugin.Logger.LogInfo("----------------");
                return;
            }
        }
    }
}

 