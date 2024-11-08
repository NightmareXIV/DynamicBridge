using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicBridge.Configuration;
public class MoodleInfo
{
    public Guid Guid;
    public bool Cancel = false;

    public MoodleInfo(Guid guid, bool cancel)
    {
        Guid = guid;
        Cancel = cancel;
    }
}
