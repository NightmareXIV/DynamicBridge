using Dalamud.Game.Text.SeStringHandling;
using DynamicBridge.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicBridge.Gui;
public static class DragDrop
{
    public static void AcceptProfileDragDrop(Profile currentProfile, List<Preset> presetList, int i)
    {
        if (ImGui.BeginDragDropTarget())
        {
            if (ImGuiDragDrop.AcceptDragDropPayload("MovePreset", out var payload, ImGuiDragDropFlags.AcceptBeforeDelivery | ImGuiDragDropFlags.AcceptNoDrawDefaultRect))
            {
                if (!presetList.Any(x => x.GUID == payload))
                {
                    var item = currentProfile.GetPresetsUnion().FirstOrDefault(x => x.GUID == payload);
                    if (item == null)
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
            ImGui.EndDragDropTarget();
        }
    }

    public static void AcceptFolderDragDrop(Profile currentProfile, List<Preset> presetList)
    {
        if (ImGui.BeginDragDropTarget())
        {
            if (ImGuiDragDrop.AcceptDragDropPayload("MovePreset", out var payload))
            {
                MovePresetToList(currentProfile, payload, presetList);
            }
            ImGui.EndDragDropTarget();
        }
    }

    public static void MovePresetToList(Profile currentProfile, string payload, List<Preset> presetList)
    {
        if (!presetList.Any(x => x.GUID == payload))
        {
            var item = currentProfile.GetPresetsUnion().FirstOrDefault(x => x.GUID == payload);
            if (item == null)
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
}
