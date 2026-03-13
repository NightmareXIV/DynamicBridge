using Dalamud.Game.ClientState.Objects.SubKinds;
using DynamicBridge.Configuration;
using ECommons.EzIpcManager;
using ECommons.GameHelpers;
using LociApi.Enums;
using LociApi.Ipc;

namespace DynamicBridge.IPC.Loci;
public class LociManager
{
    public LociManager()
    {
        EzIPC.Init(this, "Loci");
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
            foreach(var x in GetStatuses())
            {
                var path = x.FSPath;
                if(path != null)
                {
                    ret.Add(path);
                }
            }
            foreach(var x in GetPresets())
            {
                var path = x.FSPath;
                if(path != null)
                {
                    ret.Add(path);
                }
            }
            foreach(var x in GetEvents())
            {
                var path = x.FSPath;
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
        StatusCache = null;
        PresetCache = null;
        EventCache = null;
        PathInfos = null;
    }

    private GetStatusSummaryList GetStatusList = new(Svc.PluginInterface);

    private List<LociStatusSummary> StatusCache = null;
    public List<LociStatusSummary> GetStatuses()
    {
        if(StatusCache != null) return StatusCache;
        try
        {
            StatusCache = GetStatusList.Invoke();
            return StatusCache;
        }
        catch(Exception e)
        {
            e.Log();
        }
        return [];
    }

    private GetPresetSummaryList GetPresetList = new(Svc.PluginInterface);

    private List<LociPresetSummary> PresetCache = null;
    public List<LociPresetSummary> GetPresets()
    {
        if(PresetCache != null) return PresetCache;
        try
        {
            PresetCache = GetPresetList.Invoke();
            return PresetCache;
        }
        catch(Exception e)
        {
            e.Log();
        }
        return [];
    }

    private GetEventSummaryList GetEventList = new(Svc.PluginInterface);
    private List<LociEventSummary> EventCache = null;
    public List<LociEventSummary> GetEvents()
    {
        if(EventCache != null) return EventCache;
        try
        {
            EventCache = GetEventList.Invoke();
            return EventCache;
        }
        catch(Exception e)
        {
            e.Log();
        }
        return [];
    }

    private ApplyStatus ApplyStatusById = new(Svc.PluginInterface);
    public void ApplyStatus(Guid guid)
    {
        try
        {
            ApplyStatusById.Invoke(guid);
        }
        catch(Exception e)
        {
            e.Log();
        }
    }

    private RemoveStatus RemoveStatusById = new(Svc.PluginInterface);
    public void RemoveStatus(Guid guid)
    {
        try
        {
            RemoveStatusById.Invoke(guid);
        }
        catch(Exception e)
        {
            e.Log();
        }
    }

    private ApplyPreset ApplyPresetById = new(Svc.PluginInterface);
    public void ApplyPreset(Guid guid)
    {
        try
        {
            ApplyPresetById.Invoke(guid);
        }
        catch(Exception e)
        {
            e.Log();
        }
    }

    private RemovePreset RemovePresetById = new(Svc.PluginInterface);
    public void RemovePreset(Guid guid)
    {
        try
        {
            RemovePresetById.Invoke(guid);
        }
        catch(Exception e)
        {
            e.Log();
        }
    }

    private SetEventState SetEventState = new(Svc.PluginInterface);
    public bool SetEvent(Guid guid, bool state)
    {
        try
        {
            return SetEventState.Invoke(guid, state) is (LociApiEc.Success or LociApiEc.NoChange);
        }
        catch(Exception e)
        {
            e.Log();
            return false;
        }
    }
}
