﻿using BubbleGauntlet.Extensions;
using BubbleGauntlet.Utilities;
using HarmonyLib;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Area;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Items;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Blueprints.Loot;
using Kingmaker.Blueprints.Root;
using Kingmaker.Blueprints.Root.Strings;
using Kingmaker.Designers;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.Designers.EventConditionActionSystem.Conditions;
using Kingmaker.Designers.EventConditionActionSystem.Evaluators;
using Kingmaker.DialogSystem.Blueprints;
using Kingmaker.ElementsSystem;
using Kingmaker.Globalmap.Blueprints;
using Kingmaker.Localization;
using Kingmaker.RandomEncounters.Settings;
using Kingmaker.UI.Common;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Class.LevelUp.Actions;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Interaction;
using Kingmaker.Utility;
using Kingmaker.View;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Kingmaker.QA.Statistics.ExperienceGainStatistic;

namespace BubbleGauntlet {

    public static class ContentManager {

        public static BlueprintAbility FakeBlueprint = new();
        public static BlueprintUnit BubbleMasterBlueprint => BP.GetBlueprint<BlueprintUnit>("0234cbc0cc844da4d9cb225d6ed76a18");
        public static BlueprintUnit ServiceVendorBlueprint => BP.GetBlueprint<BlueprintUnit>("9a1443603c9353d4194a583a31228c8b");

        public static BlueprintDialog ServiceDialog;
        public static BlueprintDialog BubbleDialog;
        public static BlueprintDialog DescendDialog;
        public static LocalizedString DescendSpeaker;


        public static List<BlueprintUnit> Vendors = new();

        public static BlueprintDialog VendorDialog;
        public static StartTrade StartVending;

        private static (string vendor, string shop)[] ShopNames = {
            (vendor: "Hordeum the Vulgar", shop: "Kobold or Go Home: Wilderness Supply Store"),
        };

