using BubbleGauntlet.Utilities;
using Kingmaker.AI.Blueprints;
using Kingmaker.Blueprints;
using System;
using System.Collections.Generic;

namespace BubbleGauntlet.Bosses {
    public class AiCastSpellBuilder {
        private List<ConsiderationReference> actorConsiderations = new();
        private BlueprintAiCastSpell cast;

        private List<ConsiderationReference> targetConsiderations = new();

        private AiCastSpellBuilder(string name) {
            cast = Helpers.CreateBlueprint<BlueprintAiCastSpell>($"ai-cast-spell://{name}", cast => {
                cast.BaseScore = 10.0f;
                cast.CombatCount = 10;
                cast.Components = Array.Empty<BlueprintComponent>();
                cast.MinDifficulty = Kingmaker.Settings.GameDifficultyOption.Casual;
                cast.m_ForceTargetEnemy = true;
                cast.m_VariantsSet = Array.Empty<BlueprintAbilityReference>();
                cast.Locators = Array.Empty<EntityReference>();
            });
        }

        public static AiCastSpellBuilder New(string name) => new(name);
        public AiCastSpellBuilder Ability(string abilityRef) {
            cast.m_Ability = new BlueprintAbilityReference { deserializedGuid = BlueprintGuid.Parse(abilityRef) };
            return this;
        }

        public AiCastSpellBuilder Ability(BlueprintAbilityReference ability) {
            cast.m_Ability = ability;
            return this;
        }

        public BlueprintAiActionReference Build() {
            cast.m_ActorConsiderations = actorConsiderations.ToArray();
            cast.m_TargetConsiderations = targetConsiderations.ToArray();

            return cast.ToReference<BlueprintAiActionReference>();
        }

        public AiCastSpellBuilder Charges(int count) {
            cast.CombatCount = count;
            return this;
        }

        public AiCastSpellBuilder Score(float score) {
            cast.BaseScore = score;
            return this;
        }

        public AiCastSpellBuilder Self() {
            cast.m_ForceTargetSelf = true;
            cast.m_ForceTargetEnemy = false;
            return this;
        }
        public AiCastSpellBuilder WhenActor(ConsiderationReference when) {
            actorConsiderations.Add(when);
            return this;
        }

        public AiCastSpellBuilder WhenTarget(ConsiderationReference when) {
            targetConsiderations.Add(when);
            return this;
        }
    }
}
