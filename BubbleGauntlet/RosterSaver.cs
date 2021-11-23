using BubbleGauntlet.Extensions;
using BubbleGauntlet.Utilities;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Root;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.View;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace BubbleGauntlet {
    public static class RosterSaver {

        public static BlueprintCharacterClass PaladinClass => BP.Get<BlueprintCharacterClass>("bfa11238e7ae3544bbeb4d0b92e897ec");
        public static BlueprintFeatureSelection PaladinDeity => BP.Get<BlueprintFeatureSelection>("a7c8b73528d34c2479b4bd638503da1d");
        public static BlueprintFeature Iomedae => BP.Get<BlueprintFeature>("88d5da04361b16746bf5b65795e0c38c");
        public static BlueprintFeature Shelyn => BP.Get<BlueprintFeature>("b382afa31e4287644b77a8b30ed4aa0b");
        public static BlueprintFeature Progress => BP.Get<BlueprintFeature>("30fabaa0beb24c33b705df040ad5e5d2");
        
        public static BlueprintUnit Dummy => BP.Get<BlueprintUnit>("fad59e6db3aa470ca7e8962e2daa12dc");
        public static BlueprintFeature SeelahFeature => BP.Get<BlueprintFeature>("777ae11136378a64883059457966a325");

        public static BlueprintFeature SavedState;

        public static void GenerateBlueprint() {
            var addClassLevels = Dummy.GetComponent<AddClassLevels>();
            addClassLevels.m_CharacterClass = PaladinClass.ToReference<BlueprintCharacterClassReference>();
        }

        public static UnitEntityData HydrateUnit() {
            //GenerateBlueprint();
            //        int targetExp = xp ?? BlueprintRoot.Instance.Progression.XPTable.GetBonus(this.GetCustomCompanionStartLevel());
            //int mythicExperience = this.MainCharacter.Value.Progression.MythicExperience;
            var acl = Progress.GetComponent<AddClassLevels>();
            Main.LogNotNull("acl", acl);
            var basicFeatsRef = BP.Ref<BlueprintFeatureSelectionReference>("247a4068296e8be42890143f451b4b45");
            var basicFeats = acl.Selections.FirstOrDefault(s => s.m_Selection.Equals(basicFeatsRef));
            basicFeats.m_Features[0] = BP.Ref<BlueprintFeatureReference>("2a6091b97ad940943b46262600eaeaeb");
            Dummy.GetComponent<ClassLevelLimit>().LevelLimit = 1;
            UnitEntityData newCompanion = Game.Instance.EntityCreator.SpawnUnit(Dummy, Vector3.zero, Quaternion.identity, Game.Instance.Player.CrossSceneState);
            newCompanion.Descriptor.CustomName = "Peach";
            Main.LogNotNull("acl", acl);
            //AddClassLevels.LevelUp(acl, newCompanion.Descriptor, 1);

            return newCompanion;
        }
    }
}
