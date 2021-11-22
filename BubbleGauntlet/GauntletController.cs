using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Root.Strings.GameLog;
using Kingmaker.Designers;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic;
using Kingmaker.Utility;
using Kingmaker.View;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using BubbleGauntlet.Extensions;
using static Kingmaker.QA.Statistics.ExperienceGainStatistic;
using Kingmaker.UnitLogic.FactLogic;
using BubbleGauntlet.Utilities;
using Kingmaker.AreaLogic.Etudes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using HarmonyLib;
using Kingmaker.UI;
using Kingmaker.PubSubSystem;

namespace BubbleGauntlet {
    public static class GauntletController {

        public static AreaMap CurrentMap;

        public static UnitEntityData ServiceVendor;
        public static UnitEntityData Bubble;
        public static FloorState Floor;


        internal static void EnterFloor() {
            if (Floor == null)
                return;

            if (Floor.Level > 1)
                Game.Instance.Player.AdvanceMythicExperience(Game.Instance.Player.MythicExperience + 1);
        }


        public static void CreateBubbleMaster() {
            Game.Instance.Player.Money = 2000;
            var (bubblePosition, bubbleLook) = CurrentMap.BubbleLocation;
            Bubble = Game.Instance.EntityCreator.SpawnUnit(ContentManager.BubbleMasterBlueprint, bubblePosition, Quaternion.identity, null);
            Bubble.LookAt(bubblePosition + bubbleLook);

            var (servicePos, serviceLook) = CurrentMap.ServiceLocation;
            ServiceVendor = Game.Instance.EntityCreator.SpawnUnit(ContentManager.ServiceVendorBlueprint, servicePos, Quaternion.identity, null);
            ServiceVendor.LookAt(servicePos + serviceLook);
            ServiceVendor.State.Size = Kingmaker.Enums.Size.Small;
        }

        public static EncounterType ActiveEncounterType => Floor.Encounters[Floor.ActiveEncounter];

        internal static void CompleteEncounter() {

            Floor.EncountersRemaining--;
            Floor.Events[ActiveEncounterType].Remaining--;

            if (ActiveEncounterType == EncounterType.EliteFight) {
                int budget = D.Roll(Floor.Level + 1) * 2000 + 2000;
                int min = budget / 2;

                var gift = ContentManager.AllItems.Where(i => i.Cost >= min && i.Cost < budget).Random();
                Game.Instance.Player.Inventory.Add(gift);
            }

            Floor.ActiveEncounter++;

            ProgressIndicator.Refresh();
        }

        internal static void SetNextEncounter(EncounterType type) {
            Floor.Encounters[Floor.ActiveEncounter] = type;
            ProgressIndicator.Refresh();
        }

        public static UnitEntityData Vendor;


        internal static void HideBubble() {
            Main.Log("HideBubble (Fade)");
            Bubble.View.FadeHide();
            Bubble.IsInGame = false;
            ServiceVendor.View.FadeHide();
            ServiceVendor.IsInGame = false;
        }

        internal static void ShowBubble() {
            Main.Log("ShowBubble");
            Bubble.IsInGame = true;
            ServiceVendor.IsInGame = true;
            Main.Log("Showing service vendor???");
        }


        internal static void InstallGauntletController() {
            Floor = Game.Instance.Player.Ensure<Gauntlet>().Floor;

            if (Floor.Level == 0) {
                /* FIRST TIME!!!! */
                Floor.Descend();
            }

            CurrentMap = MapManager.MapForArea(Game.Instance.CurrentlyLoadedArea.AssetGuid.m_Guid);

            Main.Log("Floor state initialised/loaded");

            Bubble = null;
            ServiceVendor = null;

            foreach (var unit in Game.Instance.State.LoadedAreaState.GetAllSceneStates()
                                                  .Where(state => state.IsSceneLoaded)
                                                  .SelectMany(state => state.AllEntityData)
                                                  .OfType<UnitEntityData>()) {
                if (unit.Blueprint == ContentManager.BubbleMasterBlueprint)
                    Bubble = unit;
                else if (unit.Blueprint == ContentManager.ServiceVendorBlueprint)
                    ServiceVendor = unit;
                else if (unit.Blueprint.Speed.Value == 5)
                    Vendor = unit;
                else if (unit.GroupId == "bubble-encounter")
                    CombatManager.CombatMonsters.Add(unit);
                else
                    unit.MarkForDestroy();
            }

            if (Bubble == null) {
                //new game
                Main.Log("Could not find bubble, creating new");
                GauntletController.CreateBubbleMaster();
            } 

            Game.Instance.Player.ExperienceRatePercent = 100;

            GauntletController.Vendor = Game.Instance.State.LoadedAreaState.GetAllSceneStates()
                                                  .Where(state => state.IsSceneLoaded)
                                                  .SelectMany(state => state.AllEntityData)
                                                  .OfType<UnitEntityData>()
                                                  .FirstOrDefault(u => u.Blueprint.Speed.Value == 5);

            if (GauntletController.Vendor != null) {
                Main.Log("Restoring a vendor");
            }
            ProgressIndicator.Refresh();
        }

        public static void ExitCombat() {

            CompleteEncounter();

            int exp = FightExperience(Floor.Level);
            GameHelper.GainExperience(exp, null, GainType.Mob);

            int gold = FightGold(Floor.Level);
            Game.Instance.Player.GainMoney(gold);

            GauntletController.ShowBubble();
        }


        public static int FightExperienceFactor = 1;
        public static int FightExperienceFloorNudge = 5;
        public static int FightExperience(int floor) => (floor + FightExperienceFloorNudge) * floor * floor * FightExperienceFactor;

        public static int FightGoldSquared = 10;
        public static int FightGoldFactor = 500;
        public static int FightGold(int floor) => (floor * floor * FightGoldSquared) + (floor * FightGoldFactor);

        public static int FloorBonusFlat = 2000;
        public static int FloorBonusFactor = 0;
        internal static System.Action OnEncounterComplete;

        public static int FloorExperience(int floor) => floor * FloorBonusFactor + FloorBonusFlat;
    }

    [HarmonyPatch]
    public static class StopSaveInEncounter {

        [HarmonyPatch(typeof(Game), nameof(Game.SaveGame)), HarmonyPrefix]
        public static bool SaveGame() {
            if (GauntletController.ActiveEncounterType != EncounterType.None) {
                EventBus.RaiseEvent<IWarningNotificationUIHandler>(delegate (IWarningNotificationUIHandler h) {
                    h.HandleWarning("Cannot save while an encounter is active", true);
                }, true);
                return false;
            }
            //return false;
            return true;
        }

    }
}
