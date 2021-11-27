using Kingmaker.Blueprints;
using Kingmaker.DialogSystem.Blueprints;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BubbleGauntlet {
    public class EncounterSlice {
        public BlueprintBookPage FirstPage;

        public BlueprintCueBaseReference EntryPoint => FirstPage.ToReference<BlueprintCueBaseReference>();
        public BlueprintAnswer ExitPoint;

        public void Reset() {

        }

        public enum Type {
            Unknown,
            Problem,
            Opportunity
        }

        public enum FlagComparison {
            Equal,
            Greater,
            GreaterEqual,
            Less,
            LessEqual,
            NotEqual
        }

        public Type EncounterType;

        public void SetNext(BlueprintCueBaseReference next) {
            ExitPoint.NextCue.Cues.Clear();
            ExitPoint.NextCue.Cues.Add(next);
        }

        private Dictionary<string, int> Flags = new();

        public GameAction AdjustFlag(string name, int delta) {
            return new DynamicGameAction(() => {
                if (Flags.TryGetValue(name, out var current)) {
                    Flags[name] = current + delta;
                } else {
                    Flags[name] = delta;
                }
            });
        }
        public Condition WhenFlag(string name, int reference, FlagComparison mode = FlagComparison.Equal) {
            return new DynamicCondition(() => {
                var value = Flags.Get(name, 0);
                return mode switch {
                    FlagComparison.Equal => value == reference,
                    FlagComparison.Greater => value > reference,
                    FlagComparison.GreaterEqual => value >= reference,
                    FlagComparison.Less => value < reference,
                    FlagComparison.LessEqual => value <= reference,
                    FlagComparison.NotEqual => value != reference,
                    _ => false,
                };
            });
        }


        private static EncounterSlice Build(string name, Type type, Action<EncounterSlice, DialogBuilder, PageBuilder> act) {
            EncounterSlice slice = new();
            DialogBuilder builder = new(name);
            var root = builder.RootPage("Encounter Title");

            var dummyAnswerList = new AnswerListBuilder<DummyAnswerHolder>("answers", builder, null);
            dummyAnswerList.Add("continue", "Continue on").Commit(out slice.ExitPoint);

            slice.EncounterType = type;
            root.BasicCue("title", $"<size=150%><line-indent=15%>{slice.EncounterType.ToString().MakeTitle(150)}</line-indent></size>");

            act(slice, builder, root);

            root.Commit();
            builder.Build();
            slice.FirstPage = root.Blueprint;
            return slice;
        }

        public static EncounterSlice MakeTest2() {
            return Build("slice-test2", Type.Problem, (slice, builder, root) => {

                root.BasicCue("intro-0", "You come across a long chamber covered in cobwebs.");
                root.BasicCue("intro-1", "From behind numerous pillars you hear the sound of small, skittering, feet. Tiny spiders blanket the floor!");
                root.BasicCue("intro-2", "On the opposite wall you see a door, while closer to where you are you also see a more solid, iron-banded door.");

                var strongDoorPick = slice.SimpleCheck("pick-door-strong", builder, "sneak_to_door", StatType.SkillThievery, 20, (check, success, fail) => {
                    check.AdjustDC(-4, slice.WhenFlag("sneak_to_door", 1));
                    success.BasicCue("pick-door-good", "You easily manipulate the pins and slip into the next corridor, the sounds of the spiderlings fading behind you.");
                    fail.BasicCue("pick-door-bad", "With little time to think you attempt to rake the lock, but security pins delay your efforts; you suffer many tiny bites before managing to escape.");
                    All(success, fail, x => x.Answers().AddExisting(slice.ExitPoint).Commit() );
                });

                var strongDoorBreak = slice.SimpleCheck("break-door-strong", builder, "sneak_to_door", StatType.Strength, 12, (check, success, fail) => {
                    check.AdjustDC(-3, slice.WhenFlag("sneak_to_door", 1));
                    success.BasicCue("shatter-door-good", "You shatter the door and escape, the sounds of the spiderlings fading behind you.");
                    fail.BasicCue("shatter-door-bad", "You break through the door, but not before suffering many tiny bites from the spiderling horde.");
                    All(success, fail, x => x.Answers().AddExisting(slice.ExitPoint).Commit() );
                });

                var strongDoorSneak = slice.SimpleCheck("sneak-door-strong", builder, "sneak_to_door", StatType.SkillStealth, 15, (success, fail) => {
                    success.BasicCue("sneak-strong-good", "You manage to avoid most of spiderlings, they are coming, but you've bought yourself time.");
                    success.BasicCue("sneak-strong-bad", "You approach the heavy door, it looks solid with a functioning lock.");

                    fail.BasicCue("sneak-strong-good", "The spiderlings immediately notice your laughable attempts at sneaking and are coming for you!");
                    fail.BasicCue("sneak-strong-bad", "You approach the heavy door, it looks solid with a functioning lock.");

                    All(success, fail, x => {
                        x.Answers()
                            .Add("try-break", "Try to break down the door").ContinueWith(strongDoorBreak).Commit()
                            .Add("try-pick", "Try to pick the lock").ContinueWith(strongDoorPick).Commit()
                            .Commit();
                    });
                });


                root.Answers()
                    .Add("try-sneak", "Try to sneak towards the closer door")
                        .ContinueWith(strongDoorSneak)
                        .Commit()
                    .Commit();
            });
        }

        private static void All(PageBuilder a, PageBuilder b, Action<PageBuilder> p) {
            p(a);
            p(b);
        }

        public static EncounterSlice MakeOpporunity_Grave() {
            return Build("slice-grave", Type.Opportunity, (slice, builder, root) => {

                root.BasicCue("intro", "You come across a warm grave pulsing with arcane energy");

                var detectTraps = slice.SimpleCheck("detect-traps", builder, root, "detected_icons", StatType.SkillPerception, 12,
                    "You don't find any traps, but you find some iconography that suggest the name of deity in which this grave was corrupted",
                    "Your search comes up empty");

                var senseMagic = slice.SimpleCheck("sense-magic", builder, root, "detected_energy", StatType.SkillKnowledgeArcana, 12,
                    "You identify the source of arcange energy as a specific group of Demodands that operate out of <insert-location>",
                    "You sense that the energy is demonic in origin but cannot pinpoint its source");


                var goodLoot = builder.NewPage("good-loot");
                goodLoot.BasicCue("loot-good", "Your rituals are a success! A magical item slowly emerges from the soil");
                //goodLoot.OnShow(GiveLoot);
                goodLoot.Answers().AddExisting(slice.ExitPoint).Commit();
                goodLoot.Commit();

                var badLoot = builder.NewPage("bad-loot");
                badLoot.BasicCue("lood-bad", "Your rituals do not succeed, but you sense a wave of gratidue for trying.");
                //goodLoot.OnShow(GiveBuff)
                badLoot.Answers().AddExisting(slice.ExitPoint).Commit();
                badLoot.Commit();

                var sanctify = builder.NewCheck("check-sanctify", StatType.SkillLoreReligion);
                sanctify
                    .OnSuccess(goodLoot)
                    .OnFail(badLoot)
                    .DC(45)
                    .AdjustDC(-10, slice.WhenFlag("detected_icons", 1))
                    .AdjustDC(-10, slice.WhenFlag("detected_energy", 1))
                    .Once()
                    .Commit();

                var openNoSanctify = builder.NewPage("open-no-sanctify");
                openNoSanctify.BasicCue("no-time", "You determine there is no time to sanctify the grave.");
                openNoSanctify.BasicCue("empty-casket", "You dig up a casket that is curiously empty...");
                openNoSanctify.Answers().AddExisting(slice.ExitPoint).Commit();
                openNoSanctify.Commit();

                root.Answers()
                    .Add("try-detect", "Try to detect the source of the energy")
                        .Once()
                        .ContinueWith(senseMagic)
                        .Commit()
                    .Add("try-check-traps", "Check the grave for traps")
                        .Once()
                        .ContinueWith(detectTraps)
                        .Commit()
                    .Add("try-sanctify", "Sanctify the grave")
                        .Once()
                        .ContinueWith(sanctify)
                        .Commit()
                    .Add("open-grave", "Open the grave")
                        .ContinueWith(openNoSanctify)
                        .Commit()
                    .Commit();
            });
        }

        private CheckBuilder SimpleCheck(string name, DialogBuilder builder, string flag, StatType skill, int dc, Action<CheckBuilder, PageBuilder, PageBuilder> act) {
            var goodOutcome = builder.NewPage($"{name}/good");
            var badOutcome = builder.NewPage($"{name}/bad");

            if (flag != null)
                goodOutcome.OnShow(AdjustFlag(flag, 1));

            var check = builder.NewCheck(name, skill);
            check
                .DC(dc)
                .OnSuccess(goodOutcome)
                .OnFail(badOutcome);

            act(check, goodOutcome, badOutcome);

            check.Commit();

            goodOutcome.Commit();
            badOutcome.Commit();

            return check;
        }


        private CheckBuilder SimpleCheck(string name, DialogBuilder builder, string flag, StatType skill, int dc, Action<PageBuilder, PageBuilder> act) {
            return SimpleCheck(name, builder, flag, skill, dc, (_, a, b) => act(a, b));
        }

        private CheckBuilder SimpleCheck(string name, DialogBuilder builder, PageBuilder continueWith, string flag, StatType skill, int dc, string good, string bad) {
            var goodOutcome = builder.NewPage($"{name}/good");
            var badOutcome = builder.NewPage($"{name}/bad");

            goodOutcome
                .BasicCue("check-good", good)
                .ContinueWith(continueWith)
                .OnShow(AdjustFlag(flag, 1))
                .Commit();
            badOutcome
                .BasicCue("check-bad", bad)
                .ContinueWith(continueWith)
                .Commit();

            var detectTraps = builder.NewCheck(name, skill);
            detectTraps
                .DC(dc)
                .Once()
                .OnSuccess(goodOutcome).OnFail(badOutcome)
                .Commit();

            return detectTraps;
        }
    }
}