        public static void CreateBubbleDialog() {
            var hydrateCustom = new DynamicGameAction(() => {
                long goldBefore = Game.Instance.Player.Money;
                if (goldBefore < Game.Instance.Player.GetCustomCompanionCost())
                    return;


                var toAdd = RosterSaver.HydrateUnit();

                if (toAdd == null)
                    return;

                Game.Instance.Player.AddCompanion(toAdd);
                toAdd.IsInGame = true;
                Vector3 vector = Game.Instance.Player.MainCharacter.Value.Position;
                if (AstarPath.active) {
                    FreePlaceSelector.PlaceSpawnPlaces(2, toAdd.View.Corpulence, vector);
                    vector = FreePlaceSelector.GetRelaxedPosition(1, true);
                }
                toAdd.Position = vector;
                Game.Instance.Player.Money = goldBefore - Game.Instance.Player.GetCustomCompanionCost();
                Main.Log($"Subtracted gold, now have: {Game.Instance.Player.Money}");
                GameHelper.GainExperience(2000, null, GainType.Quest);
            });
            var createCustom = new DynamicGameAction(() => {
                long goldBefore = Game.Instance.Player.Money;
                if (goldBefore < Game.Instance.Player.GetCustomCompanionCost())
                    return;

                Game.Instance.Player.CreateCustomCompanion(() => {

                    if (Game.Instance.Player.Party.Count >= 6)
                        return;

                    var toAdd = Game.Instance.LevelUpController.Unit;

                    if (toAdd == null)
                        return;

                    Game.Instance.Player.AddCompanion(toAdd);
                    toAdd.IsInGame = true;
                    Vector3 vector = Game.Instance.Player.MainCharacter.Value.Position;
                    if (AstarPath.active) {
                        FreePlaceSelector.PlaceSpawnPlaces(2, toAdd.View.Corpulence, vector);
                        vector = FreePlaceSelector.GetRelaxedPosition(1, true);
                    }
                    toAdd.Position = vector;
                    Game.Instance.Player.Money = goldBefore - Game.Instance.Player.GetCustomCompanionCost();
                    Main.Log($"Subtracted gold, now have: {Game.Instance.Player.Money}");
                });
            });

            var basicShop = new StartTrade();
            basicShop.Vendor = new DialogFirstSpeaker();

            var dismissVendor = new DynamicGameAction(() => {
                Main.LogNotNull("vendor", GauntletController.Vendor);
                GauntletController.Vendor.MarkForDestroy();
                GauntletController.Floor.Shopping = false;
                GauntletController.CompleteEncounter();
            });

            CueBuilder mainRoot = null;

            var serviceBuilder = new DialogBuilder();
            {
                mainRoot = serviceBuilder.Root("I am here to help, in any small way I can.\nI can hire brave adventurers, raise dead companions, and I stock a variety of common items.");
                mainRoot.Speaker(ServiceVendorBlueprint.ToReference<BlueprintUnitReference>());

                mainRoot.Answers()
                    .Add("Raise all dead companions. <{bubble_res_cost}> (no dead companions)")
                        .When(new HasDeadCompanions().Invert())
                        .Disabled()
                        .Commit()
                    .Add("Raise all dead companions. <{bubble_res_cost}> (not enough gold)")
                        .When(new HasEnoughMoneyForRes().Invert(), new HasDeadCompanions())
                        .Disabled()
                        .Commit()
                    .Add("Raise all dead companions. <{bubble_res_cost}>")
                        .When(new HasEnoughMoneyForRes(), new HasDeadCompanions())
                        .AddAction(new ResurrectCompanions())
                        .Commit()

                    .Add("Hire a companion. <{custom_companion_cost}> (not enough gold)")
                        .When(new HasEnoughMoneyForCustomCompanion().Invert())
                        .Disabled()
                        .Commit()
#if BUBBLEDEV
                    .Add("Recall a saved companion. <{custom_companion_cost}> (not enough gold)")
                        .When(new HasEnoughMoneyForCustomCompanion().Invert())
                        .Disabled()
                        .Commit()
#endif

                    .Add("Hire a companion. <{custom_companion_cost}>")
                        .When(new HasEnoughMoneyForCustomCompanion())
                        .AddAction(createCustom)
                        .Commit()
#if BUBBLEDEV
                    .Add("Recall a saved companion. <{custom_companion_cost}>")
                        .When(new HasEnoughMoneyForCustomCompanion())
                        .AddAction(hydrateCustom)
                        .Commit()
#endif

                    .Add("What do you have for sale?")
                        .AddAction(basicShop)
                        .Commit()

                    .Add("DEV: Advance mythic level")
                        .AddAction(new DynamicGameAction(() => {
                            Game.Instance.Player.AdvanceMythicExperience(Game.Instance.Player.MythicExperience + 1);
                        }))
                        .Commit()
                    .Add("DEV: Advance character level")
                        .AddAction(new DynamicGameAction(() => {
                            foreach (var c in Game.Instance.Player.PartyCharacters) {
                                var progression = c.Value.Descriptor.Progression;
                                var wanted = progression.CharacterLevel + 1;
                                if (progression.ExperienceTable.HasBonusForLevel(wanted)) {
                                    var next = progression.ExperienceTable.GetBonus(wanted);
                                    progression.AdvanceExperienceTo(next + 1);

                                }

                            }
                        }))
                        .Commit()

                    .Add("I have no need of you at this juncture.")
                        .Commit()

                    .Commit();
                mainRoot.Commit();
            }

            var dialogBuilder = new DialogBuilder();
            {
                mainRoot = dialogBuilder.Root("{bubble_gauntlet_welcome}\n{bubble_encounters_remaining}.");
                mainRoot.Speaker(BubbleMasterBlueprint.ToReference<BlueprintUnitReference>());
                mainRoot.When(new DynamicCondition(() => !GauntletController.Floor.Shopping));
                var backstory = dialogBuilder.Cue("I am the bubbliest bubble that ever bubbled");
                var descendCue = dialogBuilder.Cue("Ready yourself!");

                descendCue.OnStop(new DescendAction()).Commit();

                backstory.ContinueWith(mainRoot).Commit();

                var rootAnswers = mainRoot.Answers();

                rootAnswers.Add("ONWARD! Rest then contine to the next floor.")
                    .When(FloorState.NoEncounters)
                    .ContinueWith(descendCue)
                    .Commit();

                foreach (var eventType in EncounterTemplate.All) {
                    rootAnswers.Add($"I will {eventType.Name}! [{eventType.Tag.ToTag()} left]")
                        .Enabled(FloorState.HasEncounters, eventType.Available)
                        .AddAction(eventType.Action)
                        .Commit();
                }

                rootAnswers
                    .Add("Tell me more about yourself.")
                        .ContinueWith(backstory)
                        .Commit()

                    .Add("I must still wait before bubbling.")
                        .Commit();

                rootAnswers.Commit();
            }

            var descendBuilder = new DialogBuilder();
            {
                var root = descendBuilder.RootPage("descend-root");
                root.BasicCue("You descend to the next floor");
                root.OnShow(new DynamicGameAction(() => {
                    int exp = GauntletController.FloorExperience(GauntletController.Floor.Level);
                    GameHelper.GainExperience(exp, null, GainType.Quest);
                }));
                root.Answers()
                    .Add("Continue")
                        .AddAction(new DynamicGameAction(() => {
                            GauntletController.Floor.Descend();
                            var nextArea = MapManager.Maps.Where(map => map != GauntletController.CurrentMap).Random();
                            Game.Instance.LoadArea(nextArea.AreaEnter, Kingmaker.EntitySystem.Persistence.AutoSaveMode.None);
                            ProgressIndicator.Refresh();
                        }))
                        .Commit()
                    .Commit();
                root.Commit();
            }

            var vendorIsActive = new DialogBuilder();
            {
                var root = vendorIsActive.Root("Would you like me to dismiss the vendor so you can continue?");
                root.Speaker(BubbleMasterBlueprint.ToReference<BlueprintUnitReference>());
                root.When(new DynamicCondition(() => GauntletController.Floor.Shopping));
                root.Answers()
                        .Add("No, I still have shopping to do.").Commit()
                        .Add("Yes, I am ready to continue")
                            .AddAction(dismissVendor)
                            .ContinueWith(mainRoot)
                            .Commit()
                        .Commit();

                root.Commit();
            }




            mainRoot.Commit();

            ServiceDialog = Helpers.CreateDialog("bubble-dialog-service", dialog => {
                dialog.FirstCue.Cues.Add(serviceBuilder.Build());
                dialog.TurnFirstSpeaker = true;
            });

            BubbleDialog = Helpers.CreateDialog("bubble-dialog-bubblemaster", dialog => {
                dialog.FirstCue.Cues.Add(dialogBuilder.Build());
                dialog.FirstCue.Cues.Add(vendorIsActive.Build());
            });

            DescendDialog = Helpers.CreateDialog("bubble-dialog-descend", dialog => {
                dialog.FirstCue.Cues.Add(descendBuilder.Build());
                dialog.Type = DialogType.Book;
                dialog.TurnFirstSpeaker = true;
                dialog.TurnPlayer = true;
            });

            DescendSpeaker = Helpers.CreateString("bubble-descend-speaker", "Bubbles");

        }

