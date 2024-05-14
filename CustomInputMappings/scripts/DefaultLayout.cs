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
        public class DefaultLayout
        {
            public string guidName;
            public Rewired.ControllerType controllerType;
            public int layoutId;
            public Rewired.ElementAssignment elementAssignment;

            public DefaultLayout(string guidName, Rewired.ControllerType controllerType, int layoutId, Rewired.ElementAssignment elementAssignment) {
                this.guidName = guidName;
                this.controllerType = controllerType;
                this.layoutId = layoutId;
                this.elementAssignment = elementAssignment;
            }
        }
    }
}

 