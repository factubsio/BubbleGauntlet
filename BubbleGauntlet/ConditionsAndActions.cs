using Kingmaker;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BubbleGauntlet {

    public class InvertedCondition : Condition {
        private readonly Condition underlying;

        public InvertedCondition(Condition underlying) {
            this.underlying = underlying;
        }

        public override bool CheckCondition() {
            return !underlying.CheckCondition();
        }

        public override string GetConditionCaption() {
            return $"NOT: {underlying.GetConditionCaption()}";
        }
    }

    public class DynamicCondition : Condition {
        private Func<bool> predicate;

        public DynamicCondition(Func<bool> predicate) {
            this.predicate = predicate;
        }
        public override bool CheckCondition() {
            return predicate();
        }

        public override string GetConditionCaption() {
            return "predicated condition";
        }
    }

    public class FalseCondition : Condition {
        public override bool CheckCondition() {
            return false;
        }

        public override string GetConditionCaption() {
            return "<false>";
        }

        public static FalseCondition Instance = new();
    }

    public class DynamicGameAction : GameAction {
        private readonly Action action;

        public DynamicGameAction(Action action) {
            this.action = action;
        }

        public override string GetCaption() {
            return "dynamic-action";
        }

        public override void RunAction() {
            action();
        }
    }

    public class ResurrectCompanions : GameAction {
        readonly ContextActionResurrect action = new();
        public override string GetCaption() {
            return "res all";
        }

        public static bool IsCompanionDead(UnitEntityData unit) {
            return unit.State.IsDead || unit.State.HasCondition(UnitCondition.DeathDoor);

        }

        public override void RunAction() {
            var context = new MechanicsContext(GauntletController.Bubble, null, ContentManager.FakeBlueprint);
            bool charge = false;

            foreach (var companion in Game.Instance.SelectionCharacter.ActualGroup) {
                if (IsCompanionDead(companion)) {
                    using (context.GetDataScope(new TargetWrapper(companion))) {
                        action.RunAction();
                    }
                    charge = true;
                }
            }
            if (charge) {
                Game.Instance.Player.SpendMoney(HasEnoughMoneyForRes.Cost);
            }
        }
    }

    public class HasDeadCompanions : Condition {
        public override bool CheckCondition() {
            return Game.Instance.SelectionCharacter.ActualGroup.Any(ResurrectCompanions.IsCompanionDead);
        }

        public override string GetConditionCaption() {
            return "any dead companions";
        }
    }

    public class HasEnoughMoneyForRes : Condition {
        public const float ResFactor = 0.5f;

        public static int Cost => (int)(Game.Instance.Player.GetCustomCompanionCost() * ResFactor);

        public override bool CheckCondition() {
            return Game.Instance.Player.Money >= Cost;
        }

        public override string GetConditionCaption() {
            return "enough money for res";
        }
    }

    public class DescendAction : GameAction {
        public override string GetCaption() {
            return "descend";
        }

        public override void RunAction() {
            Main.Log("Descending");
            Game.Instance.DialogController.StartDialogWithoutTarget(ContentManager.DescendDialog, ContentManager.DescendSpeaker);
        }
    }


}
