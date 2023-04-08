using BubbleGauntlet.Components;
using BubbleGauntlet.Extensions;
using BubbleGauntlet.Utilities;
using HarmonyLib;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums.Damage;
using Kingmaker.PubSubSystem;
using Kingmaker.ResourceLinks;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.AreaEffects;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Mechanics.Conditions;
using Kingmaker.UnitLogic.Mechanics.Properties;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.Utility;
using Kingmaker.Utility.UnitDescription;
using Owlcat.Runtime.Core.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Kingmaker.Visual.Animation.Kingmaker.Actions.UnitAnimationActionCastSpell;

namespace BubbleGauntlet {
    public class AbilityDeliverThemedChain : AbilityDeliverChain {

        private string ThemedProjectile => GauntletController.Floor.DamageTheme switch {
            DamageEnergyType.Fire => "7172842b720c3534897ebda2e0624c2d",
            DamageEnergyType.Cold => "d6c9daec1256561408a7a72a6979359e",
            DamageEnergyType.Sonic => "4be4353c12c18c340823fc7f00727bd1",
            DamageEnergyType.Electricity => "c7734162c01abdc478418bfb286ed7a5",
            DamageEnergyType.Acid => "086fb81945498de4f92731f51b4b245f",
            DamageEnergyType.NegativeEnergy => "ba3833e8ac0a32144ae3bb1d1182f463",
            DamageEnergyType.Holy => "4899e8d4ec9237a4d835ed1d28a66d89",
            DamageEnergyType.Unholy => "53149f7913f1a8649b8ee538ce139436",
            DamageEnergyType.Divine => "f00eb27234fbc39448b142f1257c8886",
            _ => throw new NotImplementedException(),
        };

        public override IEnumerator<AbilityDeliveryTarget> Deliver(AbilityExecutionContext context, TargetWrapper target) {
            m_Projectile = BP.Ref<BlueprintProjectileReference>(ThemedProjectile);
            m_ProjectileFirst = BP.Ref<BlueprintProjectileReference>(ThemedProjectile);
            return base.Deliver(context, target);
        }
    }

    public class AbilityDeliverThemedProjectile : AbilityDeliverProjectile {

        private string ThemedProjectile => GauntletController.Floor.DamageTheme switch {
            DamageEnergyType.Fire => "52c3a84f628ddde4dbfb38e4a581b01a",
            DamageEnergyType.Cold => "72b45860bdfb81f4284aa005c04594dd",
            DamageEnergyType.Sonic => "c7fd792125b79904881530dbc2ff83de",
            DamageEnergyType.Electricity => "71af6bc04a9a8794c9b6f8439649bb6c",
            DamageEnergyType.Acid => "cf6b3f4577782be43bd7f22f288388b2",
            DamageEnergyType.NegativeEnergy => "f8f8f0402c24a584bb076a2f222d4270",
            DamageEnergyType.Holy => "7363081f6144d604da3645a6ea94fcb1",
            DamageEnergyType.Unholy => "2758f6a35e0e3544f8d7367e57f70d61",
            DamageEnergyType.Divine => "ad2d2104408586e4281489a039e6c291",
            _ => throw new NotImplementedException(),
        };

        public override IEnumerator<AbilityDeliveryTarget> Deliver(AbilityExecutionContext context, TargetWrapper target) {
            m_Projectiles[0] = BP.Ref<BlueprintProjectileReference>(ThemedProjectile);
            return base.Deliver(context, target);
        }
    }

    public class BubbleApplyThemeDamage : ContextActionDealDamage {
        public override void RunAction() {
            if (Context.MaybeCaster.IsAlly(Target.Unit))
                return;

            DiceType type = DiceType.D4;
            int bonus = 0;

            switch (AbilityContext.AbilityBlueprint.Animation) {
                case CastAnimationStyle.Touch:
                    bonus = 0;
                    type = DiceType.D2;
                    break;
                case CastAnimationStyle.BreathWeapon:
                    bonus = 2;
                    type = DiceType.D3;
                    break;
                case CastAnimationStyle.Omni:
                    bonus = 1;
                    type = DiceType.D4;
                    break;
            }

            if (GauntletController.Floor.Level < 4)
                bonus -= 2;
            AbilityContext.AbilityBlueprint.GetComponent<AbilityDeliverEffect>();


            var damage = new DamageInfo {
                Dices = new DiceFormula((GauntletController.Floor.Level + 1) / 2, type),
                Bonus = bonus,
                PreRolledValue = null,
                HalfBecauseSavingThrow = false,
                Empower = false,
                Maximize = false,
                CriticalModifier = null,
            };
            DamageType = new DamageTypeDescription {
                Energy = GauntletController.Floor.DamageTheme,
                Type = Kingmaker.RuleSystem.Rules.Damage.DamageType.Energy,
            };
            DealHitPointsDamage(damage);
        }
    }

