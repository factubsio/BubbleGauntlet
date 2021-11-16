﻿using HarmonyLib;
using System.Linq;
using JetBrains.Annotations;
using Kingmaker;
using Kingmaker.Blueprints.JsonSystem;
using System;
using BubbleGauntlet.Config;
using BubbleGauntlet.Utilities;
using UnityModManagerNet;
using UnityEngine;
using Kingmaker.UI.SettingsUI;
using Kingmaker.PubSubSystem;
using System.Collections.Generic;
using Kingmaker.UI;
using System.Reflection;
using Kingmaker.Cheats;
using Kingmaker.DialogSystem.Blueprints;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.View.MapObjects;
using Kingmaker.ElementsSystem;
using Kingmaker.Utility;
using Kingmaker.UI.Log.CombatLog_ThreadSystem;
using Kingmaker.UI.Log.CombatLog_ThreadSystem.LogThreads.Common;
using Kingmaker.Blueprints.Root.Strings.GameLog;
using Kingmaker.UI.MVVM._VM.Tooltip.Templates;
using Kingmaker.Visual.Particles;
using Kingmaker.Designers;
using static Kingmaker.QA.Statistics.ExperienceGainStatistic;

namespace BubbleGauntlet {

    public class BubbleSettings {

        private BubbleSettings() { }

        public static UISettingsEntitySliderFloat MakeSliderFloat(string key, string name, string tooltip, float min, float max, float step) {
            var slider = ScriptableObject.CreateInstance<UISettingsEntitySliderFloat>();
            slider.m_Description = Helpers.CreateString($"{key}.description", name);
            slider.m_TooltipDescription = Helpers.CreateString($"{key}.tooltip-description", tooltip);
            slider.m_MinValue = min;
            slider.m_MaxValue = max;
            slider.m_Step = step;
            slider.m_ShowValueText = true;
            slider.m_DecimalPlaces = 1;
            slider.m_ShowVisualConnection = true;

            return slider;
        }

        public static UISettingsGroup MakeSettingsGroup(string key, string name, params UISettingsEntityBase[] settings) {
            UISettingsGroup group = ScriptableObject.CreateInstance<UISettingsGroup>();
            group.Title = Helpers.CreateString(key, name);

            group.SettingsList = settings;

            return group;
        }

        public void Initialize() {
        }

        private static readonly BubbleSettings instance = new();
        public static BubbleSettings Instance { get { return instance; } }
    }


    //[HarmonyPatch(typeof(UISettingsManager), "Initialize")]
    //static class SettingsInjector {
    //    static bool Initialized = false;

    //    [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "harmony patch")]
    //    static void Postfix() {
    //        if (Initialized) return;
    //        Initialized = true;
    //        Main.LogHeader("Injecting settings");

    //        BubbleSettings.Instance.Initialize();

    //        //Game.Instance.UISettingsManager.m_GameSettingsList.Add(
    //        //    BubbleSettings.MakeSettingsGroup("bubble.speed-tweaks", "Bubble speed tweaks",
    //        //        BubbleSettings.Instance.GlobalMapSpeedSlider,
    //        //        BubbleSettings.Instance.InCombatSpeedSlider,
    //        //        BubbleSettings.Instance.OutOfCombatSpeedSlider,
    //        //        BubbleSettings.Instance.TacticalCombatSpeedSlider));
    //    }
    //}



    public static class IListExtensions {
        public static Condition Invert(this Condition cond) {
            return new InvertedCondition(cond);
        }
        public static string ToTag(this string str) {
            return '{' + str + '}';
        }
        /// <summary>
        /// Shuffles the element order of the specified list.
        /// </summary>
        public static void Shuffle<T>(this IList<T> ts) {
            var count = ts.Count;
            var last = count - 1;
            for (var i = 0; i < last; ++i) {
                var r = UnityEngine.Random.Range(i, count);
                var tmp = ts[i];
                ts[i] = ts[r];
                ts[r] = tmp;
            }
        }
    }

    public class DeckTemplate<T> {
        private readonly List<T> all;
        public DeckTemplate(params T[] cards) {
            all = new(cards);
        }
        public DeckTemplate(IEnumerable<T> cards) {
            all = new(cards);
        }

        public Deck<T> Deck => new(all);

    }

    public class Deck<T> {
        private readonly List<T> vals;
        private int Head => vals.Count - 1;
        public bool Empty => vals.Count == 0;

        public T Next => Take(1).FirstOrDefault();

        public Deck(List<T> all) {
            vals = new(all);
            vals.Shuffle();
        }

        public IEnumerable<T> Take(int count) {
            for (int i = 0; i < count && vals.Count > 0; i++) {
                T val = vals[Head];
                vals.RemoveAt(Head);
                yield return val;
            }
        }

        public bool TryPeek(out T val) {
            if (vals.Count == 0) {
                val = default;
                return false;
            } else {
                val = vals[Head];
                return true;
            }
        }
    }


    public interface IBuilder<T> {
        public T Create();
    }




