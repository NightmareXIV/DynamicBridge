using Dalamud.Game.Text.SeStringHandling;
using DynamicBridge.Configuration;
using DynamicBridge.IPC.Honorific;
using ECommons.Configuration;
using Newtonsoft.Json;
using System.Data;

namespace DynamicBridge.Gui
{
    public static class GuiPresets
    {
        private static string[] Filters = ["", "", "", "", "", "", "", "", "", "", "", "", "", "", ""];
        private static bool[] OnlySelected = new bool[15];
        private static ImGuiEx.RealtimeDragDrop<Preset> DragDrop = new("MovePresetItem", (x) => x.GUID);
        private static bool Focus = false;
        private static string Open = null;
        public static void DrawUser()
        {
            if(UI.Profile != null)
            {
                DrawProfile(UI.Profile, true, true, false);
            }
            else
            {
                UI.ProfileSelectorCommon();
            }
        }

        public static void DrawGlobal()
        {
            DrawProfile(C.GlobalProfile, false, false, true);
        }

        private static void DrawProfile(Profile Profile, bool drawFallback, bool drawHeader, bool drawGlobalSection)
        {
            Profile.GetPresetsListUnion().Each(f => f.RemoveAll(x => x == null));

            void Buttons()
            {
                if(ImGuiEx.IconButton(FontAwesomeIcon.Plus))
                {
                    if(Open != null && Profile.PresetsFolders.TryGetFirst(x => x.GUID == Open, out var open))
                    {
                        open.Presets.Add(new());
                    }
                    else
                    {
                        Profile.Presets.Add(new());
                    }
                }
                ImGuiEx.Tooltip("Add new empty preset into default or focused folder");
                ImGui.SameLine();
                if(ImGuiEx.IconButton(FontAwesomeIcon.Paste))
                {
                    try
                    {
                        var str = (EzConfig.DefaultSerializationFactory.Deserialize<Preset>(Paste()));
                        if(str != null)
                        {
                            if(Open != null && Profile.PresetsFolders.TryGetFirst(x => x.GUID == Open, out var open))
                            {
                                open.Presets.Add(str);
                            }
                            else
                            {
                                Profile.Presets.Add(str);
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
                ImGuiEx.Tooltip($"Paste previously copied preset from clipboard");
                ImGui.SameLine();

                if(ImGuiEx.IconButton(FontAwesomeIcon.FolderPlus))
                {
                    Profile.PresetsFolders.Add(new() { Name = $"Preset folder {Profile.PresetsFolders.Count + 1}" });
                }
                ImGuiEx.Tooltip("Add new preset folder");

                ImGui.SameLine();

            }

            void RightButtons()
            {
                /*ImGui.PushFont(UiBuilder.IconFont);
                ImGuiEx.ButtonCheckbox(FontAwesomeIcon.SearchPlus.ToIconString(), ref Focus);
                ImGui.PopFont();
                ImGuiEx.Tooltip("Toggle focus mode. While focus mode active, only one selected folder will be visible.");
                ImGui.SameLine();*/
                UI.ForceUpdateButton();
                ImGui.SameLine();
            }

            if(drawHeader)
            {
                UI.ProfileSelectorCommon(Buttons, RightButtons);
            }
            else
            {
                ImGuiEx.RightFloat(RightButtons);
                Buttons();
                ImGui.SameLine();
                ImGuiEx.TextWrapped($"Global Presets are available for use on each of your characters.");
            }

            string newOpen = null;

            if(!Focus || Open == "" || Open == null)
            {
                if(ImGuiEx.TreeNode($"Main presets##global", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    newOpen = "";
                    if(DragDrop.AcceptPayload(out var result, ImGuiDragDropFlags.AcceptBeforeDelivery | ImGuiDragDropFlags.AcceptNoDrawDefaultRect))
                    {
                        DragDropUtils.AcceptFolderDragDrop(Profile, result, Profile.Presets);
                    }
                    DrawPresets(Profile, Profile.Presets, out var postAction, "", false, drawGlobalSection);
                    ImGui.TreePop();
                    postAction?.Invoke();
                }
                else
                {
                    if(DragDrop.AcceptPayload(out var result, ImGuiDragDropFlags.AcceptBeforeDelivery | ImGuiDragDropFlags.AcceptNoDrawDefaultRect))
                    {
                        DragDropUtils.AcceptFolderDragDrop(Profile, result, Profile.Presets);
                    }
                }
            }

            for(var presetFolderIndex = 0; presetFolderIndex < Profile.PresetsFolders.Count; presetFolderIndex++)
            {
                var presetFolder = Profile.PresetsFolders[presetFolderIndex];
                if(Focus && Open != presetFolder.GUID && Open != null) continue;
                if(presetFolder.HiddenFromSelection)
                {
                    ImGuiEx.RightFloat($"RFCHP{presetFolder.GUID}", () => ImGuiEx.Text(ImGuiColors.DalamudGrey, "Hidden from rules"));
                }
                if(ImGuiEx.TreeNode($"{presetFolder.Name}###presetfolder{presetFolder.GUID}"))
                {
                    newOpen = presetFolder.GUID;
                    CollapsingHeaderClicked();
                    if(DragDrop.AcceptPayload(out var result, ImGuiDragDropFlags.AcceptBeforeDelivery | ImGuiDragDropFlags.AcceptNoDrawDefaultRect))
                    {
                        DragDropUtils.AcceptFolderDragDrop(Profile, result, presetFolder.Presets);
                    }
                    DrawPresets(Profile, presetFolder.Presets, out var postAction, presetFolder.GUID, false, drawGlobalSection);
                    ImGui.TreePop();
                    postAction?.Invoke();
                }
                else
                {
                    CollapsingHeaderClicked();
                    if(DragDrop.AcceptPayload(out var result, ImGuiDragDropFlags.AcceptBeforeDelivery | ImGuiDragDropFlags.AcceptNoDrawDefaultRect))
                    {
                        DragDropUtils.AcceptFolderDragDrop(Profile, result, presetFolder.Presets);
                    }
                }
                void CollapsingHeaderClicked()
                {
                    if(ImGui.IsItemHovered() && ImGui.IsItemClicked(ImGuiMouseButton.Right)) ImGui.OpenPopup($"Folder{presetFolder.GUID}");
                    if(ImGui.BeginPopup($"Folder{presetFolder.GUID}"))
                    {
                        ImGuiEx.SetNextItemWidthScaled(150f);
                        ImGui.InputTextWithHint("##name", "Folder name", ref presetFolder.Name, 200);
                        if(ImGui.Selectable("Export to clipboard"))
                        {
                            Copy(EzConfig.DefaultSerializationFactory.Serialize(presetFolder, false));
                        }
                        if(presetFolder.HiddenFromSelection)
                        {
                            if(ImGui.Selectable("Show in Rules section")) presetFolder.HiddenFromSelection = false;
                        }
                        else
                        {
                            if(ImGui.Selectable("Hide from Rules section")) presetFolder.HiddenFromSelection = true;
                        }
                        if(ImGui.Selectable("Move up", false, ImGuiSelectableFlags.DontClosePopups) && presetFolderIndex > 0)
                        {
                            (Profile.PresetsFolders[presetFolderIndex], Profile.PresetsFolders[presetFolderIndex - 1]) = (Profile.PresetsFolders[presetFolderIndex - 1], Profile.PresetsFolders[presetFolderIndex]);
                        }
                        if(ImGui.Selectable("Move down", false, ImGuiSelectableFlags.DontClosePopups) && presetFolderIndex < Profile.PresetsFolders.Count - 1)
                        {
                            (Profile.PresetsFolders[presetFolderIndex], Profile.PresetsFolders[presetFolderIndex + 1]) = (Profile.PresetsFolders[presetFolderIndex + 1], Profile.PresetsFolders[presetFolderIndex]);
                        }
                        ImGui.Separator();

                        if(ImGui.BeginMenu("Delete folder..."))
                        {
                            if(ImGui.Selectable("...and move profiles to default folder (Hold CTRL)"))
                            {
                                if(ImGuiEx.Ctrl)
                                {
                                    new TickScheduler(() =>
                                    {
                                        foreach(var x in presetFolder.Presets)
                                        {
                                            Profile.Presets.Add(x);
                                        }
                                        Profile.PresetsFolders.Remove(presetFolder);
                                    });
                                }
                            }
                            if(ImGui.Selectable("...and delete included profiles (Hold CTRL+SHIFT)"))
                            {
                                if(ImGuiEx.Ctrl && ImGuiEx.Shift)
                                {
                                    new TickScheduler(() => Profile.PresetsFolders.Remove(presetFolder));
                                }
                            }
                            ImGui.EndMenu();
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
            }
            if(drawFallback)
            {
                if(ImGuiEx.TreeNode($"Fallback preset"))
                {
                    DrawPresets(Profile, [Profile.FallbackPreset], out _, $"FallbackPreset-8c680b09-acd0-43ab-9413-26a4e38841fc", true, drawGlobalSection);
                    Open = newOpen;
                    ImGui.TreePop();
                }
            }
        }

        private static void SetWidthNextImGuiWithButtonToRight(FontAwesomeIcon icon)
        {
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - (ImGui.CalcTextSize(icon.ToIconString()).X + ImGui.GetStyle().ItemSpacing.X * 3));
        }

        private static void DrawPresets(Profile currentProfile, List<Preset> presetList, out Action postAction, string extraID, bool isFallback, bool isGlobal)
        {
            postAction = null;
            var cnt = 3;
            if(C.EnableHonorific) cnt++;
            if(C.EnableCustomize) cnt++;
            if(C.EnableGlamourer) cnt++;
            if(C.EnablePenumbra) cnt++;
            if(C.EnableMoodles) cnt++;
            DragDrop.Begin();
            ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, Utils.CellPadding);
            if(ImGui.BeginTable($"##presets{extraID}", cnt, ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders | ImGuiTableFlags.Resizable | ImGuiTableFlags.Reorderable))
            {
                ImGui.TableSetupColumn("  ", ImGuiTableColumnFlags.NoResize | ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoSort);
                ImGui.TableSetupColumn("Name");
                if(C.EnableGlamourer) ImGui.TableSetupColumn("Glamourer");
                if(C.EnableCustomize) ImGui.TableSetupColumn("Customize+");
                if(C.EnableHonorific) ImGui.TableSetupColumn("Honorific");
                if(C.EnablePenumbra) ImGui.TableSetupColumn("Penumbra");
                if(C.EnableMoodles) ImGui.TableSetupColumn("Moodles");
                ImGui.TableSetupColumn(" ", ImGuiTableColumnFlags.NoResize | ImGuiTableColumnFlags.WidthFixed);
                ImGui.TableHeadersRow();

                var isStaticExists = currentProfile.IsStaticExists() && !isFallback;

                for(var i = 0; i < presetList.Count; i++)
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

                    var preset = presetList[i];

                    ImGui.PushID(preset.GUID);

                    if(isStaticExists)
                    {
                        ImGui.PushStyleColor(ImGuiCol.Text, preset.IsStatic ? ImGuiColors.DalamudOrange : ImGuiColors.DalamudGrey);
                    }
                    ImGui.TableNextRow();
                    DragDrop.SetRowColor(preset.GUID);
                    ImGui.TableNextColumn();
                    //Sorting
                    if(isFallback)
                    {
                        ImGuiEx.TextV(" ");
                        ImGuiEx.HelpMarker($"Values from this preset will be used as default in current profile.");
                    }
                    else
                    {
                        DragDrop.NextRow();
                        if(ImGui.RadioButton("##static", preset.IsStatic))
                        {
                            preset.IsStatic = !preset.IsStatic;
                            if(preset.IsStatic)
                            {
                                currentProfile.SetStatic(preset);
                            }
                            P.ForceUpdate = true;
                        }
                        ImGuiEx.Tooltip("Set this preset as static, applying it unconditionally on this character disregarding any rules.");
                        ImGui.SameLine();
                        var moveIndex = i;
                        DragDrop.DrawButtonDummy(preset.GUID, (payload) =>
                        {
                            DragDropUtils.AcceptProfileDragDrop(currentProfile, payload, presetList, moveIndex);
                        });

                        ImGui.SameLine();
                        if(ImGuiEx.IconButton(FontAwesomeIcon.CaretDown))
                        {
                            ImGui.OpenPopup($"MoveTo##{preset.GUID}");
                        }
                        if(ImGui.BeginPopup($"MoveTo##{preset.GUID}"))
                        {
                            if(ImGui.Selectable("- Main folder -", currentProfile.Presets.Any(x => x.GUID == preset.GUID)))
                            {
                                DragDropUtils.MovePresetToList(currentProfile, preset.GUID, currentProfile.Presets);
                            }
                            foreach(var x in currentProfile.PresetsFolders)
                            {
                                if(ImGui.Selectable($"{x.Name}##{x.GUID}", x.Presets.Any(x => x.GUID == preset.GUID)))
                                {
                                    DragDropUtils.MovePresetToList(currentProfile, preset.GUID, x.Presets);
                                }
                            }
                            ImGui.EndPopup();
                        }
                    }

                    ImGui.TableNextColumn();

                    //name
                    if(isFallback)
                    {
                        ImGuiEx.TextV("Base Preset");
                    }
                    else
                    {
                        var isEmpty = preset.Name == "";
                        var isNonUnique = currentProfile.GetPresetsUnion().Count(x => x.Name == preset.Name) > 1;
                        if(isEmpty)
                        {
                            ImGui.PushFont(UiBuilder.IconFont);
                            ImGuiEx.Text(ImGuiColors.DalamudRed, Utils.IconWarning);
                            ImGui.PopFont();
                            ImGuiEx.Tooltip("Name can not be empty");
                            ImGui.SameLine();
                        }
                        else if(isNonUnique)
                        {
                            ImGui.PushFont(UiBuilder.IconFont);
                            ImGuiEx.Text(ImGuiColors.DalamudRed, Utils.IconWarning);
                            ImGui.PopFont();
                            ImGuiEx.Tooltip("Name must be unique");
                            ImGui.SameLine();
                        }
                        ImGuiEx.SetNextItemFullWidth();
                        ImGui.InputTextWithHint("##name", "Preset name", ref preset.Name, 100, Utils.CensorFlags);
                    }


                    //Glamourer
                    {
                        if(C.EnableGlamourer)
                        {
                            ImGui.TableNextColumn();
                            if(C.StickyGlamourer && C.Sticky)
                            {
                                SetWidthNextImGuiWithButtonToRight(FontAwesomeIcon.Dice);
                            }
                            else
                            {
                                ImGuiEx.SetNextItemFullWidth();
                            }
                            if(ImGui.BeginCombo("##glamour", ((string[])[.. preset.Glamourer.Select(P.GlamourerManager.TransformName), .. preset.ComplexGlamourer]).PrintRange(out var fullList, "- None -"), C.ComboSize))
                            {
                                if(ImGui.IsWindowAppearing()) Utils.ResetCaches();
                                FiltersSelection();
                                ImGui.PushStyleVar(ImGuiStyleVar.IndentSpacing, Utils.IndentSpacing);
                                var noresults = true;
                                // normal
                                {
                                    List<(string[], Action)> items = [];
                                    var designs = P.GlamourerManager.GetDesigns().OrderBy(x => P.GlamourerManager.TransformName(x.Identifier.ToString()));
                                    foreach(var x in designs)
                                    {
                                        var name = x.Name;
                                        var id = x.Identifier.ToString();
                                        var fullPath = P.GlamourerManager.GetFullPath(id);
                                        var transformedName = P.GlamourerManager.TransformName(id);
                                        if(currentProfile.Pathes.Count > 0 && !fullPath.StartsWithAny(currentProfile.Pathes)) continue;
                                        if(Filters[filterCnt].Length > 0 && !transformedName.Contains(Filters[filterCnt], StringComparison.OrdinalIgnoreCase)) continue;
                                        var contains = preset.Glamourer.Contains(id);
                                        if(OnlySelected[filterCnt] && !contains) continue;
                                        items.Add((transformedName.SplitDirectories()[0..^1], () =>
                                        {
                                            if(Utils.CollectionSelectable(contains ? Colors.TabGreen : null, $"{name}  ##{x.Identifier}", id, preset.Glamourer))
                                            {
                                                preset.StickyRandomG = Random.Shared.Next(0, preset.Glamourer.Count + preset.ComplexGlamourer.Count);
                                                if(C.AutofillFromGlam && preset.Name == "" && preset.Glamourer.Contains(id)) preset.Name = name;
                                                if(C.AutoApplyOnChange)
                                                {
                                                    P.ForceUpdate = true;
                                                }
                                            }
                                            if(ImGui.IsItemHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Middle))
                                            {
                                                if(contains)
                                                {
                                                    preset.StickyRandomG = preset.Glamourer.IndexOf(id);
                                                    P.ForceUpdate = true;
                                                }
                                            }
                                        }

                                        ));
                                        noresults = false;
                                    }
                                    foreach(var x in preset.Glamourer)
                                    {
                                        if(designs.Any(d => d.Identifier.ToString() == x)) continue;
                                        items.Add(([], () =>
                                        {
                                            if(Utils.CollectionSelectable(ImGuiColors.DalamudRed, $"{x}", x, preset.Glamourer, true))
                                            {
                                                preset.StickyRandomG = Random.Shared.Next(0, preset.Glamourer.Count + preset.ComplexGlamourer.Count);
                                            }
                                        }

                                        ));
                                    }
                                    Utils.DrawFolder(items);
                                }

                                //complex
                                {
                                    List<(string[], Action)> items = [];
                                    var designs = C.ComplexGlamourerEntries;
                                    ImGui.PushStyleColor(ImGuiCol.Text, EColor.YellowBright);
                                    foreach(var x in designs)
                                    {
                                        var name = x.Name;
                                        if(Filters[filterCnt].Length > 0 && !name.Contains(Filters[filterCnt], StringComparison.OrdinalIgnoreCase)) continue;
                                        if(OnlySelected[filterCnt] && !preset.ComplexGlamourer.Contains(name)) continue;
                                        var contains = preset.ComplexGlamourer.Contains(name);
                                        items.Add((name.SplitDirectories()[0..^1], () =>
                                        {
                                            if(Utils.CollectionSelectable(contains ? Colors.TabYellow : null, $"{name}##{x.GUID}", name, preset.ComplexGlamourer))
                                            {
                                                preset.StickyRandomG = Random.Shared.Next(0, preset.Glamourer.Count + preset.ComplexGlamourer.Count);
                                                if(C.AutofillFromGlam && preset.Name == "" && preset.ComplexGlamourer.Contains(name)) preset.Name = name;
                                                if(C.AutoApplyOnChange)
                                                {
                                                    P.ForceUpdate = true;
                                                }
                                            }
                                        }
                                        ));
                                        noresults = false;
                                    }
                                    ImGui.PopStyleColor();
                                    foreach(var x in preset.ComplexGlamourer)
                                    {
                                        if(designs.Any(d => d.Name == x)) continue;
                                        items.Add(([], () =>
                                        {
                                            if(Utils.CollectionSelectable(ImGuiColors.DalamudRed, $"{x}", x, preset.ComplexGlamourer, true))
                                            {
                                                preset.StickyRandomG = Random.Shared.Next(0, preset.Glamourer.Count + preset.ComplexGlamourer.Count);
                                            }
                                        }

                                        ));
                                    }
                                    if(items.Count > 0)
                                    {
                                        if(ImGuiEx.TreeNode(Colors.TabYellow, "Layered Designs"))
                                        {
                                            Utils.DrawFolder(items);
                                            ImGui.TreePop();
                                        }
                                    }
                                }

                                if(noresults && Filters[filterCnt].Length == 0) OnlySelected[filterCnt] = false;
                                ImGui.PopStyleVar();

                                ImGui.EndCombo();
                            }
                            if(fullList != null) ImGuiEx.Tooltip(UI.RandomNotice + fullList);

                            //Add random Glamourer Button
                            if(C.StickyGlamourer && C.Sticky)
                            {
                                ImGui.SameLine();
                                if(ImGuiEx.IconButton(FontAwesomeIcon.Dice, "GlamourerButton"))
                                {
                                    if(preset.Glamourer.Count + preset.ComplexGlamourer.Count > 1)
                                    {
                                        var old = preset.StickyRandomG;
                                        preset.StickyRandomG = Random.Shared.Next(0, preset.Glamourer.Count + preset.ComplexGlamourer.Count);
                                        P.ForceUpdate = true;
                                        if(preset.StickyRandomG == old)
                                        {
                                            preset.StickyRandomG = (preset.StickyRandomG + 1) % (preset.Glamourer.Count + preset.ComplexGlamourer.Count);
                                        }
                                        ;
                                    }
                                    else { preset.StickyRandomG = 0; }
                                }
                                ImGuiEx.Tooltip($"Randomize Glamourer Used.");
                            }
                            filterCnt++;
                        }
                    }


                    //customize+
                    {
                        if(C.EnableCustomize)
                        {
                            var noresults = true;
                            ImGui.TableNextColumn();
                            if(isGlobal)
                            {
                                ImGuiEx.HelpMarker("All registered profiles are displayed in global profile, but only ones that are assigned to your current character will be used.", EColor.OrangeBright, FontAwesomeIcon.ExclamationTriangle.ToIconString(), false);
                                ImGui.SameLine();
                            }
                            if(C.StickyCustomize && C.Sticky)
                            {
                                SetWidthNextImGuiWithButtonToRight(FontAwesomeIcon.Dice);
                            }
                            else
                            {
                                ImGuiEx.SetNextItemFullWidth();
                            }
                            if(ImGui.BeginCombo("##customize", preset.Customize.Select(P.CustomizePlusManager.TransformName).PrintRange(out var fullList, "- None -"), C.ComboSize))
                            {
                                ImGui.PushStyleVar(ImGuiStyleVar.IndentSpacing, Utils.IndentSpacing);
                                if(ImGui.IsWindowAppearing()) Utils.ResetCaches();
                                FiltersSelection();
                                var profiles = P.CustomizePlusManager.GetProfiles(isGlobal ? null : currentProfile.Characters.Select(Utils.GetCharaNameFromCID)).OrderBy(x => P.CustomizePlusManager.TransformName($"{x.UniqueId}"));
                                var index = 0;
                                List<(string[], Action)> items = [];
                                foreach(var x in profiles)
                                {
                                    index++;
                                    ImGui.PushID(index);
                                    var name = P.CustomizePlusManager.TransformName($"{x.UniqueId}");
                                    var fullPath = P.CustomizePlusManager.GetFullPath($"{x.UniqueId}");
                                    if(currentProfile.CustomizePathes.Count > 0 && !fullPath.StartsWithAny(currentProfile.CustomizePathes)) continue;
                                    if(Filters[filterCnt].Length > 0 && !name.Contains(Filters[filterCnt], StringComparison.OrdinalIgnoreCase)) continue;
                                    if(OnlySelected[filterCnt] && !preset.Customize.Contains($"{x.UniqueId}")) continue;
                                    var contains = preset.Customize.Contains($"{x.UniqueId}");
                                    items.Add((name.SplitDirectories()[0..^1], () =>
                                    {
                                        if(Utils.CollectionSelectable(contains ? Colors.TabGreen : null, $"{name}  ", $"{x.UniqueId}", preset.Customize))
                                        {
                                            preset.StickyRandomC = Random.Shared.Next(0, preset.CustomizeFiltered().ToArray().Length);
                                            if(C.AutofillFromGlam && preset.Name == "" && preset.Customize.Contains($"{x.UniqueId}")) preset.Name = name;
                                        }
                                        if(ImGui.IsItemHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Middle))
                                        {
                                            if(contains)
                                            {
                                                preset.StickyRandomC = preset.Customize.IndexOf($"{x.UniqueId}");
                                                P.ForceUpdate = true;
                                            }
                                        }
                                    }

                                    ));
                                    noresults = false;
                                    ImGui.PopID();
                                }
                                foreach(var x in preset.Customize)
                                {
                                    if(profiles.Any(d => d.UniqueId.ToString() == x)) continue;
                                    ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.DalamudRed);
                                    items.Add(([], () =>
                                    {
                                        if(Utils.CollectionSelectable(ImGuiColors.DalamudRed, $"{x}", x, preset.Customize, true))
                                        {
                                            preset.StickyRandomC = Random.Shared.Next(0, preset.CustomizeFiltered().ToArray().Length);
                                        }
                                    }

                                    ));
                                    ImGui.PopStyleColor();
                                }
                                if(noresults && Filters[filterCnt].Length == 0) OnlySelected[filterCnt] = false;
                                Utils.DrawFolder(items);
                                ImGui.PopStyleVar();
                                ImGui.EndCombo();
                            }
                            if(fullList != null) ImGuiEx.Tooltip(UI.RandomNotice + fullList);
                            //Add random Customize+ Button
                            if(C.StickyCustomize && C.Sticky)
                            {
                                ImGui.SameLine();
                                if(ImGuiEx.IconButton(FontAwesomeIcon.Dice, "CustomizeButton"))
                                {
                                    if(preset.CustomizeFiltered().ToArray().Length > 1)
                                    {
                                        var old = preset.StickyRandomC;
                                        preset.StickyRandomC = Random.Shared.Next(0, preset.CustomizeFiltered().ToArray().Length);
                                        P.ForceUpdate = true;
                                        if(preset.StickyRandomC == old)
                                        {
                                            preset.StickyRandomC = (preset.StickyRandomC + 1) % preset.CustomizeFiltered().ToArray().Length;
                                        }
                                        ;
                                    }
                                    else { preset.StickyRandomC = 0; }
                                }
                                ImGuiEx.Tooltip($"Randomize Customize Used.");
                            }
                            filterCnt++;
                        }
                    }


                    //Honorific
                    {
                        if(C.EnableHonorific)
                        {
                            var noresults = true;
                            ImGui.TableNextColumn();
                            if(isGlobal && !C.HonotificUnfiltered)
                            {
                                ImGuiEx.HelpMarker("All registered titles are displayed in global profile, but only ones that are assigned to your current character will be used UNLESS \"Allow selecting titles registered for other characters\" is enabled in settings.", EColor.OrangeBright, FontAwesomeIcon.ExclamationTriangle.ToIconString(), false);
                                ImGui.SameLine();
                            }
                            if(C.StickyHonorific && C.Sticky)
                            {
                                SetWidthNextImGuiWithButtonToRight(FontAwesomeIcon.Dice);
                            }
                            else
                            {
                                ImGuiEx.SetNextItemFullWidth();
                            }
                            if(ImGui.BeginCombo("##honorific", preset.Honorific.PrintRange(out var fullList, "- None -"), C.ComboSize))
                            {
                                ImGui.PushStyleVar(ImGuiStyleVar.IndentSpacing, Utils.IndentSpacing);
                                if(ImGui.IsWindowAppearing()) Utils.ResetCaches();
                                FiltersSelection();
                                IEnumerable<ulong> charas = C.HonotificUnfiltered || isGlobal ? C.SeenCharacters.Keys : currentProfile.Characters;
                                List<(string[], Action)> items = [];
                                List<TitleData> allTitles = [];
                                foreach(var chara in charas)
                                {
                                    var titles = P.HonorificManager.GetTitleData([chara]).OrderBy(x => x.Title);
                                    allTitles.AddRange(titles);
                                    var index = 0;
                                    foreach(var x in titles)
                                    {
                                        index++;
                                        ImGui.PushID(index);
                                        var name = x.Title;
                                        if(Filters[filterCnt].Length > 0 && !name.Contains(Filters[filterCnt], StringComparison.OrdinalIgnoreCase)) continue;
                                        if(OnlySelected[filterCnt] && !preset.Honorific.Contains(name)) continue;
                                        if(x.Color != null) ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(x.Color.Value, 1f));
                                        var contains = preset.Honorific.Contains(x.Title);
                                        items.Add(([Utils.GetCharaNameFromCID(chara)], () =>
                                        {
                                            if(Utils.CollectionSelectable(contains ? Colors.TabGreen : null, $"{name}  ", x.Title, preset.Honorific))
                                            {
                                                preset.StickyRandomH = Random.Shared.Next(0, preset.HonorificFiltered().ToArray().Length);
                                                if(C.AutofillFromGlam && preset.Name == "" && preset.Honorific.Contains(x.Title)) preset.Name = name;
                                            }
                                            if(ImGui.IsItemHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Middle))
                                            {
                                                if(contains)
                                                {
                                                    preset.StickyRandomH = preset.Honorific.IndexOf(x.Title);
                                                    P.ForceUpdate = true;
                                                }
                                            }
                                        }

                                        ));
                                        noresults = false;
                                        if(x.Color != null) ImGui.PopStyleColor();
                                        ImGui.PopID();
                                    }
                                }
                                foreach(var x in preset.Honorific)
                                {
                                    if(allTitles.Any(d => d.Title == x)) continue;
                                    items.Add(([], () =>
                                    {
                                        if(Utils.CollectionSelectable(ImGuiColors.DalamudRed, $"{x}", x, preset.Honorific, true))
                                        {
                                            preset.StickyRandomH = Random.Shared.Next(0, preset.HonorificFiltered().ToArray().Length);
                                        }
                                    }

                                    ));
                                }
                                if(noresults && Filters[filterCnt].Length == 0) OnlySelected[filterCnt] = false;
                                Utils.DrawFolder(items);
                                ImGui.PopStyleVar();
                                ImGui.EndCombo();
                            }
                            if(fullList != null) ImGuiEx.Tooltip(UI.RandomNotice + fullList);
                            //Add random Honorific Button
                            if(C.StickyHonorific && C.Sticky)
                            {
                                ImGui.SameLine();
                                if(ImGuiEx.IconButton(FontAwesomeIcon.Dice, "HonorificButton"))
                                {
                                    if(preset.HonorificFiltered().ToArray().Length > 1)
                                    {
                                        var old = preset.StickyRandomH;
                                        preset.StickyRandomH = Random.Shared.Next(0, preset.HonorificFiltered().ToArray().Length);
                                        P.ForceUpdate = true;
                                        if(preset.StickyRandomH == old)
                                        {
                                            preset.StickyRandomH = (preset.StickyRandomH + 1) % preset.HonorificFiltered().ToArray().Length;
                                        }
                                        ;
                                    }
                                    else { preset.StickyRandomH = 0; }
                                }
                                ImGuiEx.Tooltip($"Randomize Honorific Used.");
                            }
                            filterCnt++;
                        }

                    }

                    //Penumbra
                    {
                        if(C.EnablePenumbra)
                        {
                            var noresults = true;
                            ImGui.TableNextColumn();
                            if(C.StickyPenumbra && C.Sticky)
                            {
                                SetWidthNextImGuiWithButtonToRight(FontAwesomeIcon.Dice);
                            }
                            else
                            {
                                ImGuiEx.SetNextItemFullWidth();
                            }
                            string fullList = null;
                            if(ImGui.BeginCombo("##penumbra", preset.PenumbraType != SpecialPenumbraAssignment.Use_Named_Collection ? preset.PenumbraType.ToString().Replace("_", " ") : preset.Penumbra.PrintRange(out fullList, "- None -"), C.ComboSize))
                            {
                                ImGui.PushStyleVar(ImGuiStyleVar.IndentSpacing, Utils.IndentSpacing);
                                if(ImGui.IsWindowAppearing()) Utils.ResetCaches();
                                ImGuiEx.Text($"Assignment Type:");
                                ImGuiEx.EnumCombo($"##asstype", ref preset.PenumbraType);
                                if(preset.PenumbraType == SpecialPenumbraAssignment.Use_Named_Collection)
                                {
                                    FiltersSelection();
                                    var collections = P.PenumbraManager.GetCollectionNames().Order();
                                    var index = 0;
                                    List<(string[], Action)> items = [];
                                    foreach(var x in collections)
                                    {
                                        index++;
                                        ImGui.PushID(index);
                                        var name = x;
                                        if(Filters[filterCnt].Length > 0 && !name.Contains(Filters[filterCnt], StringComparison.OrdinalIgnoreCase)) continue;
                                        if(OnlySelected[filterCnt] && !preset.Penumbra.Contains(name)) continue;
                                        var contains = preset.Penumbra.Contains(name);
                                        items.Add(([], () =>
                                        {
                                            if(Utils.CollectionSelectable(contains ? Colors.TabGreen : null, $"{x}  ", x, preset.Penumbra))
                                            {
                                                preset.StickyRandomP = Random.Shared.Next(0, preset.Penumbra.Count);
                                                if(C.AutofillFromGlam && preset.Name == "" && preset.Penumbra.Contains(x)) preset.Name = name;
                                            }
                                            if(ImGui.IsItemHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Middle))
                                            {
                                                if(contains)
                                                {
                                                    preset.StickyRandomP = preset.Penumbra.IndexOf(name);
                                                    P.ForceUpdate = true;
                                                }
                                            }
                                        }

                                        ));
                                        noresults = false;
                                        ImGui.PopID();
                                    }
                                    foreach(var x in preset.Penumbra)
                                    {
                                        if(collections.Contains(x)) continue;
                                        items.Add(([], () =>
                                        {
                                            if(Utils.CollectionSelectable(ImGuiColors.DalamudRed, $"{x}", x, preset.Penumbra, true))
                                            {
                                                preset.StickyRandomP = Random.Shared.Next(0, preset.Penumbra.Count);
                                            }
                                            ;
                                        }

                                        ));
                                    }
                                    if(noresults && Filters[filterCnt].Length == 0) OnlySelected[filterCnt] = false;
                                    Utils.DrawFolder(items);
                                }
                                ImGui.PopStyleVar();
                                ImGui.EndCombo();
                            }
                            if(fullList != null) ImGuiEx.Tooltip(UI.RandomNotice + fullList);
                            if(C.StickyPenumbra && C.Sticky)
                            {
                                ImGui.SameLine();
                                if(ImGuiEx.IconButton(FontAwesomeIcon.Dice, "PenumbraButton"))
                                {
                                    if(preset.Penumbra.Count > 1)
                                    {
                                        var old = preset.StickyRandomP;
                                        preset.StickyRandomP = Random.Shared.Next(0, preset.Penumbra.Count);
                                        P.ForceUpdate = true;
                                        if(preset.StickyRandomP == old)
                                        {
                                            preset.StickyRandomP = (preset.StickyRandomP + 1) % preset.Penumbra.Count;
                                        }
                                        ;
                                    }
                                    else { preset.StickyRandomP = 0; }
                                }
                                ImGuiEx.Tooltip($"Randomize Penumbra Used.");
                            }
                            filterCnt++;
                        }
                    }

                    //Moodles
                    {
                        if(C.EnableMoodles)
                        {
                            var noresults = true;
                            ImGui.TableNextColumn();
                            ImGuiEx.SetNextItemFullWidth();
                            if(ImGui.BeginCombo("##moodles", preset.Moodles.Select(Utils.GetName).PrintRange(out var fullList, "- None -"), C.ComboSize))
                            {
                                ImGui.PushStyleVar(ImGuiStyleVar.IndentSpacing, Utils.IndentSpacing);
                                if(ImGui.IsWindowAppearing()) Utils.ResetCaches();
                                void ToggleMoodle(Vector4 selectedCol, Guid id, string name)
                                {
                                    var cont = preset.Moodles.Any(x => x.Guid == id);
                                    ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, Vector2.Zero);
                                    if(ImGui.BeginTable($"{id}Moodle", 2, ImGuiTableFlags.NoSavedSettings | ImGuiTableFlags.SizingFixedFit))
                                    {
                                        ImGui.PushFont(UiBuilder.IconFont);
                                        var size = ImGuiHelpers.GetButtonSize(FontAwesomeIcon.Link.ToIconString());
                                        ImGui.PopFont();
                                        ImGui.TableSetupColumn("Moodle", ImGuiTableColumnFlags.WidthStretch);
                                        ImGui.TableSetupColumn("Button", ImGuiTableColumnFlags.WidthFixed, size.X);
                                        ImGui.TableNextRow();
                                        ImGui.TableNextColumn();
                                        if(ImGuiEx.SelectableNode(cont ? selectedCol : null, name + "      ", ref cont, cont ? ImGuiTreeNodeFlags.Bullet : ImGuiTreeNodeFlags.Leaf))
                                        {
                                            if(cont)
                                            {
                                                preset.Moodles.Add(new(id, false));
                                                if(C.AutofillFromGlam && preset.Name == "") preset.Name = name;
                                            }
                                            else
                                            {
                                                new TickScheduler(() => preset.Moodles.RemoveAll(z => z.Guid == id));
                                            }
                                        }
                                        ImGui.TableNextColumn();
                                        if(cont)
                                        {
                                            var e = preset.Moodles.First(x => x.Guid == id);
                                            ImGui.PushFont(UiBuilder.IconFont);
                                            ImGuiEx.ButtonCheckbox(FontAwesomeIcon.Link.ToIconString(), ref e.Cancel, true);
                                            ImGui.PopFont();
                                            ImGuiEx.Tooltip($"Cancel this moodle(s) once preset is no longer applied");
                                        }
                                        ImGui.EndTable();
                                    }
                                    ImGui.PopStyleVar();
                                }

                                FiltersSelection();
                                var moodles = P.MoodlesManager.GetMoodles().OrderBy(x => x.FullPath);
                                var moodlePresets = P.MoodlesManager.GetPresets().OrderBy(x => x.FullPath);
                                var index = 0;
                                if(ImGuiEx.TreeNode(Colors.TabGreen, "Moodles", ImGuiTreeNodeFlags.DefaultOpen))
                                {
                                    List<(string[], Action)> items = [];
                                    foreach(var x in moodles)
                                    {
                                        index++;
                                        ImGui.PushID(index);
                                        var name = x.FullPath;
                                        if(Filters[filterCnt].Length > 0 && !name.Contains(Filters[filterCnt], StringComparison.OrdinalIgnoreCase)) continue;
                                        if(OnlySelected[filterCnt] && !preset.Moodles.Any(z => z.Guid == x.ID)) continue;
                                        if(currentProfile.MoodlesPathes.Count > 0 && !name.StartsWithAny(currentProfile.MoodlesPathes)) continue;
                                        var parts = name.SplitDirectories();
                                        items.Add((parts[0..^1], () => ToggleMoodle(Colors.TabGreen, x.ID, parts[^1])));
                                        noresults = false;
                                        ImGui.PopID();
                                    }
                                    Utils.DrawFolder(items);
                                    ImGui.TreePop();
                                }
                                if(ImGuiEx.TreeNode(Colors.TabYellow, "Moodle Presets", ImGuiTreeNodeFlags.DefaultOpen))
                                {
                                    List<(string[], Action)> items = [];
                                    foreach(var x in moodlePresets)
                                    {
                                        index++;
                                        ImGui.PushID(index);
                                        var name = x.FullPath;
                                        if(Filters[filterCnt].Length > 0 && !name.Contains(Filters[filterCnt], StringComparison.OrdinalIgnoreCase)) continue;
                                        if(OnlySelected[filterCnt] && !preset.Moodles.Any(z => z.Guid == x.ID)) continue;
                                        if(currentProfile.MoodlesPathes.Count > 0 && !name.StartsWithAny(currentProfile.MoodlesPathes)) continue;
                                        var parts = name.SplitDirectories();
                                        items.Add((parts[0..^1], () => ToggleMoodle(Colors.TabYellow, x.ID, parts[^1])));
                                        noresults = false;
                                        ImGui.PopID();
                                    }
                                    Utils.DrawFolder(items);
                                    ImGui.TreePop();
                                }
                                foreach(var x in preset.Moodles)
                                {
                                    if(moodles.Any(z => z.ID == x.Guid)) continue;
                                    if(moodlePresets.Any(z => z.ID == x.Guid)) continue;
                                    Utils.CollectionSelectable(ImGuiColors.DalamudRed, $"{x}", x, preset.Moodles, true);
                                }
                                if(noresults && Filters[filterCnt].Length == 0) OnlySelected[filterCnt] = false;
                                ImGui.PopStyleVar();
                                ImGui.EndCombo();
                            }
                            if(fullList != null) ImGuiEx.Tooltip("All of these Moodles/Presets will be applied:\n" + fullList);
                            filterCnt++;
                        }
                    }

                    ImGui.TableNextColumn();

                    //Delete
                    if(ImGuiEx.IconButton(FontAwesomeIcon.Copy))
                    {
                        Safe(() => Copy(JsonConvert.SerializeObject(preset)));
                    }
                    ImGuiEx.Tooltip("Copy to clipboard");
                    if(!isFallback)
                    {
                        ImGui.SameLine();
                        if(ImGuiEx.IconButton(FontAwesomeIcon.Trash) && ImGui.GetIO().KeyCtrl)
                        {
                            new TickScheduler(() => presetList.RemoveAll(x => x.GUID == preset.GUID));
                        }
                        ImGuiEx.Tooltip("Hold CTRL+Click to delete");
                    }

                    if(isStaticExists) ImGui.PopStyleColor();
                    ImGui.PopID();
                }
                ImGui.EndTable();
                postAction = () => DragDrop.End();
            }
            ImGui.PopStyleVar();
        }
    }
}