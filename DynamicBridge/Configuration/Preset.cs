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
        internal string CensoredName => C.NoNames ? GUID : Name;
        public List<string> Glamourer = [];
        public List<string> ComplexGlamourer = [];
        public List<string> Honorific = [];
        public List<string> Customize = [];
        public List<string> Penumbra = [];
        public List<MoodleInfo> Moodles = [];
        public SpecialPenumbraAssignment PenumbraType = SpecialPenumbraAssignment.Use_Named_Collection;
        public bool IsStatic = false;
        public int StickyRandomG = 0;
        public int StickyRandomH = 0;
        public int StickyRandomC = 0;
        public int StickyRandomP = 0;
    }
}
