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
        public List<PresetFolder> PresetFolders = [];
        public string ForcedPreset = null;

        public IEnumerable<Preset> GetPresetsUnion()
        {
            foreach(var x in Presets) yield return x;
            foreach(var x in PresetFolders)
            {
                foreach(var z in x.Presets) yield return z;
            }
        }

        public IEnumerable<List<Preset>> GetPresetsListUnion()
        {
            yield return Presets;
            foreach (var x in PresetFolders) yield return x.Presets;
        }
    }
}
