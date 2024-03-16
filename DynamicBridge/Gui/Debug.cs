using DynamicBridge.Core;
using DynamicBridge.IPC;
using DynamicBridge.IPC.Customize;
using DynamicBridge.IPC.Glamourer;
using DynamicBridge.IPC.Honorific;
using ECommons.GameHelpers;
using ECommons.Reflection;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
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
        public static bool? ForceDisguise = null;
        static List<string> TestData = null;
        public static void Draw()
        {
            if(ImGui.CollapsingHeader("Pathes"))
            {
                foreach(var x in Utils.GetCombinedPathes())
                {
                    for (int i = 0; i < x.Indentation; i++)
                    {
                        ImGuiEx.Spacing();
                    }
                    ImGuiEx.TextV($"{x.Name}");
                }
            }
            if (ImGui.CollapsingHeader("Items"))
            {
                ImGuiEx.Text($"Current: {Utils.GetCurrentGear().Print()}");
                ImGuiEx.Text($"Last: {P.LastItems.Print()}");
            }
            if (ImGui.CollapsingHeader("Task manager"))
            {
                if (ImGui.Button("Step mode on")) P.TaskManager.SetStepMode(true);
                ImGui.SameLine();
                if (ImGui.Button("Step mode off")) P.TaskManager.SetStepMode(false);
                ImGui.SameLine();
                if (ImGui.Button("Step")) P.TaskManager.Step();
                ImGui.SameLine();
                if (ImGui.Button("Abort")) P.TaskManager.Abort();
                foreach(var x in P.TaskManager.TaskStack)
                {
                    ImGuiEx.Text($"{x}");
                }
            }
            ImGuiEx.Checkbox("Disguise", ref ForceDisguise);
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

            if(ImGui.CollapsingHeader("Glamourer test"))
            {
                foreach (var d in P.GlamourerManager.GetDesigns()) 
                {
                    if (ImGui.Selectable($"{d}"))
                    {
                        P.GlamourerManager.ApplyByGuid(d.Identifier);
                    }
                }
                if (ImGui.Button("Revert")) P.GlamourerManager.Revert();
                ImGuiEx.Text($"Automation state: {P.GlamourerManager.Reflector.GetAutomationGlobalState()}");
                ImGuiEx.Text($"Automation state for chara: {P.GlamourerManager.Reflector.GetAutomationStatusForChara()}");
                if (ImGui.Button("Enable automation")) P.GlamourerManager.Reflector.SetAutomationGlobalState(true);
                if (ImGui.Button("Disable automation")) P.GlamourerManager.Reflector.SetAutomationGlobalState(false);
            }

            if(ImGui.CollapsingHeader("Honorific test"))
            {
                foreach (var d in HonorificManager.GetTitleData([Player.CID]))
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
                ImGuiEx.Text($"Saved: {P.CustomizePlusManager.SavedProfileID}");
                ImGuiEx.Text($"wasset: {P.CustomizePlusManager.WasSet}");
                foreach (var d in P.CustomizePlusManager.GetProfiles())
                {
                    if (ImGui.Selectable($"{d.Name} / {d.characterName} / {d.ID} / {d.IsEnabled}"))
                    {
                        P.CustomizePlusManager.SetProfile(d.Name, Player.Name);
                    }
                }
                if (ImGui.Button("Revert c+")) P.CustomizePlusManager.RestoreState();
            }

            if(ImGui.CollapsingHeader("C+ reflector"))
            {
                try
                {
                    if (DalamudReflector.TryGetDalamudPlugin("CustomizePlus", out var plugin, false, true) && DalamudReflector.TryGetLocalPlugin(plugin, out var lp, out var lpType))
                    {
                        var context = lp.GetFoP("loader").GetFoP<AssemblyLoadContext>("context");
                        ImGuiEx.Text(context.Assemblies.Select(x => x).Print("\n"));
                        var profileManager = plugin.GetFoP("_services").Call([plugin.GetType().Assembly], "GetService", ["CustomizePlus.Profiles.ProfileManager"], []);
                            //ReflectionHelper.CallStatic(context.Assemblies, "Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions",null, "GetRequiredService", ["CustomizePlus.Profiles.ProfileManager"], [plugin.GetFoP("_services")]);
                        ImGuiEx.TextWrapped(profileManager.GetType().GetMethods().Select(x => x.Name).Print());
                    }
                    else
                    {
                        ImGuiEx.Text($"Could not find plugin");
                    }
                }
                catch(Exception e)
                {
                    e.Log();
                    ImGuiEx.Text(e.ToString());
                }
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
