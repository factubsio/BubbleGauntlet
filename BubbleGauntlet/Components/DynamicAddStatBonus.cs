using Kingmaker.UnitLogic.FactLogic;
using System;

namespace BubbleGauntlet.Components {
    public class DynamicAddStatBonus : AddStatBonus {
        public DynamicGauntletValue Calculate;

        public DynamicAddStatBonus() { }

        public override void OnTurnOn() {
            Value = Calculate.Floor();
            base.OnTurnOn();
        }
    }
}
