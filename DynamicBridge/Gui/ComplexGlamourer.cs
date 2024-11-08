using Dalamud.Interface.Components;
using DynamicBridge.Configuration;
using DynamicBridge.IPC.Glamourer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicBridge.Gui;
public static unsafe class ComplexGlamourer
{
    private static string Filter = "";
    private static bool OnlySelected = false;
    public static void Draw()
    {
        if(!C.EnableGlamourer)
        {
            ImGuiEx.Text(EColor.RedBright, "Glamourer disabled in settings. Function unavailable.");
            return;
        }
        ImGuiEx.TextWrapped($"Here you can create layered designs for Glamourer. Upon application, they will be applied sequentially one after another. You will be able to select layered designs for profiles together with normal entries.");
        if(ImGuiComponents.IconButtonWithText(FontAwesomeIcon.Plus, "Add new entry"))
        {
            C.ComplexGlamourerEntries.Add(new());
        }
        foreach(var gEntry in C.ComplexGlamourerEntries)
        {
            ImGui.PushID(gEntry.GUID);
            if(ImGui.CollapsingHeader($"{gEntry.Name}###entry"))
            {
                ImGuiEx.TextV($"1. Name Layered Design:");
                ImGui.SameLine();
                ImGuiEx.SetNextItemFullWidth();
                ImGui.InputText($"##name", ref gEntry.Name, 100);
                ImGuiEx.TextV($"2. Select Glamourer designs:");
                ImGui.SameLine();
                if(ImGui.BeginCombo("##glamour", gEntry.Designs.Select(P.GlamourerManager.TransformName).PrintRange(out var fullList, "- None -"), C.ComboSize))
                {
                    if(ImGui.IsWindowAppearing()) Utils.ResetCaches();
                    FiltersSelection();
                    var designs = P.GlamourerManager.GetDesigns().OrderBy(x => x.Name);
                    foreach(var x in designs)
                    {
                        var name = x.Name;
                        var id = x.Identifier.ToString();
                        var transformedName = P.GlamourerManager.TransformName(id);
                        if(Filter.Length > 0 && !transformedName.Contains(Filter, StringComparison.OrdinalIgnoreCase)) continue;
                        if(OnlySelected && !gEntry.Designs.Contains(id)) continue;
                        ImGuiEx.CollectionCheckbox($"{transformedName}##{x.Identifier}", id, gEntry.Designs);
                    }
                    foreach(var x in gEntry.Designs)
                    {
                        if(designs.Any(d => d.Identifier.ToString() == x)) continue;
                        ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.DalamudRed);
                        ImGuiEx.CollectionCheckbox($"{x}", x, gEntry.Designs, false, true);
                        ImGui.PopStyleColor();
                    }
                    ImGui.EndCombo();
                }
                ImGuiEx.Text($"3. Change order if needed");
                for(var i = 0; i < gEntry.Designs.Count; i++)
                {
                    var design = gEntry.Designs[i];
                    ImGui.PushID(design);
                    if(ImGui.ArrowButton("up", ImGuiDir.Up) && i > 0)
                    {
                        (gEntry.Designs[i - 1], gEntry.Designs[i]) = (gEntry.Designs[i], gEntry.Designs[i - 1]);
                    }
                    ImGui.SameLine();
                    if(ImGui.ArrowButton("down", ImGuiDir.Down) && i < gEntry.Designs.Count - 1)
                    {
                        (gEntry.Designs[i + 1], gEntry.Designs[i]) = (gEntry.Designs[i], gEntry.Designs[i + 1]);
                    }
                    ImGui.SameLine();
                    ImGuiEx.Text($"{P.GlamourerManager.TransformName(design)}");
                    ImGui.PopID();
                }
                if(ImGuiEx.ButtonCtrl("Delete"))
                {
                    new TickScheduler(() => C.ComplexGlamourerEntries.RemoveAll(x => x.GUID == gEntry.GUID));
                }
            }
            ImGui.PopID();
        }

        void FiltersSelection()
        {
            ImGui.SetWindowFontScale(0.8f);
            ImGuiEx.SetNextItemFullWidth();
            ImGui.InputTextWithHint($"##fltr", "Filter...", ref Filter, 50);
            ImGui.Checkbox($"Only selected", ref OnlySelected);
            ImGui.SetWindowFontScale(1f);
            ImGui.Separator();
        }
    }
}