    public class DamageRedirectionComponent : UnitFactComponentDelegate, IRulebookHandler<RuleCalculateDamage>, ITargetRulebookHandler<RuleCalculateDamage>, ISubscriber, ITargetRulebookSubscriber {

        public void OnEventAboutToTrigger(RuleCalculateDamage evt) {
            if (evt.ParentRule.IsFake)
                return;

            var friends = CombatManager.CombatMonsters.Where(m => m != Owner && !m.State.IsDead).ToArray();
            if (friends.Length == 0)
                return;

            var fakeDamage = evt.DamageBundle.First.Copy();
            fakeDamage.CopyFrom(evt.DamageBundle.First);
            fakeDamage.ResetDecline();
            fakeDamage.IgnoreReduction = true;

            var fakeDeal = new RuleDealDamage(evt.Initiator, evt.Target, fakeDamage) {
                SourceAbility = Context.SourceAbility,
                SourceArea = Context.AssociatedBlueprint as BlueprintAbilityAreaEffect,
                IsFake = true
            };
            var fakeCalc = new RuleCalculateDamage(fakeDeal);
            int toShare = Context.TriggerRule(fakeCalc).CalculatedDamage.Sum(x => x.ValueWithoutReduction);
            toShare = (int)Math.Ceiling(toShare * 0.75f);
            float overflow = 0;

            int totalFriendHP = friends.Sum(f => f.MaxHP);

            Main.Log($"Actual damage: {toShare}, Total friend hp: {totalFriendHP}");

            foreach (var friend in friends) {
                float myFactor = friend.MaxHP / (float)totalFriendHP;
                float myDamageRaw = toShare * myFactor;
                int damage = (int)myDamageRaw;
                float overflowed = myDamageRaw - damage;
                float oldOverflow = overflow;
                overflow += overflowed;
                if (overflow > 1) {
                    damage++;
                    overflow--;
                }
                Main.Log($"friend: hp:{friend.MaxHP} factor:{myFactor} raw:{myDamageRaw} damage:{damage}   (oo:{oldOverflow} + overflowed:{overflowed} = overflow:{overflow}");

                if (friend.HPLeft - damage <= 0)
                    damage = friend.HPLeft - 1;

                var sharedDamage = evt.DamageBundle.First.Copy();
                sharedDamage.CopyFrom(evt.DamageBundle.First);
                sharedDamage.ResetDecline();
                sharedDamage.PreRolledValue = damage;
                var damageFriend = new RuleDealDamage(evt.Initiator, friend, sharedDamage);
                Context.TriggerRule(damageFriend);
            }

            evt.DamageBundle.First.AddDecline(new(DamageDeclineType.Total, this.Fact));
            evt.DamageBundle.First.PreRolledValue = 0;
            evt.DamageBundle.First.SourceFact = Fact;
        }

        public void OnEventDidTrigger(RuleCalculateDamage evt) { }
    }

    public class ThemeImmunityComponent : UnitFactComponentDelegate, IRulebookHandler<RuleCalculateDamage>, ITargetRulebookHandler<RuleCalculateDamage>, ISubscriber, ITargetRulebookSubscriber {
        private DamageEnergyType Type => GauntletController.Floor.DamageTheme;

        public void OnEventAboutToTrigger(RuleCalculateDamage evt) {
            foreach (BaseDamage baseDamage in evt.DamageBundle) {
                DamageEnergyType? nullable = baseDamage is EnergyDamage energyDamage ? new DamageEnergyType?(energyDamage.EnergyType) : null;
                DamageEnergyType type = Type;
                if (nullable.GetValueOrDefault() == type & nullable != null && !Owner.State.HasCondition(UnitCondition.SuppressedEnergyImmunity)) {
                    baseDamage.AddDecline(new(DamageDeclineType.Total, this.Fact));
                }
            }
        }

