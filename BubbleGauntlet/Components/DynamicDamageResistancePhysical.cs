using Kingmaker.Items;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic.FactLogic;

namespace BubbleGauntlet.Components {
    public class DynamicDamageResistancePhysical : AddDamageResistancePhysical {
        public override bool Bypassed(ComponentRuntime runtime, BaseDamage damage, ItemEntityWeapon weapon) {
            return false;
        }

        public DynamicGauntletValue GauntletValue;

        public override bool IsStackable => true;

        public override int CalculateValue(ComponentRuntime runtime) {
            return GauntletValue.Floor();  
        }

    }
}
