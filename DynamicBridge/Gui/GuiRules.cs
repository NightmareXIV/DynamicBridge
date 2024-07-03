using ECommons.ExcelServices;
using ECommons.GameHelpers;
using ECommons.ImGuiMethods.TerritorySelection;
using Lumina.Excel.GeneratedSheets;
using Newtonsoft.Json;
using System.IO;
using System.Windows.Forms;
using ECommons.GameFunctions;
using DynamicBridge.Configuration;
using Action = System.Action;
using Emote = Lumina.Excel.GeneratedSheets.Emote;
using OtterGui.Widgets;
using ECommons.Throttlers;
using System.Runtime.InteropServices;
using ECommons;
using Dalamud.Interface.Components;
using DynamicBridge.Core;
using Dalamud.Interface.Style;

namespace DynamicBridge.Gui
{
    public unsafe static class GuiRules
    {
        static Vector2 iconSize => new(24f);
        static string[] Filters = ["", "", "", "", "", "", "", "", "", "", "", "", "", "", "","","","","","",""];
        static bool[] OnlySelected = new bool[20];
        static string CurrentDrag = "";

        public static void Draw()
        {
            if (UI.Profile != null)
            {
                var Profile = UI.Profile;
                Profile.Rules.RemoveAll(x => x == null);
                void ButtonsLeft()
                {
                    if (ImGuiEx.IconButton(FontAwesomeIcon.Plus))
                    {
                        Profile.Rules.Add(new());
                    }
                    ImGuiEx.Tooltip(Lang.AddNewRule);
                    ImGui.SameLine();
                    if (ImGuiEx.IconButton(FontAwesomeIcon.Paste, Lang.PasteRuleFromClipboard))
                    {
                        try
                        {
                            Profile.Rules.Add(JsonConvert.DeserializeObject<ApplyRule>(Clipboard.GetText()) ?? throw new NullReferenceException());
                        }
                        catch (Exception e)
                        {
                            Notify.Error(Lang.FailedToPasteFromClipboardN + e.Message);
                        }
                    }
                    if (Profile.IsStaticExists())
                    {
                        ImGuiEx.HelpMarker(Lang.PresetIsSelectedAsStaticAutomationDisabled.Params(Profile.GetStaticPreset()?.CensoredName), GradientColor.Get(EColor.RedBright, EColor.YellowBright, 1000), FontAwesomeIcon.ExclamationTriangle.ToIconString());
                    }
                    ImGui.SameLine();
                }
                void ButtonsRight()
                {
                    UI.ForceUpdateButton();
                    ImGui.SameLine();
                }
                
                UI.ProfileSelectorCommon(ButtonsLeft, ButtonsRight);

                var active = (bool[])[
                    C.Cond_State,
                    C.Cond_Biome,
                    C.Cond_Emote,
                    C.Cond_Gearset,
                    C.Cond_House,
                    C.Cond_Job,
                    C.Cond_Time,
                    C.Cond_Weather,
                    C.Cond_World,
                    C.Cond_Zone,
                    C.Cond_ZoneGroup,
                ];

                List<(Vector2 RowPos, Vector2 ButtonPos, Action BeginDraw, Action AcceptDraw)> MoveCommands = [];

                ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, Utils.CellPadding);
                if (ImGui.BeginTable("##main", 3 + active.Count(x => x), ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders | ImGuiTableFlags.Resizable | ImGuiTableFlags.Reorderable))
                {
                    ImGui.TableSetupColumn("  ", ImGuiTableColumnFlags.NoResize | ImGuiTableColumnFlags.WidthFixed);
                    if(C.Cond_State) ImGui.TableSetupColumn(Lang.RuleState);
                    if (C.Cond_Biome) ImGui.TableSetupColumn(Lang.RuleBiome);
                    if (C.Cond_Weather) ImGui.TableSetupColumn(Lang.RuleWeather);
                    if (C.Cond_Time) ImGui.TableSetupColumn(Lang.RuleTime);
                    if (C.Cond_ZoneGroup) ImGui.TableSetupColumn(Lang.RuleZoneGroup);
                    if (C.Cond_Zone) ImGui.TableSetupColumn(Lang.RuleZone);
                    if (C.Cond_House) ImGui.TableSetupColumn(Lang.RuleHouse);
                    if (C.Cond_Emote) ImGui.TableSetupColumn(Lang.RuleEmote);
                    if (C.Cond_Job) ImGui.TableSetupColumn(Lang.RuleJob);
                    if (C.Cond_World) ImGui.TableSetupColumn(Lang.RuleWorld);
                    if (C.Cond_Gearset) ImGui.TableSetupColumn(Lang.RuleGearset);
                    ImGui.TableSetupColumn(Lang.RulePreset);
                    ImGui.TableSetupColumn(" ", ImGuiTableColumnFlags.NoResize | ImGuiTableColumnFlags.WidthFixed);
                    ImGui.TableHeadersRow();

                    for (int i = 0; i < Profile.Rules.Count; i++)
                    {
                        var filterCnt = 0;
                        void FiltersSelection()
                        {
                            ImGui.SetWindowFontScale(0.8f);
                            ImGuiEx.SetNextItemFullWidth();
                            ImGui.InputTextWithHint($"##fltr{filterCnt}", Lang.Filter, ref Filters[filterCnt], 50);
                            ImGui.Checkbox(Lang.OnlySelected + "##{filterCnt}", ref OnlySelected[filterCnt]);
                            ImGui.SetWindowFontScale(1f);
                            ImGui.Separator();
                        }
                        var rule = Profile.Rules[i];
                        var col = !rule.Enabled;
                        var col2 = P.LastRule.Any(x => x.GUID == rule.GUID);
                        if (col2) ImGui.PushStyleColor(ImGuiCol.Text, EColor.Green);
                        if (col) ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.DalamudGrey3);
                        ImGui.PushID(rule.GUID);
                        ImGui.TableNextRow(); 
                        if (CurrentDrag == rule.GUID)
                        {
                            var color = GradientColor.Get(EColor.Green, EColor.Green with { W = EColor.Green.W / 4 }, 500).ToUint();
                            ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg0, color);
                            ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg1, color);
                        }
                        ImGui.TableNextColumn();

