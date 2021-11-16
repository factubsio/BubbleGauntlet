using Kingmaker.TextTools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BubbleGauntlet {
    public class ParameterisedTextTemplate : TextTemplate {
        private readonly Func<string, string> gen;

        public ParameterisedTextTemplate(Func<string, string> gen) {
            this.gen = gen;
        }

        public override string Generate(bool capitalized, List<string> parameters) {
            return gen(parameters.FirstOrDefault());
        }
    }
    public class FixedTextTemplate : TextTemplate {
        private readonly Func<string> gen;

        public FixedTextTemplate(Func<string> gen) {
            this.gen = gen;
        }

        public override string Generate(bool capitalized, List<string> parameters) {
            return gen();
        }
    }

    public static class BubbleTemplates {
        private static readonly HashSet<string> Added = new();
        private static void Add(string tag, Func<string> simple) {
            Added.Add(tag);
            TextTemplateEngine.s_TemplatesByTag[tag] = new FixedTextTemplate(simple);
        }
        private static void Add(string tag, Func<string, string> func) {
            Added.Add(tag);
            TextTemplateEngine.s_TemplatesByTag[tag] = new ParameterisedTextTemplate(func);
        }
        public static void AddAll() {
            Add("bubble_encounters_remaining", () => {
                int val = GauntletController.Floor.EncountersRemaining;
                string count;
                if (val == 0)
                    return "You have completed all the available encounters on this floor!";
                else if (val == 1)
                    count = "1 more encounter";
                else
                    count = $"{val} more encounters";
                return $"You must finish {count} before you can continue to the next floor.";

            });
            Add("bubble_gauntlet_welcome", () => {
                int val = GauntletController.Floor.EncountersRemaining;
                int level = GauntletController.Floor.Level;
                if (level == 1 && val == GauntletController.Floor.TotalEncounters) {
                    return "Welcome to the Gauntlet of the bubble! Riches and more await...\n" +
                        "You must complete a number of encounters before continuing to the next floor\n" +
                        "I can help with basic services like healing, for a fee...";
                } else {
                    return "Riches and more await...\n" +
                        "Are you ready to continue your quest?";
                }

            });
            Add("bubble_fight_remaining", () => GauntletController.Floor.Events[EncounterType.Fight].Remaining.ToString());
            Add("bubble_elite_remaining", () => GauntletController.Floor.Events[EncounterType.EliteFight].Remaining.ToString());
            Add("bubble_rest_remaining", () => GauntletController.Floor.Events[EncounterType.Rest].Remaining.ToString());
            Add("bubble_shop_remaining", () => GauntletController.Floor.Events[EncounterType.Shop].Remaining.ToString());
            Add("bubble_res_cost", () => HasEnoughMoneyForRes.Cost.ToString());

        }
        public static void RemoveAll() {
            foreach (var k in Added)
                TextTemplateEngine.s_TemplatesByTag.Remove(k);
        }

    }

}
