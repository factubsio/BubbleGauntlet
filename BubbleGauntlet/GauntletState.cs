﻿using Kingmaker;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem;
using Kingmaker.Enums.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.Utility;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static BubbleGauntlet.GauntletController;

namespace BubbleGauntlet {

    public class EncounterState {
        [JsonProperty]
        public int Remaining = 0;
    }


    public class Gauntlet : EntityPart<Player> {
        [JsonProperty]
        public FloorState Floor = new();
    }

    public class FloorState {
        [JsonProperty]
        public DamageEnergyType DamageTheme;


        [JsonProperty]
        public EncounterType[] Encounters = new EncounterType[10];
        [JsonProperty]
        public bool Shopping = false;
        [JsonProperty]
        public int TotalEncounters => 10;
        [JsonProperty]
        public int EncountersRemaining = 0;
        [JsonProperty]
        public int ActiveEncounter = 0;

        //[JsonIgnore]
        //public int EncountersCompleted => TotalEncounters - EncountersRemaining;

        [JsonProperty]
        public int Level = 0;
        [JsonProperty]
        public Dictionary<EncounterType, EncounterState> Events = new() {
            { EncounterType.Fight, new EncounterState { Remaining = 8 } },
            { EncounterType.EliteFight, new EncounterState { Remaining = 3 } },
            { EncounterType.Shop, new EncounterState { Remaining = 2 } },
            { EncounterType.Rest, new EncounterState { Remaining = 4 } },
        };

        public void Descend() {
            Level += 1;
            DamageTheme = FUN.EnergyTypes.Random();
            foreach (var kv in Events)
                kv.Value.Remaining = EncounterTemplate.Get(kv.Key).Count;
            EncountersRemaining = TotalEncounters;
            ActiveEncounter = 0;
            for (int i = 0; i < Encounters.Length; i++) {
                Encounters[i] = EncounterType.None;
            }
            GauntletController.EnterFloor();
        }

        public FloorState() { }

        [JsonIgnore]
        public static readonly Condition NoEncounters = new DynamicCondition(() => GauntletController.Floor.EncountersRemaining == 0);
        [JsonIgnore]
        public static readonly Condition HasEncounters = new DynamicCondition(() => GauntletController.Floor.EncountersRemaining > 0);
    }
}
