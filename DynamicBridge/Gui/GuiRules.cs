using DynamicBridge.Configuration;
using DynamicBridge.Core;
using ECommons.Configuration;
using ECommons.ExcelServices;
using ECommons.GameHelpers;
using ECommons.ImGuiMethods.TerritorySelection;
using ECommons.Throttlers;
using Lumina.Excel.Sheets;
using Newtonsoft.Json;
using System.IO;
using Action = System.Action;
using Emote = Lumina.Excel.Sheets.Emote;
using Weather = Lumina.Excel.Sheets.Weather;

namespace DynamicBridge.Gui;

public static unsafe class GuiRules
{
    private static Vector2 iconSize => new(24f);

    private static string[] Filters = ["", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", ""];
    private static bool[] OnlySelected = new bool[20];
    private static string CurrentDrag = "";
    private static Dictionary<int, bool> showDayNightCycleDict = [];
    private static ImGuiEx.RealtimeDragDrop<ApplyRule> DragDrop = new("MoveRuleItem", (x) => x.GUID);
    private static string Open = null;
    private static bool Focus = false;
    public static void Draw()
    {
        if(UI.Profile != null)
        {
            var currentProfile = UI.Profile;
            currentProfile.Rules.RemoveAll(x => x == null);
            void ButtonsLeft()
            {
                if(ImGuiEx.IconButton(FontAwesomeIcon.Plus))
                {
                    if(Open != null && currentProfile.RulesFolders.TryGetFirst(x => x.GUID == Open, out var open))
                    {
                        open.Rules.Add(new());
                    }
                    else
                    {
                        currentProfile.Rules.Add(new());
                    }
                }
                ImGuiEx.Tooltip("Add new rule");

                ImGui.SameLine();
                if(ImGuiEx.IconButton(FontAwesomeIcon.Paste))
                {
                    try
                    {
                        var folder = EzConfig.DefaultSerializationFactory.Deserialize<ApplyRuleFolder>(Paste()) ?? throw new NullReferenceException();
                        if(folder.Rules.Count == 0) throw new InvalidOperationException();
                        currentProfile.RulesFolders.Add(folder);
                    }
                    catch(Exception ex)
                    {
                        ex.LogDebug();
                        try
                        {
                            var str = (EzConfig.DefaultSerializationFactory.Deserialize<ApplyRule>(Paste()));
                            if(str != null)
                            {
                                if(Open != null && currentProfile.RulesFolders.TryGetFirst(x => x.GUID == Open, out var open))
                                {
                                    open.Rules.Add(str);
                                }
                                else
                                {
                                    currentProfile.Rules.Add(str);
                                }
                            }
                            else
                            {
                                Notify.Error("Could not import from clipboard");
                            }
                        }
                        catch(Exception e)
                        {
                            Notify.Error(e.Message);
                        }
                    }
                }
                ImGuiEx.Tooltip($"Paste previously copied rule or folder from clipboard");
                ImGui.SameLine();

                if(ImGuiEx.IconButton(FontAwesomeIcon.FolderPlus))
                {
                    currentProfile.RulesFolders.Add(new() { Name = $"Rule folder {currentProfile.RulesFolders.Count + 1}" });
                }
                ImGuiEx.Tooltip("Add new rule folder");

                if(currentProfile.IsStaticExists())
                {
                    ImGuiEx.HelpMarker($"Preset {currentProfile.GetStaticPreset()?.CensoredName} is selected as static. Automation disabled.", GradientColor.Get(EColor.RedBright, EColor.YellowBright, 1000), FontAwesomeIcon.ExclamationTriangle.ToIconString());
                }
                ImGui.SameLine();
            }
            void ButtonsRight()
            {
                UI.ForceUpdateButton();
                ImGui.SameLine();
            }

            UI.ProfileSelectorCommon(ButtonsLeft, ButtonsRight);

            string newOpen = null;

            if(!Focus || Open == "" || Open == null)
            {
                if(ImGuiEx.TreeNode($"Main rules##globalrules", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    CollapsingHeaderClicked(currentProfile, -1, null);
                    newOpen = "";
                    if(DragDrop.AcceptPayload(out var result, ImGuiDragDropFlags.AcceptBeforeDelivery | ImGuiDragDropFlags.AcceptNoDrawDefaultRect))
                    {
                        DragDropUtils.AcceptFolderDragDrop(currentProfile, result, currentProfile.Rules);
                    }
                    DrawRuleFolder(currentProfile, currentProfile.Rules, out var postAction, "");
                    ImGui.TreePop();
                    postAction?.Invoke();
                }
                else
                {
                    CollapsingHeaderClicked(currentProfile, -1, null);
                    if(DragDrop.AcceptPayload(out var result, ImGuiDragDropFlags.AcceptBeforeDelivery | ImGuiDragDropFlags.AcceptNoDrawDefaultRect))
                    {
                        DragDropUtils.AcceptFolderDragDrop(currentProfile, result, currentProfile.Rules);
                    }
                }
            }

            for(var ruleFolderIndex = 0; ruleFolderIndex < currentProfile.RulesFolders.Count; ruleFolderIndex++)
            {
                var rulesFolder = currentProfile.RulesFolders[ruleFolderIndex];
                if(Focus && Open != rulesFolder.GUID && Open != null) continue;
                if(!rulesFolder.Enabled)
                {
                    ImGuiEx.RightFloat($"Disabled{rulesFolder.GUID}", () => ImGuiEx.Text(ImGuiColors.DalamudRed, "Disabled"));
                }
                if(ImGuiEx.TreeNode($"{rulesFolder.Name}###rulefolder{rulesFolder.GUID}"))
                {
                    newOpen = rulesFolder.GUID;
                    CollapsingHeaderClicked(currentProfile, ruleFolderIndex, rulesFolder);
                    if(DragDrop.AcceptPayload(out var result, ImGuiDragDropFlags.AcceptBeforeDelivery | ImGuiDragDropFlags.AcceptNoDrawDefaultRect))
                    {
                        DragDropUtils.AcceptFolderDragDrop(currentProfile, result, rulesFolder.Rules);
                    }
                    DrawRuleFolder(currentProfile, rulesFolder.Rules, out var postAction, rulesFolder.GUID);
                    ImGui.TreePop();
                    postAction?.Invoke();
                }
                else
                {
                    CollapsingHeaderClicked(currentProfile, ruleFolderIndex, rulesFolder);
                    if(DragDrop.AcceptPayload(out var result, ImGuiDragDropFlags.AcceptBeforeDelivery | ImGuiDragDropFlags.AcceptNoDrawDefaultRect))
                    {
                        DragDropUtils.AcceptFolderDragDrop(currentProfile, result, rulesFolder.Rules);
                    }
                }
            }
            Open = newOpen;

        }
        else
        {
            UI.ProfileSelectorCommon();
        }
    }

    private static void CollapsingHeaderClicked(Profile profile, int ruleFolderIndex, ApplyRuleFolder ruleFolder)
    {
        if(ImGui.IsItemHovered() && ImGui.IsItemClicked(ImGuiMouseButton.Right)) ImGui.OpenPopup($"Folder{ruleFolder?.GUID}");
        if(ImGui.BeginPopup($"Folder{ruleFolder?.GUID}"))
        {
            if(ruleFolder != null)
            {
                ImGuiEx.SetNextItemWidthScaled(150f);
                ImGui.InputTextWithHint("##name", "Folder name", ref ruleFolder.Name, 200);
                if(ImGui.Selectable("Export to clipboard"))
                {
                    Copy(EzConfig.DefaultSerializationFactory.Serialize(ruleFolder, false));
                }
                if(ruleFolder.Enabled)
                {
                    if(ImGui.Selectable("Disable")) ruleFolder.Enabled = false;
                }
                else
                {
                    if(ImGui.Selectable("Enable")) ruleFolder.Enabled = true;
                }
                if(ImGui.Selectable("Move up", false, ImGuiSelectableFlags.DontClosePopups) && ruleFolderIndex > 0)
                {
                    (profile.RulesFolders[ruleFolderIndex], profile.RulesFolders[ruleFolderIndex - 1]) = (profile.RulesFolders[ruleFolderIndex - 1], profile.RulesFolders[ruleFolderIndex]);
                }
                if(ImGui.Selectable("Move down", false, ImGuiSelectableFlags.DontClosePopups) && ruleFolderIndex < profile.RulesFolders.Count - 1)
                {
                    (profile.RulesFolders[ruleFolderIndex], profile.RulesFolders[ruleFolderIndex + 1]) = (profile.RulesFolders[ruleFolderIndex + 1], profile.RulesFolders[ruleFolderIndex]);
                }
                ImGui.Separator();

                if(ImGui.BeginMenu("Delete folder..."))
                {
                    if(ImGui.Selectable("...and move rules to default folder (Hold CTRL)"))
                    {
                        if(ImGuiEx.Ctrl)
                        {
                            new TickScheduler(() =>
                            {
                                foreach(var x in ruleFolder.Rules)
                                {
                                    profile.Rules.Add(x);
                                }
                                profile.RulesFolders.Remove(ruleFolder);
                            });
                        }
                    }
                    if(ImGui.Selectable("...and delete included rules (Hold CTRL+SHIFT)"))
                    {
                        if(ImGuiEx.Ctrl && ImGuiEx.Shift)
                        {
                            new TickScheduler(() => profile.RulesFolders.Remove(ruleFolder));
                        }
                    }
                    ImGui.EndMenu();
                }
            }
            else
            {
                if(ImGui.Selectable("Export to clipboard"))
                {
                    Copy(EzConfig.DefaultSerializationFactory.Serialize(new ApplyRuleFolder() { Name = "Exported Default Folder", Rules = profile.Rules }, false));
                }
            }

            ImGui.EndPopup();
        }
        else
        {
            if(!ImGui.IsMouseDragging(ImGuiMouseButton.Left))
            {
                ImGuiEx.Tooltip("Right-click to open context menu");
            }
        }
    }

    static void DrawRuleFolder(Profile currentProfile, List<ApplyRule> rulesList, out Action postAction, string extraID)
    {
        postAction = null;
        var active = (bool[])[
                C.Cond_Delay,
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
                C.Cond_Players,
                C.Cond_OnlineStatus,
            ];

        ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, Utils.CellPadding);
        DragDrop.Begin();
        if(ImGui.BeginTable($"##rules{extraID}", 3 + active.Count(x => x), ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders | ImGuiTableFlags.Resizable | ImGuiTableFlags.Reorderable))
        {
            ImGui.TableSetupColumn("  ", ImGuiTableColumnFlags.NoResize | ImGuiTableColumnFlags.WidthFixed);
            if(C.Cond_State) ImGui.TableSetupColumn("State");
            if(C.Cond_Biome) ImGui.TableSetupColumn("Biome");
            if(C.Cond_Weather) ImGui.TableSetupColumn("Weather");
            if(C.Cond_Time) ImGui.TableSetupColumn("Time");
            if(C.Cond_ZoneGroup) ImGui.TableSetupColumn("Zone Group");
            if(C.Cond_Zone) ImGui.TableSetupColumn("Zone");
            if(C.Cond_House) ImGui.TableSetupColumn("House");
            if(C.Cond_Emote) ImGui.TableSetupColumn("Emote");
            if(C.Cond_Job) ImGui.TableSetupColumn("Job");
            if(C.Cond_World) ImGui.TableSetupColumn("World");
            if(C.Cond_Gearset) ImGui.TableSetupColumn("Gearset");
            if(C.Cond_Players) ImGui.TableSetupColumn("Players");
            if(C.Cond_OnlineStatus) ImGui.TableSetupColumn("Online Status");
            if(C.Cond_Delay) ImGui.TableSetupColumn("Delays");
            ImGui.TableSetupColumn("Preset");
            ImGui.TableSetupColumn(" ", ImGuiTableColumnFlags.NoResize | ImGuiTableColumnFlags.WidthFixed);
            ImGui.TableHeadersRow();

            for(var i = 0; i < rulesList.Count; i++)
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
                var rule = rulesList[i];
                var col = !rule.Enabled;
                var col2 = P.LastRule.Any(x => x.GUID == rule.GUID);
                if(col2) ImGui.PushStyleColor(ImGuiCol.Text, EColor.Green);
                if(col) ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.DalamudGrey3);
                ImGui.PushID(rule.GUID);
                ImGui.TableNextRow();
                DragDrop.SetRowColor(rule.GUID);
                if(CurrentDrag == rule.GUID)
                {
                    var color = GradientColor.Get(EColor.Green, EColor.Green with { W = EColor.Green.W / 4 }, 500).ToUint();
                    ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg0, color);
                    ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg1, color);
                }
                ImGui.TableNextColumn();
                DragDrop.NextRow();

