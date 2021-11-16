using BubbleGauntlet.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BubbleGauntlet {
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
