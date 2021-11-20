using BubbleGauntlet.Utilities;
using Kingmaker;
using Kingmaker.Blueprints.Items;
using Kingmaker.Controllers.Rest;
using Kingmaker.ElementsSystem;
using Kingmaker.UI.MVVM._VM.Tooltip.Bricks;
using Kingmaker.UI.MVVM._VM.Tooltip.Templates;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.Utility;
using Kingmaker.View;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace BubbleGauntlet {
    public enum EncounterType {
        Fight,
        EliteFight,
        Rest,
        Shop,
    }

    public class EncounterTemplate {

        public readonly EncounterType Type;

        public readonly string Name;
        public readonly string Tag;

        public readonly EncounterGameAction Action;
        public readonly Condition Available;

        public int Count { get; private set; }

        public EncounterTemplate(EncounterType type, string name, string tag) {
            Type = type;
            Name = name;
            Tag = tag;

            switch (type) {
                case EncounterType.Fight:
                    Count = 8;
                    Action = new FightEncounter(this);
                    break;
                case EncounterType.EliteFight:
                    Count = 0;
                    Action = new FightEncounter(this);
                    break;
                case EncounterType.Rest:
                    Count = 4;
                    Action = new RestEncounter(this);
                    break;
                case EncounterType.Shop:
                    Count = 2;
                    Action = new ShopEncounter(this);
                    break;
            }
            Available = new DynamicCondition(() => GauntletController.Floor.Events[Type].Remaining > 0);
        }

        public override bool Equals(object obj) {
            return obj is EncounterTemplate type &&
                   Type == type.Type;
        }

        public override int GetHashCode() {
            return Type.GetHashCode();
        }

        public static readonly EncounterTemplate Fight = new(EncounterType.Fight, "fight", "bubble_fight_remaining");
        public static readonly EncounterTemplate EliteFight = new(EncounterType.EliteFight, "fight an elite", "bubble_elite_remaining");
        public static readonly EncounterTemplate Rest = new(EncounterType.Rest, "take a nap", "bubble_rest_remaining");
        public static readonly EncounterTemplate Shop = new(EncounterType.Shop, "visit a magical vendor", "bubble_shop_remaining");

        public static EncounterTemplate Get(EncounterType type) {
            return type switch {
                EncounterType.Fight => Fight,
                EncounterType.EliteFight => EliteFight,
                EncounterType.Rest => Rest,
                EncounterType.Shop => Shop,
                _ => throw new Exception(),
            };

        }

        public static IEnumerable<EncounterTemplate> All {
            get {
                yield return Fight;
                yield return EliteFight;
                yield return Rest;
                yield return Shop;
            }
        }
    }
    public abstract class EncounterGameAction : GameAction {
        protected FloorState State => GauntletController.Floor;
        protected EncounterTemplate Event;
        protected virtual bool Instant => false;

        public EncounterGameAction(EncounterTemplate eventType) {
            Event = eventType;
        }

        public override string GetCaption() {
            return "encounter";
        }

        public override void RunAction() {
            try {
                if (Execute()) {
                    GauntletController.SetNextEncounter(Event.Type);
                    if (Instant)
                        GauntletController.CompleteEncounter();
                }
            } catch (Exception ex) {
                Main.Error(ex, "running encounter action");
            }
        }

        public abstract bool Execute();
    }

    public class RestEncounter : EncounterGameAction {
        public RestEncounter(EncounterTemplate type) : base(type) {
        }

        protected override bool Instant => true;

        public override bool Execute() {
            foreach (var unit in Game.Instance.SelectionCharacter.ActualGroup) {
                var status = new RestStatus();
                RestController.HealAndApplyRest(unit, status);
            }
            return true;
        }
    }

    public static class D {
        public static int RollRecursive(int initial, int depth) {
            int val = Roll(initial);
            for (int i = 0; i < depth; i++) {
                val = Roll(val);
            }
            return val;
        }
        public static bool Bool(int chance) => Roll(100) <= chance;

        public static int Roll(int amount, int count = 1) {
            int sum = 0;
            for (int i = 0; i < count; i++) {
                sum += UnityEngine.Random.Range(1, amount + 1);
            }
            return sum;
        }

        public static int Range(int from, int to) {
            return UnityEngine.Random.Range(from, to + 1);
        }
    }

    public class ShopEncounter : EncounterGameAction {
        public ShopEncounter(EncounterTemplate type) : base(type) {
        }

        public override bool Execute() {
            GauntletController.Floor.Shopping = true;

            var vendorBp = ContentManager.Vendors.Random();

            var vendor = Game.Instance.EntityCreator.SpawnUnit(vendorBp, GauntletController.CurrentMap.VendorSpawnLocation, Quaternion.identity, null);

            RollTable<Weighted<(int biasDir, int biasAmount, string type)>> typeTable = new();
            foreach (var t in ContentManager.ItemTables.Keys) {
                typeTable.Add((D.Roll(6), new VendorPreference(D.Range(-1, 1), D.Range(0, 2), t)));
            }
            int budget = D.Range(500_000, 500_000 * 4);

            Main.Log($"budget: {budget}");
            Main.Log(typeTable.Render);

            var part = vendor.Ensure<UnitPartVendor>();
            vendor.MarkNotOptimizableInSave();
            while (budget > 0) {
                BlueprintItem item;
                var (biasDir, biasAmount, name) = typeTable.Next.name;
                var input = ContentManager.ItemTables[name];
                int raw = D.RollRecursive(input.Count - 1, biasAmount);
                item = biasDir switch {
                    -1 => input[input.Count - (raw + 1)],
                    0 => input.Random(),
                    1 => input[raw],
                    _ => null,
                };
                part.Inventory.Add(item, 1, null);
                budget -= item.Cost;
            }

            GauntletController.Vendor = vendor;
            Main.Log("Created vendor...");


            return true;
        }
    }

    public class FightEncounter : EncounterGameAction {
        public FightEncounter(EncounterTemplate type) : base(type) { }

        public override string GetCaption() {
            return "generate an encounter";
        }

        public override bool Execute() {
            Main.Log("executing?");
            var monsters = (Event == EncounterTemplate.EliteFight) ? GauntletController.GetEliteFight() : GauntletController.GetNormalFight();
            if (monsters == null) {
                Main.Log("could not find a monster group...");
                return false;
            }

            GauntletController.HideBubble();
            GauntletController.InstantiateCombatTemplate(monsters);
            return true;
        }
    }

    public class TooltipTemplateEncounterGen : TooltipBaseTemplate {
        public List<string> Monsters;
        public float Scale;
        public override IEnumerable<ITooltipBrick> GetBody(TooltipTemplateType type) {
            yield return new TooltipBrickText($"Budget scale [0.6-1.5]: {Scale:0.0}");
            yield return new TooltipBrickText("Monsters", TooltipTextType.Bold);
            foreach (var monster in Monsters)
                yield return new TooltipBrickText(monster);
        }

        public override IEnumerable<ITooltipBrick> GetHeader(TooltipTemplateType type) {
            yield return new TooltipBrickText("Encounter generation details", TooltipTextType.BoldCentered);
        }
    }

    internal struct VendorPreference {
        public int BiasDirection;
        public int BiasStrength;
        public string ItemType;

        public VendorPreference(int item1, int item2, string t) {
            BiasDirection = item1;
            BiasStrength = item2;
            this.ItemType = t;
        }

        public override bool Equals(object obj) {
            return obj is VendorPreference other &&
                   BiasDirection == other.BiasDirection &&
                   BiasStrength == other.BiasStrength &&
                   ItemType == other.ItemType;
        }

        public override int GetHashCode() {
            int hashCode = 1697558596;
            hashCode = hashCode * -1521134295 + BiasDirection.GetHashCode();
            hashCode = hashCode * -1521134295 + BiasStrength.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(ItemType);
            return hashCode;
        }

        public void Deconstruct(out int item1, out int item2, out string t) {
            item1 = BiasDirection;
            item2 = BiasStrength;
            t = this.ItemType;
        }

        public static implicit operator (int, int, string t)(VendorPreference value) {
            return (value.BiasDirection, value.BiasStrength, value.ItemType);
        }

        public static implicit operator VendorPreference((int, int, string t) value) {
            return new VendorPreference(value.Item1, value.Item2, value.t);
        }

        public override string ToString() => $"{ItemType}, bias:{BiasDirection}, bias_strength:{BiasStrength}";
    }
}
