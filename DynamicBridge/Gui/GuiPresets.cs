using DynamicBridge.Configuration;
using DynamicBridge.IPC.Honorific;
using ECommons.Configuration;
using Newtonsoft.Json;
using System.Data;

namespace DynamicBridge.Gui
{
    public static class GuiPresets
    {
        static string[] Filters = ["", "", "", "", "", "", "", "", "", "", "", "", "", "", ""];
        static bool[] OnlySelected = new bool[15];
        static string CurrentDrag = null;
        static bool Focus = false;
        static string Open = null;
        public static void DrawUser()
        {
            if (UI.Profile != null)
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

        static void DrawProfile(Profile Profile, bool drawFallback, bool drawHeader, bool drawGlobalSection)
        {
            Profile.GetPresetsListUnion().Each(f => f.RemoveAll(x => x == null));

            void Buttons()
            {
                if (ImGuiEx.IconButton(FontAwesomeIcon.Plus))
                {
                    if (Open != null && Profile.PresetsFolders.TryGetFirst(x => x.GUID == Open, out var open))
                    {
                        open.Presets.Add(new());
                    }
                    else
                    {
                        Profile.Presets.Add(new());
                    }
                }
                ImGuiEx.Tooltip(Lang.Presets_AddNewEmpty);
                ImGui.SameLine();
                if (ImGuiEx.IconButton(FontAwesomeIcon.Paste))
                {
                    try
                    {
                        var str = (EzConfig.DefaultSerializationFactory.Deserialize<Preset>(Paste()));
                        if (str != null)
                        {
                            if (Open != null && Profile.PresetsFolders.TryGetFirst(x => x.GUID == Open, out var open))
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
                            Notify.Error(Lang.CouldNotImportFromClipboard);
                        }
                    }
                    catch (Exception e)
                    {
                        Notify.Error(e.Message);
                    }
                }
                ImGuiEx.Tooltip(Lang.PastePreviouslyCopiedPresetFromClipboard);
                ImGui.SameLine();

                if (ImGuiEx.IconButton(FontAwesomeIcon.FolderPlus))
                {
                    Profile.PresetsFolders.Add(new() { Name = Lang.Presets_PresetFolder.Params(Profile.PresetsFolders.Count + 1) });
                }
                ImGuiEx.Tooltip(Lang.AddNewPresetFolder);

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

            if (drawHeader)
            {
                UI.ProfileSelectorCommon(Buttons, RightButtons);
            }
            else
            {
                ImGuiEx.RightFloat(RightButtons);
                Buttons();
                ImGui.SameLine();
                ImGuiEx.TextWrapped(Lang.GlobalPresetsNote);
            }

            string newOpen = null;

            if (!Focus || Open == "" || Open == null)
            {
                if (ImGuiEx.TreeNode(Lang.MainPresetsFolder + "##global", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    newOpen = "";
                    DragDrop.AcceptFolderDragDrop(Profile, Profile.Presets, ImGuiDragDropFlags.AcceptBeforeDelivery | ImGuiDragDropFlags.AcceptNoDrawDefaultRect);
                    DrawPresets(Profile, Profile.Presets, out var postAction, "", false, drawGlobalSection);
                    ImGui.TreePop();
                    postAction?.Invoke();
                }
                else
                {
                    DragDrop.AcceptFolderDragDrop(Profile, Profile.Presets);
                }
            }

            for (int presetFolderIndex = 0; presetFolderIndex < Profile.PresetsFolders.Count; presetFolderIndex++)
            {
                var presetFolder = Profile.PresetsFolders[presetFolderIndex];
                if (Focus && Open != presetFolder.GUID && Open != null) continue;
                if (presetFolder.HiddenFromSelection)
                {
                    ImGuiEx.RightFloat($"RFCHP{presetFolder.GUID}", () => ImGuiEx.Text(ImGuiColors.DalamudGrey, Lang.HiddenFromRules));
                }
                if (ImGuiEx.TreeNode($"{presetFolder.Name}###presetfolder{presetFolder.GUID}"))
                {
                    newOpen = presetFolder.GUID;
                    CollapsingHeaderClicked();
                    DragDrop.AcceptFolderDragDrop(Profile, presetFolder.Presets, ImGuiDragDropFlags.AcceptBeforeDelivery | ImGuiDragDropFlags.AcceptNoDrawDefaultRect);
                    DrawPresets(Profile, presetFolder.Presets, out var postAction, presetFolder.GUID, false, drawGlobalSection);
                    ImGui.TreePop();
                    postAction?.Invoke();
                }
                else
                {
                    CollapsingHeaderClicked();
                    DragDrop.AcceptFolderDragDrop(Profile, presetFolder.Presets);
                }
                void CollapsingHeaderClicked()
                {
                    if (ImGui.IsItemHovered() && ImGui.IsItemClicked(ImGuiMouseButton.Right)) ImGui.OpenPopup($"Folder{presetFolder.GUID}");
                    if (ImGui.BeginPopup($"Folder{presetFolder.GUID}"))
                    {
                        ImGuiEx.SetNextItemWidthScaled(150f);
                        ImGui.InputTextWithHint("##name", Lang.FolderName, ref presetFolder.Name, 200);
                        if (ImGui.Selectable(Lang.ExportToClipboard))
                        {
                            Copy(EzConfig.DefaultSerializationFactory.Serialize(presetFolder, false));
                        }
                        if (presetFolder.HiddenFromSelection)
                        {
                            if (ImGui.Selectable(Lang.ShowInRulesSection)) presetFolder.HiddenFromSelection = false;
                        }
                        else
                        {
                            if (ImGui.Selectable(Lang.HideFromRulesSection)) presetFolder.HiddenFromSelection = true;
                        }
                        if (ImGui.Selectable(Lang.MoveUp, false, ImGuiSelectableFlags.DontClosePopups) && presetFolderIndex > 0)
                        {
                            (Profile.PresetsFolders[presetFolderIndex], Profile.PresetsFolders[presetFolderIndex - 1]) = (Profile.PresetsFolders[presetFolderIndex - 1], Profile.PresetsFolders[presetFolderIndex]);
                        }
                        if (ImGui.Selectable(Lang.MoveDown, false, ImGuiSelectableFlags.DontClosePopups) && presetFolderIndex < Profile.PresetsFolders.Count - 1)
                        {
                            (Profile.PresetsFolders[presetFolderIndex], Profile.PresetsFolders[presetFolderIndex + 1]) = (Profile.PresetsFolders[presetFolderIndex + 1], Profile.PresetsFolders[presetFolderIndex]);
                        }
                        ImGui.Separator();

                        if (ImGui.BeginMenu(Lang.DeleteFolder))
                        {
                            if (ImGui.Selectable(Lang.AndMoveProfilesToDefaultFolderHoldCTRL))
                            {
                                if (ImGuiEx.Ctrl)
                                {
                                    new TickScheduler(() =>
                                    {
                                        foreach (var x in presetFolder.Presets)
                                        {
                                            Profile.Presets.Add(x);
                                        }
                                        Profile.PresetsFolders.Remove(presetFolder);
                                    });
                                }
                            }
                            if (ImGui.Selectable(Lang.AndDeleteIncludedProfilesHoldCTRLSHIFT))
                            {
                                if (ImGuiEx.Ctrl && ImGuiEx.Shift)
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
                        if (!ImGui.IsMouseDragging(ImGuiMouseButton.Left))
                        {
                            ImGuiEx.Tooltip(Lang.RightClickToOpenContextMenu);
                        }
                    }
                }
            }
            if (drawFallback)
            {
                if (ImGuiEx.TreeNode(Lang.FallbackPresetSection))
                {
                    DrawPresets(Profile, [Profile.FallbackPreset], out _, $"FallbackPreset-8c680b09-acd0-43ab-9413-26a4e38841fc", true, drawGlobalSection);
                    Open = newOpen;
                    ImGui.TreePop();
                }
            }
        }

        static void DrawPresets(Profile currentProfile, List<Preset> presetList, out Action postAction, string extraID, bool isFallback, bool isGlobal)
        {
            postAction = null;
            var cnt = 3;
            if (C.EnableHonorific) cnt++;
            if (C.EnableCustomize) cnt++;
            if (C.EnableGlamourer) cnt++;
            if (C.EnablePenumbra) cnt++;
            if (C.EnableMoodles) cnt++;
            List<(Vector2 RowPos, Vector2 ButtonPos, Action BeginDraw, Action AcceptDraw)> MoveCommands = [];
            ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, Utils.CellPadding);
            if (ImGui.BeginTable($"##presets{extraID}", cnt, ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders | ImGuiTableFlags.Resizable | ImGuiTableFlags.Reorderable))
            {
                ImGui.TableSetupColumn("  ", ImGuiTableColumnFlags.NoResize | ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoSort);
                ImGui.TableSetupColumn("Name");
                if (C.EnableGlamourer) ImGui.TableSetupColumn("Glamourer");
                if (C.EnableCustomize) ImGui.TableSetupColumn("Customize+");
                if (C.EnableHonorific) ImGui.TableSetupColumn("Honorific");
                if (C.EnablePenumbra) ImGui.TableSetupColumn("Penumbra");
                if (C.EnableMoodles) ImGui.TableSetupColumn("Moodles");
                ImGui.TableSetupColumn(" ", ImGuiTableColumnFlags.NoResize | ImGuiTableColumnFlags.WidthFixed);
                ImGui.TableHeadersRow();

                var isStaticExists = currentProfile.IsStaticExists() && !isFallback;

                for (int i = 0; i < presetList.Count; i++)
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

                    var preset = presetList[i];

                    ImGui.PushID(preset.GUID);

                    if (isStaticExists)
                    {
                        ImGui.PushStyleColor(ImGuiCol.Text, preset.IsStatic ? ImGuiColors.DalamudOrange : ImGuiColors.DalamudGrey);
                    }
                    ImGui.TableNextRow();
                    if (CurrentDrag == preset.GUID)
                    {
                        var col = GradientColor.Get(EColor.Green, EColor.Green with { W = EColor.Green.W / 4 }, 500).ToUint();
                        ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg0, col);
                        ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg1, col);
                    }
                    ImGui.TableNextColumn();

                    //Sorting
                    if (isFallback)
                    {
                        ImGuiEx.TextV(" ");
                        ImGuiEx.HelpMarker(Lang.FallbackNotice);
                    }
                    else
                    {
                        var rowPos = ImGui.GetCursorPos();
                        if (ImGui.RadioButton("##static", preset.IsStatic))
                        {
                            preset.IsStatic = !preset.IsStatic;
                            if (preset.IsStatic)
                            {
                                currentProfile.SetStatic(preset);
                            }
                            P.ForceUpdate = true;
                        }
                        ImGuiEx.Tooltip(Lang.PresetStaticSetNotice);
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
                            ImGui.Button($"{FontAwesomeIcon.ArrowsUpDownLeftRight.ToIconString()}##Move{preset.GUID}");
                            ImGui.PopFont();
                            if (ImGui.IsItemHovered())
                            {
                                ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeAll);
                            }
                            if (ImGui.BeginDragDropSource(ImGuiDragDropFlags.SourceNoPreviewTooltip))
                            {
                                ImGuiDragDrop.SetDragDropPayload("MovePreset", preset.GUID);
                                CurrentDrag = preset.GUID;
                                InternalLog.Verbose($"DragDropSource = {preset.GUID}");
                                ImGui.EndDragDropSource();
                            }
                            else if (CurrentDrag == preset.GUID)
                            {
                                InternalLog.Verbose($"Current drag reset!");
                                CurrentDrag = null;
                            }
                        }, delegate { DragDrop.AcceptProfileDragDrop(currentProfile, presetList, moveIndex); }
                        ));

                        ImGui.SameLine();
                        if (ImGuiEx.IconButton(FontAwesomeIcon.CaretDown))
                        {
                            ImGui.OpenPopup($"MoveTo##{preset.GUID}");
                        }
                        if (ImGui.BeginPopup($"MoveTo##{preset.GUID}"))
                        {
                            if (ImGui.Selectable(Lang.MoveToMainFolder, currentProfile.Presets.Any(x => x.GUID == preset.GUID)))
                            {
                                DragDrop.MovePresetToList(currentProfile, preset.GUID, currentProfile.Presets);
                            }
                            foreach (var x in currentProfile.PresetsFolders)
                            {
                                if (ImGui.Selectable($"{x.Name}##{x.GUID}", x.Presets.Any(x => x.GUID == preset.GUID)))
                                {
                                    DragDrop.MovePresetToList(currentProfile, preset.GUID, x.Presets);
                                }
                            }
                            ImGui.EndPopup();
                        }
                    }

