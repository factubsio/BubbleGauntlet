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

namespace BubbleGauntlet {
    public static class GauntletController {

        public static AreaMap CurrentMap;

        public static UnitEntityData ServiceVendor;
        public static UnitEntityData Bubble;
        public static FloorState Floor;
        public static List<CombatEncounterTemplate> NormalFights = new();

        public static CombatEncounterTemplate GetEliteFight() {
            return null;
        }

        public static CombatEncounterTemplate GetNormalFight() {
            var valid = NormalFights.Where(f => f.IsAppropriate);

            if (valid.Empty())
                return null;

            return valid.Random();
        }

        public static void Initialize() {

            NormalFights.AddRange(MonsterDB.CombatTemplates.Values);
            Main.Log($"Added {NormalFights.Count} combat templates");
        }

        public static void InstantiateCombatTemplate(CombatEncounterTemplate template) {

            Main.CleanUpArena();
            var (center, look) = CurrentMap.CombatLocation;

            List<Vector3> existing = new();

            float encounterBudgetScale = UnityEngine.Random.Range(0.9f, 1.8f);

            List<string> Generated = new();
            UnitEntityData last = null;

            foreach (var toSpawn in template.GetMonsters(encounterBudgetScale)) {
                var unitBp = BP.GetBlueprint<BlueprintUnit>(toSpawn.ToString());
                Vector3 pos = Vector3.zero;
                Generated.Add(unitBp.CharacterName);

                for (int i = 0; i < 8; i++) {
                    var rand = UnityEngine.Random.onUnitSphere * UnityEngine.Random.Range(2.5f, 5);
                    rand.y = 0;

                    pos = center + rand;

                    if (existing.All(e => Vector3.Distance(pos, e) > 3))
                        break;
                }

                existing.Add(pos);

                var unit = Game.Instance.EntityCreator.SpawnUnit(unitBp, pos, Quaternion.identity, null);

                //if (Floor.Level > 2)
                unit.Facts.RemoveAll<EntityFact>(fact => {
                    if (fact.Blueprint.HasComponent<AddEnergyImmunity>())
                        return true;
                    return false;
                });
                unit.AddBuff(FUN.ThemeBuffs[Floor.DamageTheme], unit);

                //if (AstarPath.active) {
                //    FreePlaceSelector.PlaceSpawnPlaces(2, Math.Max(unit.View.Corpulence, 4.0f) * 2.0f, center);
                //    pos = FreePlaceSelector.GetRelaxedPosition(1, true);
                //    unit.Position = pos;
                //}

                unit.LookAt(pos + look);
                unit.GroupId = "bubble-encounter";
                last = unit;
            }

            if (last != null) {
                Main.Log("Making elite...");
                Main.Log($" CL: {last.Progression.CharacterLevel}, MAXCL: {last.Progression.MaxCharacterLevel}, MAXACL: {last.Progression.MaxAvailableCharacterLevel}");
                //last.Progression.CharacterLevel += 3;
                var ogName = last.Descriptor.CharacterName;
                last.Descriptor.CustomName = $"Slightly Bubbly {ogName}";
                Main.LogNotNull("bubbleylevels", FUN.AddBubblyLevels);
                Main.LogNotNull("bubbleylevels.class", FUN.AddBubblyLevels.CharacterClass);
                AddClassLevels.LevelUp(FUN.AddBubblyLevels, last.Descriptor, D.Roll(Floor.Level + 2));
                last.AddBuff(FUN.BubblyBuffVisual, last);
                Main.Log("Done");
            }

            Main.CombatLog($"Combat encounter generated", GameLogStrings.Instance.DefaultColor, new TooltipTemplateEncounterGen {
                Monsters = Generated,
                Scale = encounterBudgetScale
            });
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

            //int index = 0;
            //float start_x = bubblePosition.x - 2;
            //bubblePosition.z += 3;
            //foreach (var kv in FUN.ThemeBuffs) {
            //    bubblePosition.x = start_x + index * 1.2f;
            //    var dummy = Game.Instance.EntityCreator.SpawnUnit(ContentManager.BubbleMasterBlueprint, bubblePosition, Quaternion.identity, null);
            //    dummy.Descriptor.CustomName = $"{kv.Key} Bubble";
            //    dummy.AddBuff(kv.Value, dummy);
            //    if (index++ == 4) {
            //        bubblePosition.z += 1.5f;
            //        index = 0;
            //    }

            //}
        }

        internal static void CompleteEncounter() {
            Floor.ActiveEncounter++;
            ProgressIndicator.Refresh();
        }

        internal static void SetNextEncounter(EncounterType type) {
            Floor.Encounters[Floor.EncountersCompleted] = type;
            Floor.EncountersRemaining--;
            Floor.Events[type].Remaining--;
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
            CurrentMap = ContentManager.MapForArea(Game.Instance.CurrentlyLoadedArea.AssetGuid.m_Guid);

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
                else if (unit.GroupId != "bubble-encounter" && unit.Blueprint.Speed.Value != 5)
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

        internal static void ExitCombat() {

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
        public static int FloorExperience(int floor) => floor * FloorBonusFactor + FloorBonusFlat;
    }
}
