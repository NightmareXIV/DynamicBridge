using DynamicBridge.Core;
using ECommons.ExcelServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicBridge.Configuration
{
    [Serializable]
    public class ApplyRule
    {
        [NonSerialized] internal string GUID = Guid.NewGuid().ToString();
        public int StickyRandom = 0;
        public bool Enabled = true;

        public List<CharacterState> States = [];
        public List<SpecialTerritory> SpecialTerritories = [];
        public List<Biome> Biomes = [];
        public List<uint> Territories = [];
        public List<uint> Weathers = [];
        public List<long> Houses = [];
        public List<uint> Emotes = [];
        public List<Job> Jobs = [];
        public List<ETime> Times = [];
        public List<uint> Worlds = [];
        public List<int> Gearsets = [];

        public List<string> SelectedPresets = [];
        public bool Passthrough = false;
        public NotConditions Not = new();

        [Serializable]
        public class NotConditions
        {
            public List<CharacterState> States = [];
            public List<SpecialTerritory> SpecialTerritories = [];
            public List<Biome> Biomes = [];
            public List<uint> Territories = [];
            public List<uint> Weathers = [];
            public List<long> Houses = [];
            public List<uint> Emotes = [];
            public List<Job> Jobs = [];
            public List<ETime> Times = [];
            public List<uint> Worlds = [];
            public List<int> Gearsets = [];
        }
    }
}