                //Sorting
                var rowPos = ImGui.GetCursorPos();
                ImGui.Checkbox("##enable", ref rule.Enabled);
                ImGuiEx.Tooltip("Enable this rule");

                var moveIndex = i;

                ImGui.SameLine();
                DragDrop.DrawButtonDummy(rule.GUID, (payload) =>
                {
                    DragDropUtils.AcceptProfileDragDrop(currentProfile, payload, rulesList, moveIndex);
                });

                ImGui.SameLine();
                ImGui.PushFont(UiBuilder.IconFont);
                if(col2) ImGui.PushStyleColor(ImGuiCol.Text, EColor.GreenBright);
                ImGuiEx.ButtonCheckbox("\uf103", ref rule.Passthrough);
                if(col2) ImGui.PopStyleColor();
                ImGui.PopFont();
                ImGuiEx.Tooltip("Enable passthrough for this rule. DynamicBridge will continue searching after encountering this rule. All valid found rules will be applied one after another sequentially.");


                if(C.Cond_State)
                {
                    ImGui.TableNextColumn();
                    //Conditions
                    ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                    if(ImGui.BeginCombo("##conditions", rule.States.PrintRange(rule.Not.States, out var fullList), C.ComboSize))
                    {
                        FiltersSelection();
                        foreach(var cond in Enum.GetValues<CharacterState>())
                        {
                            var name = cond.ToString().Replace("_", " ");
                            if(Filters[filterCnt].Length > 0 && !name.Contains(Filters[filterCnt], StringComparison.OrdinalIgnoreCase)) continue;
                            if(OnlySelected[filterCnt] && !rule.States.Contains(cond)) continue;
                            if(ThreadLoadImageHandler.TryGetTextureWrap(Path.Combine(Svc.PluginInterface.AssemblyLocation.DirectoryName, "res", "state", $"{(int)cond}.png"), out var texture))
                            {
                                ImGui.Image(texture.Handle, iconSize);
                                ImGui.SameLine();
                            }
                            DrawSelector(name, cond, rule.States, rule.Not.States);
                        }
                        ImGui.EndCombo();
                    }
                    if(fullList != null) ImGuiEx.Tooltip(UI.AnyNotice + fullList);
                }
                filterCnt++;