        public void OnEventDidTrigger(RuleCalculateDamage evt) { }

        public override void OnTurnOn() {
            Owner.Ensure<UnitPartDamageReduction>().AddImmunity(Fact, this, Type);
        }

        // Token: 0x06009F60 RID: 40800 RVA: 0x0006ACD6 File Offset: 0x00068ED6
        public override void OnTurnOff() {
            UnitPartDamageReduction unitPartDamageReduction = Owner.Get<UnitPartDamageReduction>();
            if (unitPartDamageReduction == null) {
                return;
            }
            unitPartDamageReduction.RemoveImmunity(Fact, this);
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
        public static BlueprintAbility SpawnPool;
        public static BlueprintBuff BubblyEliteVisual;

        public static AddClassLevels AddEliteLevels;

        public static BlueprintBuff NovaOnMiss;
        public static BlueprintBuff ConeOnAny;
        public static BlueprintBuff ChainOnHit;
        public static List<BlueprintBuff> EliteAttacks = new();
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
        public static BlueprintFeature EliteStats;
        public static BlueprintBuff Fracturing;
        public static BlueprintBuff Vanguard;
        public static BlueprintBuff PainShare;
        public static List<BlueprintBuff> EliteDefenses = new();

        public static void Install() {
            Main.Log("Installing FUN");
            BlueprintCharacterClass bseClass = ClassBuilder.New("bse-elite")
                .FortSaves(SaveProgression.High)
                .WillSaves(SaveProgression.High)
                .ReflexSaves(SaveProgression.High)
                .HP(DiceType.D12)
                .BAB(BABProgression.Full)
                .Name("Bubbly")
                .Description("A bubblier than normal monster")
                .SkillPoints(1)
                .ClassSkills(StatType.SkillMobility)
                .Blueprint;

            var bseProgression = ProgressionBuilder.New("bubble-standard-elite-progression")
                .ForClasses(bseClass.ToReference<BlueprintCharacterClassReference>())
                .AddFeatures(1, BP.Ref<BlueprintFeatureBaseReference>("86669ce8759f9d7478565db69b8c19ad"))
                .Blueprint;
            bseClass.m_Progression = bseProgression.ToReference<BlueprintProgressionReference>();

            BubblyEliteVisual = Helpers.CreateBlueprint<BlueprintBuff>("bubble-elite-fx", buff => {
                buff.m_Flags = BlueprintBuff.Flags.StayOnDeath;
                buff.SetNameDescription("Elite", "Some bubbles float to the surface and attain the rank of <b>Elite</b>.\nBeware!\nThese monsters are extremely tough and possess abilities far beyond a regular bubble.");
            });


            BlueprintCharacterClass minibossClass = ClassBuilder.New("bse-miniboss")
                .FortSaves(SaveProgression.High)
                .WillSaves(SaveProgression.High)
                .ReflexSaves(SaveProgression.High)
                .HP(DiceType.D20)
                .BAB(BABProgression.Full)
                .Name("Elite")
                .Description("An elite bubble")
                .SkillPoints(1)
                .ClassSkills(StatType.SkillMobility)
                .Blueprint;

            EliteStats = Helpers.CreateBlueprint<BlueprintFeature>("elite-stats", elite => {
                elite.SetNameDescription("Elite Stats", "This monster is extra bubbly");
                elite.Ranks = 1;
                elite.IsClassFeature = true;
                elite.HideInUI = false;
                elite.AddComponent<AddStatBonus>(bonus => {
                    bonus.Value = 2;
                    bonus.Descriptor = Kingmaker.Enums.ModifierDescriptor.UntypedStackable;
                    bonus.Stat = StatType.Dexterity;
                });
            });

            var minibossProgression = ProgressionBuilder.New("bubble-miniboss-progression")
                .ForClasses(minibossClass.ToReference<BlueprintCharacterClassReference>())
                .AddFeatures(1, EliteStats.ToReference<BlueprintFeatureBaseReference>())
                .AddFeatures(3, EliteStats.ToReference<BlueprintFeatureBaseReference>())
                .AddFeatures(5, EliteStats.ToReference<BlueprintFeatureBaseReference>())
                .AddFeatures(7, EliteStats.ToReference<BlueprintFeatureBaseReference>())
                .AddFeatures(9, EliteStats.ToReference<BlueprintFeatureBaseReference>())
                .Blueprint;
            minibossClass.m_Progression = minibossProgression.ToReference<BlueprintProgressionReference>();


            AddBubblyLevels = new AddClassLevels {
                CharacterClass = bseClass
            };
            AddEliteLevels = new AddClassLevels {
                CharacterClass = minibossClass
            };

            BlueprintComponent[] themeComponents = {
                new ThemeDamageComponent(),
                new ThemeImmunityComponent(),
            };

            foreach (var energyType in EnergyTypes) {
                ThemeBuffs[energyType] = Helpers.CreateBlueprint<BlueprintBuff>($"bubble-theme-buff-{energyType.ToString().Replace(' ', '_')}", buff => {
                    buff.FxOnStart = new();
                    buff.FxOnStart.AssetId = energyType switch {
                        DamageEnergyType.Fire => "26fa35beb7d89bf4dafb93033941700c",
                        DamageEnergyType.Cold => "6997425ac95b2a347a261dbd4139c9a9",
                        DamageEnergyType.Sonic => "39da71647ad4747468d41920d0edd721",
                        DamageEnergyType.Electricity => "6035a889bae45f242908569a7bc25c93",
                        DamageEnergyType.Acid => "b2b0f28a5be7ee349b819925143d1e70",
                        DamageEnergyType.NegativeEnergy => "d7c1a0c281a1a784cbab128b0c7a9e7b",
                        DamageEnergyType.Holy => "2014f4bf65c37b64d9609f0e87fa2e9a",
                        DamageEnergyType.Unholy => "7257fc7d2af24cb48954f376a0bb9691",
                        DamageEnergyType.Divine => "2014f4bf65c37b64d9609f0e87fa2e9a",
                        _ => null
                    };
                    buff.SetNameDescription($"Gauntlet Monster ({energyType})", "Monsters in the gauntlet gain features based on the current floor theme");
                    buff.Components = themeComponents;
                });
            }

            BubblyBuffVisual = Helpers.CreateBlueprint<BlueprintBuff>("bubble-bubbly-fx", buff => {
                buff.FxOnStart = new();
                buff.FxOnStart.AssetId = "81c4df5abd309ef4b86f787a95669f1f";
                buff.m_Flags = BlueprintBuff.Flags.StayOnDeath;
                buff.SetNameDescription("Bubbly", "The bubbles infuse this monster with extra-planar might");
            });

            //var area = BP.GetBlueprint<BlueprintAbilityAreaEffect>("3659ce23ae102ca47a7bf3a30dd98609");
            //area.Size = 5.Feet();

            //SpawnPool = Helpers.CreateBlueprint<BlueprintAbility>("bubble-pool", pool => {
            //    pool.SetNameDescription("Bubble chain", "A bubbling pool of thematic energy");
            //    pool.Range = AbilityRange.Long;
            //    pool.CanTargetPoint = true;
            //    pool.EffectOnEnemy = AbilityEffectOnUnit.Harmful;
            //    pool.Animation = CastAnimationStyle.Point;
            //    pool.Type = AbilityType.Supernatural;
            //    pool.LocalizedDuration = Helpers.EmptyString;
            //    pool.LocalizedSavingThrow = Helpers.EmptyString;
            //    pool.MaterialComponent = new();
            //    pool.DisableLog = true;
            //    pool.AddComponent<AbilityEffectRunAction>(run => {
            //        run.Actions = Helpers.CreateActionList(
            //            new ContextActionSpawnAreaEffect {
            //                m_AreaEffect = BP.Ref<BlueprintAbilityAreaEffectReference>("3659ce23ae102ca47a7bf3a30dd98609"),
            //                DurationValue = new() {
            //                    Rate = Kingmaker.UnitLogic.Mechanics.DurationRate.Minutes,
            //                    m_IsExtendable = true,
            //                    DiceCountValue = new() { },
            //                    BonusValue = new() {
            //                        Value = 100,
            //                        ValueType = Kingmaker.UnitLogic.Mechanics.ContextValueType.Simple,
            //                    }
            //                },
            //            }
            //        );
            //    });
            //});

            var chain = Helpers.CreateBlueprint<BlueprintAbility>("bubble-chain", chain => {
                chain.SetNameDescription("Bubble chain", "Chains of bubbly power");
                chain.Range = AbilityRange.Weapon;
                chain.CanTargetSelf = false;
                chain.CanTargetEnemies = true;
                chain.CanTargetFriends = false;
                chain.CanTargetPoint = false;
                chain.EffectOnEnemy = AbilityEffectOnUnit.Harmful;
                chain.EffectOnAlly = AbilityEffectOnUnit.None;
                chain.Animation = CastAnimationStyle.Touch;
                chain.Type = AbilityType.Supernatural;
                chain.LocalizedDuration = Helpers.EmptyString;
                chain.LocalizedSavingThrow = Helpers.EmptyString;
                chain.MaterialComponent = new();
                chain.DisableLog = true;
                chain.AddComponent<AbilityDeliverThemedChain>(projectile => {
                    projectile.m_Projectile = BP.Ref<BlueprintProjectileReference>("c7734162c01abdc478418bfb286ed7a5");
                    projectile.m_ProjectileFirst = BP.Ref<BlueprintProjectileReference>("c7734162c01abdc478418bfb286ed7a5");
                    projectile.m_TargetType = TargetType.Enemy;
                    projectile.m_Condition = new();
                    projectile.Radius = 45.Feet();
                    projectile.TargetsCount = new() { Value = 12 };
                });
                chain.AddComponent<AbilityEffectRunAction>(run => {
                    run.Actions = Helpers.CreateActionList(new BubbleApplyThemeDamage());
                });
            });

            var Cone = Helpers.CreateBlueprint<BlueprintAbility>("bubble-cone", cone => {
                cone.SetNameDescription("Bubble cone", "A wide field beam of bubbly power");
                cone.Range = AbilityRange.Projectile;
                cone.CanTargetSelf = false;
                cone.CanTargetEnemies = true;
                cone.CanTargetFriends = false;
                cone.CanTargetPoint = false;
                cone.EffectOnEnemy = AbilityEffectOnUnit.Harmful;
                cone.EffectOnAlly = AbilityEffectOnUnit.None;
                cone.Animation = CastAnimationStyle.BreathWeapon;
                cone.Type = AbilityType.Supernatural;
                cone.LocalizedDuration = Helpers.EmptyString;
                cone.LocalizedSavingThrow = Helpers.EmptyString;
                cone.MaterialComponent = new();
                cone.DisableLog = true;
                cone.AddComponent<AbilityDeliverThemedProjectile>(projectile => {
                    projectile.m_Projectiles = Helpers.Arr(BP.Ref<BlueprintProjectileReference>("cf6b3f4577782be43bd7f22f288388b2"));
                    projectile.m_Length = 15.Feet();
                    projectile.m_LineWidth = 5.Feet();
                    projectile.Type = AbilityProjectileType.Cone;
                });
                cone.AddComponent<AbilityEffectRunAction>(run => {
                    run.Actions = Helpers.CreateActionList(new BubbleApplyThemeDamage());
                });
            });

            var Nova = Helpers.CreateBlueprint<BlueprintAbility>("bubble-nova", nova => {
                nova.SetNameDescription("Bubble nova", "An eruption of bubbly power");
                nova.Range = AbilityRange.Personal;
                nova.CanTargetSelf = true;
                nova.Hidden = true;
                nova.Animation = CastAnimationStyle.Omni;
                nova.Type = AbilityType.Supernatural;
                nova.LocalizedDuration = Helpers.EmptyString;
                nova.LocalizedSavingThrow = Helpers.EmptyString;
                nova.MaterialComponent = new();
                nova.DisableLog = true;
                nova.AddComponent<AbilityTargetsAround>(range => {
                    range.m_Radius = 15.Feet();
                    range.m_Condition = new();
                    range.name = "nova-range";
                });
                nova.AddComponent<AbilitySpawnFx>(fx => {
                    fx.PrefabLink = new PrefabLink { AssetId = "2644dac00cee8b840a35f2445c4dffd9" };
                    fx.PositionAnchor = AbilitySpawnFxAnchor.None;
                    fx.OrientationAnchor = AbilitySpawnFxAnchor.None;
                    fx.name = "nova-fx";
                });
                nova.AddComponent<AbilityEffectRunAction>(run => {
                    run.Actions = Helpers.CreateActionList(new BubbleApplyThemeDamage());

                });
            });
            NovaOnMiss = TriggerOn(Nova, TriggerType.OnlyMiss, TriggerOnUnit.Self, "Bubbling Miss", "Missing is no obstacle to a determined bubble! This monster explodes with thematic energy when it misses with an attack.");
            ConeOnAny = TriggerOn(Cone, TriggerType.HitOrMiss, TriggerOnUnit.Target, "Bubbling Splash", "This bubble splashes thematic energy across the battlefield on every attack, hit or miss!");
            ChainOnHit = TriggerOn(chain, TriggerType.OnlyHit, TriggerOnUnit.Target, "Bubbling Chain", "Sometimes a bubble likes to make the worst case even worse... On hit this monster will chain thematic energy through your entire party.");

            EliteAttacks.Add(NovaOnMiss);
            EliteAttacks.Add(ConeOnAny);
            EliteAttacks.Add(ChainOnHit);


            var vanguardActualBuff = Helpers.CreateBlueprint<BlueprintBuff>("bubblegauntlet-vanguard-targetbuff", buff => {
                buff.SetNameDescription("Bubble Shielded", "This bubble is under the protection of an elite and has DR/- equal to (3 + Floor/2).");
                buff.AddComponent<DynamicDamageResistancePhysical>(res => {
                    res.GauntletValue = floor => 3 + floor / 2;
                });
            });

            var vanguardAreaEffect = Helpers.CreateBlueprint<BlueprintAbilityAreaEffect>("bubbelgauntlet-vanguard-area", area => {
                area.AffectEnemies = false;
                area.AggroEnemies = false;
                area.m_TargetType = BlueprintAbilityAreaEffect.TargetType.Ally;
                area.SpellResistance = false;
                area.Shape = AreaEffectShape.Cylinder;
                area.Size = 20.Feet();
                area.Fx = new PrefabLink { AssetId = "bbd6decdae32bce41ae8f06c6c5eb893" };
                area.AddComponent<AbilityAreaEffectBuff>(apply => {
                    apply.Condition = new() {
                        Conditions = Helpers.Arr(new ContextConditionIsCaster().Not())
                    };
                    apply.m_Buff = vanguardActualBuff.ToReference<BlueprintBuffReference>();
                });
            });

            Vanguard = Helpers.CreateBlueprint<BlueprintBuff>("bubblegauntlet-vanguardbuff", buff => {
                buff.SetNameDescription("Bubble Shield", "This bubble shields its friends, granting all allies DR/- equal to (3 + Floor/2).");
                buff.AddComponent<AddAreaEffect>(area => {
                    area.m_AreaEffect = vanguardAreaEffect.ToReference<BlueprintAbilityAreaEffectReference>();
                });
            });

            //Main.Log("VANGUARD: \n" + string.Join("\n", Validator.Check(Vanguard).Select(e => $"ERROR: {e}")));
            //Main.Log("VANGUARD_AREA: \n" + string.Join("\n", Validator.Check(vanguardAreaEffect).Select(e => $"ERROR: {e}")));
            //Main.Log("VANGUARD_ACTUAL: \n" + string.Join("\n", Validator.Check(vanguardActualBuff).Select(e => $"ERROR: {e}")));
            //Main.Log("VANGAURD_COMPONENT: \n" + string.Join("\n", Validator.Check(vanguardActualBuff.GetComponent<DynamicDamageResistancePhysical>()).Select(e => $"ERROR: {e}")));

            PainShare = Helpers.CreateBlueprint<BlueprintBuff>("bubblegauntlet-pain-share", buff => {
                buff.SetNameDescription("Pain Share", "This bubble is so well-liked that the first part of any damage dealt to it will instead be split amongst its friends");
                buff.AddComponent<DamageRedirectionComponent>();
            });

            Fracturing = Helpers.CreateBlueprint<BlueprintBuff>("bubblegauntlet-fracturing", buff => {
                buff.SetNameDescription("Recursive Bubble", "When popped this bubble will split into d3+1 new sub-bubbles, subbles.");
                buff.AddComponent<DeathActions>(death => {
                    death.Actions = Helpers.CreateActionList(new DynamicContextAction(ctxAction => {
                        var generated = new List<string>();
                        var existing = new List<Vector3>();
                        var count = D.Roll(3) + 1;
                        var caster = ctxAction.Context.MaybeCaster;
                        for (int i = 0; i < count; i++) {
                            var onSphere = UnityEngine.Random.onUnitSphere;
                            onSphere.y = 0;
                            var pos = caster.Position + onSphere;
                            var subble = CombatManager.SpawnMonster(pos, caster.Position - pos, existing, generated, ContentManager.BubbleToSubble[caster.Blueprint]);
                            subble.State.Size = Kingmaker.Enums.Size.Tiny;
                        }
                    }));
                });
            });

            EliteDefenses.Add(Fracturing);
            EliteDefenses.Add(Vanguard);
            EliteDefenses.Add(PainShare);

            Main.Log("FUN has been installed");

        }

        public enum TriggerOnUnit {
            Self,
            Target,
        }

        public enum TriggerType {
            OnlyMiss,
            OnlyHit,
            HitOrMiss,
            OnlyCrit,
        }

        public static bool OnMiss(this TriggerType type) {
            return type switch {
                TriggerType.OnlyMiss => true,
                TriggerType.HitOrMiss => true,
                _ => false
            };
        }

        private static BlueprintBuff TriggerOn(BlueprintAbility ability, TriggerType on, TriggerOnUnit onUnit, string name, string description) {
            return Helpers.CreateBlueprint<BlueprintBuff>("bubble-elite-attack-" + ability.name, feature => {
                feature.Ranks = 1;
                feature.SetNameDescription(name, description);
                feature.AddComponent<AddInitiatorAttackWithWeaponTrigger>(trigger => {
                    trigger.OnlyHit = !on.OnMiss();
                    trigger.CheckWeaponRangeType = true;
                    trigger.OnMiss = on == TriggerType.OnlyMiss;
                    trigger.CriticalHit = on == TriggerType.OnlyCrit;
                    trigger.ActionsOnInitiator = onUnit == TriggerOnUnit.Self;
                    trigger.name = "exec-on-trigger";
                    trigger.Action = Helpers.CreateActionList(
                        new ContextActionCastSpell {
                            m_Spell = ability.ToReference<BlueprintAbilityReference>(),
                            DC = new(),
                            SpellLevel = new(),
                            name = "bubble-cast-ability",
                        }
                    );
                });
            });
        }
    }