                        //Sorting
                        var rowPos = ImGui.GetCursorPos();
                        ImGui.Checkbox("##enable", ref rule.Enabled);
                        ImGuiEx.Tooltip(Lang.EnableThisRule);

                        ImGui.SameLine();
                        ImGui.PushFont(UiBuilder.IconFont);
                        var cur = ImGui.GetCursorPos();
                        var size = ImGuiHelpers.GetButtonSize(FontAwesomeIcon.ArrowsUpDownLeftRight.ToIconString());
                        ImGui.Dummy(size);
                        ImGui.PopFont();
                        var moveIndex = i;
                        MoveCommands.Add((rowPos, cur, delegate
                        {
                            ImGui.PushFont(UiBuilder.IconFont);
                            ImGui.Button($"{FontAwesomeIcon.ArrowsUpDownLeftRight.ToIconString()}##Move{rule.GUID}");
                            ImGui.PopFont();
                            if (ImGui.IsItemHovered())
                            {
                                ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeAll);
                            }
                            if (ImGui.BeginDragDropSource(ImGuiDragDropFlags.SourceNoPreviewTooltip))
                            {
                                ImGuiDragDrop.SetDragDropPayload("MoveRule", rule.GUID);
                                CurrentDrag = rule.GUID;
                                InternalLog.Verbose($"DragDropSource = {rule.GUID}");
                                ImGui.EndDragDropSource();
                            }
                            else if (CurrentDrag == rule.GUID)
                            {
                                InternalLog.Verbose($"Current drag reset!");
                                CurrentDrag = null;
                            }
                        }, delegate { DragDrop.AcceptRuleDragDrop(Profile, moveIndex); }
                        ));