                if(C.Cond_Biome)
                {
                    ImGui.TableNextColumn();
                    //Biome
                    ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                    if(ImGui.BeginCombo("##biome", rule.Biomes.PrintRange(rule.Not.Biomes, out var fullList), C.ComboSize))
                    {
                        FiltersSelection();
                        foreach(var cond in Enum.GetValues<Biome>())
                        {
                            if(cond == Biome.No_biome) continue;
                            var name = cond.ToString().Replace("_", " ");
                            if(Filters[filterCnt].Length > 0 && !name.Contains(Filters[filterCnt], StringComparison.OrdinalIgnoreCase)) continue;
                            if(OnlySelected[filterCnt] && !rule.Biomes.Contains(cond)) continue;
                            if(ThreadLoadImageHandler.TryGetTextureWrap(Path.Combine(Svc.PluginInterface.AssemblyLocation.DirectoryName, "res", "biome", $"{(int)cond}.png"), out var texture))
                            {
                                ImGui.Image(texture.Handle, iconSize);
                                ImGui.SameLine();
                            }
                            DrawSelector(name, cond, rule.Biomes, rule.Not.Biomes);
                        }
                        ImGui.EndCombo();
                    }
                    if(fullList != null) ImGuiEx.Tooltip(UI.AnyNotice + fullList);
                }
                filterCnt++;

                if(C.Cond_Weather)
                {
                    ImGui.TableNextColumn();
                    //Weather
                    ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                    if(ImGui.BeginCombo("##weather", rule.Weathers.Select(x => P.WeatherManager.Weathers[x]).ToHashSet().PrintRange(rule.Not.Weathers.Select(x => P.WeatherManager.Weathers[x]).ToHashSet(), out var fullList), C.ComboSize))
                    {
                        FiltersSelection();
                        foreach(var cond in P.WeatherManager.WeatherNames)
                        {
                            var name = cond.Key;
                            if(name.IsNullOrEmpty()) continue;
                            if(Filters[filterCnt].Length > 0 && !name.Contains(Filters[filterCnt], StringComparison.OrdinalIgnoreCase)) continue;
                            if(OnlySelected[filterCnt] && !rule.Weathers.ContainsAny(cond.Value)) continue;
                            if(ThreadLoadImageHandler.TryGetIconTextureWrap((uint)Svc.Data.GetExcelSheet<Weather>().GetRow(cond.Value.First()).Icon, false, out var texture))
                            {
                                ImGui.Image(texture.Handle, iconSize);
                                ImGui.SameLine();
                            }
                            DrawSelector($"{cond.Key}##{cond.Value.First()}", P.WeatherManager.WeatherNames[cond.Key], rule.Weathers, rule.Not.Weathers);
                        }
                        ImGui.EndCombo();
                    }
                    if(fullList != null) ImGuiEx.Tooltip(UI.AnyNotice + fullList);
                }
                filterCnt++;