        public static bool Installed = false;

        public static void InstallGauntletContent() {
            if (Installed)
                return;
            Installed = true;

            Main.Log("Initializing Gauntlet Blueprints");

            if (!BlueprintRoot.Instance.NewGameSettings.StoryList.Any(e => e.Title.Key == "bubblegauntlet-mm-title")) {
                BlueprintRoot.Instance.NewGameSettings.StoryList.Add(new NewGameRoot.StoryEntity {
                    Title = Helpers.CreateString("bubblegauntlet-mm-title", "Bubble Gauntlet"),
                    Description = Helpers.CreateString("bubblegauntlet-mm-desc", $"{"WANTED".MakeTitle()}: Adventurers stupid enough to trust a Bubble\n\n" +
                        "You must bubble through dangerous bubbles while bubbling monsters of the bubbly variety!"),
                    KeyArt = AssetLoader.LoadInternal("sprites", "NewGame.png", new Vector2Int(1232, 820), TextureFormat.BC7)
                });
                BlueprintRoot.Instance.NewGameSettings.StoryList.RemoveAt(0);
            }

            MonsterDB.Initialize();
            CombatManager.Install();
            FUN.Install();
            CreateVendorBlueprints();
            CreateBubbleDialog();
            UnlockMythicNonsense();
            CreateElites();

            CreateServiceVendorBlueprint();
            CreateBubbleMasterBlueprint();

            MapManager.Install();

            //Never go to global map but I think it's required or the game will cry
            var globalMapLocation_dummy = BP.GetBlueprint<BlueprintGlobalMapPoint>("556d74cd8f75e674981862b10a84fa70");
            var initialPreset = BP.GetBlueprint<BlueprintAreaPreset>("b3e2cc3c1ccef09489209a20fde3fb72");

            var bp = Helpers.CreateBlueprint<BlueprintAreaPreset>("bubble-gauntlet-preset", null, preset => {
                try {
                    preset.m_Area = MapManager.Maps.Last().Area.ToReference<BlueprintAreaReference>();
                    preset.m_EnterPoint = MapManager.Maps.Last().AreaEnter.ToReference<BlueprintAreaEnterPointReference>();
                    preset.m_GlobalMapLocation = globalMapLocation_dummy.ToReference<BlueprintGlobalMapPoint.Reference>();
                    preset.m_OverrideGameDifficulty = initialPreset.m_OverrideGameDifficulty;
                    preset.m_OverrideGameDifficulty.Preset.DamageToParty = 0.2f;
                    preset.m_PlayerCharacter = initialPreset.m_PlayerCharacter;
                    preset.AddResources = new();
                    preset.m_KingdomIncomePerClaimed = new();
                    preset.m_Stats = new();
                    preset.HasKingdom = false;
                    preset.StartGameActions = new();
                    preset.StartGameActions.Actions = new GameAction[] {
                        new DynamicGameAction(() => {
                            LevelUpHelper.AddStartingItems(Game.Instance.Player.MainCharacter.Value);
                        }),
                    };
                } catch (Exception ex) {
                    Main.Error(ex, "makign preset");
                }
            });


            Main.Log("validating new game preset");
            bp.Validate();

            Main.Log("setting new game preset (BlueprintRoot)");
            BlueprintRoot.Instance.NewGamePreset = bp;
            Main.Log("Set new game preset");
        }


