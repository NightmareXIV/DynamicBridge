using ECommons.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicBridge.Configuration
{
    [Serializable]
    public class Config : IEzConfig
    {
        public HashSet<ulong> Blacklist = [];
        public List<HousingRecord> Houses = [];
        public List<ComplexGlamourerEntry> ComplexGlamourerEntries = [];
        public List<Preset> GlobalPresets = [];
        public List<PresetFolder> GlobalPresetsFolders = [];
        public Dictionary<ulong, Profile> Profiles = [];
        public bool Debug = false;
        public bool EnableGlamourer = true;
        public bool EnableCustomize = true;
        public bool EnableHonorific = true;
        [NonSerialized] internal bool EnablePalette = false;
        public GlamourerNoRuleBehavior GlamNoRuleBehaviour = GlamourerNoRuleBehavior.RevertToNormal;
        public bool ManageGlamourerAutomation = false;
    }

    public enum GlamourerNoRuleBehavior
    {
        RevertToNormal,
        RevertToAutomation,
        StoreRestore
    }
}
