﻿using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using System;
using System.Collections.Generic;
using System.Linq;
using BubbleGauntlet.Extensions;

namespace BubbleGauntlet.Utilities {
    static class SpellTools {
        public static void AddToSpellList(this BlueprintAbility spell, BlueprintSpellList spellList, int level) {
            AddComponentIfMissing(spellList);
            AddToListIfMissing(spellList);
            if (spellList == SpellList.WizardSpellList) {
                var school = spell.School;
                AddComponentIfMissing(specialistSchoolList.Value[(int)school]);
                AddToListIfMissing(specialistSchoolList.Value[(int)school]);

                for (int i = 0; i < thassilonianSchoolList.Value.Length; i++) {
                    if (thassilonianOpposedSchools.Value[i] != null && !thassilonianOpposedSchools.Value[i].Contains(school)) {
                        AddComponentIfMissing(thassilonianSchoolList.Value[i]);
                        AddToListIfMissing(thassilonianSchoolList.Value[i]);
                    }
                }

                if (school == SpellSchool.Enchantment || school == SpellSchool.Illusion) {
                    AddComponentIfMissing(SpellList.FeyspeakerSpelllist);
                    AddToListIfMissing(SpellList.FeyspeakerSpelllist);
                }
            }
            void AddComponentIfMissing(BlueprintSpellList list) {
                if (list == null) { return; }
                if (!spell.GetComponents<SpellListComponent>().Any(c => c.m_SpellList.Get() == list && c.SpellLevel == level)) {
                    var comp = new SpellListComponent {
                        SpellLevel = level,
                        m_SpellList = list.ToReference<BlueprintSpellListReference>()
                    };
                    spell.AddComponent(comp);
                }
            }
            void AddToListIfMissing(BlueprintSpellList list) {
                if (list == null) { return; }
                if (!list.SpellsByLevel[level].Spells.Contains(spell)) {
                    list.SpellsByLevel[level].Spells.Add(spell);
                    list.SpellsByLevel[level].m_Spells.Sort((x, y) => x.Get().Name.CompareTo(y.Get().Name));
                }
            }
        }
        static readonly Lazy<BlueprintSpellList[]> specialistSchoolList = new Lazy<BlueprintSpellList[]>(() => {
            var result = new BlueprintSpellList[(int)SpellSchool.Universalist + 1];
            result[(int)SpellSchool.Abjuration] = SpellList.WizardAbjurationSpellList;
            result[(int)SpellSchool.Conjuration] = SpellList.WizardConjurationSpellList;
            result[(int)SpellSchool.Divination] = SpellList.WizardDivinationSpellList;
            result[(int)SpellSchool.Enchantment] = SpellList.WizardEnchantmentSpellList;
            result[(int)SpellSchool.Evocation] = SpellList.WizardEvocationSpellList;
            result[(int)SpellSchool.Illusion] = SpellList.WizardIllusionSpellList;
            result[(int)SpellSchool.Necromancy] = SpellList.WizardNecromancySpellList;
            result[(int)SpellSchool.Transmutation] = SpellList.WizardTransmutationSpellList;
            return result;
        });
        static readonly Lazy<BlueprintSpellList[]> thassilonianSchoolList = new Lazy<BlueprintSpellList[]>(() => {
            var result = new BlueprintSpellList[(int)SpellSchool.Universalist + 1];
            result[(int)SpellSchool.Abjuration] = SpellList.ThassilonianAbjurationSpellList;
            result[(int)SpellSchool.Conjuration] = SpellList.ThassilonianConjurationSpellList;
            result[(int)SpellSchool.Enchantment] = SpellList.ThassilonianEnchantmentSpellList;
            result[(int)SpellSchool.Evocation] = SpellList.ThassilonianEvocationSpellList;
            result[(int)SpellSchool.Illusion] = SpellList.ThassilonianIllusionSpellList;
            result[(int)SpellSchool.Necromancy] = SpellList.ThassilonianNecromancySpellList;
            result[(int)SpellSchool.Transmutation] = SpellList.ThassilonianTransmutationSpellList;
            return result;
        });
        static readonly Lazy<SpellSchool[][]> thassilonianOpposedSchools = new Lazy<SpellSchool[][]>(() => {
            var result = new SpellSchool[(int)SpellSchool.Universalist + 1][];

            result[(int)SpellSchool.Abjuration] = new SpellSchool[] { SpellSchool.Evocation, SpellSchool.Necromancy };
            result[(int)SpellSchool.Conjuration] = new SpellSchool[] { SpellSchool.Evocation, SpellSchool.Illusion };
            result[(int)SpellSchool.Enchantment] = new SpellSchool[] { SpellSchool.Necromancy, SpellSchool.Transmutation };
            result[(int)SpellSchool.Evocation] = new SpellSchool[] { SpellSchool.Abjuration, SpellSchool.Conjuration };
            result[(int)SpellSchool.Illusion] = new SpellSchool[] { SpellSchool.Conjuration, SpellSchool.Transmutation };
            result[(int)SpellSchool.Necromancy] = new SpellSchool[] { SpellSchool.Abjuration, SpellSchool.Enchantment };
            result[(int)SpellSchool.Transmutation] = new SpellSchool[] { SpellSchool.Enchantment, SpellSchool.Illusion };
            return result;
        });
        public static SpellsLevelEntry CreateSpellLevelEntry(params int[] count) {
            var entry = new SpellsLevelEntry();
            entry.Count = count;
            return entry;
        }
        public static class Spellbook {
            public static BlueprintSpellbook AccursedWitchSpellbook => BP.Get<BlueprintSpellbook>("b897fe0947e4b804082b1a687c21e6e2");
            public static BlueprintSpellbook AeonSpellbook => BP.Get<BlueprintSpellbook>("6091d66a2a9876b4891b989804cfbcb6");
            public static BlueprintSpellbook AlchemistSpellbook => BP.Get<BlueprintSpellbook>("027d37761f3804042afa96fe3e9086cc");
            public static BlueprintSpellbook AngelfireApostleSpellbook => BP.Get<BlueprintSpellbook>("d0313b3110357b14ebd3102c2d4cde20");
            public static BlueprintSpellbook AngelSpellbook => BP.Get<BlueprintSpellbook>("015658ac45811b843b036e4ccc96c772");
            public static BlueprintSpellbook ArcanistSpellbook => BP.Get<BlueprintSpellbook>("33903fe5c4abeaa45bc249adb9d98848");
            public static BlueprintSpellbook ArmagsBladeSpellbook => BP.Get<BlueprintSpellbook>("2b3065f56d77f4845891bfa5b78436da");
            public static BlueprintSpellbook AzataSpellbook => BP.Get<BlueprintSpellbook>("b21b9f5e2831c2549a782d8128fb905b");
            public static BlueprintSpellbook BardSpellbook => BP.Get<BlueprintSpellbook>("bc04fc157a8801d41b877ad0d9af03dd");
            public static BlueprintSpellbook BloodragerSpellbook => BP.Get<BlueprintSpellbook>("e19484252c2f80e4a9439b3681b20f00");
            public static BlueprintSpellbook ClericSpellbook => BP.Get<BlueprintSpellbook>("4673d19a0cf2fab4f885cc4d1353da33");
            public static BlueprintSpellbook CrossbloodedSpellbook => BP.Get<BlueprintSpellbook>("cb0be5988031ebe4c947086a1170eacc");
            public static BlueprintSpellbook CrusaderSpellbook => BP.Get<BlueprintSpellbook>("673d39f7da699aa408cdda6282e7dcc0");
            public static BlueprintSpellbook DemonSpellbook => BP.Get<BlueprintSpellbook>("e3daa889c72982e45a026f62cc84937d");
            public static BlueprintSpellbook DragonAzataSpellbook => BP.Get<BlueprintSpellbook>("778f544f8ed404649a261dce9d514655");
            public static BlueprintSpellbook DruidSpellbook => BP.Get<BlueprintSpellbook>("fc78193f68150454483a7eea8b605b71");
            public static BlueprintSpellbook EldritchFontSpellbook => BP.Get<BlueprintSpellbook>("67b574abd845f3e42983ea6eb53a6f90");
            public static BlueprintSpellbook EldritchScionSpellbook => BP.Get<BlueprintSpellbook>("e2763fbfdb91920458c4686c3e7ed085");
            public static BlueprintSpellbook EldritchScoundrelSpellbook => BP.Get<BlueprintSpellbook>("4f96fb20f06b7494a8b2bd731a70af6c");
            public static BlueprintSpellbook EmpyrealSpellbook => BP.Get<BlueprintSpellbook>("cb9d75e5cb08572439a6b5cd562297a5");
            public static BlueprintSpellbook ExploiterWizardSpellbook => BP.Get<BlueprintSpellbook>("d09794fb6f93e4a40929a965b434070d");
            public static BlueprintSpellbook FeyspeakerSpellbook => BP.Get<BlueprintSpellbook>("c8c471f1f9889e1408347d3c7987b4f1");
            public static BlueprintSpellbook HunterSpellbook => BP.Get<BlueprintSpellbook>("885cd422aa357e2409146b38bb1fec51");
            public static BlueprintSpellbook InquisitorSpellbook => BP.Get<BlueprintSpellbook>("57fab75111f377248810ece84193a5a5");
            public static BlueprintSpellbook LeyLineGuardianWitchSpellbook => BP.Get<BlueprintSpellbook>("fb4e216b322d3924e9c7041016b46999");
            public static BlueprintSpellbook LichSkeletalBardMinorSpellbook => BP.Get<BlueprintSpellbook>("d731dfb39ea26754c89b58d0969ea9e0");
            public static BlueprintSpellbook LichSkeletalClericSpellbookFeature => BP.Get<BlueprintSpellbook>("9043af050a2fee641825e23d9c9a251a");
            public static BlueprintSpellbook LichSkeletalInquisitorMinorSpellbook => BP.Get<BlueprintSpellbook>("d71cb34f8a66a2c479ba8289a1683112");
            public static BlueprintSpellbook LichSkeletalMagusSpellbookMinor => BP.Get<BlueprintSpellbook>("c9ff1f4b3b26dcb47ba75b218ccadd23");
            public static BlueprintSpellbook LichSpellbook => BP.Get<BlueprintSpellbook>("08a80074263809c4b9616aac05af90ae");
            public static BlueprintSpellbook MagusSpellbook => BP.Get<BlueprintSpellbook>("5d8d04e76dff6c5439de99af0d57be63");
            public static BlueprintSpellbook MasterSpySpellbook => BP.Get<BlueprintSpellbook>("12bfcf91d5416b047a2a9110ff8968c5");
            public static BlueprintSpellbook MonsterMythicSpellbook => BP.Get<BlueprintSpellbook>("7425972fc2e2f294a8f8b56568337d00");
            public static BlueprintSpellbook NatureMageSpellbook => BP.Get<BlueprintSpellbook>("3ed7e38dc8134af28e1a2b105f74fb7b");
            public static BlueprintSpellbook OracleSpellbook => BP.Get<BlueprintSpellbook>("6c03364712b415941a98f74522a81273");
            public static BlueprintSpellbook PaladinSpellbook => BP.Get<BlueprintSpellbook>("bce4989b070ce924b986bf346f59e885");
            public static BlueprintSpellbook RangerSpellbook => BP.Get<BlueprintSpellbook>("762858a4a28eaaf43aa00f50441d7027");
            public static BlueprintSpellbook SageSpellbook => BP.Get<BlueprintSpellbook>("cc2052732997b654e93eac268a39a0a9");
            public static BlueprintSpellbook ShamanSpellbook => BP.Get<BlueprintSpellbook>("44f16931dabdff643bfe2a48138e769f");
            public static BlueprintSpellbook SkaldSpellbook => BP.Get<BlueprintSpellbook>("8f159d2f22ced074ea32995eb5a446a0");
            public static BlueprintSpellbook SorcererSpellbook => BP.Get<BlueprintSpellbook>("b3db3766a4b605040b366265e2af0e50");
            public static BlueprintSpellbook SwordSaintSpellbook => BP.Get<BlueprintSpellbook>("682545e11e5306c45b14ca78bcbe3e62");
            public static BlueprintSpellbook ThassilonianAbjurationSpellbook => BP.Get<BlueprintSpellbook>("58b15cc36ceda8942a7a29aafa755452");
            public static BlueprintSpellbook ThassilonianConjurationSpellbook => BP.Get<BlueprintSpellbook>("cbc30bcc7b8adec48a53a6540f5596ae");
            public static BlueprintSpellbook ThassilonianEnchantmentSpellbook => BP.Get<BlueprintSpellbook>("9e4b96d7b02f8c8498964aeee6eaef9b");
            public static BlueprintSpellbook ThassilonianEvocationSpellbook => BP.Get<BlueprintSpellbook>("05b105ddee654db4fb1547ba48ffa160");
            public static BlueprintSpellbook ThassilonianIllusionSpellbook => BP.Get<BlueprintSpellbook>("74b87962a97d56c4583979216631eb4c");
            public static BlueprintSpellbook ThassilonianNecromancySpellbook => BP.Get<BlueprintSpellbook>("97cd3941ce333ce46ae09436287ed699");
            public static BlueprintSpellbook ThassilonianTransmutationSpellbook => BP.Get<BlueprintSpellbook>("5785f40e7b1bfc94ea078e7156aa9711");
            public static BlueprintSpellbook TricksterSpellbook => BP.Get<BlueprintSpellbook>("2ff51e0531ed8e545ab4cb35c32d40f4");
            public static BlueprintSpellbook TricksterWizardSpellbook => BP.Get<BlueprintSpellbook>("bbe483b903854104a11606412803f214");
            public static BlueprintSpellbook UnletteredArcanistSpellbook => BP.Get<BlueprintSpellbook>("b242f991107095d41ae129f46ed5d97a");
            public static BlueprintSpellbook WarpriestSpellbook => BP.Get<BlueprintSpellbook>("7d7d51be2948d2544b3c2e1596fd7603");
            public static BlueprintSpellbook WitchSpellbook => BP.Get<BlueprintSpellbook>("dd04f9239f655ea438976742728e4909");
            public static BlueprintSpellbook WizardSpellbook => BP.Get<BlueprintSpellbook>("5a38c9ac8607890409fcb8f6342da6f4");