                if(C.Cond_Time && !C.Cond_Time_Precise)
                {
                    ImGui.TableNextColumn();
                    //Time
                    ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                    if(ImGui.BeginCombo("##Time", rule.Times.PrintRange(rule.Not.Times, out var fullList), C.ComboSize))
                    {
                        FiltersSelection();
                        foreach(var cond in Enum.GetValues<ETime>())
                        {
                            var name = cond.ToString().Replace("_", " ");
                            if(Filters[filterCnt].Length > 0 && !name.Contains(Filters[filterCnt], StringComparison.OrdinalIgnoreCase)) continue;
                            if(OnlySelected[filterCnt] && !rule.Times.Contains(cond)) continue;
                            DrawSelector(name, cond, rule.Times, rule.Not.Times);
                            ImGuiEx.Tooltip($"{ETimeChecker.Names[cond]}");
                        }
                        ImGui.EndCombo();
                    }
                    if(fullList != null) ImGuiEx.Tooltip(UI.AnyNotice + fullList);
                }
                else if(C.Cond_Time && C.Cond_Time_Precise)
                {
                    ImGui.TableNextColumn();
                    if(!showDayNightCycleDict.ContainsKey(i))
                    {
                        showDayNightCycleDict[i] = false;
                    }
                    //Precise Time
                    ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                    if(!showDayNightCycleDict[i] && ImGui.Button($"Open Time Editor##{i}"))
                    {
                        showDayNightCycleDict[i] = true;
                    }
                    else if(showDayNightCycleDict[i] && ImGui.Button($"Close Time Editor##{i}"))
                    {
                        showDayNightCycleDict[i] = false;
                    }
                    var windowPos = ImGui.GetCursorScreenPos();
                    if(showDayNightCycleDict[i])
                    {
                        ImGui.SetNextWindowPos(windowPos);
                        // ImGui.SetNextWindowSize(new Vector2(400, 80), ImGuiCond.Always);
                        var open = showDayNightCycleDict[i];
                        ImGui.Begin($"Time Editor##{i}", ref open, ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoTitleBar);

                        rule.Precise_Times = RenderTimeline(rule.Precise_Times);
                        if(!ImGui.IsWindowHovered(ImGuiHoveredFlags.RootAndChildWindows) && ImGui.IsAnyMouseDown())
                        {
                            showDayNightCycleDict[i] = false;
                        }

                        ImGui.End();
                    }
                }
                filterCnt++;

                if(C.Cond_ZoneGroup)
                {
                    ImGui.TableNextColumn();
                    //Zone groups
                    ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                    if(ImGui.BeginCombo("##zgroup", rule.SpecialTerritories.Select(x => SpecialTerritoryChecker.Renames.TryGetValue(x, out var s) ? s : x.ToString().Replace("_", " ")).PrintRange(rule.Not.SpecialTerritories.Select(x => SpecialTerritoryChecker.Renames.TryGetValue(x, out var s) ? s : x.ToString().Replace("_", " ")), out var fullList), C.ComboSize))
                    {
                        FiltersSelection();
                        foreach(var cond in Enum.GetValues<SpecialTerritory>())
                        {
                            var name = SpecialTerritoryChecker.Renames.TryGetValue(cond, out var s) ? s : cond.ToString().Replace("_", " ");
                            if(Filters[filterCnt].Length > 0 && !name.Contains(Filters[filterCnt], StringComparison.OrdinalIgnoreCase)) continue;
                            if(OnlySelected[filterCnt] && !rule.SpecialTerritories.Contains(cond)) continue;
                            if(ThreadLoadImageHandler.TryGetTextureWrap(Path.Combine(Svc.PluginInterface.AssemblyLocation.DirectoryName, "res", "zgrp", $"{(int)cond}.png"), out var texture))
                            {
                                ImGui.Image(texture.Handle, iconSize);
                                ImGui.SameLine();
                            }
                            DrawSelector(name, cond, rule.SpecialTerritories, rule.Not.SpecialTerritories);
                        }
                        ImGui.EndCombo();
                    }
                    if(fullList != null) ImGuiEx.Tooltip(UI.AnyNotice + fullList);
                }
                filterCnt++;

