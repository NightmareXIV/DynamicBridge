using Dalamud.Game.ClientState.Objects.Types;
using ECommons.EzIpcManager;
using ECommons.GameHelpers;
using Penumbra.Api.Enums;
using Penumbra.Api.IpcSubscribers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace DynamicBridge.IPC.Penumbra;
public class PenumbraManager
{
    private bool IsAssignmentSet = false;
    private Guid? OldAssignment = null;

    public PenumbraManager()
    {
        EzIPC.Init(this, "Penumbra");
    }

    private GetCollections GetCollections = new(Svc.PluginInterface);
    public IEnumerable<string> GetCollectionNames()
    {
        try
        {
            return GetCollections.Invoke().Select(x => x.Value);
        }
        catch(Exception e)
        {
            e.Log();
            return [];
        }
    }

    public Guid GetGuidForCollection(string collectionName)
    {
        try
        {
            return new GetCollectionsByIdentifier(Svc.PluginInterface).Invoke(collectionName).FirstOrDefault().Id;
        }
        catch(Exception e)
        {
            e.Log();
        }
        return default;
    }

    public void SetAssignment(string newAssignment)
    {
        try
        {
            (PenumbraApiEc, (Guid Id, string Name)? OldCollection) result;
            if(newAssignment == "")
                result = new SetCollectionForObject(Svc.PluginInterface).Invoke(0, null, true, true);
            else
                result = new SetCollectionForObject(Svc.PluginInterface).Invoke(0, GetGuidForCollection(newAssignment), true, true);
            if(!result.Item1.EqualsAny(PenumbraApiEc.Success, PenumbraApiEc.NothingChanged))
            {
                var e = $"Error setting Penumbra assignment: {result.Item1}";
                PluginLog.Error(e);
                Notify.Error(e);
            }
            else
            {
                if(!IsAssignmentSet)
                {
                    IsAssignmentSet = true;
                    OldAssignment ??= result.OldCollection?.Id;
                }
                if(result.Item1 == PenumbraApiEc.Success) P.TaskManager.Enqueue(RedrawLocalPlayer);
            }
        }
        catch(Exception e)
        {
            e.Log();
        }
    }

    public void UnsetAssignmentIfNeeded()
    {
        if(!IsAssignmentSet) return;
        try
        {
            var result = new SetCollectionForObject(Svc.PluginInterface).Invoke(0, OldAssignment, true, true);
            OldAssignment = null;
            IsAssignmentSet = false;
            if(result.Item1 == PenumbraApiEc.Success) P.TaskManager.Enqueue(RedrawLocalPlayer);
        }
        catch(Exception e)
        {
            e.Log();
        }
    }

    public void RedrawLocalPlayer()
    {
        try
        {
            new RedrawObject(Svc.PluginInterface).Invoke(0);
        }
        catch(Exception e)
        {
            e.Log();
        }
    }
}