    public class ProgressionBuilder {
        private BlueprintProgression bp;
        private Dictionary<int, LevelEntry> levelEntries = new();

        public static ProgressionBuilder New(string name) {
            ProgressionBuilder builder = new();
            builder.bp = Helpers.CreateBlueprint<BlueprintProgression>(name, prog => {
                prog.m_Archetypes = Array.Empty<BlueprintProgression.ArchetypeWithLevel>();
                prog.m_AlternateProgressionClasses = Array.Empty<BlueprintProgression.ClassWithLevel>();
                prog.m_UIDeterminatorsGroup = Array.Empty<BlueprintFeatureBaseReference>();
                prog.UIGroups = Array.Empty<UIGroup>();
                prog.Groups = Array.Empty<FeatureGroup>();
                prog.Components = Array.Empty<BlueprintComponent>();
                prog.m_DisplayName = Helpers.EmptyString;
                prog.m_Description = Helpers.EmptyString;
                prog.m_DescriptionShort = Helpers.EmptyString;
            });

            return builder;
        }

        public ProgressionBuilder ForClasses(params BlueprintCharacterClassReference[] clazz) {
            bp.m_Classes = clazz.Select(c => new BlueprintProgression.ClassWithLevel { m_Class = c }).ToArray();
            return this;
        }

