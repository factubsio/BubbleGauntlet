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
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.Utility.UnitDescription;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace BubbleGauntlet {

    public class ThemeImmunityComponent : UnitFactComponentDelegate, IRulebookHandler<RuleCalculateDamage>, ITargetRulebookHandler<RuleCalculateDamage>, ISubscriber, ITargetRulebookSubscriber {

        private DamageEnergyType Type => GauntletController.Floor.DamageTheme;


        public void OnEventAboutToTrigger(RuleCalculateDamage evt) {
            foreach (BaseDamage baseDamage in evt.DamageBundle)
			{
				var energyDamage = baseDamage as EnergyDamage;
				DamageEnergyType? nullable = (energyDamage != null) ? new DamageEnergyType?(energyDamage.EnergyType) : null;
				DamageEnergyType type = this.Type;
				if ((nullable.GetValueOrDefault() == type & nullable != null) && !base.Owner.State.HasCondition(UnitCondition.SuppressedEnergyImmunity))
				{
					baseDamage.IncreaseDeclineTo(DamageDeclineType.Total);
				}
			}
        }

        public void OnEventDidTrigger(RuleCalculateDamage evt) { }

        public override void OnTurnOn() {
            base.Owner.Ensure<UnitPartDamageReduction>().AddImmunity(base.Fact, this, this.Type);
        }

        // Token: 0x06009F60 RID: 40800 RVA: 0x0006ACD6 File Offset: 0x00068ED6
        public override void OnTurnOff()
		{
			UnitPartDamageReduction unitPartDamageReduction = base.Owner.Get<UnitPartDamageReduction>();
			if (unitPartDamageReduction == null)
			{
				return;
			}
			unitPartDamageReduction.RemoveImmunity(base.Fact, this);
		}
    }

    public class ThemeDamageComponent : EntityFactComponentDelegate<AddInitiatorAttackWithWeaponTrigger.ComponentData>, IInitiatorRulebookHandler<RulePrepareDamage>, IRulebookHandler<RulePrepareDamage>, ISubscriber, IInitiatorRulebookSubscriber {
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
        public static BlueprintBuff BubblyBuffVisual;
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

        public static Dictionary<DamageEnergyType, BlueprintBuff> ThemeBuffs = new();

        public static void Install() {
            Main.Log("Installing FUN");
            var bseClass = Helpers.CreateBlueprint<BlueprintCharacterClass>("bubble-standard-elite", cls => {
                cls.ClassSkills = new StatType[] {
                    StatType.SkillMobility,
                };
                cls.SkillPoints = 1;
                cls.LocalizedDescription = Helpers.CreateString("bse-desc", "A bubblier than normal monster");
                cls.LocalizedDescriptionShort = Helpers.CreateString("bse-desc.short", "A bubblier than normal monster");
                cls.LocalizedName = Helpers.CreateString("bse-name", "Bubbly");
                cls.m_Difficulty = 1;
                cls.HitDie = Kingmaker.RuleSystem.DiceType.D12;
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


            BlueprintComponent[] themeComponents = {
                new ThemeDamageComponent(),
                new ThemeImmunityComponent(),
            };


            foreach (var energyType in EnergyTypes) {
                ThemeBuffs[energyType] = Helpers.CreateBlueprint<BlueprintBuff>("bubble-theme-buff", buff => {
                    buff.FxOnStart = new();
                    buff.FxOnStart.AssetId = energyType switch {
                        DamageEnergyType.Fire => "f5eaec10b715dbb46a78890db41fa6a0",
                        DamageEnergyType.Cold => "6997425ac95b2a347a261dbd4139c9a9",
                        DamageEnergyType.Sonic => "39da71647ad4747468d41920d0edd721",
                        DamageEnergyType.Electricity => "6035a889bae45f242908569a7bc25c93",
                        DamageEnergyType.Acid => "b2b0f28a5be7ee349b819925143d1e70",
                        DamageEnergyType.NegativeEnergy => "d7c1a0c281a1a784cbab128b0c7a9e7b",
                        DamageEnergyType.Holy => "3cf209e5299921349a1c159f35cfa369",
                        DamageEnergyType.Unholy => "7257fc7d2af24cb48954f376a0bb9691",
                        DamageEnergyType.Divine => "3cf209e5299921349a1c159f35cfa369",
                        _ => null
                    };
                    buff.SetNameDescription($"Gauntlet Monster ({energyType})", "Monsters in the gauntlet gain features based on the current floor theme");
                    buff.Components = themeComponents;
                });
            }

            BubblyBuffVisual = Helpers.CreateBlueprint<BlueprintBuff>("bubble-bubbly-fx", buff => {
                buff.FxOnStart = new();
                //buff.FxOnStart.AssetId = "6e01d9f56e260ea4088836571d0e6404";
                buff.FxOnStart.AssetId = "81c4df5abd309ef4b86f787a95669f1f";
                buff.m_Flags = BlueprintBuff.Flags.StayOnDeath;
                buff.SetNameDescription("Bubbly", "The bubbles infuse this monster with extra-planar might");
            });

            Main.Log("FUN has been installed");

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
