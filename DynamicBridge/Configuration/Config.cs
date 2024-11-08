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
        public bool Enable = true;
        public HashSet<ulong> Blacklist = [];
        public List<HousingRecord> Houses = [];
        public List<ComplexGlamourerEntry> ComplexGlamourerEntries = [];
        public Profile GlobalProfile = new() { Name = "Global profile" };
        [Obsolete] public Dictionary<ulong, Profile> Profiles = [];
        public List<Profile> ProfilesL = [];
        public bool Debug = false;
        public bool EnableGlamourer = true;
        public bool EnableCustomize = true;
        public bool EnableHonorific = true;
        public bool EnablePenumbra = true;
        public bool EnableMoodles = true;
        public GlamourerNoRuleBehavior GlamNoRuleBehaviour = GlamourerNoRuleBehavior.RevertToNormal;
        public bool RevertBeforeAutomationRestore = false;
        public bool RevertGlamourerBeforeApply = false;
        public bool ManageGlamourerAutomation = false;
        public bool AllowNegativeConditions = false;
        public bool GlamourerFullPath = false;
        public string LastVersion = "0";
        public ImGuiComboFlags ComboSize = ImGuiComboFlags.HeightLarge;
        public bool UpdateJobGSChange = true;
        public bool UpdateGearChange = false;
        public Dictionary<ulong, string> SeenCharacters = [];

        public bool Cond_State = true;
        public bool Cond_Biome = true;
        public bool Cond_Weather = true;
        public bool Cond_Time = true;
        public bool Cond_ZoneGroup = true;
        public bool Cond_Zone = true;
        public bool Cond_House = true;
        public bool Cond_Emote = true;
        public bool Cond_Job = true;
        public bool Cond_World = false;
        public bool Cond_Gearset = false;

        public Dictionary<ulong, List<GearsetEntry>> GearsetNameCacheCID = [];

        public string CensorSeed = Guid.NewGuid().ToString();
        public bool NoNames = false;
        public bool LesserCensor = false;
        public bool ShowTutorial = true;
        public bool UnifyJobs = true;
        public bool HonotificUnfiltered = false;
        public bool AutofillFromGlam = false;
    }

    public enum GlamourerNoRuleBehavior
    {
        RevertToNormal,
        RevertToAutomation,
        StoreRestore
    }
}
