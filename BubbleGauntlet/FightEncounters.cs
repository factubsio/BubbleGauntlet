using BubbleGauntlet.Utilities;
using Kingmaker.Blueprints;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BubbleGauntlet {

    public struct CombatEliteTemplate {
        public BlueprintUnit Blueprint;
        public int MinFloor;
        public int MaxFloor;

        public CombatEliteTemplate(BlueprintUnit blueprint, int minFloor, int maxFloor) {
            Blueprint = blueprint;
            MinFloor = minFloor;
            MaxFloor = maxFloor;
        }

        public override bool Equals(object obj) {
            return obj is CombatEliteTemplate other &&
                   EqualityComparer<BlueprintUnit>.Default.Equals(Blueprint, other.Blueprint) &&
                   MinFloor == other.MinFloor &&
                   MaxFloor == other.MaxFloor;
        }

        public override int GetHashCode() {
            int hashCode = -201745204;
            hashCode = hashCode * -1521134295 + EqualityComparer<BlueprintUnit>.Default.GetHashCode(Blueprint);
            hashCode = hashCode * -1521134295 + MinFloor.GetHashCode();
            hashCode = hashCode * -1521134295 + MaxFloor.GetHashCode();
            return hashCode;
        }

        public void Deconstruct(out BlueprintUnit blueprint, out int minFloor, out int maxFloor) {
            blueprint = Blueprint;
            minFloor = MinFloor;
            maxFloor = MaxFloor;
        }

        public static implicit operator (BlueprintUnit Blueprint, int MinFloor, int MaxFloor)(CombatEliteTemplate value) {
            return (value.Blueprint, value.MinFloor, value.MaxFloor);
        }

        public static implicit operator CombatEliteTemplate((BlueprintUnit Blueprint, int MinFloor, int MaxFloor) value) {
            return new CombatEliteTemplate(value.Blueprint, value.MinFloor, value.MaxFloor);
        }
    }

    public class CombatEncounterTemplate {
        public readonly List<Weighted<MonsterPool>> Monsters;
        public UnityEngine.RangeInt LevelRangeInclusive;
        public string Name;

        public CombatEncounterTemplate(string name, int minFloor, int maxFloor, List<Weighted<MonsterPool>> monsters) {
            Main.Log($"Created template {name} with {monsters.Count} pools");
            LevelRangeInclusive = new UnityEngine.RangeInt(minFloor, maxFloor - minFloor + 1);
            Name = name;
            Monsters = new(monsters);
        }

        public bool IsAppropriate => LevelRangeInclusive.Contains(GauntletController.Floor.Level);

        public IEnumerable<string> GetMonsters(float budgetScale) {
            foreach (var pool in Monsters) {
                var deck = pool.name.monsters.Deck;
                float remaining = pool.count * budgetScale;
                Main.Log($"creating group with budget: {remaining} ({pool.count} * {budgetScale}");
                while (remaining > 0 && !deck.Empty) {
                    var maybe = deck.Next;
                    float cr = maybe.CR;
                    if (cr <= remaining) {
                        yield return maybe.Guid;
                        remaining -= cr;
                    }
                }
            }
        }
    }

    public class MonsterPoolBuilder : IBuilder<MonsterPool> {
        private List<Weighted<MonsterName>> names = new();
        public MonsterPoolBuilder Add(MonsterName name, int count = 1) {
            names.Add(new Weighted<MonsterName>(count, name));
            return this;
        }

        public MonsterPool Create() {
            var pool = new MonsterPool(names.ToArray());

            return pool;

        }
    }


    public class MonsterPool {
        public readonly DeckTemplate<MonsterRef> monsters;
        public readonly List<Weighted<MonsterName>> Raw;
        public readonly int levelMin;
        public readonly int levelMax;

        public MonsterPool(Weighted<MonsterName>[] names) {
            Raw = new(names);
            monsters = new(names
                .SelectMany(m => Enumerable.Repeat(MonsterDB.Get(m.name), m.count)));
        }

        public static MonsterPoolBuilder New() { return new(); }

    }

}