    public static class DialogTester {


        public static void Run() {
            var descendEvent = new DialogBuilder();

            var exitPage = descendEvent.NewPage("end-descend");
            exitPage.BasicCue("You see the next floor ahead.");
            exitPage.OnShow(new DynamicGameAction(() => {
                int exp = GauntletController.Floor.Level * 500 + 500;
                GameHelper.GainExperience(exp, null, GainType.Quest);
            }));
            exitPage.Answers()
                .Add("To adventure!").Commit()
                .Commit();
            exitPage.Commit();

            var entryPage = descendEvent.RootPage("To adventure!");
            var slice = EncounterSlice.MakeOpporunity_Grave();
            var slice2 = EncounterSlice.MakeTest2();
            slice.SetNext(slice2.EntryPoint);
            slice2.SetNext(exitPage.Reference);

            entryPage.BasicCue("The gauntlet extends down seemingly forever, but to keep your sanity you must take it floor by floor.");

            entryPage.Answers()
                .Add("Venture forth...")
                    .ContinueWith(References.Static(slice.EntryPoint))
                    .Commit()
                .Commit();
            entryPage.Commit();

            var dialog = Helpers.CreateBlueprint<BlueprintDialog>("bubble-dialog-TEST", dialog => {
                dialog.Conditions = new();
                dialog.StartActions = new();
                dialog.FinishActions = new();
                dialog.ReplaceActions = new();

                dialog.FirstCue = new();
                dialog.FirstCue.Cues = new();
                dialog.FirstCue.Cues.Add(descendEvent.Build());
                dialog.Type = DialogType.Book;
                dialog.TurnFirstSpeaker = true;
                dialog.TurnPlayer = true;
            });


            var speaker = Helpers.CreateString("bubble-TEST-speaker", "Bubbles");
            Game.Instance.DialogController.StartDialogWithoutTarget(dialog, speaker);
        }

    }

    class GameEventHandler : IAreaHandler, IPartyCombatHandler, IWarningNotificationUIHandler, IAreaActivationHandler {
        public void HandlePartyCombatStateChanged(bool inCombat) {
            var bubble = GauntletController.Bubble;
            ProgressIndicator.Visible = !inCombat;
            if (!inCombat && bubble != null) {
                GauntletController.ExitCombat();
            }
        }

        void IWarningNotificationUIHandler.HandleWarning(WarningNotificationType warningType, bool addToLog) {
            if (warningType == WarningNotificationType.GameSavedInProgress) {
                Main.Log("SAVING");
                Main.Log(StackTraceUtility.ExtractStackTrace());
                //GauntletController.Save();
            }
            if (warningType == WarningNotificationType.GameLoaded) {
                if (Game.Instance.State.LoadedAreaState.Blueprint.AssetGuid == ContentManager.Area?.AssetGuid) {
                }
            }
        }

        void IAreaHandler.OnAreaDidLoad() { }
        void IWarningNotificationUIHandler.HandleWarning(string text, bool addToLog) { }
        void IAreaHandler.OnAreaBeginUnloading() { }

        void IAreaActivationHandler.OnAreaActivated() {

            ProgressIndicator.Install();
            GauntletController.LoadCheck();

            Main.Log("Removing all exits");
            foreach (var entity in Game.Instance.State.LoadedAreaState.GetAllSceneStates()
                                                                      .Where(state => state.IsSceneLoaded)
                                                                      .SelectMany(state => state.AllEntityData)
                                                                      .OfType<MapObjectEntityData>()
                                                                      .Where(mapObject => mapObject.Parts.Get<AreaTransitionPart>() != null)) {
                entity.MarkForDestroy();
            }

        }
    }
#if DEBUG
    [EnableReloading]
#endif
    static class Main {
        public static string ModPath;
        private static Harmony harmony;
        public static bool Enabled;

