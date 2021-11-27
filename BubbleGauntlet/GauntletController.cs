using BubbleGauntlet.Bosses;
using HarmonyLib;
using Kingmaker;
using Kingmaker.Designers;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.PubSubSystem;
using Kingmaker.UnitLogic.Class.LevelUp.Actions;
using Kingmaker.Utility;
using System.Linq;
using UnityEngine;
using static Kingmaker.QA.Statistics.ExperienceGainStatistic;

namespace BubbleGauntlet {

    public static class GauntletController {
        public static IMinorBoss Boss = null;
        public static UnitEntityData Bubble;
        public static AreaMap CurrentMap;
        public static FloorState Floor;
        public static UnitEntityData ServiceVendor;
        public static UnitEntityData Vendor;
        public static TooltipTemplateEncounterGen[] FightDetails = new TooltipTemplateEncounterGen[10];

        public const int FightExperienceFactor = 1;
        public const int FightExperienceFloorNudge = 5;
        public const int FightGoldFactor = 500;
        public const int FightGoldSquared = 10;
        public const int FloorBonusFactor = 0;
        public const int FloorBonusFlat = 2000;


        private static void Reset() {
            Boss = null;
            Bubble = null;
            CurrentMap = null;
            ServiceVendor = null;
            Vendor = null;
        }

        internal static System.Action OnEncounterComplete;

        public static EncounterType ActiveEncounterType => Floor.Encounters[Floor.ActiveEncounter];

        public static bool InBossStage => Boss != null;

        public static void BeginBossStage(IMinorBoss boss) {
            Main.Log($"Attempting to load boss... {boss.Name}");
            Boss = boss;
            MapManager.Load(boss.Map);
        }

        public static void CreateBubbleAndTrumpetGirl() {
            var (bubblePosition, bubbleLook) = CurrentMap.BubbleLocation;
            Bubble = Game.Instance.EntityCreator.SpawnUnit(ContentManager.BubbleMasterBlueprint, bubblePosition, Quaternion.identity, null);
            Bubble.State.Features.IsIgnoredByCombat.Retain();
            Bubble.State.Features.Immortality.Retain();
            Bubble.LookAt(bubblePosition + bubbleLook);

            var (servicePos, serviceLook) = CurrentMap.ServiceLocation;
            ServiceVendor = Game.Instance.EntityCreator.SpawnUnit(ContentManager.ServiceVendorBlueprint, servicePos, Quaternion.identity, null);
            ServiceVendor.State.Features.IsIgnoredByCombat.Retain();
            ServiceVendor.State.Features.Immortality.Retain();
            ServiceVendor.LookAt(servicePos + serviceLook);
            ServiceVendor.State.Size = Kingmaker.Enums.Size.Small;
        }

        public static void ExitCombat() {
            CompleteEncounter();

            int exp = FightExperience(Floor.Level);
            GameHelper.GainExperience(exp, null, GainType.Mob);

            int gold = FightGold(Floor.Level);
            Game.Instance.Player.GainMoney(gold);

            GauntletController.ShowBubble();
        }

        public static int FightExperience(int floor) => (floor + FightExperienceFloorNudge) * floor * floor * FightExperienceFactor;

        public static int FightGold(int floor) => (floor * floor * FightGoldSquared) + (floor * FightGoldFactor);

        public static int FloorExperience(int floor) => floor * FloorBonusFactor + FloorBonusFlat;

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

        internal static void EnterFloor() {
            if (Floor == null)
                return;

            for (int i = 0; i < FightDetails.Length; i++) {
                FightDetails[i] = null;
            }

            if (Floor.Level > 1)
                Game.Instance.Player.AdvanceMythicExperience(Game.Instance.Player.MythicExperience + 1);
        }

        internal static void HideBubble() {
            Bubble.View.FadeHide();
            Bubble.IsInGame = false;
            ServiceVendor.View.FadeHide();
            ServiceVendor.IsInGame = false;
        }

        internal static void InstallGauntletController() {
            Floor = Game.Instance.Player.Ensure<Gauntlet>().Floor;

            /* FIRST TIME!!!! */
            if (Floor.Level == 0) {
                Reset();
                Floor.Descend();

                LevelUpHelper.AddStartingItems(Game.Instance.Player.MainCharacter.Value);
#if BUBBLEDEV
                Game.Instance.Player.Money = 1000000;
#else
                Game.Instance.Player.Money = 2000;
#endif
            }

            ProgressIndicator.Visible = !InBossStage;

            if (InBossStage) {
                Boss.Begin();
                ProgressIndicator.Visible = false;
                return;
            }

            ProgressIndicator.Visible = true;

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
                GauntletController.CreateBubbleAndTrumpetGirl();
            } else {
                Bubble.Position = CurrentMap.BubbleLocation.at;
                Bubble.LookAt(Bubble.Position + CurrentMap.BubbleLocation.look);

                ServiceVendor.Position = CurrentMap.ServiceLocation.at;
                ServiceVendor.LookAt(Bubble.Position + CurrentMap.ServiceLocation.look);
            }

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

        internal static void SetNextEncounter(EncounterType type) {
            Floor.Encounters[Floor.ActiveEncounter] = type;
            ProgressIndicator.Refresh();
        }

        internal static void ShowBubble() {
            Bubble.IsInGame = true;
            ServiceVendor.IsInGame = true;
        }
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