        public ProgressionBuilder AddFeatures(int level, params BlueprintFeatureBaseReference[] features) {
            if (!levelEntries.TryGetValue(level, out var entry)) {
                entry = new LevelEntry { Level = level };
                levelEntries[level] = entry;
            }
            entry.m_Features.AddRange(features);
            return this;
        }

        public BlueprintProgression Blueprint {
            get {
                bp.LevelEntries = levelEntries.Values.ToArray();
                return bp;
            }
        }

    }

    public class ClassBuilder {

        private BlueprintCharacterClass bp;

        public static ClassBuilder New(string name) {
            ClassBuilder builder = new();
            builder.bp = Helpers.CreateBlueprint<BlueprintCharacterClass>(name, cls => {
                cls.m_Archetypes = Array.Empty<BlueprintArchetypeReference>();
                cls.m_StartingItems = Array.Empty<BlueprintItemReference>();
                cls.m_EquipmentEntities = Array.Empty<KingmakerEquipmentEntityReference>();
                cls.m_SignatureAbilities = Array.Empty<BlueprintFeatureReference>();
                cls.MaleEquipmentEntities = Array.Empty<EquipmentEntityLink>();
                cls.FemaleEquipmentEntities = Array.Empty<EquipmentEntityLink>();
                cls.RecommendedAttributes = Array.Empty<StatType>();
                cls.NotRecommendedAttributes = Array.Empty<StatType>();
                cls.Components = Array.Empty<BlueprintComponent>();
                cls.ClassSkills = Array.Empty<StatType>();
            });
            return builder;
        }

