﻿using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Utility;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace BubbleGauntlet.Config {
    public class Blueprints : IUpdatableSettings {
        [JsonProperty]
        private bool OverrideIds = false;
        [JsonProperty]
        public readonly SortedDictionary<string, Guid> NewBlueprints = new SortedDictionary<string, Guid>();
        [JsonProperty]
        private readonly SortedDictionary<string, Guid> AutoGenerated = new SortedDictionary<string, Guid>();
        [JsonProperty]
        private readonly SortedDictionary<string, Guid> UnusedGUIDs = new SortedDictionary<string, Guid>();
        private readonly SortedDictionary<string, Guid> UsedGUIDs = new SortedDictionary<string, Guid>();

        public void OverrideSettings(IUpdatableSettings userSettings) { }

        public BlueprintGuid GetGUID(string name) {

            if (!NewBlueprints.TryGetValue(name, out Guid Id)) {
#if DEBUG
                if (!AutoGenerated.TryGetValue(name, out Id)) {
                    Id = Guid.NewGuid();
                    AutoGenerated.Add(name, Id);
                    Main.LogDebug($"Generated new GUID: {name} - {Id}");
                } else {
                    Main.LogDebug($"WARNING: GUID: {name} - {Id} is autogenerated");
                }
#else
                Main.Log($"*** ERROR: GUID for {name} not found");
#endif
            }
            if (Id == null) { Main.Log($"ERROR: GUID for {name} not found"); }
            UsedGUIDs[name] = Id;
            return new BlueprintGuid(Id);
        }

        //    static void GenerateUnused() {
        //        ModSettings.Blueprints.AutoGenerated.ForEach(entry => {
        //            if (!ModSettings.Blueprints.UsedGUIDs.ContainsKey(entry.Key)) {
        //                ModSettings.Blueprints.UnusedGUIDs[entry.Key] = entry.Value;
        //            }
        //        });
        //        ModSettings.Blueprints.NewBlueprints.ForEach(entry => {
        //            if (!ModSettings.Blueprints.UsedGUIDs.ContainsKey(entry.Key)) {
        //                ModSettings.Blueprints.UnusedGUIDs[entry.Key] = entry.Value;
        //            }
        //        });
        //    }
        public static void WriteBlueprints(string fileName) {
            string userConfigFolder = ModSettings.ModEntry.Path + "UserSettings";
            var userPath = Path.Combine(userConfigFolder, fileName);
            Main.Log($"Writing blueprints to: {userPath}");

            JsonSerializer serializer = new JsonSerializer {
                NullValueHandling = NullValueHandling.Include,
                Formatting = Formatting.Indented
            };
            using StreamWriter sWriter = new StreamWriter(userPath);
            using JsonWriter jWriter = new JsonTextWriter(sWriter);
            serializer.Serialize(jWriter, ModSettings.Blueprints);
        }
        //}
    }
}