            public static List<BlueprintSpellbook> AllSpellbooks = new List<BlueprintSpellbook> {
                AccursedWitchSpellbook,
                AeonSpellbook,
                AlchemistSpellbook,
                AngelfireApostleSpellbook,
                AngelSpellbook,
                ArcanistSpellbook,
                ArmagsBladeSpellbook,
                AzataSpellbook,
                BardSpellbook,
                BloodragerSpellbook,
                ClericSpellbook,
                CrossbloodedSpellbook,
                CrusaderSpellbook,
                DemonSpellbook,
                DragonAzataSpellbook,
                DruidSpellbook,
                EldritchFontSpellbook,
                EldritchScionSpellbook,
                EldritchScoundrelSpellbook,
                EmpyrealSpellbook,
                ExploiterWizardSpellbook,
                FeyspeakerSpellbook,
                HunterSpellbook,
                InquisitorSpellbook,
                LeyLineGuardianWitchSpellbook,
                LichSkeletalBardMinorSpellbook,
                LichSkeletalClericSpellbookFeature,
                LichSkeletalInquisitorMinorSpellbook,
                LichSkeletalMagusSpellbookMinor,
                LichSpellbook,
                MagusSpellbook,
                MasterSpySpellbook,
                MonsterMythicSpellbook,
                NatureMageSpellbook,
                OracleSpellbook,
                PaladinSpellbook,
                RangerSpellbook,
                SageSpellbook,
                ShamanSpellbook,
                SkaldSpellbook,
                SorcererSpellbook,
                SwordSaintSpellbook,
                ThassilonianAbjurationSpellbook,
                ThassilonianConjurationSpellbook,
                ThassilonianEnchantmentSpellbook,
                ThassilonianEvocationSpellbook,
                ThassilonianIllusionSpellbook,
                ThassilonianNecromancySpellbook,
                ThassilonianTransmutationSpellbook,
                TricksterSpellbook,
                TricksterWizardSpellbook,
                UnletteredArcanistSpellbook,
                WarpriestSpellbook,
                WitchSpellbook,
                WizardSpellbook,
            };
        }
        public static class SpellList {
            public static BlueprintSpellList AeonSpellList => BP.Get<BlueprintSpellList>("24b0c796f723a144e9891b6c4794c595");
            public static BlueprintSpellList AeonSpellMythicList => BP.Get<BlueprintSpellList>("ca8c6024bd2519f4b97162a3ad286920");
            public static BlueprintSpellList AirDomainSpellList => BP.Get<BlueprintSpellList>("9678d121f669f864d9da5dabf1ca1ce0");
            public static BlueprintSpellList AlchemistSpellList => BP.Get<BlueprintSpellList>("f60d0cd93edc65c42ad31e34a905fb2f");
            public static BlueprintSpellList AngelClericSpelllist => BP.Get<BlueprintSpellList>("c074062863fbc1e4bab02f9e6e4eb78d");
            public static BlueprintSpellList AngelMythicSpelllist => BP.Get<BlueprintSpellList>("deaffb4218ccf2f419ffd6e41603131a");
            public static BlueprintSpellList AnimalDomainSpellList => BP.Get<BlueprintSpellList>("d0ccda70fddc0e346a227068502771c1");
            public static BlueprintSpellList ArmagsBladeSpellList => BP.Get<BlueprintSpellList>("3ea72a95cce88b4449a917ad0a0f36da");
            public static BlueprintSpellList ArtificeDomainSpellList => BP.Get<BlueprintSpellList>("5ef652f325d21414d8565fcfb19d8177");
            public static BlueprintSpellList AzataMythicSpelllist => BP.Get<BlueprintSpellList>("db216faa0ff2b984399e7495755c7111");
            public static BlueprintSpellList AzataMythicSpellsSpelllist => BP.Get<BlueprintSpellList>("10c634d2b386d8d41b18a889adb8cd49");
            public static BlueprintSpellList BardSpellList => BP.Get<BlueprintSpellList>("25a5013493bdcf74bb2424532214d0c8");
            public static BlueprintSpellList BattleSpiritSpellList => BP.Get<BlueprintSpellList>("e788846aaf449404db11e51182174be8");
            public static BlueprintSpellList BloodragerSpellList => BP.Get<BlueprintSpellList>("98c05aeff6e3d384f8aec6d584973642");
            public static BlueprintSpellList BonesSpiritSpellList => BP.Get<BlueprintSpellList>("24dc5eb552a9e2c4cb9a17c355a80d2e");
            public static BlueprintSpellList ChaosDomainSpellList => BP.Get<BlueprintSpellList>("2ff9831eb262758449287f820108428d");
            public static BlueprintSpellList CharmDomainSpellList => BP.Get<BlueprintSpellList>("31c742d02fc33204cad4e02dddf028dd");
            public static BlueprintSpellList ClericSpellList => BP.Get<BlueprintSpellList>("8443ce803d2d31347897a3d85cc32f53");
            public static BlueprintSpellList CommunityDomainSpellList => BP.Get<BlueprintSpellList>("75576ed8cab010644a11f9ecd512a7f9");
            public static BlueprintSpellList DarknessDomainSpellList => BP.Get<BlueprintSpellList>("fa79e8d1fe20b0e43bf3ebca4cef93b9");
            public static BlueprintSpellList DeathDomainSpellList => BP.Get<BlueprintSpellList>("436986e90d1e81b45a1accb6fa7261f0");
            public static BlueprintSpellList DemonSpelllist => BP.Get<BlueprintSpellList>("abb1991bf6e996348bb743471ee7e1c1");
            public static BlueprintSpellList DemonUsualSpelllist => BP.Get<BlueprintSpellList>("78721d556676f264da947bfe263b1da0");
            public static BlueprintSpellList DestructionDomainSpellList => BP.Get<BlueprintSpellList>("6f9fe425726026d4f9c28c32d5d03481");
            public static BlueprintSpellList DruidSpellList => BP.Get<BlueprintSpellList>("bad8638d40639d04fa2f80a1cac67d6b");
            public static BlueprintSpellList EarthDomainSpellList => BP.Get<BlueprintSpellList>("df3bc5bda7deb9d46b0f177db3bb7876");
            public static BlueprintSpellList EvilDomainSpellList => BP.Get<BlueprintSpellList>("81bff1165d9468a44b2f815f7c26a373");
            public static BlueprintSpellList FeyspeakerSpelllist => BP.Get<BlueprintSpellList>("640b4c89527334e45b19d884dd82e500");
            public static BlueprintSpellList FireDomainSpellList => BP.Get<BlueprintSpellList>("d8f30625d1b1f9d41a24446cbf7ac52e");
            public static BlueprintSpellList FlamesSpiritSpellList => BP.Get<BlueprintSpellList>("659fbc54fc519b44dacacc78e7d46dec");
            public static BlueprintSpellList FrostSpiritSpellList => BP.Get<BlueprintSpellList>("bbae401660bbad94c865d71029d8439e");
            public static BlueprintSpellList GloryDomainSpellList => BP.Get<BlueprintSpellList>("7b3506924ed8354419b7829736ab2c7e");
            public static BlueprintSpellList GoodDomainSpellList => BP.Get<BlueprintSpellList>("dc242eb60eed94a4eb0640d773780090");
            public static BlueprintSpellList HealingDomainSpellList => BP.Get<BlueprintSpellList>("033b2b6a8899be844ae8aa91d4dab477");
            public static BlueprintSpellList HunterSpelllist => BP.Get<BlueprintSpellList>("d090b791bfe381740b98ed4ff909b1cf");
            public static BlueprintSpellList InquisitorSpellList => BP.Get<BlueprintSpellList>("57c894665b7895c499b3dce058c284b3");
            public static BlueprintSpellList KnowledgeDomainSpellList => BP.Get<BlueprintSpellList>("384627980c2a60a43800f14029fbb8a7");
            public static BlueprintSpellList LawDomainSpellList => BP.Get<BlueprintSpellList>("57b0bbdc1114ee846945f1808b13cff7");
            public static BlueprintSpellList LiberationDomainSpellList => BP.Get<BlueprintSpellList>("73406d3b9f6290e499c5fa3951a24234");
            public static BlueprintSpellList LichMythicSpelllist => BP.Get<BlueprintSpellList>("a06bcb035c214ad4db605491be9da13d");
            public static BlueprintSpellList LichSkeletalIBardMinorSpelllist => BP.Get<BlueprintSpellList>("0fbd00235f7042841ba1a3fcae0dbc58");
            public static BlueprintSpellList LichSkeletalInquisitorMinorSpelllist => BP.Get<BlueprintSpellList>("c7970aa2f3c94e245942369d348c0a1f");
            public static BlueprintSpellList LichWizardSpelllist => BP.Get<BlueprintSpellList>("7d5987082120bb943ac96cde7b2257ad");
            public static BlueprintSpellList LifeSpiritSpellList => BP.Get<BlueprintSpellList>("1e6ea0d1d642c8c43ab7e72dd8f607a9");
            public static BlueprintSpellList LuckDomainSpellList => BP.Get<BlueprintSpellList>("9e756552e9b05ce459feac658dd2d8fb");
            public static BlueprintSpellList MadnessDomainSpellList => BP.Get<BlueprintSpellList>("1d1638d47e7f8404baeed23bc35ec2f2");
            public static BlueprintSpellList MagicDomainSpellList => BP.Get<BlueprintSpellList>("f997258a28a9e5f4192d973433edec5f");
            public static BlueprintSpellList MagusSpellList => BP.Get<BlueprintSpellList>("4d72e1e7bd6bc4f4caaea7aa43a14639");
            public static BlueprintSpellList MonsterEmptySpellllist => BP.Get<BlueprintSpellList>("9e4658592e5f66146a6826120e21ed26");
            public static BlueprintSpellList NatureSpiritSpellList => BP.Get<BlueprintSpellList>("810e2a7009ed3a84c81b398b2763e7a8");
            public static BlueprintSpellList NobilityDomainSpellList => BP.Get<BlueprintSpellList>("3de1e283971828f4896a4140acd3c84c");
            public static BlueprintSpellList PaladinSpellList => BP.Get<BlueprintSpellList>("9f5be2f7ea64fe04eb40878347b147bc");
            public static BlueprintSpellList PlantDomainSpellList => BP.Get<BlueprintSpellList>("bd7b088a54b79434f90ed53551ca2189");
            public static BlueprintSpellList ProtectionDomainSpellList => BP.Get<BlueprintSpellList>("93228f4df23d2d448a0db59141af8aed");
            public static BlueprintSpellList RangerSpellList => BP.Get<BlueprintSpellList>("29f3c338532390546bc5347826a655c4");
            public static BlueprintSpellList ReposeDomainSpellList => BP.Get<BlueprintSpellList>("5376474a39713514ca2135d6f9584563");
            public static BlueprintSpellList RuneDomainSpellList => BP.Get<BlueprintSpellList>("30076fe3d5f4ef845a7bafb0be57fe44");
            public static BlueprintSpellList ShamanSpelllist => BP.Get<BlueprintSpellList>("c0c40e42f07ff104fa85492da464ac69");
            public static BlueprintSpellList SpiritWardenSpellList => BP.Get<BlueprintSpellList>("767db8f0d64e5b048a8cf4d2ddc10521");
            public static BlueprintSpellList StoneSpiritSpellList => BP.Get<BlueprintSpellList>("87a3e296757412e45910493e5fed1417");
            public static BlueprintSpellList StrengthDomainSpellList => BP.Get<BlueprintSpellList>("03db76fd27428004482f9314c334d1ab");
            public static BlueprintSpellList SunDomainSpellList => BP.Get<BlueprintSpellList>("600ffed45d0c3ec43a75dc76bb9377b6");
            public static BlueprintSpellList ThassilonianAbjurationSpellList => BP.Get<BlueprintSpellList>("280dd5167ccafe449a33fbe93c7a875e");
            public static BlueprintSpellList ThassilonianConjurationSpellList => BP.Get<BlueprintSpellList>("5b154578f228c174bac546b6c29886ce");
            public static BlueprintSpellList ThassilonianEnchantmentSpellList => BP.Get<BlueprintSpellList>("ac551db78c1baa34eb8edca088be13cb");
            public static BlueprintSpellList ThassilonianEvocationSpellList => BP.Get<BlueprintSpellList>("17c0bfe5b7c8ac3449da655cdcaed4e7");
            public static BlueprintSpellList ThassilonianIllusionSpellList => BP.Get<BlueprintSpellList>("c311aed33deb7a346ab715baef4a0572");
            public static BlueprintSpellList ThassilonianNecromancySpellList => BP.Get<BlueprintSpellList>("5c08349132cb6b04181797f58ccf38ae");
            public static BlueprintSpellList ThassilonianTransmutationSpellList => BP.Get<BlueprintSpellList>("f3a8f76b1d030a64084355ba3eea369a");
            public static BlueprintSpellList TravelDomainSpellList => BP.Get<BlueprintSpellList>("ab90308db82342f47bf0d636fe941434");
            public static BlueprintSpellList TrickeryDomainSpellList => BP.Get<BlueprintSpellList>("2c09ae283ea3e41408665c46fcf0303f");
            public static BlueprintSpellList TricksterSpelllist => BP.Get<BlueprintSpellList>("40f8cfe98ca4ebd43894267dbd3fc3ae");
            public static BlueprintSpellList TricksterSpelllistMythic => BP.Get<BlueprintSpellList>("7a5ea54564c7d494794f34d0f5a9abb3");
            public static BlueprintSpellList WarDomainSpellList => BP.Get<BlueprintSpellList>("e3c54da90a2b54b4a975a80b5d39c361");
            public static BlueprintSpellList WarpriestSpelllist => BP.Get<BlueprintSpellList>("c5a1b8df32914d74c9b44052ba3e686a");
            public static BlueprintSpellList WaterDomainSpellList => BP.Get<BlueprintSpellList>("35e15cd1b353e2d47b507c445d2f8c6f");
            public static BlueprintSpellList WavesSpiritSpellList => BP.Get<BlueprintSpellList>("190ebde2d9d80c94783bcb73b9106d99");
            public static BlueprintSpellList WeatherDomainSpellList => BP.Get<BlueprintSpellList>("eba577470b8ee8443bb4552433451990");
            public static BlueprintSpellList WindSpiritSpellList => BP.Get<BlueprintSpellList>("0bf6f90fdcb864b4486344100391b478");
            public static BlueprintSpellList WitchSpellList => BP.Get<BlueprintSpellList>("e17df9977b879b64e8a8cbb4b3569f19");
            public static BlueprintSpellList WizardAbjurationSpellList => BP.Get<BlueprintSpellList>("c7a55e475659a944f9229d89c4dc3a8e");
            public static BlueprintSpellList WizardConjurationSpellList => BP.Get<BlueprintSpellList>("69a6eba12bc77ea4191f573d63c9df12");
            public static BlueprintSpellList WizardDivinationSpellList => BP.Get<BlueprintSpellList>("d234e68b3d34d124a9a2550fdc3de9eb");
            public static BlueprintSpellList WizardEnchantmentSpellList => BP.Get<BlueprintSpellList>("c72836bb669f0c04680c01d88d49bb0c");
            public static BlueprintSpellList WizardEvocationSpellList => BP.Get<BlueprintSpellList>("79e731172a2dc1f4d92ba229c6216502");
            public static BlueprintSpellList WizardIllusionSpellList => BP.Get<BlueprintSpellList>("d74e55204daa9b14993b2e51ae861501");
            public static BlueprintSpellList WizardNecromancySpellList => BP.Get<BlueprintSpellList>("5fe3acb6f439db9438db7d396f02c75c");
            public static BlueprintSpellList WizardSpellList => BP.Get<BlueprintSpellList>("ba0401fdeb4062f40a7aa95b6f07fe89");
            public static BlueprintSpellList WizardTransmutationSpellList => BP.Get<BlueprintSpellList>("becbcfeca9624b6469319209c2a6b7f1");

