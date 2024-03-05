using ECommons;
using ECommons.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Loader;
using System.Text;
using System.Threading.Tasks;

namespace DynamicBridge.IPC.Customize;
public static class CustomizePlusReflector
{
    public static object GetProfileManager()
    {
        try
        {
            if (DalamudReflector.TryGetDalamudPlugin("CustomizePlus", out var plugin, out var context, true, true))
            {
                var profileManager = plugin.GetFoP("_services").Call([plugin.GetType().Assembly], "GetService", ["CustomizePlus.Profiles.ProfileManager"], []);
                return profileManager;
            }
        }
        catch (Exception e)
        {
            e.LogInternal();
        }
        return null;
    }

    public static List<CustomizePlusProfile> GetProfiles()
    {
        var ret = new List<CustomizePlusProfile>();
        try
        {
            var profiles = (System.Collections.IList)GetProfileManager()?.GetFoP("Profiles");
            if (profiles == null) return ret;
            foreach (var x in profiles)
            {
                if (x.GetFoP<int>("ProfileType") != 0) continue;
                ret.Add((
                    x.GetFoP("Name").GetFoP<string>("Text"),
                    x.GetFoP("CharacterName").GetFoP<string>("Text"),
                    x.GetFoP<bool>("Enabled"),
                    x.GetFoP<Guid>("UniqueId")
                    ));
            }
        }
        catch (Exception e)
        {
            e.LogInternal();
        }
        return ret;
    }

    public static void SetEnabled(Guid id, bool enabled)
    {
        try
        {
            var mgr = GetProfileManager();
            var profiles = (System.Collections.IList)mgr?.GetFoP("Profiles");
            if (profiles == null) return;
            foreach (var x in profiles)
            {
                if (x.GetFoP<Guid>("UniqueId") == id)
                {
                    mgr.Call("SetEnabled", [x, enabled, false]);
                }
            }
        }
        catch (Exception e)
        {
            e.LogInternal();
        }
    }

    public static string GetPathForProfileByGuid(Guid guid)
    {
        try
        {
            if (DalamudReflector.TryGetDalamudPlugin("CustomizePlus", out var plugin, out var context, true, true))
            {
                var manager = plugin.GetFoP("_services").Call(context.Assemblies, "GetService", ["CustomizePlus.Profiles.ProfileManager"], []);
                var designList = manager.GetFoP<System.Collections.IList>("Profiles");
                foreach (var design in designList)
                {
                    if (design.GetFoP<Guid>("UniqueId") == guid)
                    {
                        var dfs = plugin.GetFoP("_services").Call(context.Assemblies, "GetService", ["CustomizePlus.Profiles.ProfileFileSystem"], []);
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
