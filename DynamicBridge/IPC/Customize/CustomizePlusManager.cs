using Dalamud.Game.ClientState.Objects.Types;
using DynamicBridge.Configuration;
using ECommons.GameHelpers;
using Newtonsoft.Json;
using System;

namespace DynamicBridge.IPC.Customize;
public static class CustomizePlusManager
{
    public static bool WasSet = false;
    public static Guid SavedProfileID = Guid.Empty;
    public static Guid LastEnabledProfileID = Guid.Empty;
    public static CustomizePlusProfile[] GetProfiles(IEnumerable<string> chara = null)
    {
        try
        {
            //var ret = Svc.PluginInterface.GetIpcSubscriber<CustomizePlusProfile[]>("CustomizePlus.GetProfileList").InvokeFunc();
            var ret = CustomizePlusReflector.GetProfiles().ToArray();
            if (chara != null)
            {
                ret = ret.Where(x => chara.Contains(x.characterName)).ToArray();
            }
            return ret;
        }
        catch (Exception e)
        {
            e.Log();
            return [];
        }
    }

    public static void SetProfile(string profileGuidStr, string charName)
    {
        try
        {
            var charaProfiles = GetProfiles().Where(x => x.characterName == charName).ToArray();
            if (!WasSet)
            {
                if (charaProfiles.TryGetFirst(x => x.IsEnabled, out var enabledProfile))
                {
                    SavedProfileID = enabledProfile.ID;
                }
                else
                {
                    SavedProfileID = Guid.Empty;
                }
            }
            if (Guid.TryParse(profileGuidStr, out var guid))
            {
                if (charaProfiles.TryGetFirst(x => x.ID == guid, out var profile))
                {
                    //Svc.PluginInterface.GetIpcSubscriber<Guid, object>("CustomizePlus.EnableProfileByUniqueId").InvokeAction(profile.ID);
                    CustomizePlusReflector.SetEnabled(profile.ID, true);
                    LastEnabledProfileID = profile.ID;
                }
            }
            else
            {
                PluginLog.Error($"Could not parse Customize+ profile: {profileGuidStr}. Is customize+ loaded?");
            }
        }
        catch (Exception e)
        {
            e.Log();
        }
        WasSet = true;
    }

    public static void RestoreState()
    {
        if (WasSet)
        {
            try
            {
                if (SavedProfileID == Guid.Empty)
                {
                    //Svc.PluginInterface.GetIpcSubscriber<Guid, object>("CustomizePlus.DisableProfileByUniqueId").InvokeAction(LastEnabledProfileID);
                    CustomizePlusReflector.SetEnabled(LastEnabledProfileID, false);
                }
                else
                {
                    //Svc.PluginInterface.GetIpcSubscriber<Guid, object>("CustomizePlus.EnableProfileByUniqueId").InvokeAction(SavedProfileID);
                    CustomizePlusReflector.SetEnabled(SavedProfileID, true);
                }
            }
            catch (Exception e)
            {
                e.Log();
            }
            SavedProfileID = Guid.Empty;
        }
        WasSet = false;
    }



    public static string TransformName(string originalName)
    {
        if (Guid.TryParse(originalName, out Guid guid))
        {
            if (GetProfiles().TryGetFirst(x => x.ID == guid, out var entry))
            {
                if (C.GlamourerFullPath)
                {
                    return CustomizePlusReflector.GetPathForProfileByGuid(guid) ?? entry.Name;
                }
                return entry.Name;
            }
        }
        return originalName;
    }
}
