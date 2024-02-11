using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicBridge.Configuration
{
    public class Profile
    {
        [NonSerialized] internal string GUID = Guid.NewGuid().ToString();
        public string Name = "";
        public List<ApplyRule> Rules = [];
        public List<Preset> Presets = [];
        public List<PresetFolder> PresetsFolders = [];
        public string ForcedPreset = null;

        public IEnumerable<Preset> GetPresetsUnion()
        {
            foreach(var x in GetPresetsListUnion())
            {
                foreach(var z in x) yield return z;
            }
        }

        public IEnumerable<List<Preset>> GetPresetsListUnion()
        {
            yield return Presets;
            foreach (var x in PresetsFolders) yield return x.Presets;
            yield return C.GlobalPresets;
            foreach (var x in C.GlobalPresetsFolders) yield return x.Presets;
        }
    }
}
