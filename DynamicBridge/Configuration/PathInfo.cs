using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicBridge.Configuration;
public record struct PathInfo
{
    public string Name;
    public int Indentation;

    public PathInfo(string name) : this()
    {
        this.Name = name;
        this.Indentation = 0;
    }

    public PathInfo(string name, int indentation)
    {
        this.Name = name ?? throw new ArgumentNullException(nameof(name));
        this.Indentation = indentation;
    }
}