                        ImGui.SameLine();
                        ImGui.PushFont(UiBuilder.IconFont);
                        ImGuiEx.ButtonCheckbox("\uf103", ref rule.Passthrough);
                        ImGui.PopFont();
                        ImGuiEx.Tooltip(Lang.PassthroughTooltip);


                        if(C.Cond_State)
                        {
                            ImGui.TableNextColumn();
                            //Conditions
                            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                            if (ImGui.BeginCombo("##conditions", rule.States.PrintRange(rule.Not.States, out var fullList), C.ComboSize))
                            {
                                FiltersSelection();
                                foreach (var cond in Enum.GetValues<CharacterState>())
                                {
                                    var name = cond.ToString().Replace("_", " ");
                                    if (Filters[filterCnt].Length > 0 && !name.Contains(Filters[filterCnt], StringComparison.OrdinalIgnoreCase)) continue;
                                    if (OnlySelected[filterCnt] && !rule.States.Contains(cond)) continue;
                                    if (ThreadLoadImageHandler.TryGetTextureWrap(Path.Combine(Svc.PluginInterface.AssemblyLocation.DirectoryName, "res", "state", $"{(int)cond}.png"), out var texture))
                                    {
                                        ImGui.Image(texture.ImGuiHandle, iconSize);
                                        ImGui.SameLine();
                                    }
                                    DrawSelector(name, cond, rule.States, rule.Not.States);
                                }
                                ImGui.EndCombo();
                            }
                            if (fullList != null) ImGuiEx.Tooltip(UI.AnyNotice + fullList);
                        }
                        filterCnt++;

                        if (C.Cond_Biome)
                        {
                            ImGui.TableNextColumn();
                            //Biome
                            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                            if (ImGui.BeginCombo("##biome", rule.Biomes.PrintRange(rule.Not.Biomes, out var fullList), C.ComboSize))
                            {
                                FiltersSelection();
                                foreach (var cond in Enum.GetValues<Biome>())
                                {
                                    if (cond == Biome.No_biome) continue;
                                    var name = cond.ToString().Replace("_", " ");
                                    if (Filters[filterCnt].Length > 0 && !name.Contains(Filters[filterCnt], StringComparison.OrdinalIgnoreCase)) continue;
                                    if (OnlySelected[filterCnt] && !rule.Biomes.Contains(cond)) continue;
                                    if (ThreadLoadImageHandler.TryGetTextureWrap(Path.Combine(Svc.PluginInterface.AssemblyLocation.DirectoryName, "res", "biome", $"{(int)cond}.png"), out var texture))
                                    {
                                        ImGui.Image(texture.ImGuiHandle, iconSize);
                                        ImGui.SameLine();
                                    }
                                    DrawSelector(name, cond, rule.Biomes, rule.Not.Biomes);
                                }
                                ImGui.EndCombo();
                            }
                            if (fullList != null) ImGuiEx.Tooltip(UI.AnyNotice + fullList);
                        }
                        filterCnt++;