        public static GameEventHandler GameStartHijacker = new();

        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "harmony method")]
        static bool Load(UnityModManager.ModEntry modEntry) {
            harmony = new Harmony(modEntry.Info.Id);
#if DEBUG
            modEntry.OnUnload = OnUnload;
#endif
            CheatsCommon.SendAnalyticEvents?.Set(false);
            modEntry.OnUpdate = OnUpdate;
            ModSettings.ModEntry = modEntry;
            ModPath = modEntry.Path;
            Main.Log("LOADING");

            BubbleTemplates.AddAll();
            ModSettings.LoadAllSettings();

            EventBus.Subscribe(GameStartHijacker);

            if (UnityModManager.gameVersion.Minor == 1)
                UIHelpers.WidgetPaths = new WidgetPaths_1_1();
            else
                UIHelpers.WidgetPaths = new WidgetPaths_1_0();
            harmony.PatchAll();


            return true;
        }

        static void OnUpdate(UnityModManager.ModEntry modEntry, float delta) {
#if DEBUG
            if (Input.GetKeyDown(KeyCode.C) && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))) {
                FxHelper.DestroyAllBlood();
                foreach (var entity in Game.Instance.State.LoadedAreaState.GetAllSceneStates()
                                                                          .Where(state => state.IsSceneLoaded)
                                                                          .SelectMany(state => state.AllEntityData)) {
                    Main.Log($"entit: {entity} -> {entity.GetType()}");
                }
            }
            if (Input.GetKeyDown(KeyCode.I) && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))) {
                CheatsCombat.KillAll();
            }
            if (Input.GetKeyDown(KeyCode.D) && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))) {
                //DialogTester.Run();
                ContentManager.InstallGauntletContent();
            }
            if (Input.GetKeyDown(KeyCode.L) && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))) {
                CleanUpArena();
                GauntletController.Bubble?.MarkForDestroy();
            }
            if (Input.GetKeyDown(KeyCode.K) && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))) {
                ProgressIndicator.Install();
                CleanUpArena();
            }
            if (Input.GetKeyDown(KeyCode.B) && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))) {
                modEntry.GetType().GetMethod("Reload", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(modEntry, new object[] { });
            }
#endif
        }

        public static void CleanUpArena(bool full = false) {
            foreach (SceneEntitiesState sceneEntitiesState in Game.Instance.LoadedAreaState.GetAllSceneStates()) {
                if (sceneEntitiesState.IsSceneLoaded) {
                    foreach (EntityDataBase entityData in sceneEntitiesState.AllEntityData) {
                        if (entityData is UnitEntityData unitEntityData && !unitEntityData.IsPlayerFaction && unitEntityData != GauntletController.Bubble) {
                            entityData.MarkForDestroy();
                        }
                    }
                }
            }
            foreach (EntityDataBase entityDataBase2 in Game.Instance.Player.CrossSceneState.AllEntityData) {
                if (!entityDataBase2.IsInGame) {
                    UnitEntityData unitEntityData2 = entityDataBase2 as UnitEntityData;
                    if (unitEntityData2 != null && unitEntityData2.Descriptor.Get<UnitPartSummonedMonster>()) {
                        entityDataBase2.MarkForDestroy();
                    }
                }
            }
        }

        static bool OnUnload(UnityModManager.ModEntry modEntry) {
            BubbleTemplates.RemoveAll();
            harmony.UnpatchAll();
            EventBus.Unsubscribe(GameStartHijacker);
            BP.RemoveModBlueprints();
            ProgressIndicator.Uninstall();


            return true;
        }

        internal static void LogPatch(string v, object coupDeGraceAbility) {
            throw new NotImplementedException();
        }

        public static void Log(string msg) {
            //if (bubbleLog != null) {
            //    bubbleLog.WriteLine(msg);
            //    bubbleLog.Flush();
            //}

            ModSettings.ModEntry.Logger.Log(msg);
        }
        [System.Diagnostics.Conditional("DEBUG")]
        public static void LogDebug(string msg) {
            ModSettings.ModEntry.Logger.Log(msg);
        }
        public static void LogPatch(string action, [NotNull] IScriptableObjectWithAssetId bp) {
            Log($"{action}: {bp.AssetGuid} - {bp.name}");
        }
        public static void LogHeader(string msg) {
            Log($"--{msg.ToUpper()}--");
        }
        public static void Error(Exception e, string message) {
            Log(message);
            Log(e.ToString());
            PFLog.Mods.Error(message);
        }
        public static void Error(string message) {
            Log(message);
            PFLog.Mods.Error(message);
        }

        public static void CombatLog(string str, Color col, TooltipBaseTemplate tooltip = null) {
            var message = new CombatLogMessage(str, col, PrefixIcon.None, tooltip, tooltip != null);
            var messageLog = LogThreadController.Instance.m_Logs[LogChannelType.Common].First(x => x is MessageLogThread);
            messageLog.AddMessage(message);
        }
        public static void CombatLog(string str) {
            CombatLog(str, GameLogStrings.Instance.DefaultColor);
        }

        static HashSet<string> filtersEnabled = new() {
            //"state",
            //"minority",
            //"rejection",
            "interop",
        };

        static bool suppressUnfiltered = true;

        internal static void Verbose(string v, string filter = null) {
#if true && DEBUG
            if ((filter == null && !suppressUnfiltered) || filtersEnabled.Contains(filter))
                Main.Log(v);
#endif
        }

        internal static void LogNotNull(string v, object obj) {
            Main.Log($"{v} not-null: {obj != null}");
        }
    }

    public struct Weighted<T> : Utilities.IWeighted {
        public int count;
        public T name;

        public Weighted(int count, T name) {
            this.count = count;
            this.name = name;
        }

        public int Weight => count;

        public void Deconstruct(out int count, out T name) {
            count = this.count;
            name = this.name;
        }

        public static implicit operator (int count, T name)(Weighted<T> value) {
            return (value.count, value.name);
        }

        public static implicit operator Weighted<T>((int count, T name) value) {
            return new Weighted<T>(value.count, value.name);
        }

        public override string ToString() {
            return name?.ToString();
        }

    }
}
