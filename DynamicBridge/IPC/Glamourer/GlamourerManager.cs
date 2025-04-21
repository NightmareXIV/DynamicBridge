using DynamicBridge.Configuration;
using ECommons.EzIpcManager;
using Glamourer.Api.IpcSubscribers;

namespace DynamicBridge.IPC.Glamourer;

public unsafe class GlamourerManager
{
    public GlamourerReflector Reflector;
    public GlamourerCommands Commands;

    public GlamourerManager()
    {
        EzIPC.Init(this, "Glamourer");
        Reflector = new();
        Commands = new();
    }

    private List<PathInfo> PathInfos = null;
    public List<PathInfo> GetCombinedPathes()
    {
        PathInfos ??= Utils.BuildPathes(GetRawPathes());
        return PathInfos;
    }

    public void RevertToAutomation()
    {
        Commands.RevertToAutomation();
    }

    private ApplyDesign ApplyDesign = new(Svc.PluginInterface);

    public void ApplyByGuid(Guid guid)
    {
        try
        {
            //ApplyDesign.Invoke(guid, 0);
            Commands.ApplyByGuid(guid);
        }
        catch(Exception ex)
        {
            ex.Log();
        }
    }

    private GetDesignList GetDesignList = new(Svc.PluginInterface);

    private GlamourerDesignInfo[] GetDesignListIPC()
    {
        try
        {
            return GetDesignList.Invoke().Select(x => (x.Value, x.Key)).ToArray();
        }
        catch(Exception ex)
        {
            InternalLog.Error(ex.ToString());
        }
        return [];
    }

    private GetStateBase64 GetStateBase64 = new(Svc.PluginInterface);
    public string GetMyCustomization()
    {
        try
        {
            return GetStateBase64.Invoke(0).Item2;
        }
        catch(Exception e)
        {
            e.Log();
            return null;
        }
    }

    private ApplyState ApplyState = new(Svc.PluginInterface);
    public void SetMyCustomization(string customization)
    {
        try
        {
            ApplyState.Invoke(customization, 0);
        }
        catch(Exception e)
        {
            e.Log();
        }
    }

    public void ApplyToSelf(GlamourerDesignInfo design)
    {
        try
        {
            //ApplyByGuid(design.Identifier);
            Commands.ApplyByGuid(design.Identifier);
        }
        catch(Exception e)
        {
            Notify.Error(e.Message);
            e.Log();
        }
    }

    private RevertState RevertState = new(Svc.PluginInterface);
    public void Revert()
    {
        try
        {
            Commands.Revert();
            //RevertState.Invoke(0);
        }
        catch(Exception e)
        {
            Notify.Error(e.Message);
            e.Log();
        }
    }

    private GlamourerDesignInfo[] CachedDesignInfo = [];
    private ulong ValidCacheFrame = 0;
    public GlamourerDesignInfo[] GetDesigns()
    {
        var fc = CSFramework.Instance()->FrameCounter;
        if(fc != ValidCacheFrame)
        {
            ValidCacheFrame = fc;
            CachedDesignInfo = GetDesignListIPC();
        }
        return CachedDesignInfo;
    }

    private Dictionary<string, string> TransformNameCache = [];
    public string TransformName(string originalName)
    {
        if(TransformNameCache.TryGetValue(originalName, out var ret))
        {
            return ret;
        }
        if(Guid.TryParse(originalName, out var guid))
        {
            if(GetDesigns().TryGetFirst(x => x.Identifier == guid, out var entry))
            {
                if(C.GlamourerFullPath)
                {
                    return CacheAndReturn(Reflector.GetPathForDesignByGuid(guid) ?? entry.Name);
                }
                return CacheAndReturn(entry.Name);
            }
        }
        return CacheAndReturn(originalName);

        string CacheAndReturn(string name)
        {
            TransformNameCache[originalName] = name;
            return TransformNameCache[originalName];
        }
    }

    private Dictionary<string, string> FullPathCache = [];
    public string GetFullPath(string originalName)
    {
        if(FullPathCache.TryGetValue(originalName, out var ret))
        {
            return ret;
        }
        if(Guid.TryParse(originalName, out var guid))
        {
            if(GetDesigns().TryGetFirst(x => x.Identifier == guid, out var entry))
            {
                return CacheAndReturn(Reflector.GetPathForDesignByGuid(guid) ?? entry.Name);
            }
        }
        return CacheAndReturn(originalName);

        string CacheAndReturn(string name)
        {
            FullPathCache[originalName] = name;
            return FullPathCache[originalName];
        }
    }

    public List<string> GetRawPathes()
    {
        var ret = new List<string>();
        try
        {
            foreach(var x in GetDesigns())
            {
                var path = Reflector.GetPathForDesignByGuid(x.Identifier);
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
        TransformNameCache.Clear();
        PathInfos = null;
    }
}
