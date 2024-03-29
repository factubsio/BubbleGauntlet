﻿using BubbleGauntlet.Extensions;
using BubbleGauntlet.Utilities;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Root.Strings.GameLog;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UI.MVVM._VM.Tooltip.Bricks;
using Kingmaker.UI.MVVM._VM.Tooltip.Templates;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.Utility;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BubbleGauntlet {

    public static class CombatManager {
        public static List<UnitEntityData> CombatMonsters = new();
        public static List<CombatEncounterTemplate> NormalFights = new();

        private static BlueprintBuff CountDeaths;

        public static CombatEncounterTemplate GetEliteFight() {
            return null;
        }

        public static CombatEncounterTemplate GetNormalFight() {
            var valid = NormalFights.Where(f => f.IsAppropriate);

            if (valid.Empty())
                return null;

            return valid.Random();
        }

        public static void InstantiateCombatTemplate(CombatEncounterTemplate template, bool elite) {
            var Floor = GauntletController.Floor;

            Main.CleanUpArena();
            var (center, look) = GauntletController.CurrentMap.CombatLocation;

            List<Vector3> existing = new();

            float rangeScale = 1;
            if (GauntletController.Floor.Level == 1)
                rangeScale = 0.66f;

            float lowerRange = 0.9f;
            float upperRange = 1.8f;

            float encounterBudgetScale = UnityEngine.Random.Range(lowerRange * rangeScale, upperRange * rangeScale);
            //float encounterBudgetScale = UnityEngine.Random.Range(0.3f, 0.5f);

            List<string> Generated = new();
            UnitEntityData last = null;

            foreach (var toSpawn in template.GetMonsters(encounterBudgetScale)) {
                last = SpawnMonster(center, look, existing, Generated, toSpawn);
            }

            int rawBudget = template.Monsters.Sum(p => p.count);

            int totalBudget = (int)(rawBudget * encounterBudgetScale);
            int maxBudget = (int)(rawBudget * 1.8f * rangeScale);

            if (last != null) {
                Main.Log("Making elite...");
                Main.Log($" CL: {last.Progression.CharacterLevel}, MAXCL: {last.Progression.MaxCharacterLevel}, MAXACL: {last.Progression.MaxAvailableCharacterLevel}");
                //last.Progression.CharacterLevel += 3;
                var ogName = last.Descriptor.CharacterName;
                last.Descriptor.CustomName = $"Slightly Bubbly {ogName}";
                Main.LogNotNull("bubbleylevels", FUN.AddBubblyLevels);
                Main.LogNotNull("bubbleylevels.class", FUN.AddBubblyLevels.CharacterClass);
                AddClassLevels.LevelUp(FUN.AddBubblyLevels, last.Descriptor, D.Roll(Floor.Level + 2));
                last.AddBuffNotDispelable(FUN.BubblyBuffVisual);
                Generated[Generated.Count - 1] = last.CharacterName;
                Main.Log("Done");
            }

            if (elite) {
                var bp = ContentManager.Elites.Where(e => Floor.Level >= e.MinFloor && Floor.Level <= e.MaxFloor).Random().Blueprint;
                if (bp != null) {
                    //var bp = ContentManager.Elites[3].Blueprint;
                    var miniboss = SpawnMonster(center, look, existing, Generated, bp);
                    AddClassLevels.LevelUp(FUN.AddEliteLevels, miniboss.Descriptor, (1 + (Floor.Level / 3)) * 2);
                    miniboss.AddBuffNotDispelable(FUN.EliteAttacks.Random());
                    miniboss.AddBuffNotDispelable(FUN.EliteDefenses.Random());
                    miniboss.AddBuffNotDispelable(FUN.BubblyEliteVisual);
                }
            }

            var tooltip = new TooltipTemplateEncounterGen {
                Monsters = Generated,
                Budget = totalBudget,
                MaxBudget = maxBudget
            };
            GauntletController.FightDetails[GauntletController.Floor.ActiveEncounter] = tooltip;
            Main.CombatLog($"Combat encounter generated", GameLogStrings.Instance.DefaultColor, tooltip);
        }

        public static UnitEntityData SpawnMonster(Vector3 center, Vector3 look, List<Vector3> existing, List<string> Generated, string toSpawn) {
            var unitBp = BP.Get<BlueprintUnit>(toSpawn.ToString());
            return SpawnMonster(center, look, existing, Generated, unitBp);
        }

        public static UnitEntityData SpawnMonster(Vector3 center, Vector3 look, List<Vector3> existing, List<string> Generated, BlueprintUnit unitBp) {
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

            unit.AddBuffNotDispelable(CountDeaths);
            unit.State.AddCondition(Kingmaker.UnitLogic.UnitCondition.Unlootable);
            unit.Facts.RemoveAll<EntityFact>(fact => {
                if (fact.Blueprint.HasComponent<AddEnergyImmunity>())
                    return true;
                return false;
            });
            unit.AddBuffNotDispelable(FUN.ThemeBuffs[GauntletController.Floor.DamageTheme]);
            Main.Log("added unit, with novaon miss?");

            unit.LookAt(pos + look);
            unit.GroupId = "bubble-encounter";

            CombatMonsters.Add(unit);

            return unit;
        }

        internal static void Install() {
            NormalFights.AddRange(MonsterDB.CombatTemplates.Values);

            CountDeaths = Helpers.CreateBlueprint<BlueprintBuff>($"on-death", buff => {
                buff.SetNameDescription("Gauntlet monster on death", "on death");
                buff.m_Flags = BlueprintBuff.Flags.HiddenInUi;
                buff.AddComponent<DeathActions>(death => {
                    death.Actions = Helpers.CreateActionList(new DynamicGameAction(() => {
                        if (CombatMonsters.All(a => a.State.IsDead)) {
                            GauntletController.ExitCombat();
                        }
                    }));
                });
            });
        }
    }

    public class TooltipTemplateEncounterGen : TooltipBaseTemplate {
        public int Budget;
        public int MaxBudget;
        public List<string> Monsters;

        public override IEnumerable<ITooltipBrick> GetBody(TooltipTemplateType type) {
            yield return new TooltipBrickText($"Budget: {Budget} (Maximum: {MaxBudget})");
            yield return new TooltipBrickSpace();
            yield return new TooltipBrickText("Monsters", TooltipTextType.Bold);
            foreach (var monster in Monsters)
                yield return new TooltipBrickText($"• {monster}");
        }

        public override IEnumerable<ITooltipBrick> GetHeader(TooltipTemplateType type) {
            yield return new TooltipBrickText("Encounter generation details", TooltipTextType.BoldCentered);
        }
    }
}