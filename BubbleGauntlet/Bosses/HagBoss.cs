#define BALANCE

using BubbleGauntlet.Components;
using BubbleGauntlet.Config;
using BubbleGauntlet.Extensions;
using BubbleGauntlet.Utilities;
using JetBrains.Annotations;
using Kingmaker;
using Kingmaker.AI;
using Kingmaker.AI.Blueprints;
using Kingmaker.AI.Blueprints.Considerations;
using Kingmaker.AreaLogic.Cutscenes;
using Kingmaker.AreaLogic.Cutscenes.Commands;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.DialogSystem.Blueprints;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Localization;
using Kingmaker.ResourceLinks;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.AreaEffects;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.Interaction;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.Utility;
using Kingmaker.Visual.Particles;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;
using static Kingmaker.Designers.EventConditionActionSystem.NamedParameters.ParametrizedContextSetter;
using static Kingmaker.Visual.Animation.Kingmaker.Actions.UnitAnimationActionCastSpell;


namespace BubbleGauntlet.Bosses {

    public class CommandBeginCombat : CommandBase {
        public override bool IsFinished(CutscenePlayerData player) {
            return true;
        }

        public override void OnRun(CutscenePlayerData player, bool skipping) {

            for (int i = 0; i < 4; i++) {

                var unit = HagBoss.Instance.u[i];

                unit.State.Features.Immortality.Retain();
                unit.State.Features.IsUntargetable.Retain();
                unit.SwitchFactions(MonsterDB.MobFaction);
                var timer = unit.AddBuffNotDispelable(HagBoss.Instance.TimerBuff);
                timer.SetDuration(TimeSpan.FromSeconds(12));
            }

            HagBoss.Instance.SetActive(HagBoss.Instance.firstHag);
        }

        public override void SetTime(double time, CutscenePlayerData player) {
        }
    }

    public class HagBossSettings {
        [JsonProperty]
        public int MonstrousHumanoidClassLevels;
        [JsonProperty]
        public int NaturalArmorBonus;
    }

    public class HagBoss : IMinorBoss {
        public int ActiveHag = -1;
        public GameObject beamFx;
        public int firstHag = -1;
        public BlueprintBuff TimerBuff;
        public UnitEntityData[] u = new UnitEntityData[4];
        public BlueprintAbility VeryHotAbility;
        private const string AcidArrow = "9a46dfd390f943647ab4395fc997936d";
        private const string ChainLightning = "645558d63604747428d55f0dd3a4cb58";
        private const string FrigidTouchCast = "b6010dda6333bcf4093ce20f0063cd41";
        private const string HoarforstAbility = "7244a24f0c186ce4b8a89fd26feded50";
        private const string MoltenOrb = "42a65895ba0cb3a42b6019039dd2bff1";
        private const string PlagueStormShakes = "da07f3866c316e642a2db61768071c61";

        private const int SpringIndex = 0;
        private const int SummerIndex = 1;
        private const int AutumnIndex = 2;
        private const int WinterIndex = 3;

        public HagBossSettings Settings;

        private static readonly string[] seasons = {
            "Spring", "Summer", "Autumn", "Winter",
        };

        private static readonly string[] titles = {
            "Spite", "Flame", "Rot", "Chill",
        };

        private AreaMap _AreaMap;
        private GameObject activeFx;
        private readonly SharedStringAsset[] barkOnActivate = new SharedStringAsset[4];
        private BlueprintDialog ChooseDialog;
        private Cutscene cutscene;
        private Vector3 enterLoc = new(32.214f, 60.0012f, 31.717f);
#if BALANCE
        private Vector3 firstLoc = new(26.0258f, 60.0016f, 14.6158f);
#else
        private Vector3 firstLoc = new(26.0258f, 60.0016f, 19.6158f);
#endif
        private Vector3 spawnOffset = new Vector3(-2f, 0, 2f);
        private BlueprintUnitFact[] uniqueHagFact = new BlueprintUnitFact[4];
        private readonly BlueprintUnit[] unitBase = new BlueprintUnit[4];
        private readonly BlueprintBuff[] visuals = new BlueprintBuff[4];
        public static HagBoss Instance => GauntletController.Boss as HagBoss;

