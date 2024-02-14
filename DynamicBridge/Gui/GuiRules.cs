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

namespace DynamicBridge.Gui
{
    public unsafe static class GuiRules
    {
        static Vector2 iconSize => new Vector2(24f.Scale(), 24f.Scale());
        static string[] Filters = ["", "", "", "", "", "", "", "", "", "", "", "", "", "", ""];
        static bool[] OnlySelected = new bool[15];
        static string CurrentDrag = "";

        public static void Draw()
        {
            UI.ProfileSelectorCommon();
            if (Utils.Profile(UI.CurrentCID) != null)
            {
                var Profile = Utils.Profile(UI.CurrentCID);
                Profile.Rules.RemoveAll(x => x == null);
                if (ImGui.Button("Add new rule"))
                {
                    Profile.Rules.Add(new());
                }
                ImGui.SameLine();
                if (ImGui.Button("Paste from clipboard"))
                {
                    try
                    {
                        Profile.Rules.Add(JsonConvert.DeserializeObject<ApplyRule>(Clipboard.GetText()) ?? throw new NullReferenceException());
                    }
                    catch (Exception e)
                    {
                        Notify.Error("Failed to paste from clipboard:\n" + e.Message);
                    }
                }
                ImGui.SameLine();
                if (ImGui.Button("Apply rules"))
                {
                    P.SoftForceUpdate = true;
                }
                if (ImGuiEx.ButtonCtrl("Blacklist character"))
                {
                    C.Blacklist.Add(Player.CID);
                    UI.SelectedCID = 0;
                }
                ImGui.SameLine();
                if (ImGuiEx.ButtonCtrl("Import profile from clipboard"))
                {
                    try
                    {
                        C.Profiles[UI.CurrentCID] = JsonConvert.DeserializeObject<Profile>(Clipboard.GetText());
                    }
                    catch (Exception e)
                    {
                        Notify.Error(e.Message);
                    }
                }
                ImGui.SameLine();
                if (ImGui.Button("Export profile to clipboard"))
                {
                    Safe(() => Clipboard.SetText(JsonConvert.SerializeObject(Profile)));
                }
                if (Profile.IsStaticExists())
                {
                    ImGui.SameLine();
                    ImGuiEx.Text(EColor.RedBright, $"Preset {Profile.GetStaticPreset()?.Name} is selected as static. Automation disabled.");
                }

                List<(Vector2 RowPos, Vector2 ButtonPos, Action BeginDraw, Action AcceptDraw)> MoveCommands = [];
                if (ImGui.BeginTable("##main", 12, ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders | ImGuiTableFlags.Resizable | ImGuiTableFlags.Reorderable))
                {
                    ImGui.TableSetupColumn("  ", ImGuiTableColumnFlags.NoResize | ImGuiTableColumnFlags.WidthFixed);
                    ImGui.TableSetupColumn("State");
                    ImGui.TableSetupColumn("Biome");
                    ImGui.TableSetupColumn("Weather");
                    ImGui.TableSetupColumn("Time");
                    ImGui.TableSetupColumn("Zone Group");
                    ImGui.TableSetupColumn("Zone");
                    ImGui.TableSetupColumn("House");
                    ImGui.TableSetupColumn("Emote");
                    ImGui.TableSetupColumn("Job");
                    ImGui.TableSetupColumn("Preset");
                    ImGui.TableSetupColumn(" ", ImGuiTableColumnFlags.NoResize | ImGuiTableColumnFlags.WidthFixed);
                    ImGui.TableHeadersRow();

                    for (int i = 0; i < Profile.Rules.Count; i++)
                    {
                        var filterCnt = 0;
                        void FiltersSelection()
                        {
                            ImGui.SetWindowFontScale(0.8f);
                            ImGuiEx.SetNextItemFullWidth();
                            ImGui.InputTextWithHint($"##fltr{filterCnt}", "Filter...", ref Filters[filterCnt], 50);
                            ImGui.Checkbox($"Only selected##{filterCnt}", ref OnlySelected[filterCnt]);
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
                        ImGuiEx.Tooltip("Enable this rule");

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
                        ImGuiEx.Tooltip("Enable passthrough for this rule. DynamicBridge will continue searching after encountering this rule. All valid found rules will be applied one after another sequentially.");

                        ImGui.TableNextColumn();

                        {
                            //Conditions
                            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                            if (ImGui.BeginCombo("##conditions", rule.States.PrintRange(rule.Not.States, out var fullList)))
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
                            filterCnt++;
                        }

                        ImGui.TableNextColumn();

                        {
                            //Biome
                            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                            if (ImGui.BeginCombo("##biome", rule.Biomes.PrintRange(rule.Not.Biomes, out var fullList)))
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
                            filterCnt++;
                        }

                        ImGui.TableNextColumn();

                        {
                            //Weather
                            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                            if (ImGui.BeginCombo("##weather", rule.Weathers.Select(x => P.WeatherManager.Weathers[x]).ToHashSet().PrintRange(rule.Not.Weathers.Select(x => P.WeatherManager.Weathers[x]).ToHashSet(), out var fullList)))
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
                            filterCnt++;
                        }

                        ImGui.TableNextColumn();

                        {
                            //Time
                            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                            if (ImGui.BeginCombo("##Time", rule.Times.PrintRange(rule.Not.Times, out var fullList)))
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
                            filterCnt++;
                        }

                        ImGui.TableNextColumn();

                        {
                            //Zone groups
                            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                            if (ImGui.BeginCombo("##zgroup", rule.SpecialTerritories.Select(x => SpecialTerritoryChecker.Renames.TryGetValue(x, out var s)?s:x.ToString().Replace("_", " ")).PrintRange(rule.Not.SpecialTerritories.Select(x => SpecialTerritoryChecker.Renames.TryGetValue(x, out var s) ? s : x.ToString().Replace("_", " ")), out var fullList)))
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
                            filterCnt++;
                        }

                        ImGui.TableNextColumn();

                        {
                            //Zone
                            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                            if (ImGui.BeginCombo("##zone", rule.Territories.Select(x => ExcelTerritoryHelper.GetName(x)).PrintRange(rule.Not.Territories.Select(x => ExcelTerritoryHelper.GetName(x)), out var fullList)))
                            {
                                if (C.AllowNegativeConditions)
                                {
                                    if (ImGui.Selectable("Open allow list editor"))
                                    {
                                        new TerritorySelector(rule.Territories, (terr, s) =>
                                        {
                                            rule.Territories = [.. s];
                                            rule.Not.Territories.RemoveAll(x => rule.Territories.Contains(x));
                                        })
                                        {
                                            ActionDrawPlaceName = DrawPlaceName,
                                            WindowName = $"Select allow list zones"
                                        };
                                    }
                                    if (ImGui.Selectable("Open deny list editor"))
                                    {
                                        new TerritorySelector(rule.Territories, (terr, s) =>
                                        {
                                            rule.Not.Territories = [.. s];
                                            rule.Territories.RemoveAll(x => rule.Not.Territories.Contains(x));
                                        })
                                        {
                                            ActionDrawPlaceName = DrawPlaceName,
                                            WindowName = $"Select deny list zones"
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

                        ImGui.TableNextColumn();

                        {
                            //House
                            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                            if (ImGui.BeginCombo("##house", rule.Houses.Select(x => C.Houses.FirstOrDefault(h => h.ID == x)?.Name ?? $"{x:X16}").PrintRange(rule.Not.Houses.Select(x => C.Houses.FirstOrDefault(h => h.ID == x)?.Name ?? $"{x:X16}"), out var fullList)))
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
                            filterCnt++;
                        }

                        ImGui.TableNextColumn();

                        {
                            //Emote
                            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                            if (ImGui.BeginCombo("##emote", rule.Emotes.Select(x => Svc.Data.GetExcelSheet<Emote>().GetRow(x).Name.ExtractText()).PrintRange(rule.Not.Emotes.Select(x => Svc.Data.GetExcelSheet<Emote>().GetRow(x).Name.ExtractText()), out var fullList)))
                            {
                                FiltersSelection();

                                if (Player.Available && Player.Object.Character()->EmoteController.EmoteId != 0)
                                {
                                    var id = Player.Object.Character()->EmoteController.EmoteId;
                                    var cond = Svc.Data.GetExcelSheet<Emote>().GetRow(id);
                                    if (ThreadLoadImageHandler.TryGetIconTextureWrap(cond.Icon, false, out var texture))
                                    {
                                        ImGui.Image(texture.ImGuiHandle, iconSize);
                                        ImGui.SameLine();
                                    }
                                    ImGui.PushStyleColor(ImGuiCol.Text, EColor.CyanBright);
                                    DrawSelector($"Current: {id}/{cond.Name.ExtractText()}##{cond.RowId}", cond.RowId, rule.Emotes, rule.Not.Emotes);
                                    ImGui.PopStyleColor();
                                    ImGui.Separator();
                                }

                                foreach (var cond in Svc.Data.GetExcelSheet<Emote>().Where(e => e.Name?.ExtractText().IsNullOrEmpty() == false || e.Icon != 0))
                                {
                                    var name = cond.Name?.ExtractText() ?? "";
                                    if (Filters[filterCnt].Length > 0 && !name.Contains(Filters[filterCnt], StringComparison.OrdinalIgnoreCase)) continue;
                                    if (OnlySelected[filterCnt] && !rule.Emotes.Contains(cond.RowId)) continue;
                                    if (ThreadLoadImageHandler.TryGetIconTextureWrap(cond.Icon, false, out var texture))
                                    {
                                        ImGui.Image(texture.ImGuiHandle, iconSize);
                                        ImGui.SameLine();
                                    }
                                    DrawSelector($"{name}##{cond.RowId}", cond.RowId, rule.Emotes, rule.Not.Emotes);
                                }
                                ImGui.EndCombo();
                            }
                            if (fullList != null) ImGuiEx.Tooltip(UI.AnyNotice + fullList);
                            filterCnt++;
                        }

                        ImGui.TableNextColumn();

                        {
                            //Job
                            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                            rule.Jobs.RemoveAll(x => x.IsUpgradeable());
                            if (ImGui.BeginCombo("##job", rule.Jobs.PrintRange(rule.Not.Jobs, out var fullList)))
                            {
                                FiltersSelection();
                                foreach (var cond in Enum.GetValues<Job>().Where(x => !x.IsUpgradeable()).OrderByDescending(x => Svc.Data.GetExcelSheet<ClassJob>().GetRow((uint)x).Role))
                                {
                                    if (cond == Job.ADV) continue;
                                    var name = cond.ToString().Replace("_", " ");
                                    if (Filters[filterCnt].Length > 0 && !name.Contains(Filters[filterCnt], StringComparison.OrdinalIgnoreCase)) continue;
                                    if (OnlySelected[filterCnt] && !rule.Jobs.Contains(cond)) continue;
                                    if (ThreadLoadImageHandler.TryGetIconTextureWrap((uint)cond.GetIcon(), false, out var texture))
                                    {
                                        ImGui.Image(texture.ImGuiHandle, iconSize);
                                        ImGui.SameLine();
                                    }
                                    DrawSelector(name, cond, rule.Jobs, rule.Not.Jobs);
                                }
                                ImGui.EndCombo();
                            }
                            if (fullList != null) ImGuiEx.Tooltip(UI.AnyNotice + fullList);
                            filterCnt++;
                        }

                        ImGui.TableNextColumn();

                        {
                            //Glamour
                            ImGuiEx.SetNextItemFullWidth();
                            if (ImGui.BeginCombo("##glamour", rule.SelectedPresets.PrintRange(out var fullList, "- None -")))
                            {
                                FiltersSelection();
                                var designs = Profile.GetPresetsUnion().OrderBy(x => x.Name).Where(x => !x.IsStaticCategory);
                                foreach (var x in designs)
                                {
                                    var name = x.Name;
                                    if (Filters[filterCnt].Length > 0 && !name.Contains(Filters[filterCnt], StringComparison.OrdinalIgnoreCase)) continue;
                                    if (OnlySelected[filterCnt] && !rule.SelectedPresets.Contains(name)) continue;
                                    if (x.GetFolder(Profile)?.HiddenFromSelection == true) continue;
                                    ImGuiEx.CollectionCheckbox($"{name}##{x.GUID}", x.Name, rule.SelectedPresets);
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
                        ImGuiEx.Tooltip("Hold CTRL+Click to delete");

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
            bool? s = false;
            if (values.ContainsAny(value)) s = true;
            if (notValues.ContainsAny(value)) s = null;

            if(ImGuiEx.Checkbox(name, ref s))
            {
                if(!C.AllowNegativeConditions && s == null)
                {
                    s = true;
                }
                if(s == true)
                {
                    foreach (var v in value)
                    {
                        notValues.Remove(v);
                        values.Add(v);
                    }
                }
                else if(s == false)
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
            if (s == null)
            {
                ImGuiEx.Tooltip($"If matching any condition with dot, rule will not be applied.");
            }
        }
    }
}
