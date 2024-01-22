using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicBridge
{
    [Serializable]
    public class HousingRecord
    {
        [NonSerialized] internal string GUID = Guid.NewGuid().ToString();
        public long ID;
        public string Name;
    }
}
