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
        [BepInPlugin(PluginGUID, "CustomInputMappings", "0.0.2")]
        [BepInDependency("com.Phedg1Studios.CustomLocalisations", BepInDependency.DependencyFlags.HardDependency)]

        public class Plugin : BaseUnityPlugin {
            public const string PluginGUID = "com.Phedg1Studios.CustomInputMappings";
            public static Plugin plugin;

            public Dictionary<string, CustomInput> customInputs = new Dictionary<string, CustomInput>();
            public List<int> otherActionIds = new List<int>();
            public Dictionary<string, int> actionIds = new Dictionary<string, int>();
            public Dictionary<string, int> categoryIds = new Dictionary<string, int>();
            public Dictionary<int, int> categoryRemaps = new Dictionary<int, int>();
            public Dictionary<int, int> actionsRemaining = new Dictionary<int, int>();
            public int startingAction = 30;
            public int endingAction = 256;
            public int maxActionsPerCategory = 20;

            public List<DefaultLayout> defaultLayouts = new List<DefaultLayout>();

            public string actionIdsConfigPath = "";
            public string categoryIdsConfigPath = "";
            public readonly char splitChar = '=';
            public readonly char newLine = '\n';

            public List<Rewired.UI.ControlMapper.ThemedElement> categorybuttons = new List<Rewired.UI.ControlMapper.ThemedElement>();
            public string label = "Modded";
            public string labelJoiner = " ";
            public Color disabledColour = new Color(0, 0, 0, 150f / 255f);
            public Color normalColour = new Color(0.475f * (245f / 255f), 0.227f * (245f / 255f), 0.528f * (245f / 255f), 0.941f);
            public Color pressedColor = new Color(245f / 255f, 245f / 255f, 245f / 255f, 1);

            public static MethodInfo initialiseRewiredActionsInfo = typeof(Plugin).GetMethod("InitialiseRewiredActions", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            public FieldInfo actionsInfo = typeof(Rewired.Data.UserData).GetField("actions", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            public FieldInfo inputActionNameInfo = typeof(Rewired.InputAction).GetField("_name", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            public FieldInfo inputActionDescriptiveNameInfo = typeof(Rewired.InputAction).GetField("_descriptiveName", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            public FieldInfo inputActionIdInfo = typeof(Rewired.InputAction).GetField("_id", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            public FieldInfo inputActionUserEditableInfo = typeof(Rewired.InputAction).GetField("_userAssignable", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            public FieldInfo actionCategoryMapInfo = typeof(Rewired.Data.UserData).GetField("actionCategoryMap", BindingFlags.NonPublic | BindingFlags.Instance);
            public FieldInfo actionCategoriesInfo = typeof(Rewired.Data.UserData).GetField("actionCategories", BindingFlags.NonPublic | BindingFlags.Instance);
            public FieldInfo mapCategoriesInfo = typeof(Rewired.Data.UserData).GetField("mapCategories", BindingFlags.NonPublic | BindingFlags.Instance);
            public FieldInfo idInfo = typeof(Rewired.InputCategory).GetField("_id", BindingFlags.NonPublic | BindingFlags.Instance);
            public FieldInfo descriptiveNameInfo = typeof(Rewired.InputCategory).GetField("_descriptiveName", BindingFlags.NonPublic | BindingFlags.Instance);
            public FieldInfo listInfo = typeof(Rewired.Data.Mapping.ActionCategoryMap).GetField("list", BindingFlags.NonPublic | BindingFlags.Instance);
            public FieldInfo userDataInfo = typeof(Rewired.ReInput).GetField("MFTggXcZfAYQOmqicqVlwKrEWkj", BindingFlags.NonPublic | BindingFlags.Static);
            public FieldInfo mapCategoryButtonsInfo = typeof(Rewired.UI.ControlMapper.ControlMapper).GetField("mapCategoryButtons", BindingFlags.NonPublic | BindingFlags.Instance);
            public FieldInfo selectableInfo = null;
            public FieldInfo mappingSetsInfo = typeof(Rewired.UI.ControlMapper.ControlMapper).GetField("_mappingSets", BindingFlags.NonPublic | BindingFlags.Instance);


            private void Awake() {
                plugin = this;
                GeneratePaths();
                LoadActionIds();
                LoadCategoryIds();
                var harmony = new Harmony(PluginGUID);
                harmony.PatchAll();
                Logger.LogInfo($"Plugin {PluginGUID} is loaded!");
            }

            public static bool AddCustomInput(string guidName, string descriptiveName, bool userEditable) {
                CustomInput customInput = new CustomInput(guidName, userEditable);
                return AddCustomInput(customInput);
            }

            public static int AddCustomInputs(List<CustomInput> customInputs) {
                int result = 0;
                foreach (CustomInput customInput in customInputs) {
                    if (AddCustomInput(customInput)) {
                        result += 1;
                    }
                }
                return result;
            }

            public static bool AddCustomInput(CustomInput customInput) {
                if (plugin.customInputs.ContainsKey(customInput.guidName)) {
                    return false;
                }
                plugin.customInputs.Add(customInput.guidName, customInput);
                return true;
            }

            [HarmonyPatch(typeof(Rewired.ReInput))]
            [HarmonyPatch("FsxtQGDGiEFDYidBNnBSrUPRxJH")]
            public static class ReInputFsxtQGDGiEFDYidBNnBSrUPRxJH
            {
                static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
                    List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
                    int startIndexA = 0;
                    codes.Insert(startIndexA + 0, new CodeInstruction(OpCodes.Ldarg_0));
                    codes.Insert(startIndexA + 1, new CodeInstruction(OpCodes.Ldarg_1));
                    codes.Insert(startIndexA + 2, new CodeInstruction(OpCodes.Ldarg_2));
                    codes.Insert(startIndexA + 3, new CodeInstruction(OpCodes.Ldarg_3));
                    codes.Insert(startIndexA + 4, new CodeInstruction(OpCodes.Ldarg_S, 4));
                    codes.Insert(startIndexA + 5, new CodeInstruction(OpCodes.Call, initialiseRewiredActionsInfo));
                    codes.Insert(startIndexA + 6, new CodeInstruction(OpCodes.Starg_S, 4));
                    return codes.AsEnumerable();
                }
            }

            public static Rewired.Data.UserData InitialiseRewiredActions(Rewired.InputManager_Base param0, System.Func<Rewired.Data.ConfigVars, object> param1, Rewired.Data.ConfigVars param2, Rewired.Data.ControllerDataFiles param3, Rewired.Data.UserData param4) {
                AddPresentCategories(param4);
                AddOtherActionIds(param4);
                int currentIndex = param4.GetActions_Copy().Count;

                Dictionary<string, int> actionIndex = new Dictionary<string, int>();
                foreach (string guidName in plugin.customInputs.Keys) {
                    int categoryId = 0;
                    if (plugin.customInputs[guidName].userEditable) {
                        if (plugin.categoryIds.ContainsKey(guidName)) {
                            categoryId = plugin.categoryIds[guidName];
                        } else {
                            categoryId = GetNextCategoryId(param4);
                            plugin.actionsRemaining[categoryId] -= 1;
                            plugin.categoryIds.Add(guidName, categoryId);
                        }
                    }
                    plugin.customInputs[guidName].categoryId = categoryId;
                    plugin.customInputs[guidName].categoryId = categoryId;
                    plugin.customInputs[guidName].actionIndex = currentIndex;
                    param4.AddAction(categoryId);
                    currentIndex += 1;
                }

                List<Rewired.InputAction> inputActions = new List<Rewired.InputAction>();
                ICollection collection = plugin.actionsInfo.GetValue(param4) as ICollection;
                foreach (object item in collection) {
                    Rewired.InputAction inputAction = (Rewired.InputAction)item;
                    inputActions.Add(inputAction);
                }

                Rewired.Data.Mapping.ActionCategoryMap actionCategoryMap = plugin.actionCategoryMapInfo.GetValue(param4) as Rewired.Data.Mapping.ActionCategoryMap;
                foreach (CustomInput customInput in plugin.customInputs.Values) {
                    Rewired.InputAction inputAction = inputActions[customInput.actionIndex];
                    actionCategoryMap.RemoveAction(customInput.categoryId, inputAction.id);
                }

                foreach (CustomInput customInput in plugin.customInputs.Values) {
                    if (plugin.actionIds.ContainsKey(customInput.guidName)) {
                        customInput.actionId = plugin.actionIds[customInput.guidName];
                    } else {
                        customInput.actionId = GetNextActionId();
                        plugin.actionIds.Add(customInput.guidName, customInput.actionId);
                    }
                    actionCategoryMap.AddAction(customInput.categoryId, customInput.actionId);
                    plugin.inputActionNameInfo.SetValue(inputActions[customInput.actionIndex], customInput.guidName);
                    plugin.inputActionDescriptiveNameInfo.SetValue(inputActions[customInput.actionIndex], customInput.guidName);
                    plugin.inputActionDescriptiveNameInfo.SetValue(inputActions[customInput.actionIndex], customInput.guidName);
                    plugin.inputActionUserEditableInfo.SetValue(inputActions[customInput.actionIndex], customInput.userEditable);
                    plugin.inputActionIdInfo.SetValue(inputActions[customInput.actionIndex], customInput.actionId);
                }

                plugin.actionCategoryMapInfo.SetValue(param4, actionCategoryMap);
                plugin.actionsInfo.SetValue(param4, inputActions);
                SaveActionIds();
                SaveCategoryIds();

                List<int> presentCategories = GetPresentCategories();
                ICollection mapCategoriesCollection = plugin.mapCategoriesInfo.GetValue(param4) as ICollection;
                List<Rewired.InputMapCategory> inputCategories = new List<Rewired.InputMapCategory>();
                foreach (object mapCategoriesObject in mapCategoriesCollection) {
                    Rewired.InputMapCategory inputCategory = (Rewired.InputMapCategory)mapCategoriesObject;
                    if (presentCategories.Contains(inputCategory.id)) {
                        plugin.descriptiveNameInfo.SetValue(inputCategory, GetCategoryName(presentCategories.Count, presentCategories.IndexOf(inputCategory.id)));
                    }
                    inputCategories.Add(inputCategory);
                }
                plugin.mapCategoriesInfo.SetValue(param4, inputCategories);

                return param4;
            }

            [HarmonyPatch(typeof(Rewired.InputManager))]
            [HarmonyPatch("OnInitialized")]
            public static class InputManagerOnInitialized
            {
                static void Postfix(Rewired.InputManager __instance) {
                    List<int> categories = GetPresentCategories();
                    foreach (Rewired.Player player in Rewired.ReInput.players.AllPlayers) {
                        foreach (int category in categories) {
                            player.controllers.maps.SetMapsEnabled(true, category);
                        }
                    }
                }
            }

            public static string GetCategoryName(int count, int index) {
                string label = plugin.label;
                if (count > 1) {
                    label += plugin.labelJoiner + (index + 1).ToString();
                }
                return label;
            }

            public static int AddCategory(Rewired.Data.UserData userData) {
                userData.AddMapCategory();
                userData.AddActionCategory();
                int[] mapCategories = userData.GetMapCategoryIds();
                int newCategory = mapCategories[mapCategories.Length - 1];

                ICollection actionCategoriesCollection = plugin.actionCategoriesInfo.GetValue(userData) as ICollection;
                List<Rewired.InputCategory> inputCategoriesNew = new List<Rewired.InputCategory>();
                foreach (object inputCategoryObject in actionCategoriesCollection) {
                    Rewired.InputCategory inputCategory = (Rewired.InputCategory)inputCategoryObject;
                    inputCategoriesNew.Add(inputCategory);
                }
                Rewired.InputCategory inputCategoryNew = inputCategoriesNew[inputCategoriesNew.Count - 1];
                plugin.idInfo.SetValue(inputCategoryNew, newCategory);
                inputCategoriesNew[inputCategoriesNew.Count - 1] = inputCategoryNew;
                plugin.actionCategoriesInfo.SetValue(userData, inputCategoriesNew);

                Rewired.Data.Mapping.ActionCategoryMap actionCategoryMap = plugin.actionCategoryMapInfo.GetValue(userData) as Rewired.Data.Mapping.ActionCategoryMap;
                ICollection listCollection = plugin.listInfo.GetValue(actionCategoryMap) as ICollection;
                List<Rewired.Data.Mapping.ActionCategoryMap.Entry> entries = new List<Rewired.Data.Mapping.ActionCategoryMap.Entry>();
                foreach (object item in listCollection) {
                    Rewired.Data.Mapping.ActionCategoryMap.Entry entry = (Rewired.Data.Mapping.ActionCategoryMap.Entry)item;
                    entries.Add(entry);
                }
                plugin.categoryRemaps.Add(entries[entries.Count - 1].categoryId, newCategory);
                entries[entries.Count - 1].categoryId = newCategory;
                plugin.listInfo.SetValue(actionCategoryMap, entries);

                if (!plugin.actionsRemaining.ContainsKey(newCategory)) {
                    plugin.actionsRemaining.Add(newCategory, plugin.maxActionsPerCategory);
                }
                return newCategory;
            }

            public static void AddPresentCategories(Rewired.Data.UserData userData) {
                int highestCategory = -1;
                foreach (CustomInput customInput in plugin.customInputs.Values) {
                    if (plugin.categoryIds.ContainsKey(customInput.guidName)) {
                        highestCategory = Mathf.Max(plugin.categoryIds[customInput.guidName], highestCategory);
                    }
                }
                if (highestCategory > -1) {
                    for (int attempt = 0; attempt < highestCategory; attempt++) {
                        int categoryId = AddCategory(userData);
                        if (categoryId >= highestCategory) {
                            break;
                        }
                    }
                }
            }

            public static void AddOtherActionIds(Rewired.Data.UserData userData) {
                plugin.otherActionIds.Clear();
                int[] otherActionIds = userData.GetActionIds();
                foreach (int actionId in otherActionIds) {
                    plugin.otherActionIds.Add(actionId);
                }
            }

            public static int GetNextActionId() {
                for (int id = plugin.startingAction; id < plugin.endingAction; id++) {
                    if (plugin.actionIds.ContainsValue(id)) {
                        continue;
                    }
                    if (plugin.otherActionIds.Contains(id)) {
                        continue;
                    }
                    return id;
                }
                return -1;
            }

            public static int GetNextCategoryId(Rewired.Data.UserData userData) {
                int newCategory = -1;
                List<int> categoryIds = plugin.actionsRemaining.Keys.ToList();
                categoryIds.Sort();
                foreach (int categoryId in categoryIds) {
                    if (plugin.actionsRemaining[categoryId] <= 0) {
                        continue;
                    }
                    newCategory = categoryId;
                    break;
                }
                if (newCategory == -1) {
                    int highestCategory = 0;
                    if (categoryIds.Count > 0) {
                        highestCategory = categoryIds.Count - 1;
                    } else {
                        int[] existingCategoryIds = userData.GetActionCategoryIds();
                        foreach (int existingCategoryId in existingCategoryIds) {
                            highestCategory = Mathf.Max(existingCategoryId, highestCategory);
                        }
                    }
                    for (int attempt = 0; attempt < highestCategory + 1; attempt++) {
                        int categoryId = AddCategory(userData);
                        if (categoryId > highestCategory) {
                            newCategory = categoryId;
                            break;
                        }
                    }
                }
                return newCategory;
            }

            public static void AddDefaultLayouts(List<DefaultLayout> defaultLayouts) {
                foreach (DefaultLayout defaultLayout in defaultLayouts) {
                    AddDefaultLayout(defaultLayout);
                }
            }

            public static void AddDefaultLayout(DefaultLayout defaultLayout) {
                plugin.defaultLayouts.Add(defaultLayout);
            }

            [HarmonyPatch(typeof(Rewired.Data.UserDataStore_PlayerPrefs))]
            [HarmonyPatch("AddDefaultMappingsForNewActions")]
            public static class UserDataStorePlayerPrefsAddDefaultMappingsForNewActions
            {
                static void Postfix(PlayerSelectUI __instance, Rewired.ControllerIdentifier controllerIdentifier, Rewired.ControllerMap controllerMap, List<int> knownActionIds) {
                    CreateElementMap(controllerMap, knownActionIds);
                }
            }

            public static void CreateElementMap(Rewired.ControllerMap controllerMap, List<int> knownActionIds) {
                foreach (DefaultLayout defaultLayout in plugin.defaultLayouts) {
                    if (!plugin.customInputs.ContainsKey(defaultLayout.guidName)) {
                        continue;
                    }
                    if (controllerMap.controllerType != defaultLayout.controllerType) {
                        continue;
                    }
                    if (controllerMap.categoryId != plugin.customInputs[defaultLayout.guidName].categoryId) {
                        continue;
                    }
                    if (controllerMap.layoutId != defaultLayout.layoutId) {
                        continue;
                    }
                    if (!plugin.actionIds.ContainsKey(defaultLayout.guidName)) {
                        continue;
                    }
                    int actionId = plugin.actionIds[defaultLayout.guidName];
                    if (knownActionIds.Contains(actionId)) {
                        continue;
                    }
                    defaultLayout.elementAssignment.actionId = actionId;
                    Rewired.ElementAssignmentConflictCheck conflictCheck = defaultLayout.elementAssignment.ToElementAssignmentConflictCheck();
                    if (controllerMap.DoesElementAssignmentConflict(conflictCheck)) {
                        return;
                    }
                    controllerMap.CreateElementMap(defaultLayout.elementAssignment);
                }
            }

            [HarmonyPatch(typeof(Rewired.Player.ControllerHelper.MapHelper))]
            [HarmonyPatch("LoadDefaultMaps")]
            public static class MapHelperLoadDefaultMaps
            {
                static void Postfix(Rewired.Player.ControllerHelper.MapHelper __instance, Rewired.ControllerType controllerType) {

                    IEnumerable<Rewired.ControllerMap> maps = __instance.GetAllMaps();
                    List<int> uniqueIds = new List<int>();
                    foreach (Rewired.ControllerMap map in maps) {
                        if (!uniqueIds.Contains(map.controllerId)) {
                            uniqueIds.Add(map.controllerId);
                        }
                    }
                    List<int> presentCategories = GetPresentCategories();
                    foreach (int uniqueId in uniqueIds) {
                        foreach (int categoryId in presentCategories) {
                            __instance.AddEmptyMap(controllerType, uniqueId, categoryId, 0);
                        }
                    }
                    maps = __instance.GetAllMaps();
                    foreach (Rewired.ControllerMap map in maps) {
                        CreateElementMap(map, new List<int>());
                    }
                }
            }

            public static void AddLocalisations(Dictionary<string, Dictionary<string, string>> localisations) {
                CustomLocalisations.Plugin.AddLocalisations(localisations);
            }

            public static void AddLocalisations(string guidName, Dictionary<string, string> localisations) {
                CustomLocalisations.Plugin.AddLocalisations(guidName, localisations);
            }

            public static void AddLocalisation(string guidName, string language, string localisation) {
                CustomLocalisations.Plugin.AddLocalisation(guidName, language, localisation);
            }

            public void GeneratePaths() {
                actionIdsConfigPath = BepInEx.Paths.BepInExRootPath + "/config/" + PluginGUID + ".ActionIds.cfg";
                categoryIdsConfigPath = BepInEx.Paths.BepInExRootPath + "/config/" + PluginGUID + ".CategoryIds.cfg";
            }

            public static void LoadActionIds() {
                if (!File.Exists(plugin.actionIdsConfigPath)) {
                    return;
                }
                List<string> lines = new List<string>();
                StreamReader reader = new StreamReader(plugin.actionIdsConfigPath);
                while (reader.Peek() >= 0) {
                    lines.Add(reader.ReadLine());
                }
                reader.Close();
                foreach (string lineRaw in lines) {
                    string[] splitLine = lineRaw.Split(plugin.splitChar);
                    if (splitLine.Length < 2) {
                        continue;
                    }
                    if (plugin.actionIds.ContainsKey(splitLine[0])) {
                        continue;
                    }
                    int id;
                    if (!int.TryParse(splitLine[1], out id)) {
                        continue;
                    }
                    plugin.actionIds.Add(splitLine[0], id);
                }
            }

            public static List<int> GetPresentCategories() {
                List<int> presentCategories = new List<int>();
                foreach (CustomInput customInput in plugin.customInputs.Values) {
                    if (!presentCategories.Contains(customInput.categoryId)) {
                        presentCategories.Add(customInput.categoryId);
                    }
                }
                presentCategories.Sort();
                return presentCategories;
            }

            public static void LoadCategoryIds() {
                if (!File.Exists(plugin.categoryIdsConfigPath)) {
                    return;
                }
                List<string> lines = new List<string>();
                StreamReader reader = new StreamReader(plugin.categoryIdsConfigPath);
                while (reader.Peek() >= 0) {
                    lines.Add(reader.ReadLine());
                }
                reader.Close();
                foreach (string lineRaw in lines) {
                    string[] splitLine = lineRaw.Split(plugin.splitChar);
                    if (splitLine.Length < 2) {
                        continue;
                    }
                    if (plugin.categoryIds.ContainsKey(splitLine[0])) {
                        continue;
                    }
                    int id;
                    if (!int.TryParse(splitLine[1], out id)) {
                        continue;
                    }
                    plugin.categoryIds.Add(splitLine[0], id);
                    if (!plugin.actionsRemaining.ContainsKey(id)) {
                        plugin.actionsRemaining.Add(id, plugin.maxActionsPerCategory);
                    }
                    plugin.actionsRemaining[id] -= 1;
                }
            }

            public static void SaveActionIds() {
                List<string> lines = new List<string>();
                List<string> keys = plugin.actionIds.Keys.ToList();
                keys.Sort();
                foreach (string key in plugin.actionIds.Keys) {
                    lines.Add(key + plugin.splitChar + plugin.actionIds[key].ToString());
                }
                string content = string.Join(plugin.newLine.ToString(), lines.ToArray());
                StreamWriter writer = new StreamWriter(plugin.actionIdsConfigPath, false);
                writer.Write(content);
                writer.Close();
            }

            public static void SaveCategoryIds() {
                List<string> lines = new List<string>();
                List<string> keys = plugin.categoryIds.Keys.ToList();
                keys.Sort();
                foreach (string key in keys) {
                    lines.Add(key + plugin.splitChar + plugin.categoryIds[key].ToString());
                }
                string content = string.Join(plugin.newLine.ToString(), lines.ToArray());
                StreamWriter writer = new StreamWriter(plugin.categoryIdsConfigPath, false);
                writer.Write(content);
                writer.Close();
            }

            [HarmonyPatch(typeof(Rewired.UI.ControlMapper.ControlMapper))]
            [HarmonyPatch("Awake")]
            public static class ControlMapperAwake
            {
                static void Prefix(Rewired.UI.ControlMapper.ControlMapper __instance) {
                    Rewired.Data.UserData userData = plugin.userDataInfo.GetValue(null) as Rewired.Data.UserData;

                    
                    ICollection collection = plugin.mappingSetsInfo.GetValue(__instance) as ICollection;
                    List<Rewired.UI.ControlMapper.ControlMapper.MappingSet> mappingSets = new List<Rewired.UI.ControlMapper.ControlMapper.MappingSet>();
                    foreach (object item in collection) {
                        mappingSets.Add((Rewired.UI.ControlMapper.ControlMapper.MappingSet)item);
                    }
                    List<int> presentCategories = GetPresentCategories();
                    foreach (int categoryId in presentCategories) {
                        mappingSets.Add(System.Activator.CreateInstance(typeof(Rewired.UI.ControlMapper.ControlMapper.MappingSet), BindingFlags.NonPublic | BindingFlags.Instance, null, new object[] { categoryId, Rewired.UI.ControlMapper.ControlMapper.MappingSet.ActionListMode.ActionCategory, new int[] { categoryId }, new int[] { } }, null, null) as Rewired.UI.ControlMapper.ControlMapper.MappingSet);
                    }
                    plugin.mappingSetsInfo.SetValue(__instance, mappingSets.ToArray());

                    Rewired.Data.Mapping.ActionCategoryMap actionCategoryMap = plugin.actionCategoryMapInfo.GetValue(userData) as Rewired.Data.Mapping.ActionCategoryMap;
                    ICollection listCollection = plugin.listInfo.GetValue(actionCategoryMap) as ICollection;
                    List<Rewired.Data.Mapping.ActionCategoryMap.Entry> entries = new List<Rewired.Data.Mapping.ActionCategoryMap.Entry>();
                    foreach (object item in listCollection) {
                        Rewired.Data.Mapping.ActionCategoryMap.Entry entry = (Rewired.Data.Mapping.ActionCategoryMap.Entry)item;
                        entries.Add(entry);
                    }
                    for (int index = 0; index < entries.Count; index++) {
                        if (!plugin.categoryRemaps.ContainsKey(entries[index].categoryId)) {
                            continue;
                        }
                        entries[index].categoryId = plugin.categoryRemaps[entries[index].categoryId];
                    }
                    plugin.listInfo.SetValue(actionCategoryMap, entries);
                }
            }

            [HarmonyPatch(typeof(Rewired.UI.ControlMapper.ControlMapper))]
            [HarmonyPatch("DrawMapCategoriesGroup")]
            public static class ControlMapperDrawMapCategoriesGroup
            {
                static void Postfix(Rewired.UI.ControlMapper.ControlMapper __instance) {
                    FieldInfo referencesInfo = typeof(Rewired.UI.ControlMapper.ControlMapper).GetField("references", BindingFlags.NonPublic | BindingFlags.Instance);
                    object references = referencesInfo.GetValue(__instance);
                    FieldInfo mapCategoriesGroupInfo = references.GetType().GetField("_mapCategoriesGroup", BindingFlags.NonPublic | BindingFlags.Instance);
                    Rewired.UI.ControlMapper.UIGroup mapCategoriesGroup = mapCategoriesGroupInfo.GetValue(references) as Rewired.UI.ControlMapper.UIGroup;
                    mapCategoriesGroup.SetLabelActive(false);

                    int height = 45;
                    Vector2 sizeDelta;

                    RectTransform content = mapCategoriesGroup.content.parent.parent.parent.GetComponent<RectTransform>();
                    sizeDelta = content.sizeDelta;
                    sizeDelta[1] += height;
                    content.sizeDelta = sizeDelta;

                    RectTransform background = content.parent.parent.GetComponent<RectTransform>();
                    sizeDelta = background.sizeDelta;
                    sizeDelta[1] += height;
                    background.sizeDelta = sizeDelta;

                    UnityEngine.UI.VerticalLayoutGroup verticalLayoutGroup = content.GetComponent<UnityEngine.UI.VerticalLayoutGroup>();
                    verticalLayoutGroup.spacing = 0;
                }
            }

            [HarmonyPatch(typeof(Rewired.UI.ControlMapper.ControlMapper))]
            [HarmonyPatch("DrawMapCategoriesGroup")]
            public static class DrawMapCategoriesGroup
            {
                static void Postfix(Rewired.UI.ControlMapper.ControlMapper __instance) {
                    plugin.categorybuttons.Clear();

                    ICollection collection = plugin.mapCategoryButtonsInfo.GetValue(__instance) as ICollection;
                    foreach (object mapCategoryButton in collection) {
                        if (plugin.selectableInfo == null) {
                            plugin.selectableInfo = mapCategoryButton.GetType().GetField("selectable", BindingFlags.Public | BindingFlags.Instance);
                        }
                        UnityEngine.UI.Selectable selectable = plugin.selectableInfo.GetValue(mapCategoryButton) as UnityEngine.UI.Selectable;
                        plugin.categorybuttons.Add(selectable.GetComponent<Rewired.UI.ControlMapper.ThemedElement>());
                    }
                }
            }

            [HarmonyPatch(typeof(Rewired.UI.ControlMapper.ThemedElement))]
            [HarmonyPatch("Start")]
            public static class ThemedElementStart
            {
                static void Postfix(Rewired.UI.ControlMapper.ThemedElement __instance) {
                    if (!plugin.categorybuttons.Contains(__instance)) {
                        return;
                    }
                    Rewired.UI.ControlMapper.CustomButton customButton = __instance.GetComponent<Rewired.UI.ControlMapper.CustomButton>();
                    UnityEngine.UI.ColorBlock colourBlock = customButton.colors;
                    colourBlock.normalColor = plugin.normalColour;
                    colourBlock.pressedColor = plugin.pressedColor;
                    colourBlock.disabledColor = plugin.disabledColour;
                    customButton.colors = colourBlock;
                }
            }
        }
    }
}

 