        public ClassBuilder ClassSkills(params StatType[] skills) {
            bp.ClassSkills = skills;
            return this;
        }

        public ClassBuilder SkillPoints(int skillPoints) {
            bp.SkillPoints = skillPoints;
            return this;
        }

        private static BlueprintStatProgressionReference Save(SaveProgression rate) {
            return rate switch {
                SaveProgression.High => BP.Ref<BlueprintStatProgressionReference>("ff4662bde9e75f145853417313842751"),
                SaveProgression.Low => BP.Ref<BlueprintStatProgressionReference>("dc0c7c1aba755c54f96c089cdf7d14a3"),
                _ => throw new NotImplementedException(),
            };
        }


        public ClassBuilder FortSaves(SaveProgression rate) {
            bp.m_FortitudeSave = Save(rate);
            return this;
        }
        public ClassBuilder ReflexSaves(SaveProgression rate) {
            bp.m_ReflexSave = Save(rate);
            return this;
        }
        public ClassBuilder WillSaves(SaveProgression rate) {
            bp.m_WillSave = Save(rate);
            return this;
        }
        public ClassBuilder BAB(BABProgression rate) {
            bp.m_BaseAttackBonus = rate switch {
                BABProgression.Full => BP.Ref<BlueprintStatProgressionReference>("b3057560ffff3514299e8b93e7648a9d"),
                BABProgression.ThreeQ => BP.Ref<BlueprintStatProgressionReference>("4c936de4249b61e419a3fb775b9f2581"),
                BABProgression.Half => BP.Ref<BlueprintStatProgressionReference>("0538081888b2d8c41893d25d098dee99"),
                _ => throw new NotImplementedException(),
            };
            return this;
        }
        public ClassBuilder HP(DiceType d) {
            bp.HitDie = d;
            return this;
        }

