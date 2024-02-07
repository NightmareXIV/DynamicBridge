using ECommons.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicBridge.IPC;
public static class GlamourerReflector
{
    public static bool GetAutomationGlobalState()
    {
        try
        {
            if (DalamudReflector.TryGetDalamudPlugin("Glamourer", out var plugin, out var context, true, true))
            {
                var config = plugin.GetFoP("_services").Call(context.Assemblies, "GetService", ["Glamourer.Configuration"], []);
                return config.GetFoP<bool>("EnableAutoDesigns");
            }
        }
        catch (Exception ex)
        {
            ex.LogWarning();
        }
        return false;
    }
    public static void SetAutomationGlobalState(bool state)
    {
        try
        {
            if (DalamudReflector.TryGetDalamudPlugin("Glamourer", out var plugin, out var context, true, true))
            {
                var config = plugin.GetFoP("_services").Call(context.Assemblies, "GetService", ["Glamourer.Configuration"], []);
                config.SetFoP("EnableAutoDesigns", state);
            }
        }
        catch (Exception ex)
        {
            ex.LogWarning();
        }
    }
}