                if(C.Cond_Zone)
                {
                    ImGui.TableNextColumn();
                    //Zone
                    ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                    if(ImGui.BeginCombo("##zone", rule.Territories.Select(x => ExcelTerritoryHelper.GetName(x)).PrintRange(rule.Not.Territories.Select(x => ExcelTerritoryHelper.GetName(x)), out var fullList), C.ComboSize))
                    {
                        if(C.AllowNegativeConditions)
                        {
                            if(ImGui.Selectable("Open allow list editor"))
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
                            if(ImGui.Selectable("Open deny list editor"))
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
                    if(fullList != null) ImGuiEx.Tooltip(UI.AnyNotice + fullList);
                }

                if(C.Cond_House)
                {
                    ImGui.TableNextColumn();
                    //House
                    ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                    if(ImGui.BeginCombo("##house", rule.Houses.Select(x => C.Houses.FirstOrDefault(h => h.ID == x)?.Name ?? $"{x:X16}").PrintRange(rule.Not.Houses.Select(x => C.Houses.FirstOrDefault(h => h.ID == x)?.Name ?? $"{x:X16}"), out var fullList), C.ComboSize))
                    {
                        FiltersSelection();
                        foreach(var z in C.Houses)
                        {
                            var name = z.Name;
                            if(Filters[filterCnt].Length > 0 && !name.Contains(Filters[filterCnt], StringComparison.OrdinalIgnoreCase)) continue;
                            if(OnlySelected[filterCnt] && !rule.Houses.Contains(z.ID)) continue;
                            DrawSelector(name + $"##{z.GUID}", z.ID, rule.Houses, rule.Not.Houses);
                        }
                        foreach(var z in rule.Houses)
                        {
                            if(!C.Houses.Any(h => h.ID == z))
                            {
                                ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.DalamudRed);
                                ImGuiEx.CollectionCheckbox($"{z}", z, rule.Houses, delayedOperation: true);
                                ImGui.PopStyleColor();
                            }
                        }
                        ImGui.EndCombo();
                    }
                    if(fullList != null) ImGuiEx.Tooltip(UI.AnyNotice + fullList);
                }
                filterCnt++;

                if(C.Cond_Emote)
                {
                    ImGui.TableNextColumn();
                    //Emote
                    ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                    if(ImGui.BeginCombo("##emote", rule.Emotes.Select(x => Svc.Data.GetExcelSheet<Emote>().GetRow(x).Name.ExtractText()).PrintRange(rule.Not.Emotes.Select(x => Svc.Data.GetExcelSheet<Emote>().GetRow(x).Name.ExtractText()), out var fullList), C.ComboSize))
                    {
                        FiltersSelection();

                        if(Player.Available && Utils.GetAdjustedEmote() != 0)
                        {
                            var id = Utils.GetAdjustedEmote();
                            var cond = Svc.Data.GetExcelSheet<Emote>().GetRow(id);
                            if(ThreadLoadImageHandler.TryGetIconTextureWrap(cond.Icon, false, out var texture))
                            {
                                ImGui.Image(texture.Handle, iconSize);
                                ImGui.SameLine();
                            }
                            ImGui.PushStyleColor(ImGuiCol.Text, EColor.CyanBright);
                            DrawSelector($"Current: {id}/{cond.Name.ExtractText()}##{cond.RowId}", cond.RowId, rule.Emotes, rule.Not.Emotes);
                            ImGui.PopStyleColor();
                            ImGui.Separator();
                        }

                        foreach(var cond in Svc.Data.GetExcelSheet<Emote>().Where(e => e.Name.ExtractText().IsNullOrEmpty() == false || e.Icon != 0 || rule.Emotes.Contains(e.RowId)))
                        {
                            var name = cond.Name.ExtractText() ?? "";
                            if(Filters[filterCnt].Length > 0 && !name.Contains(Filters[filterCnt], StringComparison.OrdinalIgnoreCase)) continue;
                            if(OnlySelected[filterCnt] && !rule.Emotes.Contains(cond.RowId)) continue;
                            if(ThreadLoadImageHandler.TryGetIconTextureWrap(cond.Icon, false, out var texture))
                            {
                                ImGui.Image(texture.Handle, iconSize);
                                ImGui.SameLine();
                            }
                            DrawSelector($"{name.NullWhenEmpty() ?? $"Unnamed/{cond.RowId}"}##{cond.RowId}", cond.RowId, rule.Emotes, rule.Not.Emotes);
                        }
                        ImGui.EndCombo();
                    }
                    if(fullList != null) ImGuiEx.Tooltip(UI.AnyNotice + fullList);
                }
                filterCnt++;

                if(C.Cond_Job)
                {
                    ImGui.TableNextColumn();
                    //Job
                    ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                    if(ImGui.BeginCombo("##job", rule.Jobs.PrintRange(rule.Not.Jobs, out var fullList), C.ComboSize))
                    {
                        FiltersSelection();
                        foreach(var cond in Enum.GetValues<Job>().OrderByDescending(x => Svc.Data.GetExcelSheet<ClassJob>().GetRow((uint)x).Role))
                        {
                            if(cond == Job.ADV) continue;
                            if(cond.IsUpgradeable() && C.UnifyJobs) continue;
                            var name = cond.ToString().Replace("_", " ");
                            if(Filters[filterCnt].Length > 0 && !name.Contains(Filters[filterCnt], StringComparison.OrdinalIgnoreCase)) continue;
                            if(OnlySelected[filterCnt] && !rule.Jobs.Contains(cond)) continue;
                            if(ThreadLoadImageHandler.TryGetIconTextureWrap((uint)cond.GetIcon(), false, out var texture))
                            {
                                ImGui.Image(texture.Handle, iconSize);
                                ImGui.SameLine();
                            }
                            if(cond.IsUpgradeable()) ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.DalamudGrey3);
                            DrawSelector(name, cond, rule.Jobs, rule.Not.Jobs);
                            if(cond.IsUpgradeable()) ImGui.PopStyleColor();
                        }
                        ImGui.EndCombo();
                    }
                    if(fullList != null) ImGuiEx.Tooltip(UI.AnyNotice + fullList);
                }
                filterCnt++;

                if(C.Cond_World)
                {
                    ImGui.TableNextColumn();
                    //World
                    ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                    if(ImGui.BeginCombo("##world", rule.Worlds.ToWorldNames().PrintRange(rule.Not.Worlds.ToWorldNames(), out var fullList), C.ComboSize))
                    {
                        FiltersSelection();
                        foreach(var dc in ExcelWorldHelper.GetDataCenters(Enum.GetValues<ExcelWorldHelper.Region>()))
                        {
                            var worlds = ExcelWorldHelper.GetPublicWorlds().Where(x => x.DataCenter.RowId == dc.RowId);
                            ImGuiEx.Text($"{dc.Name}");
                            foreach(var cond in worlds)
                            {
                                var name = cond.Name.ToString();
                                if(Filters[filterCnt].Length > 0 && !name.Contains(Filters[filterCnt], StringComparison.OrdinalIgnoreCase)) continue;
                                if(OnlySelected[filterCnt] && !rule.Worlds.Contains(cond.RowId)) continue;
                                ImGuiEx.Spacing();
                                DrawSelector(name, cond.RowId, rule.Worlds, rule.Not.Worlds);
                            }
                        }
                        ImGui.EndCombo();
                    }
                    if(fullList != null) ImGuiEx.Tooltip(UI.AnyNotice + fullList);
                }
                filterCnt++;

                if(C.Cond_Gearset)
                {
                    if(EzThrottler.Throttle("UpdateGS", 5000)) Utils.UpdateGearsetCache();
                    ImGui.TableNextColumn();
                    //Gearset
                    ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                    var gch = currentProfile.Characters.FirstOrDefault();
                    if(ImGui.BeginCombo("##gs", rule.Gearsets.ToGearsetNames(gch).PrintRange(rule.Not.Gearsets.ToGearsetNames(gch), out var fullList), C.ComboSize))
                    {
                        FiltersSelection();
                        if(!C.GearsetNameCacheCID.TryGetValue(gch, out var gearsets)) gearsets = [];
                        foreach(var cond in gearsets)
                        {
                            var name = cond.ToString();
                            if(Filters[filterCnt].Length > 0 && !name.Contains(Filters[filterCnt], StringComparison.OrdinalIgnoreCase)) continue;
                            if(OnlySelected[filterCnt] && !rule.Gearsets.Contains(cond.Id)) continue;
                            if(ThreadLoadImageHandler.TryGetIconTextureWrap((uint)((Job)cond.ClassJob).GetIcon(), false, out var texture))
                            {
                                ImGui.Image(texture.Handle, iconSize);
                                ImGui.SameLine();
                            }
                            DrawSelector(name, cond.Id, rule.Gearsets, rule.Not.Gearsets);
                        }
                        foreach(var z in rule.Gearsets)
                        {
                            if(!gearsets.Any(h => h.Id == z))
                            {
                                ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.DalamudRed);
                                ImGuiEx.CollectionCheckbox($"{z}", z, rule.Gearsets, delayedOperation: true);
                                ImGui.PopStyleColor();
                            }
                        }
                        ImGui.EndCombo();
                    }