        public AreaMap Map => _AreaMap;

        public string Name => "SeasonalHags";

        private BlueprintUnit AutumnUnit => unitBase[AutumnIndex];

        private BlueprintUnit SpringUnit => unitBase[SpringIndex];

        private BlueprintUnit SummerUnit => unitBase[SummerIndex];

        private BlueprintUnit WinterUnit => unitBase[WinterIndex];

        public void Reset() {
            ModSettings.LoadSettings<HagBossSettings>("HagBoss.json", ref Settings, true);

            for (int i = 0; i < unitBase.Length; i++) {
                var bp = unitBase[i];
                bp.GetComponent<AddClassLevels>().Levels = Settings.MonstrousHumanoidClassLevels;
                uniqueHagFact[i].GetComponent<DynamicAddStatBonus>().Calculate = floor => Settings.NaturalArmorBonus;
            }

            foreach (var group in Game.Instance.UnitGroups) {
                if (group.Id == "hag-active") {
                    foreach (var unit in group) {
                        unit.MarkForDestroy();
                    }
                }
            }

            for (int i = 0; i < 4; i++) {
                u[i] = Game.Instance.EntityCreator.SpawnUnit(unitBase[i], firstLoc + spawnOffset * i, Quaternion.identity, null);
                u[i].GroupId = "hag-active";
                u[i].LookAt(enterLoc);
                u[i].AddBuffNotDispelable(visuals[i]);
            }
        }

        public void Begin() {
#if !BALANCE
            for (int i = 0; i < 4; i++) {
                u[i] = Game.Instance.EntityCreator.SpawnUnit(unitBase[i], firstLoc + spawnOffset * i, Quaternion.identity, null);
                u[i].GroupId = "hag-active";
                u[i].LookAt(enterLoc);
                u[i].AddBuffNotDispelable(visuals[i]);
            }
#endif

        }

        public void CreateBrains() {

            var isActive = Helpers.CreateBlueprint<HagIsActive>("hag-is-active").ToReference<ConsiderationReference>();
            var isInactive = Helpers.CreateBlueprint<HagIsActive>("hag-is-inactive", consider => {
                consider.Not = true;
            }).ToReference<ConsiderationReference>();
            var after3Rounds = Helpers.CreateBlueprint<BuffConsideration>("hag-timer-elapsed", consider => {
                consider.m_Buffs = Helpers.Arr(TimerBuff.ToReference<BlueprintBuffReference>());
                consider.HasBuffScore = 0;
                consider.NoBuffScore = 1;
            }).ToReference<ConsiderationReference>();
            var notEngaged = Helpers.CreateBlueprint<IsEngagedConsideration>("hag-is-engaged", consider => {
                consider.EngagedScore = 0;
                consider.NotEngagedScore = 1;
            }).ToReference<ConsiderationReference>();

            var attack = BP.Ref<BlueprintAiActionReference>("866ffa6c34000cd4a86fb1671f86c7d8"); //aiattack action
            SummerUnit.m_Brain = Helpers.CreateBlueprint<BlueprintBrain>("hag-summer-brain", brain => {
                brain.m_Actions = new BlueprintAiActionReference[] {
                    attack,
                    AiCastSpellBuilder.New("hag/summer/molten-orb")
                        .WhenActor(notEngaged)
                        .Ability(MoltenOrb)
                        .Score(50)
                        .Build(),
                    AiCastSpellBuilder.New("hag/summer/very-hot")
                        .Ability(VeryHotAbility.ToReference<BlueprintAbilityReference>())
                        .Charges(1)
                        .Self()
                        .WhenActor(isInactive)
                        .Score(100)
                        .Build()
                };
            }).ToReference<BlueprintBrainReference>();

            AutumnUnit.m_Brain = Helpers.CreateBlueprint<BlueprintBrain>("hag-autumn-brain", brain => {
                brain.m_Actions = Helpers.Arr(
                    attack,
                    AiCastSpellBuilder.New("hag/autumn/acid-arrow")
                        .Ability(AcidArrow)
                        .WhenActor(notEngaged)
                        .Score(50)
                        .Build(),
                    AiCastSpellBuilder.New("hag/autumn/shakes")
                        .Ability(PlagueStormShakes)
                        .Charges(1)
                        .WhenActor(isInactive)
                        .WhenActor(after3Rounds)
                        .Score(100)
                        .Build()
                );
            }).ToReference<BlueprintBrainReference>();

            WinterUnit.m_Brain = Helpers.CreateBlueprint<BlueprintBrain>("hag-winter-brain", brain => {
                brain.m_Actions = Helpers.Arr(
                    attack,
                    BP.Ref<BlueprintAiActionReference>("a688e5d1957ec494bb27995724d91ea6"),
                    AiCastSpellBuilder.New("hag/winter/hoarfrost")
                        .Ability(HoarforstAbility)
                        .Charges(4)
                        .WhenActor(isInactive)
                        .WhenActor(after3Rounds)
                        .Score(100)
                        .Build()
                );

            }).ToReference<BlueprintBrainReference>();

            SpringUnit.m_Brain = Helpers.CreateBlueprint<BlueprintBrain>("hag-spring-brain", brain => {

                brain.m_Actions = Helpers.Arr(
                    attack,
                    AiCastSpellBuilder.New("hag/spring/chain-lightning")
                        .Ability(ChainLightning)
                        .Charges(2)
                        .WhenActor(isInactive)
                        .WhenTarget(BP.Ref<ConsiderationReference>("7a2b25dcc09cd244db261ce0a70cca84"))
                        .WhenTarget(BP.Ref<ConsiderationReference>("b2490b137b8b53a4e950c1d79d1c5c1d"))
                        .Score(100)
                        .Build(),
                    BP.Ref<BlueprintAiActionReference>("178f26ed454905a45bdd8a307c546e32"),
                    BP.Ref<BlueprintAiActionReference>("2bc37879abec43749a004ffdfa7dcec7")
                );

            }).ToReference<BlueprintBrainReference>();
        }

