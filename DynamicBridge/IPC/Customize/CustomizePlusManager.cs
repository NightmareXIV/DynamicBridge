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

    [EzIPC("Profile.GetList")] Func<IList<CustomizePlusProfile>> GetProfileList;
    [EzIPC("Profile.EnableByUniqueId")] Func<Guid, int> EnableProfileByUniqueId;
    [EzIPC("Profile.DisableByUniqueId")] Func<Guid, int> DisableProfileByUniqueId;

    public CustomizePlusManager()
    {
        Reflector = new();
        EzIPC.Init(this, "CustomizePlus", safeWrapper:SafeWrapper.IPCException);
    }

    public List<string> GetRawPathes()
    {
        var ret = new List<string>();
        try
        {
            foreach (var x in GetProfiles())
            {
                var path = x.VirtualPath;
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

    IList<CustomizePlusProfile> Cache = null;
    public IEnumerable<CustomizePlusProfile> GetProfiles(IEnumerable<string> chara = null)
    {
        Cache ??= GetProfileList();
        foreach(var x in (Cache ?? []))
        {
            if (chara == null || chara.Contains(x.CharacterName)) yield return x;
        }
    }

    public void ResetCache() => Cache = null;

    public void SetProfile(string profileGuidStr, string charName)
    {
        try
        {
            var charaProfiles = GetProfiles().Where(x => x.CharacterName == charName).ToArray();
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
                    EnableProfileByUniqueId(profile.ID);
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
                    DisableProfileByUniqueId(LastEnabledProfileID);
                }
                else
                {
                    EnableProfileByUniqueId(SavedProfileID);
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
                    return entry.VirtualPath;
                }
                return entry.Name;
            }
        }
        return originalName;
    }
}
