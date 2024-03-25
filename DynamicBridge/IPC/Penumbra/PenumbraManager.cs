using Dalamud.Game.ClientState.Objects.Types;
using ECommons.EzIpcManager;
using ECommons.GameHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicBridge.IPC.Penumbra;
public class PenumbraManager
{
    [EzIPC] public readonly Func<IList<string>> GetCollections;
    [EzIPC] public readonly Func<ApiCollectionType, string> GetCollectionForType;
    [EzIPC] public readonly Func<ApiCollectionType, string, bool, bool, (PenumbraApiEc Error, string OldCollection)> SetCollectionForType;
    [EzIPC] public readonly Func<int, (bool ObjectValid, bool IndividualSet, string EffectiveCollection)> GetCollectionForObject;
    [EzIPC] public readonly Func<int, string, bool, bool, (PenumbraApiEc Error, string OldCollection)> SetCollectionForObject;
    [EzIPC] public readonly Action<GameObject, RedrawType> RedrawObject;
    string OldAssignment;

    public PenumbraManager()
    {
        EzIPC.Init(this, "Penumbra");
    }

    public void Redraw() => RedrawObject.TryInvoke(Player.Object, RedrawType.Redraw);

    public void SetAssignment(string newAssignment)
    {
        try
        {
            var result = SetCollectionForObject(0, newAssignment, true, true);
            if (!result.Error.EqualsAny(PenumbraApiEc.Success, PenumbraApiEc.NothingChanged))
            {
                var e = $"Error setting Penumbra assignment: {result.Error}";
                PluginLog.Error(e);
                Notify.Error(e);
            }
            else
            {
                OldAssignment ??= result.OldCollection;
                if(result.Error == PenumbraApiEc.Success) P.TaskManager.Enqueue(Redraw);
            }
        }
        catch(Exception e)
        {
            e.Log();
        }
    }

    public void UnsetAssignmentIfNeeded()
    {
        if (OldAssignment == null) return;
        try
        {
            var result = SetCollectionForObject(0, OldAssignment, true, true);
            OldAssignment = null;
            if (result.Error == PenumbraApiEc.Success) P.TaskManager.Enqueue(Redraw);
        }
        catch(Exception e)
        {
            e.Log();
        }
    }

    public enum ApiCollectionType : byte
    {
        Yourself = 0,
        Default = 0xE0,
        Interface = 0xE1,
        Current = 0xE2,
    }

    public enum PenumbraApiEc
    {
        Success = 0,
        NothingChanged = 1,
        CollectionMissing = 2,
        ModMissing = 3,
        OptionGroupMissing = 4,
        OptionMissing = 5,

        CharacterCollectionExists = 6,
        LowerPriority = 7,
        InvalidGamePath = 8,
        FileMissing = 9,
        InvalidManipulation = 10,
        InvalidArgument = 11,
        PathRenameFailed = 12,
        CollectionExists = 13,
        AssignmentCreationDisallowed = 14,
        AssignmentDeletionDisallowed = 15,
        InvalidIdentifier = 16,
        SystemDisposed = 17,
        AssignmentDeletionFailed = 18,
        UnknownError = 255,
    }

    public enum RedrawType
    {
        Redraw,
        AfterGPose,
    }
}
