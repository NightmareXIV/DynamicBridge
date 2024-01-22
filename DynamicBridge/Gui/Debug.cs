using DynamicBridge.IPC;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DynamicBridge.Gui
{
    public static unsafe class Debug
    {
        static List<int> TestCollection = [0, 1, 2, 3, 4, 5];
        static string Input = "";
        static bool TestBool = false;
        static bool? TestBool2 = null;
        static string cont = "Test button";
        public static void Draw()
        {
            ImGuiEx.TextWrapped(GetCallStackID());
            ImGuiEx.RightFloat(() =>
            {
                if (ImGui.Button(cont))
                {
                    //
                    cont = "A".Repeat(Random.Shared.Next(20) + 2);
                }
            });
            ImGuiEx.CollectionCheckbox("Test", [0, 1, 2], TestCollection);
            ImGuiEx.CollectionCheckbox("Testall", [0, 1, 2, 3, 4, 5], TestCollection);
            ImGuiEx.CollectionCheckbox("1", 1, TestCollection);
            ImGuiEx.CollectionCheckbox("2", 2, TestCollection);
            ImGuiEx.Checkbox("TestBool2", ref TestBool2);
            ImGuiEx.Text($"{TestCollection.Print()}");
            if (ImGui.CollapsingHeader("Time"))
            {
                var time = *ETimeChecker.ET;
                ImGuiEx.Text($"Raw: {time}");
                var date = DateTimeOffset.FromUnixTimeSeconds(time);
                ImGuiEx.Text($"Converted: {date}");
                ImGuiEx.Text($"Current interval: {ETimeChecker.GetEorzeanTimeInterval()}");
                for(int i = 0; i < 60 * 60 * 24; i += 25 * 60)
                {
                    ImGuiEx.Text($"{DateTimeOffset.FromUnixTimeSeconds(i)}: {ETimeChecker.GetTimeInterval(i)}");
                }
            }

            ImGuiEx.CheckboxBullet("Test", ref TestBool);
            if(ImGui.CollapsingHeader("Glamourer test"))
            {
                foreach (var d in GlamourerManager.GetDesigns()) 
                {
                    if (ImGui.Selectable($"{d}"))
                    {
                        GlamourerManager.ApplyByGuid(d.Identifier);
                    }
                }
                if (ImGui.Button("Revert")) GlamourerManager.Revert();
                if (ImGui.Button("Reset cache")) GlamourerManager.ResetCache();
            }

            if(ImGui.CollapsingHeader("Honorific test"))
            {
                foreach (var d in HonorificManager.GetTitleData())
                {
                    if (ImGui.Selectable($"{d.Title}"))
                    {
                        HonorificManager.SetTitle(d.Title);
                    }
                }
                if (ImGui.Button("Revert title")) HonorificManager.SetTitle();
            }

            if (ImGui.CollapsingHeader("C+ test"))
            {
                foreach (var d in CustomizePlusManager.GetProfiles())
                {
                    if (ImGui.Selectable($"{d.ProfileName}"))
                    {
                        CustomizePlusManager.SetProfile(d.ProfileName);
                    }
                }
                if (ImGui.Button("Revert c+")) CustomizePlusManager.RevertProfile();
            }

            if (ImGui.CollapsingHeader("P+ test"))
            {
                foreach (var d in PalettePlusManager.GetPalettes())
                {
                    if (ImGui.Selectable($"{d.Name}"))
                    {
                        PalettePlusManager.SetPalette(d.Name);
                    }
                }
                if (ImGui.Button("Revert p+")) PalettePlusManager.RevertPalette();
            }

            ImGuiEx.Text($"In water: {Utils.IsInWater}");

            if(ImGui.CollapsingHeader("Query territory IDs"))
            {
                ImGuiEx.InputTextMultilineExpanding("##1", ref Input, 100000);
                List<uint> result = [];
                List<string> error = [];
                foreach(var x in Input.Split("\n", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
                {
                    var success = false;
                    foreach(var z in Svc.Data.GetExcelSheet<TerritoryType>())
                    {
                        if(z.PlaceName.Value?.Name.ExtractText().EqualsIgnoreCase(x) == true || z.ContentFinderCondition.Value?.Name.ExtractText().EqualsIgnoreCase(x) == true)
                        {
                            success = true;
                            result.Add(z.RowId);
                        }
                    }
                    if (!success) error.Add($"Could not find matches for {x}");
                }
                ImGuiEx.TextWrappedCopy(result.Print(", "));
                ImGuiEx.TextWrappedCopy(error.Print("\n"));
            }
        }
    }
}
