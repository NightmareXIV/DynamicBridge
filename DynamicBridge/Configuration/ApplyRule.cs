using DynamicBridge.Core;
using ECommons.ExcelServices;

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
        public List<string> Players = [];
        public List<TimelineSegment> Precise_Times = [
            new TimelineSegment((float)0/24,(float)5/24,0),
            new TimelineSegment((float)5/24,(float)7/24,0),
            new TimelineSegment((float)7/24,(float)12/24,0),
            new TimelineSegment((float)12/24,(float)17/24,0),
            new TimelineSegment((float)17/24,(float)19/24,0),
            new TimelineSegment((float)19/24,(float)22/24,0),
            new TimelineSegment((float)22/24,(float)24/24,0)
            ];
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
            public List<string> Players = [];
        }
    }
}
