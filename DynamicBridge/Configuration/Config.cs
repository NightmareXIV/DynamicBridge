using ECommons.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicBridge.Configuration
{
    public class Config : IEzConfig
    {
        public HashSet<ulong> Blacklist = [];
        public List<HousingRecord> Houses = [];
        public List<ComplexGlamourerEntry> ComplexGlamourerEntries = [];
        public Dictionary<ulong, Profile> Profiles = [];
        public bool Debug = false;
        public bool EnableGlamourer = true;
        public bool EnableCustomize = true;
        public bool EnableHonorific = true;
        public bool EnablePalette = true;
        public bool GlamourerResetBeforeApply = false;
    }
}