                        if (C.Cond_Weather)
                        {
                            ImGui.TableNextColumn();
                            //Weather
                            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                            if (ImGui.BeginCombo("##weather", rule.Weathers.Select(x => P.WeatherManager.Weathers[x]).ToHashSet().PrintRange(rule.Not.Weathers.Select(x => P.WeatherManager.Weathers[x]).ToHashSet(), out var fullList), C.ComboSize))
                            {
                                FiltersSelection();
                                foreach (var cond in P.WeatherManager.WeatherNames)
                                {
                                    var name = cond.Key;
                                    if (name.IsNullOrEmpty()) continue;
                                    if (Filters[filterCnt].Length > 0 && !name.Contains(Filters[filterCnt], StringComparison.OrdinalIgnoreCase)) continue;
                                    if (OnlySelected[filterCnt] && !rule.Weathers.ContainsAny(cond.Value)) continue;
                                    if (ThreadLoadImageHandler.TryGetIconTextureWrap((uint)Svc.Data.GetExcelSheet<Weather>().GetRow(cond.Value.First()).Icon, false, out var texture))
                                    {
                                        ImGui.Image(texture.ImGuiHandle, iconSize);
                                        ImGui.SameLine();
                                    }
                                    DrawSelector($"{cond.Key}##{cond.Value.First()}", P.WeatherManager.WeatherNames[cond.Key], rule.Weathers, rule.Not.Weathers);
                                }
                                ImGui.EndCombo();
                            }
                            if (fullList != null) ImGuiEx.Tooltip(UI.AnyNotice + fullList);
                        }
                        filterCnt++;

                        if (C.Cond_Time)
                        {
                            ImGui.TableNextColumn();
                            //Time
                            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                            if (ImGui.BeginCombo("##Time", rule.Times.PrintRange(rule.Not.Times, out var fullList), C.ComboSize))
                            {
                                FiltersSelection();
                                foreach (var cond in Enum.GetValues<ETime>())
                                {
                                    var name = cond.ToString().Replace("_", " ");
                                    if (Filters[filterCnt].Length > 0 && !name.Contains(Filters[filterCnt], StringComparison.OrdinalIgnoreCase)) continue;
                                    if (OnlySelected[filterCnt] && !rule.Times.Contains(cond)) continue;
                                    DrawSelector(name, cond, rule.Times, rule.Not.Times);
                                    ImGuiEx.Tooltip($"{ETimeChecker.Names[cond]}");
                                }
                                ImGui.EndCombo();
                            }
                            if (fullList != null) ImGuiEx.Tooltip(UI.AnyNotice + fullList);
                        }
                        filterCnt++;

                        if (C.Cond_ZoneGroup)
                        {
                            ImGui.TableNextColumn();
                            //Zone groups
                            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                            if (ImGui.BeginCombo("##zgroup", rule.SpecialTerritories.Select(x => SpecialTerritoryChecker.Renames.TryGetValue(x, out var s)?s:x.ToString().Replace("_", " ")).PrintRange(rule.Not.SpecialTerritories.Select(x => SpecialTerritoryChecker.Renames.TryGetValue(x, out var s) ? s : x.ToString().Replace("_", " ")), out var fullList), C.ComboSize))
                            {
                                FiltersSelection();
                                foreach (var cond in Enum.GetValues<SpecialTerritory>())
                                {
                                    var name = SpecialTerritoryChecker.Renames.TryGetValue(cond, out var s) ? s : cond.ToString().Replace("_", " ");
                                    if (Filters[filterCnt].Length > 0 && !name.Contains(Filters[filterCnt], StringComparison.OrdinalIgnoreCase)) continue;
                                    if (OnlySelected[filterCnt] && !rule.SpecialTerritories.Contains(cond)) continue;
                                    if (ThreadLoadImageHandler.TryGetTextureWrap(Path.Combine(Svc.PluginInterface.AssemblyLocation.DirectoryName, "res", "zgrp", $"{(int)cond}.png"), out var texture))
                                    {
                                        ImGui.Image(texture.ImGuiHandle, iconSize);
                                        ImGui.SameLine();
                                    }
                                    DrawSelector(name, cond, rule.SpecialTerritories, rule.Not.SpecialTerritories);
                                }
                                ImGui.EndCombo();
                            }
                            if (fullList != null) ImGuiEx.Tooltip(UI.AnyNotice + fullList);
                        }
                        filterCnt++;