        public void Install() {
            PrefabLink link = new PrefabLink { AssetId = "ae7ac3acbb6b9574ab7f01bfaed77d27" };
            GameObject prefab = link.Load(false);
            beamFx = GameObject.Instantiate(prefab);
            beamFx.DestroyChildren("GroundFX/EnergyWave", "GroundFX/LockAxis_Single", "GroundFX/Glow");
            beamFx.ChildObject("GroundFX").GetComponent<SnapToLocator>().DontAttach = false;
            beamFx.transform.localScale *= 0.75f;

            var veryHotAura = Helpers.CreateBlueprint<BlueprintAbilityAreaEffect>("hag-very-hot-area", area => {
                area.AggroEnemies = false;
                area.AffectEnemies = true;
                area.m_TargetType = BlueprintAbilityAreaEffect.TargetType.Enemy;
                area.SpellResistance = false;
                area.Shape = AreaEffectShape.Cylinder;
                area.Size = 45.Feet();
                area.Fx = new PrefabLink { AssetId = "bbd6decdae32bce41ae8f06c6c5eb893" };
                area.AddComponent<AbilityAreaEffectBuff>(apply => {
                    apply.Condition = new();
                    apply.m_Buff = BP.Ref<BlueprintBuffReference>("e6f2fc5d73d88064583cb828801212f4");
                });
            });

            veryHotAura.Check("very hot");

            var VeryHotSource = Helpers.CreateBlueprint<BlueprintBuff>("hag-very-hot-source", buff => {
                buff.SetNameDescription("Heatwave", $"The {HagName(SummerIndex)} emits a wave of terrible heat that causes fatigue through intense sweats.");
                buff.AddComponent<AddAreaEffect>(area => {
                    area.m_AreaEffect = veryHotAura.ToReference<BlueprintAbilityAreaEffectReference>();
                });
            });

            VeryHotAbility = Helpers.CreateBlueprint<BlueprintAbility>("hag-very-hot-ability", ability => {
                ability.SetNameDescription("", "");
                ability.Range = AbilityRange.Personal;
                ability.CanTargetSelf = true;
                ability.EffectOnEnemy = AbilityEffectOnUnit.Harmful;
                ability.EffectOnAlly = AbilityEffectOnUnit.Helpful;
                ability.Animation = CastAnimationStyle.Self;
                ability.Type = AbilityType.Supernatural;
                ability.LocalizedDuration = Helpers.EmptyString;
                ability.LocalizedSavingThrow = Helpers.EmptyString;
                ability.MaterialComponent = new();
                ability.DisableLog = true;
                ability.AddComponent<AbilityEffectRunAction>(run => {
                    run.Actions = Helpers.CreateActionList(
                        new ContextActionApplyBuff {
                            m_Buff = VeryHotSource.ToReference<BlueprintBuffReference>(),
                            Permanent = true,
                            ToCaster = true,
                            IsNotDispelable = true,
                        });
                });
            });

            TimerBuff = Helpers.CreateBlueprint<BlueprintBuff>("hag-timer", buff => {
                buff.SetNameDescription("Future Decay", "After 3 rounds, hags that are currently invulnerable gain the use of a special ability.");
            });

            for (int i = 0; i < 4; i++) {
                barkOnActivate[i] = ScriptableObject.CreateInstance<SharedStringAsset>();
                barkOnActivate[i].String = Helpers.CreateString($"hag-bark-on-activate{i}.str", $"Now face {seasons[i]}'s wrath!");
            }

            List<List<BlueprintUnitFactReference>> factsForSeason = new() {
                new() {
                    BP.Ref<BlueprintUnitFactReference>(ChainLightning),
                    BP.Ref<BlueprintUnitFactReference>("d5a36a7ee8177be4f848b953d1c53c84"),
                    BP.Ref<BlueprintUnitFactReference>("2a9ef0e0b5822a24d88b16673a267456")
                },
                new() {
                    BP.Ref<BlueprintUnitFactReference>(MoltenOrb), //molten orb
                    VeryHotAbility.ToReference<BlueprintUnitFactReference>()
                },
                new() {
                    BP.Ref<BlueprintUnitFactReference>(AcidArrow), //acid dart
                    BP.Ref<BlueprintUnitFactReference>(PlagueStormShakes), //plaguestorm-shakes
                },
                new() {
                    BP.Ref<BlueprintUnitFactReference>(FrigidTouchCast),
                    BP.Ref<BlueprintUnitFactReference>(HoarforstAbility)
                },
            };


            var hagBase = BP.Unit("b889022a8eff1aa42bcc08f05c95c4dc");
            CreateHagUnitBlueprints(factsForSeason, hagBase);
            CreateBrains();

            _AreaMap = AreaMap.FromEnterPoint("9446cde521b6a2c49935b45350568564");

            CreateIntro();

            for (int i = 0; i < 4; i++) {
                unitBase[i].AddComponent<DialogOnClick>(click => {
                    click.m_Dialog = ChooseDialog.ToReference<BlueprintDialogReference>();
                    click.name = "start-dialog";
                    click.NoDialogActions = new();
                    click.Conditions = new();
                });
            }
        }