        private static void CreateBubbleMasterBlueprint() {
            var crink = BubbleMasterBlueprint;
            crink.SetLocalisedName("bubble-dm", "Bubbles");

            crink.AddComponent<DialogOnClick>(dialogOnClick => {
                dialogOnClick.m_Dialog = BubbleDialog.ToReference<BlueprintDialogReference>();
                dialogOnClick.name = "start-dialog";
                dialogOnClick.NoDialogActions = new();
                dialogOnClick.Conditions = new();
            });
        }

        private static void CreateServiceVendorBlueprint() {
            var service = ServiceVendorBlueprint;
            service.SetLocalisedName("buble-service", "Trumpet Girl");

            HalfPortraitInjecotr.Replacements[service.PortraitSafe.Data] = AssetLoader.LoadInternal("sprites", "definitely_not_iomedae.png", new Vector2Int(332, 432), TextureFormat.BC5);

            service.AddComponent<DialogOnClick>(onClick => {
                onClick.m_Dialog = ServiceDialog.ToReference<BlueprintDialogReference>();
                onClick.name = "start-service-dialog";
                onClick.NoDialogActions = new();
                onClick.Conditions = new();
            });
            var vendorTable = CreateServiceVendorTable();
            service.AddComponent<AddSharedVendor>(vendor => {
                vendor.m_m_Table = vendorTable.ToReference<BlueprintSharedVendorTableReference>();
            });
        }