                        if (C.Cond_Zone)
                        {
                            ImGui.TableNextColumn();
                            //Zone
                            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                            if (ImGui.BeginCombo("##zone", rule.Territories.Select(x => ExcelTerritoryHelper.GetName(x)).PrintRange(rule.Not.Territories.Select(x => ExcelTerritoryHelper.GetName(x)), out var fullList), C.ComboSize))
                            {
                                if (C.AllowNegativeConditions)
                                {
                                    if (ImGui.Selectable(Lang.OpenAllowListEditor))
                                    {
                                        new TerritorySelector(rule.Territories, (terr, s) =>
                                        {
                                            rule.Territories = [.. s];
                                            rule.Not.Territories.RemoveAll(x => rule.Territories.Contains(x));
                                        })
                                        {
                                            ActionDrawPlaceName = DrawPlaceName,
                                            WindowName = Lang.SelectAllowListZones
                                        };
                                    }
                                    if (ImGui.Selectable(Lang.OpenDenyListEditor))
                                    {
                                        new TerritorySelector(rule.Territories, (terr, s) =>
                                        {
                                            rule.Not.Territories = [.. s];
                                            rule.Territories.RemoveAll(x => rule.Not.Territories.Contains(x));
                                        })
                                        {
                                            ActionDrawPlaceName = DrawPlaceName,
                                            WindowName = Lang.SelectDenyListZones
                                        };
                                    }
                                }
                                else
                                {
                                    new TerritorySelector(rule.Territories, (terr, s) => rule.Territories = [.. s])
                                    {
                                        ActionDrawPlaceName = DrawPlaceName
                                    };
                                    ImGui.CloseCurrentPopup();
                                }
                                ImGui.EndCombo();
                            }
                            if (fullList != null) ImGuiEx.Tooltip(UI.AnyNotice + fullList);
                        }