        private void CreateHagUnitBlueprints(List<List<BlueprintUnitFactReference>> factsForSeason, BlueprintUnit hagBase) {
            for (int i = 0; i < 4; i++) {
                var season = seasons[i];
                int hag = i;


                var uniqueHagBuff = Helpers.CreateBlueprint<BlueprintFeature>($"hag-buff-{i}", buff => {
                    buff.Ranks = 1;
                    buff.SetNameDescription("Passing Seasons", "The seasons pass with each hag's death until there are no more.");
                    buff.AddComponent<DeathActions>(death => {
                        death.Actions = Helpers.CreateActionList(new DynamicGameAction(() => {
                            int next = (hag + 1) % 4;
                            if (!u[next].State.IsDead)
                                SetActive(next);
                            else
                                SetActive(-1);
                        }));
                    });
                    buff.AddComponent<AutoMetamagic>(mm => {
                        mm.Metamagic = Kingmaker.UnitLogic.Abilities.Metamagic.Selective;
                    });
                });

                uniqueHagFact[i] = Helpers.CreateBlueprint<BlueprintUnitFact>($"hag-fact-{i}", fact => {
                    fact.SetNameDescription("", "");
                    fact.ComponentsArray = new BlueprintComponent[] {
                        new DynamicAddStatBonus() {
                            Calculate = floor => 15,
                            Stat = Kingmaker.EntitySystem.Stats.StatType.AC,
                            Descriptor = Kingmaker.Enums.ModifierDescriptor.NaturalArmor,
                            name = "add-nat-ac",
                        },
                        new AddFeatureOnApply() {
                            m_Feature = uniqueHagBuff.ToReference<BlueprintFeatureReference>(),
                        },
                    };
                });


                var buff = Helpers.CreateBlueprint<BlueprintBuff>($"hag-visual-{i}", buff => {
                    buff.FxOnStart = new() {
                        AssetId = i switch {
                            0 => "6035a889bae45f242908569a7bc25c93",
                            1 => "26fa35beb7d89bf4dafb93033941700c",
                            2 => "9adb9cfafedde524097d2e29eca637bf",
                            3 => "21b65d177b9db1d4ca4961de15645d95",
                            _ => null
                        }
                    };
                    buff.SetNameDescription($"Visions of {season}", $"This hag channels the power of {season}");
                });

                visuals[i] = buff;

                unitBase[i] = Helpers.CreateCopy(hagBase);
                var unit = unitBase[i];
                unit.GetComponent<AddClassLevels>().Levels = 5;
                for (int f = 0; f < unit.m_AddFacts.Length; f++) {
                    if (unit.m_AddFacts[f].deserializedGuid == "4179c5c08d606a6439a62bf178b738e1")
                        unit.m_AddFacts[f] = uniqueHagFact[i].ToReference<BlueprintUnitFactReference>();
                }
                Helpers.AppendInPlace(ref unit.m_AddFacts, factsForSeason[i].ToArray());
                unit.name = $"bubblehag-{i}.unit";
                unit.Strength = 18;
                unit.AssetGuid = ModSettings.Blueprints.GetGUID(unit.name);
                unit.m_Faction = BP.Ref<BlueprintFactionReference>("d8de50cc80eb4dc409a983991e0b77ad");
                unit.SetLocalisedName($"bubblehag-{i}.name", $"{titles[i]} of {season}");

                var pData = new PortraitData() {
                };

                var portrait = Helpers.CreateBlueprint<BlueprintPortrait>($"hag-{i}-portrait", p => {
                    p.Data = pData;
                });
                unit.m_Portrait = portrait.ToReference<BlueprintPortraitReference>();

                Main.Log($"portrait guid: {unit.PortraitSafe.AssetGuid}");

                var sprite = AssetLoader.LoadInternal("sprites", $"{season}Hag.png", new Vector2Int(185, 242), TextureFormat.ARGB32);

                SmallPortraitInjecotr.Replacements[pData] = sprite;
                HalfPortraitInjecotr.Replacements[pData] = sprite;
            }
        }

