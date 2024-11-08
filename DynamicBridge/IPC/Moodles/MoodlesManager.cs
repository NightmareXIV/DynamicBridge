using Dalamud.Game.ClientState.Objects.SubKinds;
using DynamicBridge.Configuration;
using ECommons.EzIpcManager;
using ECommons.GameHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicBridge.IPC.Moodles;
public class MoodlesManager
{
    [EzIPC] private readonly Func<List<MoodlesMoodleInfo>> GetRegisteredMoodles;
    [EzIPC] private readonly Func<List<MoodlesProfileInfo>> GetRegisteredProfiles;
    [EzIPC] private readonly Action<Guid, IPlayerCharacter> AddOrUpdateMoodleByGUID;
    [EzIPC] private readonly Action<Guid, IPlayerCharacter> ApplyPresetByGUID;
    [EzIPC] private readonly Action<Guid, IPlayerCharacter> RemoveMoodleByGUID;
    [EzIPC] private readonly Action<Guid, IPlayerCharacter> RemovePresetByGUID;

    public MoodlesManager()
    {
        EzIPC.Init(this, "Moodles");
    }

    private List<PathInfo> PathInfos = null;
    public List<PathInfo> GetCombinedPathes()
    {
        PathInfos ??= Utils.BuildPathes(GetRawPathes());
        return PathInfos;
    }

    public List<string> GetRawPathes()
    {
        var ret = new List<string>();
        try
        {
            foreach(var x in GetMoodles())
            {
                var path = x.FullPath;
                if(path != null)
                {
                    ret.Add(path);
                }
            }
            foreach(var x in GetPresets())
            {
                var path = x.FullPath;
                if(path != null)
                {
                    ret.Add(path);
                }
            }
        }
        catch(Exception e)
        {
            e.LogInternal();
        }
        return ret;
    }

    public void ResetCache()
    {
        MoodleCache = null;
        MoodleProfilesCache = null;
        PathInfos = null;
    }

    private List<MoodlesMoodleInfo> MoodleCache = null;
    public List<MoodlesMoodleInfo> GetMoodles()
    {
        if(MoodleCache != null) return MoodleCache;
        try
        {
            MoodleCache = GetRegisteredMoodles();
            return MoodleCache;
        }
        catch(Exception e)
        {
            e.Log();
        }
        return [];
    }

    private List<MoodlesProfileInfo> MoodleProfilesCache = null;
    public List<MoodlesProfileInfo> GetPresets()
    {
        if(MoodleProfilesCache != null) return MoodleProfilesCache;
        try
        {
            MoodleProfilesCache = GetRegisteredProfiles();
            return MoodleProfilesCache;
        }
        catch(Exception e)
        {
            e.Log();
        }
        return [];
    }

    public void ApplyMoodle(Guid guid)
    {
        try
        {
            AddOrUpdateMoodleByGUID(guid, Player.Object);
        }
        catch(Exception e)
        {
            e.Log();
        }
    }

    public void RemoveMoodle(Guid guid)
    {
        try
        {
            RemoveMoodleByGUID(guid, Player.Object);
        }
        catch(Exception e)
        {
            e.Log();
        }
    }

    public void ApplyPreset(Guid guid)
    {
        try
        {
            ApplyPresetByGUID(guid, Player.Object);
        }
        catch(Exception e)
        {
            e.Log();
        }
    }

    public void RemovePreset(Guid guid)
    {
        try
        {
            RemovePresetByGUID(guid, Player.Object);
        }
        catch(Exception e)
        {
            e.Log();
        }
    }
}
