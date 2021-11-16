using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Root.Strings.GameLog;
using Kingmaker.Designers;
using Kingmaker.DialogSystem.Blueprints;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Localization;
using Kingmaker.RandomEncounters.Settings;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.Utility;
using Kingmaker.View;
using Kingmaker.View.MapObjects;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static Kingmaker.QA.Statistics.ExperienceGainStatistic;

namespace BubbleGauntlet {
    public static class GauntletController {


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
            var center = new Vector3(25f, 40f, 45f);

            List<Vector3> existing = new();

            float encounterBudgetScale = UnityEngine.Random.Range(0.66f, 1.5f);

            List<string> Generated = new();

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

                //if (AstarPath.active) {
                //    FreePlaceSelector.PlaceSpawnPlaces(2, Math.Max(unit.View.Corpulence, 4.0f) * 2.0f, center);
                //    pos = FreePlaceSelector.GetRelaxedPosition(1, true);
                //    unit.Position = pos;
                //}

                unit.LookAt(Game.Instance.Player.Position);
                unit.GroupId = "bubble-encounter";
            }

            Main.CombatLog($"Combat encounter generated", GameLogStrings.Instance.DefaultColor, new TooltipTemplateEncounterGen {
                Monsters = Generated,
                Scale = encounterBudgetScale
            });
        }

        
        public static void CreateBubbleMaster() {
            Game.Instance.Player.Money = 2000;
            var bubblePosition = AreaEnterPoint.FindAreaEnterPointOnScene(ContentManager.AreaEnter).transform.position;
            bubblePosition.z -= 4;
            Bubble = Game.Instance.EntityCreator.SpawnUnit(ContentManager.BubbleMasterBlueprint, bubblePosition, Quaternion.identity, null);
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
        }

        internal static void ShowBubble() {
            Main.Log("ShowBubble");
            Bubble.IsInGame = true;
        }

        internal static void Descend() {
            Game.Instance.DialogController.StartDialogWithoutTarget(ContentManager.DescendDialog, ContentManager.DescendSpeaker);
        }

        internal static void LoadCheck() {
            Floor = Game.Instance.Player.Ensure<Gauntlet>().Floor;

            var bubble = Game.Instance.State.LoadedAreaState.GetAllSceneStates()
                                                  .Where(state => state.IsSceneLoaded)
                                                  .SelectMany(state => state.AllEntityData)
                                                  .OfType<UnitEntityData>()
                                                  .FirstOrDefault(u => u.Blueprint == ContentManager.BubbleMasterBlueprint);

            if (bubble == null) {
                //new game
                Main.Log("Could not find bubble, creating new");
                GauntletController.CreateBubbleMaster();
            } else {
                Main.Log("Latching existing bubble");
                GauntletController.Bubble = bubble;
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
        }

        internal static void ExitCombat() {

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