        private void CreateIntro() {
            var g = Helpers.CreateBlueprint<Gate>("hag-gate", g => { });
            var lockControls = Helpers.CreateBlueprint<CommandLockControls>("hag-lock", controlLock => {
                controlLock.EntryCondition = new();
            });

            var beginCombat = Helpers.CreateBlueprint<CommandBeginCombat>("hag-begin-combat", cmb => {
                cmb.EntryCondition = new();
            });
            string[] lines = {
                "Here Spring appears with flowery chaplets bound.",
                "Here Summer in her wheaten garland crown'd;",
                "Here Autumn the rich trodden grapes besmear.",
                "And hoary Winter shivers in the rear.",
            };

            cutscene = Helpers.CreateBlueprint<Cutscene>("hag-cut", scene => {
                scene.AwakeRange = 24.0f;
                scene.OnStopped = Helpers.CreateActionList();
                scene.DefaultParameters = new() { AdditionalParams = new(), Parameters = Array.Empty<ParameterEntry>() };
                scene.Components = Array.Empty<BlueprintComponent>();
                scene.WhenTrackIsSkipped = Gate.SkipTracksModeType.DoNotSignalGate;
                var track = new Track {
                    m_EndGate = g.ToReference<GateReference>()
                };
                var controlTrack = new Track {
                    m_EndGate = g.ToReference<GateReference>()
                };
                controlTrack.m_Commands.Add(lockControls.ToReference<CommandReference>());
                for (int i = 0; i < 4; i++) {
                    var bark = MakePoemLineBark(i, lines[i]);
                    track.m_Commands.Add(bark.ToReference<CommandReference>());
                }
                track.m_Commands.Add(beginCombat.ToReference<CommandReference>());
                scene.m_Tracks.Add(controlTrack);
                scene.m_Tracks.Add(track);
            });

            {
                var builder = new DialogBuilder("hag-dialog");

                var root = builder.Root("You believe you can better the seasons themselves?\nAnd which of us you think the weakest?");

                root.Answers()
                        .Add("spring", "Spring")
                            .AddAction(new DynamicGameAction(() => {
                                firstHag = 0;
                            }))
                            .AddAction(new PlayCutscene {
                                m_Cutscene = cutscene.ToReference<CutsceneReference>(),
                                Parameters = new()
                            })
                            .Commit()
                        .Add("summer", "Summer")
                            .AddAction(new DynamicGameAction(() => {
                                firstHag = 1;
                            }))
                            .AddAction(new PlayCutscene {
                                m_Cutscene = cutscene.ToReference<CutsceneReference>(),
                                Parameters = new()
                            })
                            .Commit()
                        .Add("autumn", "Autumn")
                            .AddAction(new DynamicGameAction(() => {
                                firstHag = 2;
                            }))
                            .AddAction(new PlayCutscene {
                                m_Cutscene = cutscene.ToReference<CutsceneReference>(),
                                Parameters = new()
                            })
                            .Commit()
                        .Add("winter", "Winter")
                            .AddAction(new DynamicGameAction(() => {
                                firstHag = 3;
                            }))
                            .AddAction(new PlayCutscene {
                                m_Cutscene = cutscene.ToReference<CutsceneReference>(),
                                Parameters = new()
                            })
                            .Commit()
                    .Commit();
                root.Commit();

                ChooseDialog = Helpers.CreateDialog("bubblegauntlet-hag-choose", dialog => {
                    dialog.FirstCue.Cues.Add(builder.Build());
                });
            }
        }