            public static BlueprintSpellList[] AllSpellLists => new BlueprintSpellList[] {
                AeonSpellList,
                AeonSpellMythicList,
                AirDomainSpellList,
                AlchemistSpellList,
                AngelClericSpelllist,
                AngelMythicSpelllist,
                AnimalDomainSpellList,
                ArmagsBladeSpellList,
                ArtificeDomainSpellList,
                AzataMythicSpelllist,
                AzataMythicSpellsSpelllist,
                BardSpellList,
                BattleSpiritSpellList,
                BloodragerSpellList,
                BonesSpiritSpellList,
                ChaosDomainSpellList,
                CharmDomainSpellList,
                ClericSpellList,
                CommunityDomainSpellList,
                DarknessDomainSpellList,
                DeathDomainSpellList,
                DemonSpelllist,
                DemonUsualSpelllist,
                DestructionDomainSpellList,
                DruidSpellList,
                EarthDomainSpellList,
                EvilDomainSpellList,
                FeyspeakerSpelllist,
                FireDomainSpellList,
                FlamesSpiritSpellList,
                FrostSpiritSpellList,
                GloryDomainSpellList,
                GoodDomainSpellList,
                HealingDomainSpellList,
                HunterSpelllist,
                InquisitorSpellList,
                KnowledgeDomainSpellList,
                LawDomainSpellList,
                LiberationDomainSpellList,
                LichMythicSpelllist,
                LichSkeletalIBardMinorSpelllist,
                LichSkeletalInquisitorMinorSpelllist,
                LichWizardSpelllist,
                LifeSpiritSpellList,
                LuckDomainSpellList,
                MadnessDomainSpellList,
                MagicDomainSpellList,
                MagusSpellList,
                MonsterEmptySpellllist,
                NatureSpiritSpellList,
                NobilityDomainSpellList,
                PaladinSpellList,
                PlantDomainSpellList,
                ProtectionDomainSpellList,
                RangerSpellList,
                ReposeDomainSpellList,
                RuneDomainSpellList,
                ShamanSpelllist,
                SpiritWardenSpellList,
                StoneSpiritSpellList,
                StrengthDomainSpellList,
                SunDomainSpellList,
                ThassilonianAbjurationSpellList,
                ThassilonianConjurationSpellList,
                ThassilonianEnchantmentSpellList,
                ThassilonianEvocationSpellList,
                ThassilonianIllusionSpellList,
                ThassilonianNecromancySpellList,
                ThassilonianTransmutationSpellList,
                TravelDomainSpellList,
                TrickeryDomainSpellList,
                TricksterSpelllist,
                TricksterSpelllistMythic,
                WarDomainSpellList,
                WarpriestSpelllist,
                WaterDomainSpellList,
                WavesSpiritSpellList,
                WeatherDomainSpellList,
                WindSpiritSpellList,
                WitchSpellList,
                WizardAbjurationSpellList,
                WizardConjurationSpellList,
                WizardDivinationSpellList,
                WizardEnchantmentSpellList,
                WizardEvocationSpellList,
                WizardIllusionSpellList,
                WizardNecromancySpellList,
                WizardSpellList,
                WizardTransmutationSpellList,
            };
        }
        public static class SpellCastingClasses {
            public static BlueprintCharacterClass ArcanistClass => BP.Get<BlueprintCharacterClass>("52dbfd8505e22f84fad8d702611f60b7");
            public static BlueprintCharacterClass BardClass => BP.Get<BlueprintCharacterClass>("772c83a25e2268e448e841dcd548235f");
            public static BlueprintCharacterClass BloodragerClass => BP.Get<BlueprintCharacterClass>("d77e67a814d686842802c9cfd8ef8499");
            public static BlueprintCharacterClass ClericClass => BP.Get<BlueprintCharacterClass>("67819271767a9dd4fbfd4ae700befea0");
            public static BlueprintCharacterClass DruidClass => BP.Get<BlueprintCharacterClass>("610d836f3a3a9ed42a4349b62f002e96");
            public static BlueprintCharacterClass HunterClass => BP.Get<BlueprintCharacterClass>("34ecd1b5e1b90b9498795791b0855239");
            public static BlueprintCharacterClass InquisitorClass => BP.Get<BlueprintCharacterClass>("f1a70d9e1b0b41e49874e1fa9052a1ce");
            public static BlueprintCharacterClass MagusClass => BP.Get<BlueprintCharacterClass>("45a4607686d96a1498891b3286121780");
            public static BlueprintCharacterClass OracleClass => BP.Get<BlueprintCharacterClass>("20ce9bf8af32bee4c8557a045ab499b1");
            public static BlueprintCharacterClass PaladinClass => BP.Get<BlueprintCharacterClass>("bfa11238e7ae3544bbeb4d0b92e897ec");
            public static BlueprintCharacterClass RangerClass => BP.Get<BlueprintCharacterClass>("cda0615668a6df14eb36ba19ee881af6");
            public static BlueprintCharacterClass RogueClass => BP.Get<BlueprintCharacterClass>("299aa766dee3cbf4790da4efb8c72484");
            public static BlueprintCharacterClass ShamanClass => BP.Get<BlueprintCharacterClass>("145f1d3d360a7ad48bd95d392c81b38e");
            public static BlueprintCharacterClass SkaldClass => BP.Get<BlueprintCharacterClass>("6afa347d804838b48bda16acb0573dc0");
            public static BlueprintCharacterClass SorcererClass => BP.Get<BlueprintCharacterClass>("b3a505fb61437dc4097f43c3f8f9a4cf");
            public static BlueprintCharacterClass WarpriestClass => BP.Get<BlueprintCharacterClass>("30b5e47d47a0e37438cc5a80c96cfb99");
            public static BlueprintCharacterClass WitchClass => BP.Get<BlueprintCharacterClass>("1b9873f1e7bfe5449bc84d03e9c8e3cc");
            public static BlueprintCharacterClass WizardClass => BP.Get<BlueprintCharacterClass>("ba34257984f4c41408ce1dc2004e342e");

            public static BlueprintCharacterClass[] AllClasses => new BlueprintCharacterClass[] {
                ArcanistClass,
                BardClass,
                BloodragerClass,
                ClericClass,
                DruidClass,
                HunterClass,
                InquisitorClass,
                MagusClass,
                OracleClass,
                PaladinClass,
                RangerClass,
                RogueClass,
                ShamanClass,
                SkaldClass,
                SorcererClass,
                WarpriestClass,
                WitchClass,
                WizardClass,
            };
        }
    }
}