        private static void CreateElites() {
            Elite("f834867d15633294ea70579f3616af21", "bubble-elite-frog", $"Grippli Gripplesson", 1, 4);
            Elite("4dd913232eaf3894890b2bfaabcd8282", "bubble-elite-tztx", $"Wormy McWormface", 1, 4);
            Elite("0c1c73f377c8d19499b9fa384543b687", "bubble-elite-quasit", $"Kwa Sitt", 1, 4);
            Elite("768a3608ae4f0214f8ea290b650e35c1", "bubble-elite-werewolf", $"Man Wolf", 1, 4);
            Elite("6267190d45315e24db1e67cd012c624c", "bubble-elite-wererat", $"Man Rat", 1, 4);
        }

        private static void UnlockMythicNonsense() {

            static void UnlockMythic(string classBp) {
                BP.GetBlueprint<BlueprintCharacterClass>(classBp).RemoveComponents<MythicClassLockComponent>();
                BP.GetBlueprint<BlueprintCharacterClass>(classBp).RemoveComponents<PrerequisiteEtude>();
                BP.GetBlueprint<BlueprintCharacterClass>(classBp).RemoveComponents<PrerequisiteFeature>();
            }

            HashSet<string> allowedPaths = new() {
                "8e19495ea576a8641964102d177e34b7",     //demon
                "15a85e67b7d69554cab9ed5830d0268e",     //aeon
                "5d501618a28bdc24c80007a5c937dcb7",     //lich
                "a5a9fe8f663d701488bd1db8ea40484e",     //angel
                "9a3b2c63afa79744cbca46bea0da9a16",     //azata
                "8df873a8c6e48294abdb78c45834aa0a",     //trickster
            };

            string startingMythic = "247aa787806d5da4f89cfc3dff0b217f";
            var companionMythic = "530b6a79cb691c24ba99e1577b4beb6d";

            allowedPaths.ForEach(UnlockMythic);

            BlueprintRoot.Instance.Progression.m_CharacterMythics = BlueprintRoot.Instance.Progression.m_CharacterMythics.Where(c => {
                var guid = c.deserializedGuid.ToString();
                if (guid == startingMythic || guid == companionMythic)
                    return true;
                return allowedPaths.Contains(c.deserializedGuid.ToString());
            }).ToArray();
        }

        public static Dictionary<BlueprintUnit, BlueprintUnit> BubbleToSubble = new();

        private static (BlueprintUnit, BlueprintUnit) Elite(string baseBp, string id, string name, int minFloor, int maxFloor) {
            var elite = Helpers.CreateCopy<BlueprintUnit>(BP.GetBlueprint<BlueprintUnit>(baseBp));
            elite.SetLocalisedName(id + ".name", $"{name} {UIUtilityTexts.DirectTextSymbol}");
            var subble = Helpers.CreateCopy(elite, elite => {
                elite.SetLocalisedName(elite.name + "-subble", $"A fracture of {elite.CharacterName}");
            });
            BubbleToSubble[elite] = subble;
            Elites.Add((elite, minFloor, maxFloor));
            return (elite, subble);
        }

        public static IEnumerable<BlueprintItem> AllItems => ItemTables.Where(k => k.Key != "Usable").SelectMany(kv => kv.Value);

        public static Dictionary<string, List<BlueprintItem>> ItemTables = new();
        public static List<(BlueprintUnit Blueprint, int MinFloor, int MaxFloor)> Elites = new();
        public static BlueprintUnit EliteFrogSubling;


