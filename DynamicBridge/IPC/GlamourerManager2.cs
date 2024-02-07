using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicBridge.IPC;
public static class GlamourerManager2
{
    public static void ApplyByGuid(Guid guid)
    {
        Svc.Commands.ProcessCommand($"/glamour apply {guid}|<me>");
    }

    public static void Revert()
    {
        Svc.Commands.ProcessCommand("/glamour revert <me>");
    }

    public static void RevertToAutomation()
    {
        Svc.Commands.ProcessCommand("/glamour reapplyautomation <me>");
    }
}
