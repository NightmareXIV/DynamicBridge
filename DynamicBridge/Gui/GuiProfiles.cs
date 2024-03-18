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
        ImGuiEx.InputWithRightButtonsArea("DrawProfilesInp", () => ImGui.InputTextWithHint($"##Filter0", "Search profile name...", ref Filters[0], 100), () =>
        {
            if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.PlusCircle, "Create Empty"))
            {
                var profile = new Profile();
                C.ProfilesL.Add(profile);
                profile.Name = $"New Profile {C.ProfilesL.Count}";
            }
            ImGui.SameLine();
            ImGuiEx.Tooltip($"Create new empty profile");
            if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.Paste, "Paste from Clipboard"))
            {
                try
                {
                    var x = EzConfig.DefaultSerializationFactory.Deserialize<Profile>(Paste());
                    var newName = x.Name + $" (copy)";
                    if(C.ProfilesL.Any(z => z.Name == newName))
                    {
                        int i = 2;
                        do
                        {
                            newName = x.Name + $" (copy {i++})";
                        }
                        while (C.ProfilesL.Any(z => z.Name == newName));
                    }
                    x.Name = newName;
                    x.Characters.Clear();
                    C.ProfilesL.Add(x);
                }
                catch(Exception e)
                {
                    Notify.Error(e.Message);
                }
            }
            ImGuiEx.Tooltip($"Create new profile from data in clipboard");
            ImGui.SameLine();
        });
        ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, Utils.CellPadding);
        if (ImGui.BeginTable($"##profiles", 4, ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders | ImGuiTableFlags.SizingFixedFit))
        {
            //ImGui.TableSetupColumn("  ", ImGuiTableColumnFlags.NoResize | ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoSort);
            ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("Used by", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("Folder whitelist", ImGuiTableColumnFlags.WidthStretch);
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
                var text = profile.Characters.Take(2).Select(s => Censor.Character(Utils.GetCharaNameFromCID(s))).Print() + (profile.Characters.Count > 2?$" and {profile.Characters.Count-2} more":"");
                ImGuiEx.SetNextItemFullWidth();
                ImGuiEx.Text($"{text}");
                if(profile.Characters.Count > 2)
                {
                    ImGuiEx.Tooltip($"...\n{profile.Characters.Skip(2).Select(s => Censor.Character(Utils.GetCharaNameFromCID(s))).Print("\n")}");
                }

                ImGui.TableNextColumn();

                ImGuiEx.SetNextItemFullWidth();
                if(ImGui.BeginCombo($"##FoldersWhitelist", profile.Pathes.PrintRange(out _), C.ComboSize))
                {
                    if(profile.Pathes.Count > 1) ImGuiEx.Tooltip(profile.Pathes.Join("\n"));
                    if (ImGui.IsWindowAppearing()) Utils.ResetCaches();
                    var pathes = Utils.GetCombinedPathes();
                    foreach (var x in pathes)
                    {
                        for (int q = 0; q < x.Indentation; q++)
                        {
                            ImGuiEx.Spacing();
                        }
                        ImGuiEx.CollectionCheckbox($"{x.Name}", x.Name, profile.Pathes);
                        if (x.Glamourer)
                        {
                            ImGui.SameLine();
                            ImGuiEx.Text(ImGuiColors.DalamudGrey3, UiBuilder.IconFont, "\uf630");
                            ImGuiEx.Tooltip("This folder is present in Glamourer");
                        }
                        if (x.Customize)
                        {
                            ImGui.SameLine();
                            ImGuiEx.Text(ImGuiColors.DalamudGrey3, UiBuilder.IconFont, "\ue541");
                            ImGuiEx.Tooltip("This folder is present in Customize+");
                        }
                    }
                    foreach(var x in profile.Pathes)
                    {
                        if (pathes.Any(z => z.Name == x)) continue;
                        ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.DalamudRed);
                        ImGuiEx.CollectionCheckbox($"{x}", x, profile.Pathes);
                        ImGui.PopStyleColor();
                    }
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
                ImGuiEx.Tooltip("Select this profile for editing");
                ImGui.SameLine();
                if (ImGuiEx.IconButton(FontAwesomeIcon.Copy.ToIconString()))
                {
                    Copy(JsonConvert.SerializeObject(profile));
                }
                ImGuiEx.Tooltip("Copy this profile to clipboard");
                ImGui.SameLine();
                if (ImGuiEx.IconButton(FontAwesomeIcon.Trash.ToIconString(), enabled:ImGuiEx.Ctrl))
                {
                    new TickScheduler(() => C.ProfilesL.Remove(profile));
                }
                ImGuiEx.Tooltip("Hold CTRL and click to delete");

                ImGui.PopID();
            }

            ImGui.EndTable();
        }
        ImGui.PopStyleVar();
    }

    
}