                    ImGui.TableNextColumn();

                    //name
                    if (isFallback)
                    {
                        ImGuiEx.TextV(Lang.BasePreset);
                    }
                    else
                    {
                        var isEmpty = preset.Name == "";
                        var isNonUnique = currentProfile.GetPresetsUnion().Count(x => x.Name == preset.Name) > 1;
                        if (isEmpty)
                        {
                            ImGui.PushFont(UiBuilder.IconFont);
                            ImGuiEx.Text(ImGuiColors.DalamudRed, Utils.IconWarning);
                            ImGui.PopFont();
                            ImGuiEx.Tooltip(Lang.ErrorNameCanNotBeEmpty);
                            ImGui.SameLine();
                        }
                        else if (isNonUnique)
                        {
                            ImGui.PushFont(UiBuilder.IconFont);
                            ImGuiEx.Text(ImGuiColors.DalamudRed, Utils.IconWarning);
                            ImGui.PopFont();
                            ImGuiEx.Tooltip(Lang.ErrorNameMustBeUnique);
                            ImGui.SameLine();
                        }
                        ImGuiEx.SetNextItemFullWidth();
                        ImGui.InputTextWithHint("##name", Lang.PresetName, ref preset.Name, 100, Utils.CensorFlags);
                    }


                    //Glamourer
                    {
                        if (C.EnableGlamourer)
                        {
                            ImGui.TableNextColumn();
                            ImGuiEx.SetNextItemFullWidth();
                            if (ImGui.BeginCombo("##glamour", ((string[])[.. preset.Glamourer.Select(P.GlamourerManager.TransformName), .. preset.ComplexGlamourer]).PrintRange(out var fullList, "- None -"), C.ComboSize))
                            {
                                if (ImGui.IsWindowAppearing()) Utils.ResetCaches();
                                FiltersSelection();
                                ImGui.PushStyleVar(ImGuiStyleVar.IndentSpacing, Utils.IndentSpacing);
                                // normal
                                {
                                    List<(string[], Action)> items = [];
                                    var designs = P.GlamourerManager.GetDesigns().OrderBy(x => P.GlamourerManager.TransformName(x.Identifier.ToString()));
                                    foreach (var x in designs)
                                    {
                                        var name = x.Name;
                                        var id = x.Identifier.ToString();
                                        var transformedName = P.GlamourerManager.TransformName(x.Identifier.ToString());
                                        if (C.GlamourerFullPath && currentProfile.Pathes.Count > 0 && !transformedName.StartsWithAny(currentProfile.Pathes)) continue;
                                        if (Filters[filterCnt].Length > 0 && !transformedName.Contains(Filters[filterCnt], StringComparison.OrdinalIgnoreCase)) continue;
                                        var contains = preset.Glamourer.Contains(id);
                                        if (OnlySelected[filterCnt] && !contains) continue;

                                        items.Add((transformedName.SplitDirectories()[0..^1], () =>
                                        {
                                            if (Utils.CollectionSelectable(contains ? Colors.TabGreen : null, $"{name}  ##{x.Identifier}", id, preset.Glamourer))
                                            {
                                                if (C.AutofillFromGlam && preset.Name == "" && preset.Glamourer.Contains(id)) preset.Name = name;
                                            }
                                        }

                                        ));
                                    }
                                    foreach (var x in preset.Glamourer)
                                    {
                                        if (designs.Any(d => d.Identifier.ToString() == x)) continue;
                                        items.Add(([], () => Utils.CollectionSelectable(ImGuiColors.DalamudRed, $"{x}", x, preset.Glamourer, true)));
                                    }
                                    Utils.DrawFolder(items);
                                }

                                //complex
                                {
                                    List<(string[], Action)> items = [];
                                    var designs = C.ComplexGlamourerEntries;
                                    ImGui.PushStyleColor(ImGuiCol.Text, EColor.YellowBright);
                                    foreach (var x in designs)
                                    {
                                        var name = x.Name;
                                        if (Filters[filterCnt].Length > 0 && !name.Contains(Filters[filterCnt], StringComparison.OrdinalIgnoreCase)) continue;
                                        if (OnlySelected[filterCnt] && !preset.ComplexGlamourer.Contains(name)) continue;
                                        var contains = preset.ComplexGlamourer.Contains(name);
                                        items.Add((name.SplitDirectories()[0..^1], () =>
                                        {
                                            if (Utils.CollectionSelectable(contains ? Colors.TabYellow : null, $"{name}##{x.GUID}", name, preset.ComplexGlamourer))
                                            {
                                                if (C.AutofillFromGlam && preset.Name == "" && preset.ComplexGlamourer.Contains(name)) preset.Name = name;
                                            }
                                        }
                                        ));
                                    }
                                    ImGui.PopStyleColor();
                                    foreach (var x in preset.ComplexGlamourer)
                                    {
                                        if (designs.Any(d => d.Name == x)) continue;
                                        items.Add(([], () =>
                                        {
                                            Utils.CollectionSelectable(ImGuiColors.DalamudRed, $"{x}", x, preset.ComplexGlamourer, true);
                                        }

                                        ));
                                    }
                                    if (items.Count > 0)
                                    {
                                        if (ImGuiEx.TreeNode(Colors.TabYellow, Lang.LayeredDesigns))
                                        {
                                            Utils.DrawFolder(items);
                                            ImGui.TreePop();
                                        }
                                    }
                                }

                                ImGui.PopStyleVar();

                                ImGui.EndCombo();
                            }
                            if (fullList != null) ImGuiEx.Tooltip(UI.RandomNotice + fullList);
                            filterCnt++;
                        }
                    }


