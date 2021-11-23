using JetBrains.Annotations;
using Kingmaker.Blueprints;
using System;
using System.Collections.Generic;
using BubbleGauntlet.Config;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.Blueprints.Classes;

namespace BubbleGauntlet {
    static class BP {
        public static readonly Dictionary<BlueprintGuid, SimpleBlueprint> ModBlueprints = new Dictionary<BlueprintGuid, SimpleBlueprint>();

        public static BlueprintUnit Unit(string id) => Get<BlueprintUnit>(id);
        public static BlueprintBuff Buff(string id) => Get<BlueprintBuff>(id);
        public static BlueprintAbility Ability(string id) => Get<BlueprintAbility>(id);
        public static BlueprintCharacterClass Class(string id) => Get<BlueprintCharacterClass>(id);
        public static BlueprintProgression Progression(string id) => Get<BlueprintProgression>(id);
        public static BlueprintFeature Feature(string id) => Get<BlueprintFeature>(id);

        public static void RemoveModBlueprints() {
            foreach (var guid in ModBlueprints.Keys) {
                ResourcesLibrary.BlueprintsCache.RemoveCachedBlueprint(guid);
            }

        }

#if false
        public static IEnumerable<T> GetBlueprints<T>() where T : BlueprintScriptableObject {
            if (blueprints == null) {
                var bundle = ResourcesLibrary.s_BlueprintsBundle;
                blueprints = bundle.LoadAllAssets<BlueprintScriptableObject>();
                //blueprints = Kingmaker.Cheats.Utilities.GetScriptableObjects<BlueprintScriptableObject>();
            }
            return blueprints.Concat(ResourcesLibrary.s_LoadedBlueprints.Values).OfType<T>().Distinct();
        }
#endif
        public static T GetModBlueprint<T>(string name) where T : SimpleBlueprint {
            var assetId = ModSettings.Blueprints.GetGUID(name);
            ModBlueprints.TryGetValue(assetId, out var value);
            return value as T;
        }
        public static T Get<T>(string id) where T : SimpleBlueprint {
            var assetId = new BlueprintGuid(System.Guid.Parse(id));
            return Get<T>(assetId);
        }
        public static T Get<T>(BlueprintGuid id) where T : SimpleBlueprint {
            SimpleBlueprint asset = ResourcesLibrary.TryGetBlueprint(id);
            T value = asset as T;
            if (value == null) { Main.Log($"COULD NOT LOAD: {id} - {typeof(T)}"); }
            return value;
        }
        public static void AddBlueprint([NotNull] SimpleBlueprint blueprint) {
            AddBlueprint(blueprint, blueprint.AssetGuid);
        }
        public static void AddBlueprint([NotNull] SimpleBlueprint blueprint, string assetId) {
            var Id = BlueprintGuid.Parse(assetId);
            AddBlueprint(blueprint, Id);
        }

        public static List<Guid> AddedBlueprints = new();

        public static void AddBlueprint([NotNull] SimpleBlueprint blueprint, BlueprintGuid assetId) {
            var loadedBlueprint = ResourcesLibrary.TryGetBlueprint(assetId);
            if (loadedBlueprint == null) {
                ModBlueprints[assetId] = blueprint;
                ResourcesLibrary.BlueprintsCache.AddCachedBlueprint(assetId, blueprint);
                blueprint.OnEnable();
                Main.LogPatch("Added", blueprint);
            } else {
                Main.Log($"Failed to Add: {blueprint.name}");
                Main.Log($"Asset ID: {assetId} already in use by: {loadedBlueprint.name}");
            }
        }

        internal static T Ref<T>(string v) where T : BlueprintReferenceBase {
            var tref = Activator.CreateInstance<T>();
            tref.deserializedGuid = BlueprintGuid.Parse(v);
            return tref;
        }
    }
}