        public ClassBuilder Description(string fullDesc, string shortDesc = null) {
            bp.LocalizedDescription = Helpers.CreateString($"{bp.name}.descfull", fullDesc);
            bp.LocalizedDescriptionShort = Helpers.CreateString($"{bp.name}.descshort", shortDesc ?? fullDesc);
            return this;
        }
        public ClassBuilder Name(string name) {
            bp.LocalizedName = Helpers.CreateString($"{bp.name}.name", name);
            return this;
        }

        public BlueprintCharacterClass Blueprint => bp;

    }

    public enum BABProgression {
        Full,
        ThreeQ,
        Half,
    }

    public enum SaveProgression {
        High,
        Low,
    }

    [HarmonyPatch(typeof(LogChannel))]
    public static class LOG_BE_QUIET {

        public static HashSet<string> Quiet = new() {
            "Await input module",
            "Await event system",
        };

        [HarmonyPatch("Log"), HarmonyPatch(new Type[] { typeof(string), typeof(object[]) }), HarmonyPrefix]
        public static bool Log(string messageFormat) {
            if (Quiet.Contains(messageFormat))
                return false;
            return true;
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
            __result.Features = UnitDescriptionHelper.ExtractFeatures(sourceUnit);
            //__result.UIFeatures = UnitDescriptionHelper.ExtractFeaturesForUI(sourceUnit);
            __result.HD = sourceUnit.Progression.CharacterLevel;
        }

    }
    [HarmonyPatch(typeof(AddInitiatorAttackWithWeaponTrigger))]
    public static class HOOK_PATCH {

        [HarmonyPatch("OnEventDidTrigger"), HarmonyPostfix]
        public static void OnEventDidTrigger(AddInitiatorAttackWithWeaponTrigger __instance) {
        }

    }
}
