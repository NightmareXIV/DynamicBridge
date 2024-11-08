using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicBridge.Configuration;
public record struct GearsetEntry
{
    public int Id;
    public string Name;
    public int ClassJob;

    public GearsetEntry(int id, string name, int classJob) : this()
    {
        Id = id;
        Name = name;
        ClassJob = classJob;
    }

    public override readonly string ToString()
    {
        return $"{Id + 1}: {Name}";
    }
}
