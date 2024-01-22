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
        public bool Enabled = true;
        public List<CharacterState> States = [];
        public List<SpecialTerritory> SpecialTerritories = [];
        public List<Biome> Biomes = [];
        public List<uint> Territories = [];
        public List<uint> Weathers = [];
        public List<long> Houses = [];
        public List<uint> Emotes = [];
        public List<Job> Jobs = [];
        public List<string> SelectedPresets = [];
        public List<ETime> Times = [];
    }
}
