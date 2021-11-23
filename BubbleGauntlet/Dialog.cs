using BubbleGauntlet.Utilities;
using Kingmaker.Blueprints;
using Kingmaker.DialogSystem.Blueprints;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Stats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace BubbleGauntlet {


    public class BubbleID {
        private static int next = 1;
        public static int Get => next++;
    }



    public interface IAnswerHolder : IMustComplete {
        BlueprintAnswerBaseReference AnswerList { get; set; }
    }

    public class DummyAnswerHolder : IAnswerHolder {
        public BlueprintAnswerBaseReference AnswerList { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public bool Completed => true;
        public string Description => "dummy";
    }

    public class AnswerListBuilder<T> : Builder<BlueprintAnswersList, BlueprintAnswerBaseReference> where T : IAnswerHolder {

        public List<BlueprintAnswer> answerList = new();
        T parent;

        public AnswerListBuilder(DialogBuilder root, T parent) : base(root) {
            this.parent = parent;
            Description = $"answers_for_{parent?.Description}";
        }

        public AnswerBuilder<T> Add(string answerText) {
            return new AnswerBuilder<T>(root, BubbleID.Get, answerText, this);
        }

        public AnswerListBuilder<T> AddExisting(BlueprintAnswer answer) {
            answerList.Add(answer);
            return this;
        }

        public T Commit() {
            Blueprint = Helpers.CreateBlueprint<BlueprintAnswersList>($"bubble-anwerlist-{id}", answers => {
                answers.Conditions = new();
                answers.Answers = answerList.Select(a => a.ToReference<BlueprintAnswerBaseReference>()).ToList();
            });
            Complete();
            parent.AnswerList = Reference;
            return parent;
        }

    }

    public class AnswerBuilder<T> where T : IAnswerHolder {
        List<GameAction> actions = new();
        private DialogBuilder root;
        int key;
        string text;
        private AnswerListBuilder<T> parent;
        private List<IReferenceBuilder<BlueprintCueBaseReference>> next = new();
        private Condition[] conditions;
        private Condition[] selectCondition;

        public AnswerBuilder<T> AddAction(GameAction action) {
            actions.Add(action);
            return this;
        }


        public AnswerBuilder<T> ContinueWith(params IReferenceBuilder<BlueprintCueBaseReference>[] cues) {
            next.AddRange(cues);
            return this;
        }

        //public AnswerBuilder<T> AddSkillCheck() {
        //    return this;
        //}


        public AnswerBuilder(DialogBuilder root, int key, string text, AnswerListBuilder<T> parent) {
            this.root = root;
            this.key = key;
            this.text = text;
            this.parent = parent;
        }

        private BlueprintAnswer Create() {
            BlueprintAnswer item = Helpers.CreateBlueprint<BlueprintAnswer>($"bubble-answer-{key}", answer => {
                answer.Text = Helpers.CreateString($"bubble-answer-{key}.text", text);
                answer.NextCue = new();
                answer.NextCue.Cues = new();
                answer.OnSelect = new();
                answer.OnSelect.Actions = actions.ToArray();
                answer.ShowOnceCurrentDialog = once;
                answer.ShowOnce = once;
                answer.AlignmentShift = new();
                answer.ShowConditions = new();
                if (conditions != null)
                    answer.ShowConditions.Conditions = conditions;
                answer.SelectConditions = new();
                if (selectCondition != null) {
                    answer.SelectConditions.Conditions = selectCondition;
                }
                answer.ShowCheck = new();
                answer.CharacterSelection = new();
                if (selectCharacterOn != StatType.Unknown) {
                    answer.CharacterSelection.SelectionType = Kingmaker.DialogSystem.CharacterSelection.Type.Manual;
                    answer.CharacterSelection.ComparisonStats = new StatType[] { selectCharacterOn };
                } else
                    answer.CharacterSelection.SelectionType = Kingmaker.DialogSystem.CharacterSelection.Type.Player;
                answer.SkillChecksDC.Add(new Kingmaker.Controllers.Dialog.SkillCheckDC {

                });
            });
            if (next.Count > 0) {
                root.Fixups.Add(() => {
                    item.NextCue.Cues.AddRange(next.Select(n => n.Reference));
                });
            }
            parent.answerList.Add(item);
            return item;

        }

        public AnswerListBuilder<T> Commit(out BlueprintAnswer answer) {
            answer = Create();
            return parent;
        }

        public AnswerListBuilder<T> Commit() {
            Create();
            return parent;
        }

        public AnswerBuilder<T> When(Func<bool> predicate) {
            return When(new DynamicCondition(predicate));
        }
        public AnswerBuilder<T> When(params Condition[] conditions) {
            this.conditions = conditions;
            return this;
        }

        internal AnswerBuilder<T> Enabled(Func<bool> predicate) {
            return Enabled(new DynamicCondition(predicate));
        }
        internal AnswerBuilder<T> Enabled(params Condition[] selectCondition) {
            this.selectCondition = selectCondition;
            return this;
        }
        internal AnswerBuilder<T> Disabled() {
            return Enabled(FalseCondition.Instance);
        }

        internal AnswerBuilder<T> CharacterSelect(StatType statType) {
            selectCharacterOn = statType;
            return this;
        }

        StatType selectCharacterOn = StatType.Unknown;
        private bool once;

        internal AnswerBuilder<T> Once() {
            once = true;
            return this;
        }

    }

    public interface IReferenceBuilder<T> where T : BlueprintReferenceBase {
        T Reference { get; }
    }

    public static class References {
        public static IReferenceBuilder<T> Static<T>(T reference) where T : BlueprintReferenceBase {
            return new StaticReference<T>(reference);
        }

    }

    public class StaticReference<T> : IReferenceBuilder<T> where T : BlueprintReferenceBase {
        public T Reference => _Reference;

        private readonly T _Reference;

        public StaticReference(T reference) {
            _Reference = reference;
        }
    }

    public abstract class Builder<T, TRef> : IReferenceBuilder<TRef>, IMustComplete where T : SimpleBlueprint where TRef : BlueprintReferenceBase, new() {
        public T Blueprint;
        protected readonly int id;
        protected readonly DialogBuilder root;
        public String Description { get; protected set; }

        public TRef Reference => Blueprint.ToReference<TRef>();

        private bool _Complete;
        public string debug = "?";

        public bool Completed => _Complete;
        protected void Complete() {

            if (_Complete) {
                var ex = new Exception($"cannot complete twice ({debug})");
                Main.Log(StackTraceUtility.ExtractStackTrace());
                Main.Error(ex, "completing");
            } else {
                //Main.Log($"FIRST COMPLETION: {debug}");
                //Main.Log(StackTraceUtility.ExtractStackTrace());
            }
            _Complete = true;

        }

        protected Builder(DialogBuilder root) {
            this.root = root;
            id = BubbleID.Get;
            Description = $"{id}";
        }
    }

    public interface IMustComplete {
        public bool Completed { get; }
        public string Description { get; }

    }

    public class DialogBuilder {
        public List<Action> Fixups = new();
        public List<IMustComplete> Completable = new();
        public CueBuilder root { private set; get; }
        public PageBuilder rootPage { private set; get; }

        public CheckBuilder NewCheck(StatType type) {
            return new CheckBuilder(this, type);
        }

        public CueBuilder Root(string text) {
            root = Cue(text);
            return root;
        }

        public CueBuilder Cue(string text) {
            var builder = new CueBuilder(this, text);
            Completable.Add(builder);
            return builder;
        }

        public BlueprintCueBaseReference Build() {
            foreach (var c in Completable.Where(c => !c.Completed)) {
                Main.Log($"*** ERROR: something is incomplete: {c}:{c.Description}");
            }

            foreach (var fixup in Fixups)
                fixup();
            if (root != null)
                return root.Reference;
            else
                return rootPage.Reference;

        }

        internal PageBuilder RootPage(string v) {
            var page = NewPage(v);
            rootPage = page;
            return page;
        }

        internal PageBuilder NewPage(string title) {
            var page = new PageBuilder(this, title);
            Completable.Add(page);
            return page;

        }
    }

    public class CheckBuilder : Builder<BlueprintCheck, BlueprintCueBaseReference> {
        public CheckBuilder(DialogBuilder root, StatType type) : base(root) {
            this.type = type;
            Description = $"check-{type}";
        }

        private StatType type;
        private IReferenceBuilder<BlueprintCueBaseReference> fail, success;
        private Condition[] conditions;
        private List<DCModifier> modifiers = new();
        private bool once;
        private int dc = 10;

        public void Commit() {
            Blueprint = Helpers.CreateBlueprint<BlueprintCheck>($"bubble-check-{id}", check => {
                check.Conditions = new();
                if (conditions != null)
                    check.Conditions.Conditions = conditions;

                check.ShowOnce = once;
                check.ShowOnceCurrentDialog = once;
                check.DC = dc;
                check.DCModifiers = modifiers.ToArray();
                check.Type = type;
            });
            root.Fixups.Add(() => {
                Blueprint.m_Fail = fail.Reference;
                Blueprint.m_Success = success.Reference;
            });
            Complete();
        }

        public CheckBuilder OnComplete(IReferenceBuilder<BlueprintCueBaseReference> next) {
            OnSuccess(next);
            OnFail(next);
            return this;
        }

        public CheckBuilder DC(int dc) {
            this.dc = dc;
            return this;
        }

        public CheckBuilder OnSuccess(IReferenceBuilder<BlueprintCueBaseReference> success) {
            this.success = success;
            return this;
        }
        public CheckBuilder OnFail(IReferenceBuilder<BlueprintCueBaseReference> fail) {
            this.fail = fail;
            return this;
        }

        internal CheckBuilder When(params Condition[] conditions) {
            this.conditions = conditions;
            return this;
        }

        internal CheckBuilder Once() {
            this.once = true;
            return this;
        }

        internal CheckBuilder AdjustDC(int delta, params Condition[] conditions) {
            modifiers.Add(new() {
                Conditions = new() { Conditions = conditions },
                Mod = delta
            });
            return this;
        }
    }

    public class PageBuilder : Builder<BlueprintBookPage, BlueprintCueBaseReference>, IAnswerHolder {
        public BlueprintAnswerBaseReference AnswerList { get; set; }
        private string text;
        private List<BlueprintCueBaseReference> cues = new();
        private Condition[] conditions;
        private ActionList OnShowActions = new();

        public PageBuilder(DialogBuilder root, string title) : base(root) {
            this.text = title;
            Description = title;
        }

        public AnswerListBuilder<PageBuilder> Answers() {
            var builder = new AnswerListBuilder<PageBuilder>(root, this);
            root.Completable.Add(builder);
            return builder;
        }

        public void Commit() {
            Blueprint = Helpers.CreateBlueprint<BlueprintBookPage>($"bubble-cue-{id}", page => {
                page.Answers = new();
                if (AnswerList != null) {
                    page.Answers.Add(AnswerList);
                }
                page.Title = Helpers.CreateString($"bubble-cue-{id}.text", text);
                page.OnShow = OnShowActions;
                page.Conditions = new();
                if (this.conditions != null)
                    page.Conditions.Conditions = conditions;
                page.Cues = cues;
                //page.ImageLink = new();
                //page.ImageLink.AssetId = "Resource:8bc34ca461d25bc45abe1273b4964702:ui";
            });
            Complete();
        }

        public CueBuilder Cue(string text) {
            var cue = root.Cue(text);
            root.Fixups.Add(() => {
                cues.Add(cue.Reference);
            });
            return cue;
        }

        public PageBuilder BasicCue(string text) {
            Cue(text).Commit();
            return this;
        }

        internal PageBuilder When(params Condition []conditions) {
            this.conditions = conditions;
            return this;
        }

        internal PageBuilder OnShow(params GameAction []actions) {
            this.OnShowActions.Actions = actions;
            return this;
        }

        internal PageBuilder ContinueWith(IReferenceBuilder<BlueprintCueBaseReference> next) {
            Answers()
                .Add("continue")
                    .ContinueWith(next)
                    .Commit()
                .Commit();
            return this;
        }

        internal object SimpleCheck(string v1, StatType skillPerception, int v2, string v3, string v4) {
            throw new NotImplementedException();
        }
    }

    public class CueBuilder : Builder<BlueprintCue, BlueprintCueBaseReference>, IAnswerHolder {
        public BlueprintAnswerBaseReference AnswerList { get; set; }
        private string text;
        private GameAction[] stopActions;
        private ConditionsChecker conditions = new();
        private BlueprintUnitReference speaker;
        private bool focusSpeaker;

        public CueBuilder(DialogBuilder root, string text) : base(root) {
            this.text = text;
            Description = text;
        }

        public AnswerListBuilder<CueBuilder> Answers() {
            var builder = new AnswerListBuilder<CueBuilder>(root, this);
            root.Completable.Add(builder);
            return builder;
        }

        public void Commit() {
            Blueprint = Helpers.CreateBlueprint<BlueprintCue>($"bubble-cue-{id}", cue => {
                cue.Answers = new();
                if (AnswerList != null) {
                    cue.Answers.Add(AnswerList);
                }
                cue.Continue = new();
                cue.Continue.Cues = new();
                cue.Text = Helpers.CreateString($"bubble-cue-{id}.text", text);
                cue.Speaker = new();
                if (speaker == null)
                    cue.Speaker.NoSpeaker = true;
                else {
                    cue.Speaker.m_Blueprint = speaker;
                    cue.Speaker.MoveCamera = this.focusSpeaker;
                }
                cue.OnShow = new();
                cue.OnStop = new();
                if (stopActions != null)
                    cue.OnStop.Actions = stopActions;
                cue.AlignmentShift = new();
                cue.Conditions = conditions;
            });
            Complete();
        }

        internal CueBuilder Speaker(BlueprintUnitReference speaker, bool focus = false) {
            this.speaker = speaker;
            this.focusSpeaker = false;
            return this;
        }

        internal CueBuilder ContinueWith(CheckBuilder with) {
            root.Fixups.Add(() => {
                this.Blueprint.Continue.Cues.Add(with.Reference);
            });
            return this;
        }

        internal CueBuilder ContinueWith(IReferenceBuilder<BlueprintCueBaseReference> with) {
            root.Fixups.Add(() => {
                this.Blueprint.Continue.Cues.Add(with.Reference);
            });
            return this;
        }

        internal CueBuilder OnStop(params GameAction[] actions) {
            stopActions = actions;
            return this;
        }

        internal CueBuilder When(params Condition[] conditions) {
            this.conditions.Conditions = conditions;
            return this;
        }
    }


}
