using Dalamud.Game.ClientState.Objects.SubKinds;
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
    [EzIPC] Func<List<MoodlesMoodleInfo>> GetRegisteredMoodles { get; init; }
    [EzIPC] Func<List<MoodlesProfileInfo>> GetRegisteredProfiles { get; init; }
    [EzIPC] Action<Guid, PlayerCharacter> AddOrUpdateMoodleByGUID { get; init; }
    [EzIPC] Action<Guid, PlayerCharacter> ApplyPresetByGUID { get; init; }
    [EzIPC] Action<Guid, PlayerCharacter> RemoveMoodleByGUID { get; init; }
    [EzIPC] Action<Guid, PlayerCharacter> RemovePresetByGUID { get; init; }

    public MoodlesManager()
    {
        EzIPC.Init(this, "Moodles");
    }

    public void ResetCache()
    {
        MoodleCache = null;
        MoodleProfilesCache = null;
    }

    List<MoodlesMoodleInfo> MoodleCache = null;
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

    List<MoodlesProfileInfo> MoodleProfilesCache = null;
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
        catch (Exception e)
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
        catch (Exception e)
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
        catch (Exception e)
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
        catch (Exception e)
        {
            e.Log();
        }
    }
}
