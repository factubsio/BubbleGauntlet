using BubbleGauntlet.Config;
using BubbleGauntlet.Extensions;
using BubbleGauntlet.Utilities;
using Kingmaker;
using Kingmaker.AreaLogic.Cutscenes;
using Kingmaker.AreaLogic.Cutscenes.Commands;
using Kingmaker.Blueprints;
using Kingmaker.Controllers;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.DialogSystem.Blueprints;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Localization;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Interaction;
using Owlcat.Runtime.Visual.Effects.WeatherSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static Kingmaker.Designers.EventConditionActionSystem.NamedParameters.ParametrizedContextSetter;

namespace BubbleGauntlet.Bosses {
    public class HagEvaluator : UnitEvaluator {
        private int whichHag;

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
    public class CommandBeginCombat : CommandBase {
        public override bool IsFinished(CutscenePlayerData player) {
            return true;
        }

        public override void OnRun(CutscenePlayerData player, bool skipping) {
            for (int i = 0; i < 4; i++) {
                HagBoss.Instance.u[i].SwitchFactions(MonsterDB.MobFaction);
            }
        }

        public override void SetTime(double time, CutscenePlayerData player) {
        }
    }
    public class HagBoss : IMinorBoss {
        public static HagBoss Instance => GauntletController.Boss as HagBoss;

        private BlueprintUnit []unitBase = new BlueprintUnit[4];
        BlueprintBuff[] visuals = new BlueprintBuff[4];
        private AreaMap _AreaMap;
        private Vector3 firstLoc = new(26.0258f, 60.0016f, 19.6158f);
        private Vector3 enterLoc = new(32.214f, 60.0012f, 31.717f);

        private static string[] seasons = {
            "Spring", "Summer", "Autumn", "Winter",
        };

        private BlueprintDialog ChooseDialog;

        public void Install() {
            var hagBase = BP.Unit("b889022a8eff1aa42bcc08f05c95c4dc");
            for (int i = 0; i < 4; i++) {
                var season = seasons[i];

                var buff = Helpers.CreateBlueprint<BlueprintBuff>($"hag-visual-{i}", buff => {
                    buff.FxOnStart = new() {
                        AssetId = i switch {
                            0 => "6035a889bae45f242908569a7bc25c93",
                            1 => "f5eaec10b715dbb46a78890db41fa6a0",
                            2 => "d7c1a0c281a1a784cbab128b0c7a9e7b",
                            3 => "6997425ac95b2a347a261dbd4139c9a9",
                            _ => null
                        }
                    };
                    buff.SetNameDescription($"Spite of {seasons}", $"This hag channels the power of {season}");
                });

                visuals[i] = buff;

                unitBase[i] = Helpers.CreateCopy(hagBase);
                unitBase[i].name = $"bubblehag-{i}.unit";
                unitBase[i].AssetGuid = ModSettings.Blueprints.GetGUID(unitBase[i].name);
                unitBase[i].m_Faction = BP.Ref<BlueprintFactionReference>("d8de50cc80eb4dc409a983991e0b77ad");
                unitBase[i].SetLocalisedName($"bubblehag-{i}.name", $"Mother of {season}");

                Game.Instance.AddUnitToPersistentState(unitBase[i]);
            }
            _AreaMap = AreaMap.FromEnterPoint("9446cde521b6a2c49935b45350568564");

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
                    var bark = Bark(i, lines[i]);
                    track.m_Commands.Add(bark.ToReference<CommandReference>());
                }
                track.m_Commands.Add(beginCombat.ToReference<CommandReference>());
                scene.m_Tracks.Add(controlTrack);
                scene.m_Tracks.Add(track);
            });

            {
                var builder = new DialogBuilder();

                var root = builder.Root("You believe you can better the seasons themselves?\nAnd which of us you think the weakest?");

                root.Answers()
                        .Add("Spring")
                            .AddAction(new PlayCutscene {
                                m_Cutscene = cutscene.ToReference<CutsceneReference>(),
                                Parameters = new()
                            })
                            .Commit()
                        .Add("Summer").Commit()
                        .Add("Autumn").Commit()
                        .Add("Winter").Commit()
                    .Commit();
                root.Commit();

                ChooseDialog = Helpers.CreateDialog("bubblegauntlet-hag-choose", dialog => {
                    dialog.FirstCue.Cues.Add(builder.Build());
                });
            }
            for (int i = 0; i < 4; i++) {
                unitBase[i].AddComponent<DialogOnClick>(click => {
                    click.m_Dialog = ChooseDialog.ToReference<BlueprintDialogReference>();
                    click.name = "start-dialog";
                    click.NoDialogActions = new();
                    click.Conditions = new();
                });
            }
        }

        private static CommandBark Bark(int hag, string text) {
            int bpId = BubbleID.Get;
            int strId = BubbleID.Get;
            return Helpers.CreateBlueprint<CommandBark>($"hag-bark{bpId}", b => {
                b.Unit = new HagEvaluator(hag);
                b.SharedText = ScriptableObject.CreateInstance<SharedStringAsset>();
                b.SharedText.String = Helpers.CreateString($"hag-bark{strId}.str", text);
                b.BarkDurationByText = false;
                b.AwaitFinish = true;
                b.CommandDurationShift = 1.0f;
                b.ControlsUnit = true;
                b.EntryCondition = new();
            });
        }

        public UnitEntityData[] u = new UnitEntityData[4];
        private Cutscene cutscene;

        private UnitEntityData Spring => u[0];
        private UnitEntityData Summer => u[1];
        private UnitEntityData Autumn => u[2];
        private UnitEntityData Winter => u[3];

        public void Begin() {
            var offset = new Vector3(-2f, 0, 2f);

            for (int i =0; i < 4; i++) {
                u[i] = Game.Instance.EntityCreator.SpawnUnit(unitBase[i], firstLoc + offset * i, Quaternion.identity, null);
                u[i].GroupId = "hag-active";
                u[i].LookAt(enterLoc);
                u[i].AddBuffNotDispelable(visuals[i]);
            }

        }

        public AreaMap Map => _AreaMap;


        public string Name => "SeasonalHags";
    }
}