        private static BlueprintSharedVendorTable CreateServiceVendorTable() {

            List<(string guid, int count)> itemsToSell = new();

            TextLoader.Process("service_vendor_list.txt", line => {
                if (line[0] == '#') return;

                var parts = line.Split(' ');
                itemsToSell.Add((parts[1], int.Parse(parts[0])));
            });

            ItemTables.Clear();


            string[] Tables = {
                "Armor",
                "Belt",
                "Feet",
                "Glasses",
                "Gloves",
                "Head",
                "Neck",
                "Ring",
                "Shield",
                "Shirt",
                "Shoulders",
                "Usable",
                "Weapon",
                "Wrist",
            };


            foreach (var table in Tables) {
                var fileName = $"vendor_tables.vendortable_{table}.txt";

                List<BlueprintItem> list = new();

                TextLoader.Process(fileName, line => {
                    list.Add(BP.GetBlueprint<BlueprintItem>(line.Split(':')[1]));
                });
                ItemTables.Add(table, list);
            }

            return Helpers.CreateBlueprint<BlueprintSharedVendorTable>("bubble-master-vendor", table => {
                foreach (var (guid, count) in itemsToSell) {
                    table.AddComponent<LootItemsPackFixed>(pack => {
                        pack.m_Count = count;
                        pack.m_Item = new();
                        pack.m_Item.m_Item = BP.GetBlueprint<BlueprintItem>(guid).ToReference<BlueprintItemReference>();
                    });
                }
            });
        }

        private static void CreateVendorBlueprints() {
            StartVending = new();
            StartVending.Vendor = new DialogFirstSpeaker();

            for (int i = 0; i < ShopNames.Length; i++) {
                var (name, shop) = ShopNames[i];
                var baseUnit = BP.GetBlueprint<BlueprintUnit>("3425924a5b6a20b4f9e95d7e9fa3ccff");
                baseUnit.SetLocalisedName($"vendor{i}.name", name);
                baseUnit.Speed = new Feet(5);
                Vendors.Add(baseUnit);

                var builder = new DialogBuilder();
                var entry = builder.Root($"Welcome to {shop}, what can I do you for?");
                entry.Answers()
                    .Add("Let's see what so-called valuables you have to offer.")
                        .AddAction(StartVending)
                        .Commit()
                    .Add("I'm not interested in any of your cheap tat!").Commit()
                    .Commit();
                entry.Commit();

                VendorDialog = Helpers.CreateDialog("vendor-dialog", dialog => {
                    dialog.FirstCue.Cues.Add(builder.Build());

                });

                baseUnit.AddComponent<DialogOnClick>(onClick => {
                    onClick.m_Dialog = VendorDialog.ToReference<BlueprintDialogReference>();
                    onClick.name = "start-vendor-dialog";
                    onClick.NoDialogActions = new();
                    onClick.Conditions = new();
                });
            }
        }

    }

    [HarmonyPatch(typeof(PortraitData), "get_HalfLengthPortrait")]
    public static class HalfPortraitInjecotr {
        public static Dictionary<PortraitData, Sprite> Replacements = new();
        public static bool Prefix(PortraitData __instance, ref Sprite __result) {
            if (Replacements.TryGetValue(__instance, out __result))
                return false;
            return true;
        }
    }


    [HarmonyPatch]
    public static class ContentInjector {

        [HarmonyPatch(typeof(MainMenu), nameof(MainMenu.Awake)), HarmonyPostfix]
        public static void MainMenu_Awake() {
            ContentManager.InstallGauntletContent();
        }

        [HarmonyPatch(typeof(BlueprintsCache), nameof(BlueprintsCache.Init)), HarmonyPostfix]
        public static void BlueprintsCache_Init() {
            ContentManager.InstallGauntletContent();
        }
    }
}