                    if(fullList != null) ImGuiEx.Tooltip(UI.AnyNotice + fullList);
                }
                filterCnt++;

                if(C.Cond_Players)
                {
                    ImGui.TableNextColumn();

                    // Player Selection Dropdown
                    ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                    if(ImGui.BeginCombo("##players", rule.Players.Select(x => C.selectedPlayers.FirstOrDefault(p => x == p.Name).Name ?? $"{x:X16}").PrintRange(rule.Not.Players.Select(x => C.selectedPlayers.FirstOrDefault(p => x == p.Name).Name ?? $"{x:X16}"), out var fullList), C.ComboSize))
                    {
                        FiltersSelection();

                        foreach(var player in C.selectedPlayers)
                        {
                            var name = player.Name;

                            // Apply filtering
                            if(Filters[filterCnt].Length > 0 && !name.Contains(Filters[filterCnt], StringComparison.OrdinalIgnoreCase))
                                continue;
                            if(OnlySelected[filterCnt] && !rule.Players.Contains(name))
                                continue;

                            DrawSelector($"{name}##{player.Name}", player.Name, rule.Players, rule.Not.Players);
                        }

                        // Handle players that no longer exist in `C.selectedPlayers` but are still in `rule.Players`
                        foreach(var z in rule.Players)
                        {
                            if(!C.selectedPlayers.Any(p => p.Name == z))
                            {
                                ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.DalamudRed);
                                ImGuiEx.CollectionCheckbox($"{z}", z, rule.Players, delayedOperation: true);
                                ImGui.PopStyleColor();
                            }
                        }

                        ImGui.EndCombo();
                    }

                    if(fullList != null)
                        ImGuiEx.Tooltip(UI.AnyNotice + fullList);
                }
                filterCnt++;

                if(C.Cond_OnlineStatus)
                {
                    ImGui.TableNextColumn();
                    //Online Status
                    ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                    if(ImGui.BeginCombo("##onlinestatus", rule.OnlineStatuses.Select(x => P.OnlineStatusManager.OnlineStatuses.TryGetValue(x, out var n) ? n : $"{x}").ToHashSet().PrintRange(rule.Not.OnlineStatuses.Select(x => P.OnlineStatusManager.OnlineStatuses.TryGetValue(x, out var n) ? n : $"{x}").ToHashSet(), out var fullList), C.ComboSize))
                    {
                        FiltersSelection();
                        foreach(var cond in P.OnlineStatusManager.OnlineStatuses)
                        {
                            var name = cond.Value;
                            if(name.IsNullOrEmpty()) continue;
                            if(Filters[filterCnt].Length > 0 && !name.Contains(Filters[filterCnt], StringComparison.OrdinalIgnoreCase)) continue;
                            if(OnlySelected[filterCnt] && !rule.OnlineStatuses.Contains(cond.Key)) continue;
                            var iconSourceId = P.OnlineStatusManager.IconOverrides.TryGetValue(cond.Key, out var overrideId) ? overrideId : cond.Key;
                            var statusRow = Svc.Data.GetExcelSheet<Lumina.Excel.Sheets.OnlineStatus>().GetRowOrDefault(iconSourceId);
                            if(statusRow != null && ThreadLoadImageHandler.TryGetIconTextureWrap((uint)statusRow.Value.Icon, false, out var texture))
                            {
                                ImGui.Image(texture.Handle, iconSize);
                                ImGui.SameLine();
                            }
                            DrawSelector($"{cond.Value}##{cond.Key}", cond.Key, rule.OnlineStatuses, rule.Not.OnlineStatuses);
                        }
                        ImGui.EndCombo();
                    }
                    if(fullList != null) ImGuiEx.Tooltip(UI.AnyNotice + fullList);
                }
                filterCnt++;
                
                if(C.Cond_Delay)
                {
                    ImGui.TableNextColumn();
                    //Delays
                    ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                    var delayText = rule.ActivationDelay > 0 || rule.DeactivationDelay > 0 ?
                        $"Act:{rule.ActivationDelay}s / Dct:{rule.DeactivationDelay}s" : "- No delays -";
                    if(ImGui.BeginCombo("##delays", delayText, C.ComboSize))
                    {
                        ImGui.SetWindowFontScale(0.9f);
                        ImGui.Text("Activation Delay (seconds):");
                        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                        ImGui.SliderInt("##actdelay", ref rule.ActivationDelay, 0, 60);
                        ImGui.Spacing();
                        ImGui.Text("Deactivation Delay (seconds):");
                        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                        ImGui.SliderInt("##dctdelay", ref rule.DeactivationDelay, 0, 60);
                        ImGui.SetWindowFontScale(1f);
                        ImGui.EndCombo();
                    }
                    ImGuiEx.Tooltip("Set delays before rule activates or deactivates");
                }

                ImGui.TableNextColumn();

                {
                    //Glamour
                    ImGuiEx.SetNextItemFullWidth();
                    if(ImGui.BeginCombo("##glamour", rule.SelectedPresets.PrintRange(out var fullList, "- None -"), C.ComboSize))
                    {
                        FiltersSelection();
                        var designs = currentProfile.GetPresetsUnion().OrderBy(x => x.Name);
                        foreach(var x in designs)
                        {
                            var name = x.Name;
                            if(Filters[filterCnt].Length > 0 && !name.Contains(Filters[filterCnt], StringComparison.OrdinalIgnoreCase)) continue;
                            if(OnlySelected[filterCnt] && !rule.SelectedPresets.Contains(name)) continue;
                            if(x.GetFolder(currentProfile)?.HiddenFromSelection == true) continue;
                            if(ImGuiEx.CollectionCheckbox($"{x.CensoredName}##{x.GUID}", x.Name, rule.SelectedPresets))
                            {
                                rule.StickyRandom = Random.Shared.Next(0, rule.SelectedPresets.Count);
                            }
                        }
                        foreach(var x in rule.SelectedPresets)
                        {
                            if(designs.Any(d => d.Name == x && d.GetFolder(currentProfile)?.HiddenFromSelection != true)) continue;
                            ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.DalamudRed);
                            if(ImGuiEx.CollectionCheckbox($"{x}", x, rule.SelectedPresets, false, true))
                            {
                                rule.StickyRandom = Random.Shared.Next(0, rule.SelectedPresets.Count);
                            }
                            ImGui.PopStyleColor();
                        }
                        ImGui.EndCombo();
                    }
                    if(fullList != null) ImGuiEx.Tooltip(UI.RandomNotice + fullList);
                    filterCnt++;
                }

                ImGui.TableNextColumn();
                //Delete
                if(ImGuiEx.IconButton(FontAwesomeIcon.Copy))
                {
                    Safe(() => Copy(JsonConvert.SerializeObject(rule)));
                }
                if(C.StickyPresets && C.Sticky)
                {
                    ImGui.SameLine();
                    if(ImGuiEx.IconButton(FontAwesomeIcon.Dice))
                    {
                        if(rule.SelectedPresets.Count > 1)
                        {
                            var old = rule.StickyRandom;
                            rule.StickyRandom = Random.Shared.Next(0, rule.SelectedPresets.Count);
                            P.ForceUpdate = true;
                            if(rule.StickyRandom == old)
                            {
                                rule.StickyRandom = (rule.StickyRandom + 1) % rule.SelectedPresets.Count;
                            }
                            ;
                        }
                        else { rule.StickyRandom = 0; }
                    }
                    ImGuiEx.Tooltip($"Randomize Preset Used.");
                }
                ImGui.SameLine();
                if(ImGuiEx.IconButton(FontAwesomeIcon.Trash) && ImGui.GetIO().KeyCtrl)
                {
                    new TickScheduler(() => rulesList.Remove(rule));
                }
                ImGuiEx.Tooltip("Hold CTRL+Click to delete");

                if(col) ImGui.PopStyleColor();
                if(col2) ImGui.PopStyleColor();
                ImGui.PopID();
            }

            ImGui.EndTable();
            postAction = () => DragDrop.End();
        }
        ImGui.PopStyleVar();
    }

    private static void DrawPlaceName(TerritoryType t, Vector4? nullable, string arg2)
    {
        var cond = t.FindBiome();
        if(cond != Biome.No_biome && ThreadLoadImageHandler.TryGetTextureWrap(Path.Combine(Svc.PluginInterface.AssemblyLocation.DirectoryName, "res", "biome", $"{(int)cond}.png"), out var texture))
        {
            ImGui.Image(texture.Handle, iconSize);
            ImGui.SameLine();
        }
        ImGuiEx.Text(nullable, arg2);
    }

    private static void DrawSelector<T>(string name, T value, ICollection<T> values, ICollection<T> notValues) => DrawSelector(name, [value], values, notValues);

    private static void DrawSelector<T>(string name, IEnumerable<T> value, ICollection<T> values, ICollection<T> notValues)
    {
        var buttonSize = ImGuiHelpers.GetButtonSize(" ");
        var size = new Vector2(buttonSize.Y);
        sbyte s = 0;
        if(values.ContainsAny(value)) s = 1;
        if(notValues.ContainsAny(value)) s = -1;

        var checkbox = new TristateCheckboxEx();

        if(checkbox.Draw(name, s, out s))
        {
            if(!C.AllowNegativeConditions && s == -1)
            {
                s = 0;
            }
            if(s == 1)
            {
                foreach(var v in value)
                {
                    notValues.Remove(v);
                    values.Add(v);
                }
            }
            else if(s == 0)
            {
                foreach(var v in value)
                {
                    notValues.Remove(v);
                    values.Remove(v);
                }
            }
            else
            {
                foreach(var v in value)
                {
                    notValues.Add(v);
                    values.Remove(v);
                }
            }
        }
        if(s == -1)
        {
            ImGuiEx.Tooltip($"If matching any condition with cross, rule will not be applied.");
        }
    }
    private static List<TimelineSegment> RenderTimeline(List<TimelineSegment> precise_Times)
    {

        var cursorPos = ImGui.GetCursorScreenPos();
        var drawList = ImGui.GetWindowDrawList();
        var startX = cursorPos.X + ImGui.CalcTextSize("12:00 AM").X / 2;
        var endX = startX + ImGui.GetContentRegionAvail().X - ImGui.CalcTextSize("12:00 AM").X;
        var timelineWidth = endX - startX;
        var centerY = cursorPos.Y + 20;
        float height = 0;

        var timePoints = GetPoints(precise_Times);
        var segmentStates = GetStates(precise_Times);

        // Hover tooltip
        var mousePos = ImGui.GetMousePos();
        var hoveringTimeline = mousePos.Y > centerY - 5 && mousePos.Y < centerY + 5 && mousePos.X >= startX && mousePos.X <= endX;
        var hoverTime = (float)(Math.Round((mousePos.X - startX) / timelineWidth * 24 * 60 / 5.0) * 5) / (24 * 60);
        timePoints = timePoints.Distinct().OrderBy(x => x).ToList();
        for(var i = 0; i < timePoints.Count - 1; i++)
        {
            var x1 = startX + timePoints[i] * timelineWidth;
            var x2 = startX + timePoints[i + 1] * timelineWidth;

            var segmentState = precise_Times[i].State;
            var color = segmentState switch
            {
                1 => ImGui.GetColorU32(new Vector4(0, 1, 0, 1)),
                _ => ImGui.GetColorU32(new Vector4(1, 1, 1, 1))
            };
            if(C.AllowNegativeConditions)
            {
                color = segmentState switch
                {
                    1 => ImGui.GetColorU32(new Vector4(0, 1, 0, 1)),
                    2 => ImGui.GetColorU32(new Vector4(1, 0, 0, 1)),
                    _ => ImGui.GetColorU32(new Vector4(1, 1, 1, 1))
                };
            }

            drawList.AddLine(new Vector2(x1, centerY), new Vector2(x2, centerY), color, 2f);

            if(ImGui.IsMouseClicked(ImGuiMouseButton.Left) && x1 < mousePos.X && mousePos.X <= x2 && Math.Abs(mousePos.Y - centerY) <= 7)
            {
                var segment = precise_Times[i];
                var stateLimit = C.AllowNegativeConditions ? 3 : 2;
                segment.State = (segment.State + 1) % stateLimit;
                precise_Times[i] = segment;
            }

            if(ImGui.IsMouseClicked(ImGuiMouseButton.Right) && x1 < mousePos.X && mousePos.X <= x2 && Math.Abs(mousePos.Y - centerY) <= 7)
            {
                var segment = precise_Times[i];
                var stateLimit = C.AllowNegativeConditions ? 3 : 2;
                segment.State = (segment.State - 1 + stateLimit) % stateLimit;
                precise_Times[i] = segment;
            }
        }

        List<Vector2> labelBoundryBoxes = [];
        // Draw points + Identify if removeable
        var hoverLabel = FormatTime(hoverTime);
        var tooltipText = $"{hoverLabel} | Mouse Middle to add";
        for(var i = 0; i < timePoints.Count; i++)
        {
            var xPos = startX + timePoints[i] * timelineWidth;
            var pointPos = new Vector2(xPos, centerY);
            var color = ImGui.GetColorU32(new Vector4(0.2f, 0.6f, 1f, 1f));
            if(hoveringTimeline && Math.Abs(hoverTime - timePoints[i]) < 10f / timelineWidth)
            {
                color = ImGui.GetColorU32(new Vector4(1.0f, 0.5f, 0.0f, 1.0f));
            }
            var label = FormatTime(timePoints[i]);

            if(hoveringTimeline && Math.Abs(hoverTime - timePoints[i]) < 10f / timelineWidth)
            {
                if(timePoints[i] == 0f || timePoints[i] == 1f) { tooltipText = $"{label} | May not remove"; }
                else { tooltipText = $"{label} | Mouse Middle to remove."; }
            }

            var textSize = ImGui.CalcTextSize(label);

            var labelX = xPos - textSize.X / 2;
            var labelY = centerY + 5;

            foreach(var box in labelBoundryBoxes)
            {
                if(labelX < box.X && labelY == box.Y)
                {
                    labelY += textSize.Y + 2;
                    drawList.AddLine(pointPos, new Vector2(xPos, labelY), ImGui.GetColorU32(new Vector4(1, 1, 1, 1)), 1.0f);
                }
            }

            drawList.AddText(new Vector2(labelX, labelY), ImGui.GetColorU32(new Vector4(1, 1, 1, 1)), label);
            if(labelY + textSize.Y - cursorPos.Y + 20 > height)
            {
                height = labelY + textSize.Y - cursorPos.Y + 20;
            }
            labelBoundryBoxes.Add(new Vector2(xPos + textSize.X / 2, labelY));
            drawList.AddCircleFilled(pointPos, 5f, color);
        }
        if(hoveringTimeline)
        {
            ImGui.SetTooltip(tooltipText);
        }

        timePoints = timePoints.Distinct().OrderBy(x => Vector2.Distance(mousePos, new Vector2(startX + x * timelineWidth, centerY))).ToList();
        if(hoveringTimeline && ImGui.IsMouseClicked(ImGuiMouseButton.Middle))
        {
            if(Math.Abs(hoverTime - timePoints[0]) < 10f / timelineWidth)
            {
                if(!(timePoints[0] == 0f || timePoints[0] == 1f))
                {
                    RemoveSegment(precise_Times, hoverTime, timelineWidth);
                }
            }
            else if(Math.Abs(hoverTime - timePoints[0]) > 10f / timelineWidth)
            {
                AddSegment(precise_Times, hoverTime);
            }
        }
        ImGui.SetWindowSize(new Vector2(400, height), ImGuiCond.Always);
        return precise_Times;
    }
    private static void AddSegment(List<TimelineSegment> precise_Times, float hoverTime)
    {
        for(var i = 0; i < precise_Times.Count; i++)
        {
            var segment = precise_Times[i];

            if(hoverTime > segment.Start && hoverTime < segment.End)
            {
                // Remove the original segment
                precise_Times.RemoveAt(i);

                // Create two new segments
                var firstSegment = new TimelineSegment(segment.Start, hoverTime, segment.State);
                var secondSegment = new TimelineSegment(hoverTime, segment.End, segment.State);

                // Insert the new segments in place of the removed one
                precise_Times.Insert(i, secondSegment);
                precise_Times.Insert(i, firstSegment);

                return; // Exit after modification to prevent further iteration
            }
        }
    }
    private static void RemoveSegment(List<TimelineSegment> precise_Times, float hoverTime, float timelineWidth)
    {
        var pixelTolerance = 10f / timelineWidth;
        var closestIndex = -1;
        var closestDistance = float.MaxValue;

        // Find the closest valid segment split within tolerance
        for(var i = 0; i < precise_Times.Count - 1; i++)
        {
            var first = precise_Times[i];
            var second = precise_Times[i + 1];

            var endDistance = Math.Abs(first.End - hoverTime);
            var startDistance = Math.Abs(second.Start - hoverTime);

            if(endDistance <= pixelTolerance && startDistance <= pixelTolerance)
            {
                var totalDistance = endDistance + startDistance;
                if(totalDistance < closestDistance)
                {
                    closestDistance = totalDistance;
                    closestIndex = i;
                }
            }
        }

        // If a valid closest segment was found, merge it
        if(closestIndex != -1)
        {
            var first = precise_Times[closestIndex];
            var second = precise_Times[closestIndex + 1];

            // Create a merged segment
            var mergedSegment = new TimelineSegment(first.Start, second.End, first.State);

            // Remove the two segments
            precise_Times.RemoveAt(closestIndex + 1);
            precise_Times.RemoveAt(closestIndex);

            // Insert the merged segment
            precise_Times.Insert(closestIndex, mergedSegment);
        }
    }

    private static List<int> GetStates(List<TimelineSegment> precise_Times)
    {
        List<int> segments = [];
        foreach(var time in precise_Times)
        {
            segments.Add(time.State);
        }
        return segments;
    }

    private static List<float> GetPoints(List<TimelineSegment> precise_Times)
    {
        List<float> floats = [];
        foreach(var time in precise_Times)
        {
            floats.Add(time.Start);
        }
        floats.Add(precise_Times.Last().End);
        return floats;
    }

    private static string FormatTime(float time)
    {
        var totalMinutes = (int)(time * 24 * 60);
        var hours = (totalMinutes / 60) % 24;

        totalMinutes = (int)(Math.Round(totalMinutes / 5.0) * 5);
        var minutes = totalMinutes % 60;
        var period = hours > 12 ? "PM" : "AM";

        hours = hours % 12;
        if(hours == 0) hours = 12;
        return $"{hours}:{minutes:D2} {period}";
    }
}
