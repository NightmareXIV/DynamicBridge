using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicBridge.Configuration;
[Serializable]
public class ComplexGlamourerEntry
{
    [NonSerialized] internal string GUID = Guid.NewGuid().ToString();
    public string Name = "";
    public List<string> Designs = [];
}