                    //customize+
                    {
                        if (C.EnableCustomize)
                        {
                            ImGui.TableNextColumn();
                            if (isGlobal)
                            {
                                ImGuiEx.HelpMarker(Lang.CustomizePlusGlobalNotice, EColor.OrangeBright, FontAwesomeIcon.ExclamationTriangle.ToIconString(), false);
                                ImGui.SameLine();
                            }
                            ImGuiEx.SetNextItemFullWidth();
                            if (ImGui.BeginCombo("##customize", preset.Customize.Select(P.CustomizePlusManager.TransformName).PrintRange(out var fullList, Lang.NoneSelection), C.ComboSize))
                            {
                                ImGui.PushStyleVar(ImGuiStyleVar.IndentSpacing, Utils.IndentSpacing);
                                if (ImGui.IsWindowAppearing()) Utils.ResetCaches();
                                FiltersSelection();
                                var profiles = P.CustomizePlusManager.GetProfiles(isGlobal ? null : currentProfile.Characters.Select(Utils.GetCharaNameFromCID).Select(z => z.Split("@")[0])).OrderBy(x => P.CustomizePlusManager.TransformName($"{x.ID}"));
                                var index = 0;
                                List<(string[], Action)> items = [];
                                foreach (var x in profiles)
                                {
                                    index++;
                                    ImGui.PushID(index);
                                    var name = P.CustomizePlusManager.TransformName($"{x.ID}");
                                    if (C.GlamourerFullPath && currentProfile.CustomizePathes.Count > 0 && !name.StartsWithAny(currentProfile.CustomizePathes)) continue;
                                    if (Filters[filterCnt].Length > 0 && !name.Contains(Filters[filterCnt], StringComparison.OrdinalIgnoreCase)) continue;
                                    if (OnlySelected[filterCnt] && !preset.Customize.Contains($"{x.ID}")) continue;
                                    var contains = preset.Customize.Contains($"{x.ID}");
                                    items.Add((name.SplitDirectories()[0..^1], () =>
                                    {
                                        if (Utils.CollectionSelectable(contains ? Colors.TabGreen : null, $"{name}  ", $"{x.ID}", preset.Customize))
                                        {
                                            if (C.AutofillFromGlam && preset.Name == "" && preset.Customize.Contains($"{x.ID}")) preset.Name = name;
                                        }
                                    }

                                    ));
                                    ImGui.PopID();
                                }
                                foreach (var x in preset.Customize)
                                {
                                    if (profiles.Any(d => d.ID.ToString() == x)) continue;
                                    ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.DalamudRed);
                                    items.Add(([], () => Utils.CollectionSelectable(ImGuiColors.DalamudRed, $"{x}  ", x, preset.Customize, true)));
                                    ImGui.PopStyleColor();
                                }
                                Utils.DrawFolder(items);
                                ImGui.PopStyleVar();
                                ImGui.EndCombo();
                            }
                            if (fullList != null) ImGuiEx.Tooltip(UI.RandomNotice + fullList);
                            filterCnt++;
                        }
                    }


