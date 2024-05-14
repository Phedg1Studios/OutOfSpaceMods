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
    namespace CustomInputMappings
    {
        public class CustomInput
        {
            public string guidName;
            public bool userEditable;
            public int categoryId;
            public int actionId;
            public int actionIndex;

            public CustomInput(string guidName, bool userEditable) {
                this.guidName = guidName;
                this.userEditable = userEditable;
            }
        }
    }
}

 