                        if(C.Cond_House)
                        {
                            ImGui.TableNextColumn();
                            //House
                            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                            if (ImGui.BeginCombo("##house", rule.Houses.Select(x => C.Houses.FirstOrDefault(h => h.ID == x)?.Name ?? $"{x:X16}").PrintRange(rule.Not.Houses.Select(x => C.Houses.FirstOrDefault(h => h.ID == x)?.Name ?? $"{x:X16}"), out var fullList), C.ComboSize))
                            {
                                FiltersSelection();
                                foreach (var z in C.Houses)
                                {
                                    var name = z.Name;
                                    if (Filters[filterCnt].Length > 0 && !name.Contains(Filters[filterCnt], StringComparison.OrdinalIgnoreCase)) continue;
                                    if (OnlySelected[filterCnt] && !rule.Houses.Contains(z.ID)) continue;
                                    DrawSelector(name + $"##{z.GUID}", z.ID, rule.Houses, rule.Not.Houses);
                                }
                                foreach (var z in rule.Houses)
                                {
                                    if (!C.Houses.Any(h => h.ID == z))
                                    {
                                        ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.DalamudRed);
                                        ImGuiEx.CollectionCheckbox($"{z}", z, rule.Houses, delayedOperation: true);
                                        ImGui.PopStyleColor();
                                    }
                                }
                                ImGui.EndCombo();
                            }
                            if (fullList != null) ImGuiEx.Tooltip(UI.AnyNotice + fullList);
                        }
                        filterCnt++;

                        if (C.Cond_Emote)
                        {
                            ImGui.TableNextColumn();
                            //Emote
                            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                            if (ImGui.BeginCombo("##emote", rule.Emotes.Select(x => Svc.Data.GetExcelSheet<Emote>().GetRow(x).Name.ExtractText()).PrintRange(rule.Not.Emotes.Select(x => Svc.Data.GetExcelSheet<Emote>().GetRow(x).Name.ExtractText()), out var fullList), C.ComboSize))
                            {
                                FiltersSelection();

                                if (Player.Available && Utils.GetAdjustedEmote() != 0)
                                {
                                    var id = Utils.GetAdjustedEmote();
                                    var cond = Svc.Data.GetExcelSheet<Emote>().GetRow(id);
                                    if (ThreadLoadImageHandler.TryGetIconTextureWrap(cond.Icon, false, out var texture))
                                    {
                                        ImGui.Image(texture.ImGuiHandle, iconSize);
                                        ImGui.SameLine();
                                    }
                                    ImGui.PushStyleColor(ImGuiCol.Text, EColor.CyanBright);
                                    DrawSelector(
                                        Lang.CurrentEmote
                                        .Params(cond.RowId, cond.Name.ExtractText().NullWhenEmpty() ?? $"Unnamed")
                                        +"##{cond.RowId}", cond.RowId, rule.Emotes, rule.Not.Emotes);
                                    ImGui.PopStyleColor();
                                    ImGui.Separator();
                                }

                                foreach (var cond in Svc.Data.GetExcelSheet<Emote>().Where(e => e.Name?.ExtractText().IsNullOrEmpty() == false || e.Icon != 0 || rule.Emotes.Contains(e.RowId)))
                                {
                                    var name = cond.Name?.ExtractText() ?? "";
                                    if (Filters[filterCnt].Length > 0 && !name.Contains(Filters[filterCnt], StringComparison.OrdinalIgnoreCase)) continue;
                                    if (OnlySelected[filterCnt] && !rule.Emotes.Contains(cond.RowId)) continue;
                                    if (ThreadLoadImageHandler.TryGetIconTextureWrap(cond.Icon, false, out var texture))
                                    {
                                        ImGui.Image(texture.ImGuiHandle, iconSize);
                                        ImGui.SameLine();
                                    }
                                    DrawSelector($"{name.NullWhenEmpty() ?? $"Unnamed/{cond.RowId}"}##{cond.RowId}", cond.RowId, rule.Emotes, rule.Not.Emotes);
                                }
                                ImGui.EndCombo();
                            }
                            if (fullList != null) ImGuiEx.Tooltip(UI.AnyNotice + fullList);
                        }
                        filterCnt++;

                        if (C.Cond_Job)
                        {
                            ImGui.TableNextColumn();
                            //Job
                            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                            if (ImGui.BeginCombo("##job", rule.Jobs.PrintRange(rule.Not.Jobs, out var fullList), C.ComboSize))
                            {
                                FiltersSelection();
                                foreach (var cond in Enum.GetValues<Job>().OrderByDescending(x => Svc.Data.GetExcelSheet<ClassJob>().GetRow((uint)x).Role))
                                {
                                    if (cond == Job.ADV) continue;
                                    if (cond.IsUpgradeable() && C.UnifyJobs) continue;
                                    var name = cond.ToString().Replace("_", " ");
                                    if (Filters[filterCnt].Length > 0 && !name.Contains(Filters[filterCnt], StringComparison.OrdinalIgnoreCase)) continue;
                                    if (OnlySelected[filterCnt] && !rule.Jobs.Contains(cond)) continue;
                                    if (ThreadLoadImageHandler.TryGetIconTextureWrap((uint)cond.GetIcon(), false, out var texture))
                                    {
                                        ImGui.Image(texture.ImGuiHandle, iconSize);
                                        ImGui.SameLine();
                                    }
                                    if (cond.IsUpgradeable()) ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.DalamudGrey3);
                                    DrawSelector(name, cond, rule.Jobs, rule.Not.Jobs);
                                    if (cond.IsUpgradeable()) ImGui.PopStyleColor();
                                }
                                ImGui.EndCombo();
                            }
                            if (fullList != null) ImGuiEx.Tooltip(UI.AnyNotice + fullList);
                        }
                        filterCnt++;

                        if (C.Cond_World)
                        {
                            ImGui.TableNextColumn();
                            //World
                            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                            if (ImGui.BeginCombo("##world", rule.Worlds.ToWorldNames().PrintRange(rule.Not.Worlds.ToWorldNames(), out var fullList), C.ComboSize))
                            {
                                FiltersSelection();
                                foreach (var dc in ExcelWorldHelper.GetDataCenters(Enum.GetValues<ExcelWorldHelper.Region>()))
                                {
                                    var worlds = ExcelWorldHelper.GetPublicWorlds().Where(x => x.DataCenter.Row == dc.RowId);
                                    ImGuiEx.Text($"{dc.Name}");
                                    foreach (var cond in worlds)
                                    {
                                        var name = cond.Name.ToString();
                                        if (Filters[filterCnt].Length > 0 && !name.Contains(Filters[filterCnt], StringComparison.OrdinalIgnoreCase)) continue;
                                        if (OnlySelected[filterCnt] && !rule.Worlds.Contains(cond.RowId)) continue;
                                        ImGuiEx.Spacing();
                                        DrawSelector(name, cond.RowId, rule.Worlds, rule.Not.Worlds);
                                    }
                                }
                                ImGui.EndCombo();
                            }
                            if (fullList != null) ImGuiEx.Tooltip(UI.AnyNotice + fullList);
                        }
                        filterCnt++;

                        if (C.Cond_Gearset)
                        {
                            if(EzThrottler.Throttle("UpdateGS", 5000)) Utils.UpdateGearsetCache();
                            ImGui.TableNextColumn();
                            //Gearset
                            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                            var gch = Profile.Characters.FirstOrDefault();
                            if (ImGui.BeginCombo("##gs", rule.Gearsets.ToGearsetNames(gch).PrintRange(rule.Not.Gearsets.ToGearsetNames(gch), out var fullList), C.ComboSize))
                            {
                                FiltersSelection();
                                if (!C.GearsetNameCacheCID.TryGetValue(gch, out var gearsets)) gearsets = [];
                                foreach (var cond in gearsets)
                                {
                                    var name = cond.ToString();
                                    if (Filters[filterCnt].Length > 0 && !name.Contains(Filters[filterCnt], StringComparison.OrdinalIgnoreCase)) continue;
                                    if (OnlySelected[filterCnt] && !rule.Gearsets.Contains(cond.Id)) continue;
                                    if (ThreadLoadImageHandler.TryGetIconTextureWrap((uint)((Job)cond.ClassJob).GetIcon(), false, out var texture))
                                    {
                                        ImGui.Image(texture.ImGuiHandle, iconSize);
                                        ImGui.SameLine();
                                    }
                                    DrawSelector(name, cond.Id, rule.Gearsets, rule.Not.Gearsets);
                                }
                                foreach (var z in rule.Gearsets)
                                {
                                    if (!gearsets.Any(h => h.Id == z))
                                    {
                                        ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.DalamudRed);
                                        ImGuiEx.CollectionCheckbox($"{z}", z, rule.Gearsets, delayedOperation: true);
                                        ImGui.PopStyleColor();
                                    }
                                }
                                ImGui.EndCombo();
                            }
                            
                            if (fullList != null) ImGuiEx.Tooltip(UI.AnyNotice + fullList);
                        }
                        filterCnt++;

                        ImGui.TableNextColumn();

                        {
                            //Glamour
                            ImGuiEx.SetNextItemFullWidth();
                            if (ImGui.BeginCombo("##glamour", rule.SelectedPresets.PrintRange(out var fullList, "- None -"), C.ComboSize))
                            {
                                FiltersSelection();
                                var designs = Profile.GetPresetsUnion().OrderBy(x => x.Name);
                                foreach (var x in designs)
                                {
                                    var name = x.Name;
                                    if (Filters[filterCnt].Length > 0 && !name.Contains(Filters[filterCnt], StringComparison.OrdinalIgnoreCase)) continue;
                                    if (OnlySelected[filterCnt] && !rule.SelectedPresets.Contains(name)) continue;
                                    if (x.GetFolder(Profile)?.HiddenFromSelection == true) continue;
                                    ImGuiEx.CollectionCheckbox($"{x.CensoredName}##{x.GUID}", x.Name, rule.SelectedPresets);
                                }
                                foreach (var x in rule.SelectedPresets)
                                {
                                    if (designs.Any(d => d.Name == x && d.GetFolder(Profile)?.HiddenFromSelection != true)) continue;
                                    ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.DalamudRed);
                                    ImGuiEx.CollectionCheckbox($"{x}", x, rule.SelectedPresets, false, true);
                                    ImGui.PopStyleColor();
                                }
                                ImGui.EndCombo();
                            }
                            if (fullList != null) ImGuiEx.Tooltip(UI.RandomNotice + fullList);
                            filterCnt++;
                        }

                        ImGui.TableNextColumn();
                        //Delete
                        if (ImGuiEx.IconButton(FontAwesomeIcon.Copy))
                        {
                            Safe(() => Clipboard.SetText(JsonConvert.SerializeObject(rule)));
                        }
                        ImGui.SameLine();
                        if (ImGuiEx.IconButton(FontAwesomeIcon.Trash) && ImGui.GetIO().KeyCtrl)
                        {
                            new TickScheduler(() => Profile.Rules.RemoveAll(x => x.GUID == rule.GUID));
                        }
                        ImGuiEx.Tooltip(Lang.HoldCTRLClickToDelete);

                        if (col) ImGui.PopStyleColor();
                        if (col2) ImGui.PopStyleColor();
                        ImGui.PopID();
                    }

                    ImGui.EndTable();
                    foreach (var x in MoveCommands)
                    {
                        ImGui.SetCursorPos(x.ButtonPos);
                        x.BeginDraw();
                        x.AcceptDraw();
                        ImGui.SetCursorPos(x.RowPos);
                        ImGui.Dummy(new Vector2(ImGui.GetContentRegionAvail().X, ImGuiHelpers.GetButtonSize(" ").Y));
                        x.AcceptDraw();
                    }
                }
                ImGui.PopStyleVar();
            }
            else
            {
                UI.ProfileSelectorCommon();
            }
        }

        private static void DrawPlaceName(TerritoryType t, Vector4? nullable, string arg2)
        {
            var cond = t.FindBiome();
            if (cond != Biome.No_biome && ThreadLoadImageHandler.TryGetTextureWrap(Path.Combine(Svc.PluginInterface.AssemblyLocation.DirectoryName, "res", "biome", $"{(int)cond}.png"), out var texture))
            {
                ImGui.Image(texture.ImGuiHandle, iconSize);
                ImGui.SameLine();
            }
            ImGuiEx.Text(nullable, arg2);
        }
        static void DrawSelector<T>(string name, T value, ICollection<T> values, ICollection<T> notValues) => DrawSelector(name, [value], values, notValues);

        static void DrawSelector<T>(string name, IEnumerable<T> value, ICollection<T> values, ICollection<T> notValues)
        {
            var buttonSize = ImGuiHelpers.GetButtonSize(" ");
            var size = new Vector2(buttonSize.Y);
            sbyte s = 0;
            if (values.ContainsAny(value)) s = 1;
            if (notValues.ContainsAny(value)) s = -1;

            var checkbox = new TristateCheckboxEx();

            if (checkbox.Draw(name, s, out s))
            {
                if(!C.AllowNegativeConditions && s == -1)
                {
                    s = 0;
                }
                if(s == 1)
                {
                    foreach (var v in value)
                    {
                        notValues.Remove(v);
                        values.Add(v);
                    }
                }
                else if(s == 0)
                {
                    foreach (var v in value)
                    {
                        notValues.Remove(v);
                        values.Remove(v);
                    }
                }
                else
                {
                    foreach (var v in value)
                    {
                        notValues.Add(v);
                        values.Remove(v);
                    }
                }
            }
            if (s == -1)
            {
                ImGuiEx.Tooltip(Lang.DenyConditionsTooltip);
            }
        }
    }
}
