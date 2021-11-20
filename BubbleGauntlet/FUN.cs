using BubbleGauntlet.Extensions;
using BubbleGauntlet.Utilities;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.Root;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums.Damage;
using Kingmaker.PubSubSystem;
using Kingmaker.ResourceLinks;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.Utility.UnitDescription;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace BubbleGauntlet {
    public class GauntletAdditionalDamage : EntityFactComponentDelegate<AddInitiatorAttackWithWeaponTrigger.ComponentData>, IInitiatorRulebookHandler<RulePrepareDamage>, IRulebookHandler<RulePrepareDamage>, ISubscriber, IInitiatorRulebookSubscriber {
        public void OnEventAboutToTrigger(RulePrepareDamage evt) {
            var state = GauntletController.Floor;

            if (state.Level == 1)
                return;

            var weaponAttack = evt.ParentRule.AttackRoll?.RuleAttackWithWeapon;
            if (weaponAttack == null)
                return;

            if (!weaponAttack.AttackRoll.IsHit)
                return;

            int bonus = 0;
            if (state.Level == 2)
                bonus = -3;

            //floor2-4: 1
            //floor5-7: 2
            //floor8-10: 3, etc.
            int diceCount = (state.Level + 1) / 3;

            DamageTypeDescription damageType = new();
            damageType.Energy = state.DamageTheme;
            damageType.Type = DamageType.Energy;

            BaseDamage damage = new DamageDescription {
                TypeDescription = damageType,
                Dice = new DiceFormula(diceCount, DiceType.D6),
                Bonus = bonus,
            }.CreateDamage();
            evt.Add(damage);
        }

        public void OnEventDidTrigger(RulePrepareDamage evt) { }
    }

    public static class FUN {
        public static AddClassLevels AddBubblyLevels;
        public static BlueprintUnitFact ExtraDamageFact;

        public static DamageEnergyType[] EnergyTypes = {
            DamageEnergyType.Fire,
            DamageEnergyType.Cold,
            DamageEnergyType.Sonic,
            DamageEnergyType.Electricity,
            DamageEnergyType.Acid,
            DamageEnergyType.NegativeEnergy,
            DamageEnergyType.Holy,
            DamageEnergyType.Unholy,
            DamageEnergyType.Divine,
        };

        public static void Install() {
            var bseClass = Helpers.CreateBlueprint<BlueprintCharacterClass>("bubble-standard-elite", cls => {
                cls.ClassSkills = new StatType[] {
                    StatType.SkillMobility,
                };
                cls.SkillPoints = 1;
                cls.LocalizedDescription = Helpers.CreateString("bse-desc", "A bubblier than normal monster");
                cls.LocalizedDescriptionShort = Helpers.CreateString("bse-desc.short", "A bubblier than normal monster");
                cls.LocalizedName = Helpers.CreateString("bse-name", "Bubbly");
                cls.m_Difficulty = 1;
                cls.HitDie = Kingmaker.RuleSystem.DiceType.D10;
                cls.m_WillSave = BP.Ref<BlueprintStatProgressionReference>("ff4662bde9e75f145853417313842751");             //high
                cls.m_FortitudeSave = BP.Ref<BlueprintStatProgressionReference>("ff4662bde9e75f145853417313842751");        //high
                cls.m_ReflexSave = BP.Ref<BlueprintStatProgressionReference>("ff4662bde9e75f145853417313842751");           //high
                cls.m_BaseAttackBonus = BP.Ref<BlueprintStatProgressionReference>("b3057560ffff3514299e8b93e7648a9d");      //full

                cls.m_Archetypes = Array.Empty<BlueprintArchetypeReference>();
                cls.m_StartingItems = Array.Empty<BlueprintItemReference>();
                cls.m_EquipmentEntities = Array.Empty<KingmakerEquipmentEntityReference>();
                cls.m_SignatureAbilities = Array.Empty<BlueprintFeatureReference>();
                cls.MaleEquipmentEntities = Array.Empty<EquipmentEntityLink>();
                cls.FemaleEquipmentEntities = Array.Empty<EquipmentEntityLink>();
                cls.RecommendedAttributes = Array.Empty<StatType>();
                cls.NotRecommendedAttributes = Array.Empty<StatType>();
                cls.Components = Array.Empty<BlueprintComponent>();
            });

            var bseProgression = Helpers.CreateBlueprint<BlueprintProgression>("bubble-standard-elite-progression", prog => {

                prog.m_Archetypes = Array.Empty<BlueprintProgression.ArchetypeWithLevel>();
                prog.m_AlternateProgressionClasses = Array.Empty<BlueprintProgression.ClassWithLevel>();
                prog.m_UIDeterminatorsGroup = Array.Empty<BlueprintFeatureBaseReference>();
                prog.UIGroups = Array.Empty<UIGroup>();
                prog.Groups = Array.Empty<FeatureGroup>();
                prog.Components = Array.Empty<BlueprintComponent>();
                prog.m_DisplayName = Helpers.EmptyString;
                prog.m_Description = Helpers.EmptyString;
                prog.m_DescriptionShort = Helpers.EmptyString;

                prog.m_Classes = new BlueprintProgression.ClassWithLevel[] {
                    new () { m_Class = bseClass.ToReference<BlueprintCharacterClassReference>() }
                };

                prog.LevelEntries = new LevelEntry[] {
                    Helpers.CreateLevelEntry(1,
                        BP.GetBlueprint<BlueprintFeature>("86669ce8759f9d7478565db69b8c19ad") //diehard
                    )
                };

            });

            bseClass.m_Progression = bseProgression.ToReference<BlueprintProgressionReference>();

            AddBubblyLevels = new AddClassLevels {
                CharacterClass = bseClass
            };

            ExtraDamageFact = Helpers.CreateBlueprint<BlueprintFeature>("bubble-add-damage", dmg => {
                dmg.m_DisplayName = Helpers.EmptyString;
                dmg.m_DescriptionShort = Helpers.EmptyString;
                dmg.m_Description = Helpers.EmptyString;
                dmg.Groups = Array.Empty<FeatureGroup>();
                dmg.AddComponent<GauntletAdditionalDamage>();
            });

        }
    }

    [HarmonyPatch(typeof(UnitDescriptionHelper))]
    public static class UnitDescriptionHelper_Patch {
        
        [HarmonyPatch("GetDescription"), HarmonyPatch(new Type[] { typeof(UnitEntityData), typeof(UnitEntityData), typeof(UnitEntityData) }), HarmonyPostfix]
        public static void GetDescription(UnitEntityData descriptionUnit, UnitEntityData whippingBoy, UnitEntityData sourceUnit, ref UnitDescription __result) {
            if (sourceUnit != null)
                __result.Classes = sourceUnit.Progression.Classes.Select(d => new UnitDescription.ClassData {
                    Class = d.CharacterClass,
                    Level = d.Level
                }).ToArray();
        }

    }
}
