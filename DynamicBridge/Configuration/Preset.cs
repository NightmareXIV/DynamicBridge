using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicBridge.Configuration
{
    [Serializable]
    public class Preset
    {
        [NonSerialized] internal string GUID = Guid.NewGuid().ToString();
        public string Name = "";
        public List<string> Glamourer = [];
        public List<string> ComplexGlamourer = [];
        public List<string> Honorific = [];
        public List<string> Palette = [];
        public List<string> Customize = [];
        public bool IsStatic = false;
        public bool IsStaticCategory = false;
    }
}
