using ECommons.GameHelpers;
using ECommons.Reflection;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Windows.Forms.AxHost;

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

    public static bool GetAutomationStatusForChara()
    {
        try
        {
            if (DalamudReflector.TryGetDalamudPlugin("Glamourer", out var plugin, out var context, true, true))
            {
                var adm = plugin.GetFoP("_services").Call<System.Collections.IEnumerable>(context.Assemblies, "GetService", ["Glamourer.Automation.AutoDesignManager"], []);
                foreach(var profile in adm)
                {
                    if (profile.GetFoP<bool>("Enabled"))
                    {
                        foreach(var identifier in profile.GetFoP<System.Collections.IEnumerable>("Identifiers"))
                        {
                            if (identifier.GetFoP("PlayerName").ToString().EqualsIgnoreCase(Player.Name))
                            {
                                return true;
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            InternalLog.Warning(ex.ToString());
        }
        return false;
    }

    public static string GetPathForDesignByGuid(Guid guid)
    {
        try
        {
            if (DalamudReflector.TryGetDalamudPlugin("Glamourer", out var plugin, out var context, true, true))
            {
                var manager = plugin.GetFoP("_services").Call(context.Assemblies, "GetService", ["Glamourer.Designs.DesignManager"], []);
                var designList = manager.GetFoP<System.Collections.IList>("Designs");
                foreach(var design in designList)
                {
                    if(design.GetFoP<Guid>("Identifier") == guid)
                    {
                        var dfs = plugin.GetFoP("_services").Call(context.Assemblies, "GetService", ["Glamourer.Designs.DesignFileSystem"], []);
                        object[] findLeafArray = [design, null];
                        if (dfs.Call<bool>("FindLeaf", findLeafArray, false))
                        {
                            var ret = findLeafArray[1].Call<string>("FullName", []);
                            //PluginLog.Information($"Path for {guid} is {ret}");
                            return ret;
                        }
                        return null;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            InternalLog.Warning(ex.ToString());
        }
        return null;
    }
}
