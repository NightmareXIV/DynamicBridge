using Dalamud.Interface.Components;
using DynamicBridge.Configuration;
using ECommons.Configuration;
using ECommons.GameHelpers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;

namespace DynamicBridge.Gui;
public static class GuiProfiles
{
    static string[] Filters = ["", "", "", ""];

    public static void Draw()
    {
        ImGuiEx.InputWithRightButtonsArea("DrawProfilesInp", () => ImGui.InputTextWithHint($"##Filter0", Lang.SearchProfileName, ref Filters[0], 100), () =>
        {
            if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.PlusCircle, Lang.CreateEmpty))
            {
                var profile = new Profile();
                C.ProfilesL.Add(profile);
                profile.Name = Lang.NewProfileName.Params(C.ProfilesL.Count);
            }
            ImGui.SameLine();
            ImGuiEx.Tooltip(Lang.CreateNewEmptyProfile);
            if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.Paste, Lang.PasteFromClipboard))
            {
                try
                {
                    var x = EzConfig.DefaultSerializationFactory.Deserialize<Profile>(Paste());
                    if (x != null)
                    {
                        var newName = x.Name + Lang.CopySuffix;
                        if (C.ProfilesL.Any(z => z.Name == newName))
                        {
                            int i = 2;
                            do
                            {
                                newName = x.Name + Lang.CopySuffixWithNumber.Params(i++);
                            }
                            while (C.ProfilesL.Any(z => z.Name == newName));
                        }
                        x.Name = newName;
                        x.Characters.Clear();
                        C.ProfilesL.Add(x);
                    }
                    else
                    {
                        Notify.Error(Lang.CouldNotImportFromClipboard);
                    }
                }
                catch(Exception e)
                {
                    Notify.Error(e.Message);
                }
            }
            ImGuiEx.Tooltip(Lang.NewProfilePasteTooltip);
            ImGui.SameLine();
        });
        ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, Utils.CellPadding);
        if (ImGui.BeginTable($"##profiles", 4, ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders | ImGuiTableFlags.SizingFixedFit))
        {
            //ImGui.TableSetupColumn("  ", ImGuiTableColumnFlags.NoResize | ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoSort);
            ImGui.TableSetupColumn(Lang.NameColumn, ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn(Lang.UsedByColumn, ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn(Lang.FolderWhitelistColumn, ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn(" ", ImGuiTableColumnFlags.NoResize | ImGuiTableColumnFlags.WidthFixed);
            ImGui.TableHeadersRow();

            for (int i = 0; i < C.ProfilesL.Count; i++)
            {
                var profile = C.ProfilesL[i];
                if (Filters[0].Length > 0 && !profile.Name.ContainsAny(StringComparison.OrdinalIgnoreCase, Filters[0])) continue;
                ImGui.PushID(profile.GUID);
                ImGui.TableNextRow();
                /*ImGui.TableNextColumn();
                ImGuiEx.IconButton(FontAwesomeIcon.ArrowsUpDownLeftRight);*/

                ImGui.TableNextColumn();
                ImGuiEx.SetNextItemFullWidth();
                ImGui.InputText($"##profilename", ref profile.Name, 100, Utils.CensorFlags);

                ImGui.TableNextColumn();
                var text = profile.Characters.Take(2).Select(s => Censor.Character(Utils.GetCharaNameFromCID(s))).Print() + (profile.Characters.Count > 2?Lang.AndMore.Params(profile.Characters.Count - 2) :"");
                ImGuiEx.SetNextItemFullWidth();
                ImGuiEx.Text($"{text}");
                if(profile.Characters.Count > 2)
                {
                    ImGuiEx.Tooltip($"...\n{profile.Characters.Skip(2).Select(s => Censor.Character(Utils.GetCharaNameFromCID(s))).Print("\n")}");
                }

                ImGui.TableNextColumn();

                ImGuiEx.SetNextItemFullWidth();
                if (ImGui.BeginCombo($"##FoldersWhitelist", profile.Pathes.Union(profile.CustomizePathes).Union(profile.MoodlesPathes).PrintRange(out _), C.ComboSize))
                {
                    if (profile.Pathes.Count > 1) ImGuiEx.Tooltip(profile.Pathes.Join("\n"));
                    if (ImGui.IsWindowAppearing()) Utils.ResetCaches();
                    void DrawPathes(List<PathInfo> pathes, List<string> targetCollection)
                    {
                        foreach (var x in pathes)
                        {
                            for (int q = 0; q < x.Indentation; q++)
                            {
                                ImGuiEx.Spacing();
                            }
                            Utils.CollectionSelectable(null, $"{x.Name}", x.Name, targetCollection);
                        }
                        foreach (var x in targetCollection)
                        {
                            if (pathes.Any(z => z.Name == x)) continue;
                            ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.DalamudRed);
                            Utils.CollectionSelectable(null, $"{x}", x, targetCollection, true);
                            ImGui.PopStyleColor();
                        }
                    }
                    ImGui.PushStyleVar(ImGuiStyleVar.IndentSpacing, Utils.IndentSpacing);
                    if (C.EnableGlamourer && ImGuiEx.TreeNode(Colors.TabBlue, "Glamourer", ImGuiTreeNodeFlags.DefaultOpen))
                    {
                        ImGui.PushID("Glam");
                        DrawPathes(P.GlamourerManager.GetCombinedPathes(), profile.Pathes);
                        ImGui.PopID();
                        ImGui.TreePop();
                    }
                    if (C.EnableCustomize && ImGuiEx.TreeNode(Colors.TabBlue, "Customize+", ImGuiTreeNodeFlags.DefaultOpen))
                    {
                        ImGui.PushID("Customize");
                        DrawPathes(P.CustomizePlusManager.GetCombinedPathes(), profile.CustomizePathes);
                        ImGui.PopID();
                        ImGui.TreePop();
                    }
                    if (C.EnableMoodles && ImGuiEx.TreeNode(Colors.TabBlue, "Moodles", ImGuiTreeNodeFlags.DefaultOpen))
                    {
                        ImGui.PushID("Moodles");
                        DrawPathes(P.MoodlesManager.GetCombinedPathes(), profile.MoodlesPathes);
                        ImGui.PopID();
                        ImGui.TreePop();
                    }
                    ImGui.PopStyleVar();
                    ImGui.EndCombo();
                }
                else
                {
                    if (profile.Pathes.Count > 1)
                    {
                        ImGuiEx.Tooltip(profile.Pathes.Join("\n"));
                    }
                }

                ImGui.TableNextColumn();
                if (ImGuiEx.IconButton(FontAwesomeIcon.Pen.ToIconString()))
                {
                    UI.SelectedProfile = profile;
                    new TickScheduler(() => UI.RequestTab = "Dynamic Rules");
                }
                ImGuiEx.Tooltip(Lang.SelectThisProfileForEditing);
                ImGui.SameLine();
                if (ImGuiEx.IconButton(FontAwesomeIcon.Copy.ToIconString()))
                {
                    Copy(JsonConvert.SerializeObject(profile));
                }
                ImGuiEx.Tooltip(Lang.CopyThisProfileToClipboard);
                ImGui.SameLine();
                if (ImGuiEx.IconButton(FontAwesomeIcon.Trash.ToIconString(), enabled:ImGuiEx.Ctrl))
                {
                    new TickScheduler(() => C.ProfilesL.Remove(profile));
                }
                ImGuiEx.Tooltip(Lang.HoldCTRLAndClickToDelete);

                ImGui.PopID();
            }

            ImGui.EndTable();
        }
        ImGui.PopStyleVar();
    }

    
}