        public bool IsActive(UnitEntityData checkHag) {
            if (ActiveHag == -1)
                return true;

            return checkHag == u[ActiveHag];
        }
        public void SetActive(int hag) {
            if (activeFx != null)
                FxHelper.Destroy(activeFx, true);
            activeFx = null;

            ActiveHag = hag;

            if (hag == -1)
                return;

            var unit = u[hag];

            unit.Resurrect();

            unit.State.Features.Immortality.Release();
            unit.State.Features.IsUntargetable.Release();
            Game.Instance.UI.Bark(unit, barkOnActivate[hag], 5);


            activeFx = FxHelper.SpawnFxOnUnit(beamFx, unit.View, null, default);
        }
        private static CommandBark MakePoemLineBark(int hag, string text) {
            return Helpers.CreateBlueprint<CommandBark>($"bark://hag-poem/{hag}", b => {
                b.Unit = new HagEvaluator(hag);
                b.SharedText = ScriptableObject.CreateInstance<SharedStringAsset>();
                b.SharedText.String = Helpers.CreateString($"str://hag-poem/{hag}", text);
                b.BarkDurationByText = false;
                b.AwaitFinish = true;
                b.CommandDurationShift = 1.0f;
                b.ControlsUnit = true;
                b.EntryCondition = new();
            });
        }

        private static string HagName(int season) => $"{titles[season]} of {seasons[season]}";
    }

    public class HagEvaluator : UnitEvaluator {
        private readonly int whichHag;

        public HagEvaluator(int v) {
            this.whichHag = v;
        }

        public override string GetCaption() {
            return "hag";
        }

        public override UnitEntityData GetValueInternal() {
            return HagBoss.Instance.u[whichHag];
        }
    }
    public class HagIsActive : Consideration {
        public bool Not = false;

        public override float Score([NotNull] DecisionContext context) {
            UnitEntityData unitEntityData = context.Target.Unit ?? context.Unit;
            if (HagBoss.Instance.IsActive(unitEntityData) != Not)
                return 1;
            else
                return 0;
        }
    }
}