                    //Honorific
                    {
                        if (C.EnableHonorific)
                        {
                            ImGui.TableNextColumn();
                            if (isGlobal && !C.HonotificUnfiltered)
                            {
                                ImGuiEx.HelpMarker(Lang.HonorificGlobalPresetNotice, EColor.OrangeBright, FontAwesomeIcon.ExclamationTriangle.ToIconString(), false);
                                ImGui.SameLine();
                            }
                            ImGuiEx.SetNextItemFullWidth();
                            if (ImGui.BeginCombo("##honorific", preset.Honorific.PrintRange(out var fullList, Lang.NoneSelection), C.ComboSize))
                            {
                                ImGui.PushStyleVar(ImGuiStyleVar.IndentSpacing, Utils.IndentSpacing);
                                if (ImGui.IsWindowAppearing()) Utils.ResetCaches();
                                FiltersSelection();
                                IEnumerable<ulong> charas = C.HonotificUnfiltered || isGlobal ? C.SeenCharacters.Keys : currentProfile.Characters;
                                List<(string[], Action)> items = [];
                                List<TitleData> allTitles = [];
                                foreach (var chara in charas)
                                {
                                    var titles = P.HonorificManager.GetTitleData([chara]).OrderBy(x => x.Title);
                                    allTitles.AddRange(titles);
                                    var index = 0;
                                    foreach (var x in titles)
                                    {
                                        index++;
                                        ImGui.PushID(index);
                                        var name = x.Title;
                                        if (Filters[filterCnt].Length > 0 && !name.Contains(Filters[filterCnt], StringComparison.OrdinalIgnoreCase)) continue;
                                        if (OnlySelected[filterCnt] && !preset.Honorific.Contains(name)) continue;
                                        if (x.Color != null) ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(x.Color.Value, 1f));
                                        var contains = preset.Honorific.Contains(x.Title);
                                        items.Add(([Utils.GetCharaNameFromCID(chara)], () =>
                                        {
                                            if (Utils.CollectionSelectable(contains ? Colors.TabGreen : null, $"{name}  ", x.Title, preset.Honorific))
                                            {
                                                if (C.AutofillFromGlam && preset.Name == "" && preset.Honorific.Contains(x.Title)) preset.Name = name;
                                            }
                                        }

                                        ));
                                        if (x.Color != null) ImGui.PopStyleColor();
                                        ImGui.PopID();
                                    }
                                }
                                foreach (var x in preset.Honorific)
                                {
                                    if (allTitles.Any(d => d.Title == x)) continue;
                                    items.Add(([], () => Utils.CollectionSelectable(ImGuiColors.DalamudRed, $"{x}  ", x, preset.Honorific, true)));
                                }
                                Utils.DrawFolder(items);
                                ImGui.PopStyleVar();
                                ImGui.EndCombo();
                            }
                            if (fullList != null) ImGuiEx.Tooltip(UI.RandomNotice + fullList);
                            filterCnt++;
                        }

                    }

                    //Penumbra
                    {
                        if (C.EnablePenumbra)
                        {
                            ImGui.TableNextColumn();
                            ImGuiEx.SetNextItemFullWidth();
                            string fullList = null;
                            if (ImGui.BeginCombo("##penumbra", preset.PenumbraType != SpecialPenumbraAssignment.Use_Named_Collection ? preset.PenumbraType.ToString().Replace("_", " ") : preset.Penumbra.PrintRange(out fullList, "- None -"), C.ComboSize))
                            {
                                ImGui.PushStyleVar(ImGuiStyleVar.IndentSpacing, Utils.IndentSpacing);
                                if (ImGui.IsWindowAppearing()) Utils.ResetCaches();
                                ImGuiEx.Text(Lang.AssignmentType);
                                ImGuiEx.EnumCombo($"##asstype", ref preset.PenumbraType);
                                if (preset.PenumbraType == SpecialPenumbraAssignment.Use_Named_Collection)
                                {
                                    FiltersSelection();
                                    var collections = P.PenumbraManager.GetCollectionNames().Order();
                                    var index = 0;
                                    List<(string[], Action)> items = [];
                                    foreach (var x in collections)
                                    {
                                        index++;
                                        ImGui.PushID(index);
                                        var name = x;
                                        if (Filters[filterCnt].Length > 0 && !name.Contains(Filters[filterCnt], StringComparison.OrdinalIgnoreCase)) continue;
                                        if (OnlySelected[filterCnt] && !preset.Penumbra.Contains(name)) continue;
                                        var contains = preset.Penumbra.Contains(name);
                                        items.Add(([], () =>
                                        {
                                            if (Utils.CollectionSelectable(contains ? Colors.TabGreen : null, $"{x}  ", x, preset.Penumbra))
                                            {
                                                if (C.AutofillFromGlam && preset.Name == "" && preset.Penumbra.Contains(x)) preset.Name = name;
                                            }
                                        }

                                        ));
                                        ImGui.PopID();
                                    }
                                    foreach (var x in preset.Penumbra)
                                    {
                                        if (collections.Contains(x)) continue;
                                        items.Add(([], () =>
                                        {
                                            Utils.CollectionSelectable(ImGuiColors.DalamudRed, $"{x}", x, preset.Penumbra, true);
                                        }

                                        ));
                                    }
                                    Utils.DrawFolder(items);
                                }
                                ImGui.PopStyleVar();
                                ImGui.EndCombo();
                            }
                            if (fullList != null) ImGuiEx.Tooltip(UI.RandomNotice + fullList);
                            filterCnt++;
                        }
                    }

                    //Moodles
                    {
                        if (C.EnableMoodles)
                        {
                            ImGui.TableNextColumn();
                            ImGuiEx.SetNextItemFullWidth();
                            if (ImGui.BeginCombo("##moodles", preset.Moodles.Select(Utils.GetName).PrintRange(out var fullList, "- None -"), C.ComboSize))
                            {
                                ImGui.PushStyleVar(ImGuiStyleVar.IndentSpacing, Utils.IndentSpacing);
                                if (ImGui.IsWindowAppearing()) Utils.ResetCaches();
                                void ToggleMoodle(Vector4 selectedCol, Guid id, string name)
                                {
                                    var cont = preset.Moodles.Any(x => x.Guid == id);
                                    ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, Vector2.Zero);
                                    if (ImGui.BeginTable($"{id}Moodle", 2, ImGuiTableFlags.NoSavedSettings | ImGuiTableFlags.SizingFixedFit))
                                    {
                                        ImGui.PushFont(UiBuilder.IconFont);
                                        var size = ImGuiHelpers.GetButtonSize(FontAwesomeIcon.Link.ToIconString());
                                        ImGui.PopFont();
                                        ImGui.TableSetupColumn("Moodle", ImGuiTableColumnFlags.WidthStretch);
                                        ImGui.TableSetupColumn("Button", ImGuiTableColumnFlags.WidthFixed, size.X);
                                        ImGui.TableNextRow();
                                        ImGui.TableNextColumn();
                                        if (ImGuiEx.Selectable(cont ? selectedCol : null, name + "      ", ref cont, cont ? ImGuiTreeNodeFlags.Bullet : ImGuiTreeNodeFlags.Leaf))
                                        {
                                            if (cont)
                                            {
                                                preset.Moodles.Add(new(id, false));
                                                if (C.AutofillFromGlam && preset.Name == "") preset.Name = name;
                                            }
                                            else
                                            {
                                                new TickScheduler(() => preset.Moodles.RemoveAll(z => z.Guid == id));
                                            }
                                        }
                                        ImGui.TableNextColumn();
                                        if (cont)
                                        {
                                            var e = preset.Moodles.First(x => x.Guid == id);
                                            ImGui.PushFont(UiBuilder.IconFont);
                                            ImGuiEx.ButtonCheckbox(FontAwesomeIcon.Link.ToIconString(), ref e.Cancel, true);
                                            ImGui.PopFont();
                                            ImGuiEx.Tooltip(Lang.MoodlesCancelTooltip);
                                        }
                                        ImGui.EndTable();
                                    }
                                    ImGui.PopStyleVar();
                                }

                                FiltersSelection();
                                var moodles = P.MoodlesManager.GetMoodles().OrderBy(x => x.FullPath);
                                var moodlePresets = P.MoodlesManager.GetPresets().OrderBy(x => x.FullPath);
                                var index = 0;
                                if (ImGuiEx.TreeNode(Colors.TabGreen, "Moodles", ImGuiTreeNodeFlags.DefaultOpen))
                                {
                                    List<(string[], Action)> items = [];
                                    foreach (var x in moodles)
                                    {
                                        index++;
                                        ImGui.PushID(index);
                                        var name = x.FullPath;
                                        if (Filters[filterCnt].Length > 0 && !name.Contains(Filters[filterCnt], StringComparison.OrdinalIgnoreCase)) continue;
                                        if (OnlySelected[filterCnt] && !preset.Moodles.Any(z => z.Guid == x.ID)) continue;
                                        if (currentProfile.MoodlesPathes.Count > 0 && !name.StartsWithAny(currentProfile.MoodlesPathes)) continue;
                                        var parts = name.SplitDirectories();
                                        items.Add((parts[0..^1], () => ToggleMoodle(Colors.TabGreen, x.ID, parts[^1])));
                                        ImGui.PopID();
                                    }
                                    Utils.DrawFolder(items);
                                    ImGui.TreePop();
                                }
                                if (ImGuiEx.TreeNode(Colors.TabYellow, Lang.MoodlePresets, ImGuiTreeNodeFlags.DefaultOpen))
                                {
                                    List<(string[], Action)> items = [];
                                    foreach (var x in moodlePresets)
                                    {
                                        index++;
                                        ImGui.PushID(index);
                                        var name = x.FullPath;
                                        if (Filters[filterCnt].Length > 0 && !name.Contains(Filters[filterCnt], StringComparison.OrdinalIgnoreCase)) continue;
                                        if (OnlySelected[filterCnt] && !preset.Moodles.Any(z => z.Guid == x.ID)) continue;
                                        if (currentProfile.MoodlesPathes.Count > 0 && !name.StartsWithAny(currentProfile.MoodlesPathes)) continue;
                                        var parts = name.SplitDirectories();
                                        items.Add((parts[0..^1], () => ToggleMoodle(Colors.TabYellow, x.ID, parts[^1])));
                                        ImGui.PopID();
                                    }
                                    Utils.DrawFolder(items);
                                    ImGui.TreePop();
                                }
                                foreach (var x in preset.Moodles)
                                {
                                    if (moodles.Any(z => z.ID == x.Guid)) continue;
                                    if (moodlePresets.Any(z => z.ID == x.Guid)) continue;
                                    Utils.CollectionSelectable(ImGuiColors.DalamudRed, $"{x}", x, preset.Moodles, true);
                                }
                                ImGui.PopStyleVar();
                                ImGui.EndCombo();
                            }
                            if (fullList != null) ImGuiEx.Tooltip(Lang.MoodlesApplicationTooltip.Params(fullList));
                            filterCnt++;
                        }
                    }

                    ImGui.TableNextColumn();

                    //Delete
                    if (ImGuiEx.IconButton(FontAwesomeIcon.Copy))
                    {
                        Safe(() => Copy(JsonConvert.SerializeObject(preset)));
                    }
                    ImGuiEx.Tooltip(Lang.CopyToClipboard);
                    if (!isFallback)
                    {
                        ImGui.SameLine();
                        if (ImGuiEx.IconButton(FontAwesomeIcon.Trash) && ImGui.GetIO().KeyCtrl)
                        {
                            new TickScheduler(() => presetList.RemoveAll(x => x.GUID == preset.GUID));
                        }
                        ImGuiEx.Tooltip(Lang.HoldCTRLClickToDelete);
                    }

                    if (isStaticExists) ImGui.PopStyleColor();
                    ImGui.PopID();
                }
                ImGui.EndTable();
                postAction = () =>
                {
                    foreach (var x in MoveCommands)
                    {
                        ImGui.SetCursorPos(x.ButtonPos);
                        x.BeginDraw();
                        x.AcceptDraw();
                        ImGui.SetCursorPos(x.RowPos);
                        ImGui.Dummy(new Vector2(ImGui.GetContentRegionAvail().X, ImGuiHelpers.GetButtonSize(" ").Y));
                        x.AcceptDraw();
                    }
                };
            }
            ImGui.PopStyleVar();
        }
    }
}