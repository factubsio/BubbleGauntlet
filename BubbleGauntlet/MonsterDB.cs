using BubbleGauntlet.Config;
using BubbleGauntlet.Extensions;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes.Experience;
using Kingmaker.UnitLogic.FactLogic;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace BubbleGauntlet {
    public struct MonsterRef {
        public int CR;
        public string Guid;
        public MonsterName Name;

        public MonsterRef(int cr, string guid, MonsterName name) {
            CR = cr;
            Guid = guid;
            Name = name;
        }
    }
    public static class TextLoader {
        public static void Process(string file, Action<string> perLine) {
            var localPath = Path.Combine(ModSettings.ModEntry.Path, "UserSettings", file);
            Stream stream;
            if (File.Exists(localPath)) {
                Main.Log($"Loading file '{file}' from local path");
                stream = File.OpenRead(localPath);
            } else {
                var assembly = Assembly.GetExecutingAssembly();
                stream = assembly.GetManifestResourceStream($"BubbleGauntlet.Config.{file}");
                if (stream == null) {
                    Main.Log(" ****  ERROR: COULD NOT LOAD STREAM");
                    return;
                }
            }

            StreamReader reader = new StreamReader(stream);
            string line;
            while ((line = reader.ReadLine()) != null) {
                line = line.Trim();
                if (line.Length == 0) continue;
                perLine(line);
            }

            stream.Dispose();

        }

    }

    public static class MonsterDB {

        private static Dictionary<MonsterName, MonsterRef> Monsters = new();
        public static BlueprintFactionReference MobFaction => BP.Ref<BlueprintFactionReference>("0f539babafb47fe4586b719d02aff7c4");


        public static void Initialize() {
            var mobRef = MobFaction;

            var pattern = new Regex(@"(.*?):(.*?):(.*)");
            TextLoader.Process("combat_tables.units_by_cr.txt", line => {
                var match = pattern.Match(line);

                int cr = int.Parse(match.Groups[1].Value);
                string guid = match.Groups[2].Value;
                string nameRaw = match.Groups[3].Value;

                MonsterName name = (MonsterName)Enum.Parse(typeof(MonsterName), nameRaw);

                var bp = BP.Get<BlueprintUnit>(guid);
                bp.m_Faction = mobRef;
                bp.Components = bp.Components.Where(c => !(c is Experience || c is AddLoot)).ToArray();

                Monsters[name] = new MonsterRef(cr, guid, name);
            });

            char[] seps = { ' ', '\t' };

            MonsterPoolBuilder current = null;
            string currentName = null;
            TextLoader.Process("combat_tables.monster_pools.txt", line => {
                if (!char.IsDigit(line[0])) {
                    if (current != null)
                        Pools[currentName] = current.Create();
                    currentName = line;
                    current = MonsterPool.New();
                } else {
                    var parts = line.Split(seps, StringSplitOptions.RemoveEmptyEntries);
                    if (Pools.TryGetValue(parts[1], out var pool)) {
                        foreach (var weight in pool.Raw) {
                            current.Add(weight.name, weight.count);
                        }
                    } else {
                        if (!Enum.TryParse<MonsterName>(parts[1], out var name)) {
                            Main.Log($"Could not find MonsterName for name: {parts[1]}, line: {line}");
                            return;
                        }
                        current.Add(name, int.Parse(parts[0]));

                    }
                }
            });
            if (current != null)
                Pools[currentName] = current.Create();

            currentName = null;
            List<Weighted<MonsterPool>> pools = new();
            int minLevel = -1, maxLevel = -1;
            TextLoader.Process("combat_tables.encounter_groups.txt", line => {
                var parts = line.Split(seps, StringSplitOptions.RemoveEmptyEntries);
                if (!char.IsDigit(line[0])) {
                    if (currentName != null) {
                        CombatTemplates[currentName] = new CombatEncounterTemplate(currentName, minLevel, maxLevel, pools);
                    }

                    pools.Clear();
                    currentName = parts[0];
                    minLevel = int.Parse(parts[1]);
                    maxLevel = int.Parse(parts[2]);
                } else {
                    if (!Pools.TryGetValue(parts[1], out var pool)) {
                        Main.Log($"Cannot find monster pool with name: {parts[1]}");
                        return;
                    } 
                    pools.Add(new Weighted<MonsterPool>(int.Parse(parts[0]), pool));
                }
            });
            if (currentName != null) {
                CombatTemplates[currentName] = new CombatEncounterTemplate(currentName, minLevel, maxLevel, pools);
            }

        }

        public static string Find(MonsterName name) {
            return Monsters[name].Guid;
        }

        public static MonsterRef Get(MonsterName name) {
            return Monsters[name];
        }

        public static Dictionary<string, MonsterPool> Pools = new();
        public static Dictionary<string, CombatEncounterTemplate> CombatTemplates = new();

    }
}
