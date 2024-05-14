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

        public static class CustomInputs
        {
            public static string prevSelection = Plugin.PluginGUID + "." + "PrevSelection";
            public static string nextSelection = Plugin.PluginGUID + "." + "NextSelection";
        }
    }
}

 