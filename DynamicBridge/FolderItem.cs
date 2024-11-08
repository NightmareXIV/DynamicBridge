using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicBridge;
public struct FolderItem
{
    public Action Action;

    public FolderItem(Action action) : this()
    {
        Action = action ?? throw new ArgumentNullException(nameof(action));
    }
}
