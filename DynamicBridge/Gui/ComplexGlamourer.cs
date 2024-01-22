using DynamicBridge.Configuration;
using DynamicBridge.IPC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicBridge.Gui;
public static unsafe class ComplexGlamourer
{
    static string Filter = "";
    static bool OnlySelected = false;
    public static void Draw()
    {
        if (!C.EnableGlamourer)
        {
            ImGuiEx.Text(EColor.RedBright, "Glamourer disabled in settings. Function unavailable.");
            return;
        }
        ImGuiEx.Text($"Here you can create complex Glamourer entries. Upon application, they will be applied sequentially one after another. You will be able to select complex entries for profiles together with normal entries.");
        if(ImGui.Button("Add new entry"))
        {
            C.ComplexGlamourerEntries.Add(new());
        }
        foreach(var gEntry in C.ComplexGlamourerEntries)
        {
            ImGui.PushID(gEntry.GUID);
            if (ImGui.CollapsingHeader($"{gEntry.Name}###entry"))
            {
                ImGuiEx.TextV($"1. Name complex Glamourer entry:");
                ImGui.SameLine();
                ImGuiEx.SetNextItemFullWidth();
                ImGui.InputText($"##name", ref gEntry.Name, 100);
                ImGuiEx.TextV($"2. Select Glamourer designs:");
                ImGui.SameLine();
                if (ImGui.BeginCombo("##glamour", gEntry.Designs.PrintRange(out var fullList, "- None -")))
                {
                    FiltersSelection();
                    var designs = GlamourerManager.GetDesigns().OrderBy(x => x.Name);
                    foreach (var x in designs)
                    {
                        var name = x.Name;
                        if (Filter.Length > 0 && !name.Contains(Filter, StringComparison.OrdinalIgnoreCase)) continue;
                        if (OnlySelected && !gEntry.Designs.Contains(name)) continue;
                        ImGuiEx.CollectionCheckbox($"{name}##{x.Identifier}", x.Name, gEntry.Designs);
                    }
                    foreach (var x in gEntry.Designs)
                    {
                        if (designs.Any(d => d.Name == x)) continue;
                        ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.DalamudRed);
                        ImGuiEx.CollectionCheckbox($"{x}", x, gEntry.Designs, false, true);
                        ImGui.PopStyleColor();
                    }
                    ImGui.EndCombo();
                }
                ImGuiEx.Text($"3. Change order if needed");
                for (int i = 0; i < gEntry.Designs.Count; i++)
                {
                    var design = gEntry.Designs[i];
                    ImGui.PushID(design);
                    if (ImGui.ArrowButton("up", ImGuiDir.Up) && i > 0)
                    {
                        (gEntry.Designs[i - 1], gEntry.Designs[i]) = (gEntry.Designs[i], gEntry.Designs[i - 1]);
                    }
                    ImGui.SameLine();
                    if (ImGui.ArrowButton("down", ImGuiDir.Down) && i < gEntry.Designs.Count - 1)
                    {
                        (gEntry.Designs[i + 1], gEntry.Designs[i]) = (gEntry.Designs[i], gEntry.Designs[i + 1]);
                    }
                    ImGui.SameLine();
                    ImGuiEx.Text($"{design}");
                    ImGui.PopID();
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
