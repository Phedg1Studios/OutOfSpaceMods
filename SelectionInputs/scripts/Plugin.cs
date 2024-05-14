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
    namespace SelectionInputs
    {
        [BepInPlugin(PluginGUID, "SelectionInputs", "0.0.1")]
        [BepInDependency("com.Phedg1Studios.CustomInputMappings", BepInDependency.DependencyFlags.HardDependency)]

        public class Plugin : BaseUnityPlugin
        {
            public const string PluginGUID = "com.Phedg1Studios.SelectionInputs";
            public static Plugin plugin;

            public Dictionary<string, Dictionary<string, string>> localisations = new Dictionary<string, Dictionary<string, string>>() {
                {CustomInputs.prevSelection, new Dictionary<string, string>() {
                    { "English", "Previous Selection" },
                } },
                {CustomInputs.nextSelection, new Dictionary<string, string>() {
                    { "English", "Next Selection" },
                } },
            };

            public List<CustomInputMappings.CustomInput> customActions = new List<CustomInputMappings.CustomInput>() {
                new CustomInputMappings.CustomInput(
                    CustomInputs.prevSelection,
                    true
                ),
                new CustomInputMappings.CustomInput(
                    CustomInputs.nextSelection,
                    true
                ),
            };

            public List<CustomInputMappings.DefaultLayout> defaultLayouts = new List<CustomInputMappings.DefaultLayout>() {
                new CustomInputMappings.DefaultLayout(
                    CustomInputs.prevSelection,
                    Rewired.ControllerType.Keyboard,
                    0,
                    new Rewired.ElementAssignment(Rewired.ControllerType.Keyboard, Rewired.ControllerElementType.Button, 0, Rewired.AxisRange.Positive, UnityEngine.KeyCode.Alpha3, Rewired.ModifierKeyFlags.None, 0, Rewired.Pole.Positive, false)
                ),
                new CustomInputMappings.DefaultLayout(
                    CustomInputs.prevSelection,
                    Rewired.ControllerType.Joystick,
                    0,
                    new Rewired.ElementAssignment(Rewired.ControllerType.Joystick, Rewired.ControllerElementType.Axis, 0, Rewired.AxisRange.Positive, UnityEngine.KeyCode.None, Rewired.ModifierKeyFlags.None, 0, Rewired.Pole.Positive, false)
                ),
                new CustomInputMappings.DefaultLayout(
                    CustomInputs.nextSelection,
                    Rewired.ControllerType.Keyboard,
                    0,
                    new Rewired.ElementAssignment(Rewired.ControllerType.Keyboard, Rewired.ControllerElementType.Button, 0, Rewired.AxisRange.Positive, UnityEngine.KeyCode.Alpha4, Rewired.ModifierKeyFlags.None, 0, Rewired.Pole.Positive, false)
                ),
                new CustomInputMappings.DefaultLayout(
                    CustomInputs.nextSelection,
                    Rewired.ControllerType.Joystick,
                    0,
                    new Rewired.ElementAssignment(Rewired.ControllerType.Joystick, Rewired.ControllerElementType.Axis, 0, Rewired.AxisRange.Positive, UnityEngine.KeyCode.None, Rewired.ModifierKeyFlags.None, 0, Rewired.Pole.Positive, false)
                ),
            };

            private void Awake() {
                plugin = this;
                var harmony = new Harmony(PluginGUID);
                harmony.PatchAll();
                foreach (CustomInputMappings.CustomInput customAction in customActions) {
                    CustomInputMappings.Plugin.AddCustomInput(customAction);
                }
                foreach (CustomInputMappings.DefaultLayout defaultLayout in defaultLayouts) {
                    CustomInputMappings.Plugin.AddDefaultLayout(defaultLayout);
                }
                foreach (string guidName in localisations.Keys) {
                    CustomInputMappings.Plugin.AddLocalisations(guidName, localisations[guidName]);
                }
                Logger.LogInfo($"Plugin {PluginGUID} is loaded!");
            }
        }
    }
}

 