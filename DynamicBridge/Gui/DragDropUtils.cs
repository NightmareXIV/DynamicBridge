using Dalamud.Game.Text.SeStringHandling;
using DynamicBridge.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicBridge.Gui;
public static class DragDropUtils
{
    public static void AcceptProfileDragDrop(Profile currentProfile, string payload, List<Preset> presetList, int i)
    {
        if(!presetList.Any(x => x.GUID == payload))
        {
            var item = currentProfile.GetPresetsUnion().FirstOrDefault(x => x.GUID == payload);
            if(item == null)
            {
                DuoLog.Error($"Fatal error: payload ID not found");
            }
            else
            {
                currentProfile.GetPresetsListUnion().Each(x => x.RemoveAll(z => z.GUID == payload));
                presetList.Add(item);
            }
        }
        MoveItemToPosition(presetList, (x) => x.GUID == payload, i);
    }

    public static void AcceptFolderDragDrop(Profile currentProfile, string payload, List<Preset> presetList)
    {
        MovePresetToList(currentProfile, payload, presetList);
    }

    public static void MovePresetToList(Profile currentProfile, string payload, List<Preset> presetList)
    {
        if(!presetList.Any(x => x.GUID == payload))
        {
            var item = currentProfile.GetPresetsUnion().FirstOrDefault(x => x.GUID == payload);
            if(item == null)
            {
                DuoLog.Error($"Fatal error: payload ID not found");
            }
            else
            {
                currentProfile.GetPresetsListUnion().Each(x => x.RemoveAll(z => z.GUID == payload));
                presetList.Insert(0, item);
            }
        }
    }

    public static void AcceptRuleDragDrop(Profile currentProfile, int i)
    {
        if(ImGui.BeginDragDropTarget())
        {
            if(ImGuiDragDrop.AcceptDragDropPayload("MoveRule", out var payload, ImGuiDragDropFlags.AcceptBeforeDelivery | ImGuiDragDropFlags.AcceptNoDrawDefaultRect))
            {
                MoveItemToPosition(currentProfile.Rules, (x) => x.GUID == payload, i);
            }
            ImGui.EndDragDropTarget();
        }
    }
}
