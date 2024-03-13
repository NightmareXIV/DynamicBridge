using Dalamud.Game.ClientState.Objects.Types;
using DynamicBridge.Configuration;
using ECommons.EzIpcManager;
using ECommons.GameHelpers;
using Newtonsoft.Json;
using System;

namespace DynamicBridge.IPC.Customize;
public class CustomizePlusManager
{
    public bool WasSet = false;
    public Guid SavedProfileID = Guid.Empty;
    public Guid LastEnabledProfileID = Guid.Empty;
    public CustomizePlusReflector Reflector;

    public CustomizePlusManager()
    {
        Reflector = new();
        EzIPC.Init(this, "CustomizePlus");
    }

    public List<string> GetRawPathes()
    {
        var ret = new List<string>();
        try
        {
            foreach (var x in GetProfiles())
            {
                var path = Reflector.GetPathForProfileByGuid(x.ID);
                if (path != null)
                {
                    ret.Add(path);
                }
            }
        }
        catch (Exception e)
        {
            e.LogInternal();
        }
        return ret;
    }

    public CustomizePlusProfile[] GetProfiles(IEnumerable<string> chara = null)
    {
        try
        {
            var ret = Reflector.GetProfiles().ToArray();
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

    public void SetProfile(string profileGuidStr, string charName)
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
                    Reflector.SetEnabled(profile.ID, true);
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

    public void RestoreState()
    {
        if (WasSet)
        {
            try
            {
                if (SavedProfileID == Guid.Empty)
                {
                    Reflector.SetEnabled(LastEnabledProfileID, false);
                }
                else
                {
                    Reflector.SetEnabled(SavedProfileID, true);
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



    public string TransformName(string originalName)
    {
        if (Guid.TryParse(originalName, out Guid guid))
        {
            if (GetProfiles().TryGetFirst(x => x.ID == guid, out var entry))
            {
                if (C.GlamourerFullPath)
                {
                    return Reflector.GetPathForProfileByGuid(guid) ?? entry.Name;
                }
                return entry.Name;
            }
        }
        return originalName;
    }
}
