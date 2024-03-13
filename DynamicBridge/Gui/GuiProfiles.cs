using Dalamud.Interface.Components;
using DynamicBridge.Configuration;
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

    public static void DrawProfiles()
    {
        ImGuiEx.InputWithRightButtonsArea("DrawProfilesInp", () => ImGui.InputTextWithHint($"##Filter0", "Search profile name...", ref Filters[0], 100), () =>
        {
            if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.PlusCircle, "Create Empty"))
            {
                var profile = new Profile();
                C.ProfilesL.Add(profile);
                profile.Name = $"New Profile {C.ProfilesL.Count}";
            }
            ImGuiEx.Tooltip($"Create new empty profile");
        });

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
                ImGui.InputText($"##profilename", ref profile.Name, 100);

                ImGui.TableNextColumn();
                var text = profile.Characters.Take(2).Select(Utils.GetCharaNameFromCID).Print() + (profile.Characters.Count > 2?$" and {profile.Characters.Count-2} more":"");
                ImGuiEx.SetNextItemFullWidth();
                ImGuiEx.Text($"{text}");
                if(profile.Characters.Count > 2)
                {
                    ImGuiEx.Tooltip($"...\n{profile.Characters.Skip(2).Select(Utils.GetCharaNameFromCID).Print("\n")}");
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
                    if (profile.Pathes.Count > 1) ImGuiEx.Tooltip(profile.Pathes.Join("\n"));
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
                if (ImGuiEx.IconButton(FontAwesomeIcon.Trash.ToIconString()))
                {
                    if(ImGuiEx.Ctrl) new TickScheduler(() => C.ProfilesL.Remove(profile));
                }
                ImGuiEx.Tooltip("Hold CTRL and click to delete");

                ImGui.PopID();
            }

            ImGui.EndTable();
        }
    }

    public static void DrawCharacters()
    {
        ImGuiEx.SetNextItemFullWidth();
        ImGui.InputTextWithHint($"##Filter1", "Search character name...", ref Filters[1], 100);

        if (ImGui.BeginTable($"##characters", 3, ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders | ImGuiTableFlags.SizingFixedSame))
        {
            ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("Assigned profile", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn(" ", ImGuiTableColumnFlags.NoResize | ImGuiTableColumnFlags.WidthFixed);
            ImGui.TableHeadersRow();

            foreach (var x in C.SeenCharacters)
            {
                if (C.Blacklist.Contains(x.Key)) continue;
                if (Filters[1].Length > 0 && !x.Value.ContainsAny(StringComparison.OrdinalIgnoreCase, Filters[1])) continue;

                ImGui.PushID(x.Key.ToString());
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGuiEx.TextV(Player.CID == x.Key ? ImGuiColors.HealerGreen : null, $"{x.Value}");
                ImGui.TableNextColumn();

                var currentProfile = C.ProfilesL.FirstOrDefault(z => z.Characters.Contains(x.Key));
                ImGuiEx.SetNextItemFullWidth();
                if (ImGui.BeginCombo($"selProfile", currentProfile?.Name ?? "- No profile -", C.ComboSize))
                {
                    if (ImGui.Selectable("- No profile -"))
                    {
                        C.ProfilesL.Each(z => z.Characters.Remove(x.Key));
                    }
                    ImGui.SetNextItemWidth(350f);
                    ImGui.InputTextWithHint($"##selProfileFltr", "Filter...", ref Filters[2], 100);
                    foreach (var profile in C.ProfilesL)
                    {
                        if (Filters[2].Length > 0 && !profile.Name.Contains(Filters[2], StringComparison.OrdinalIgnoreCase)) continue;
                        if (currentProfile == profile && ImGui.IsWindowAppearing()) ImGui.SetScrollHereY();
                        if (ImGui.Selectable($"{profile.Name}##{profile.GUID}", currentProfile == profile))
                        {
                            profile.SetCharacter(x.Key);
                        }
                    }
                    ImGui.EndCombo();
                }

                ImGui.TableNextColumn();

                if (ImGuiEx.IconButton(FontAwesomeIcon.Ban))
                {
                    C.Blacklist.Add(x.Key);
                    C.ProfilesL.Each(z => z.Characters.Remove(x.Key));
                }
                ImGuiEx.Tooltip($"Blacklist {x.Value}. This will prevent it from showing up for profile assignments. This will also undo profile assignments for {x.Value}. ");
                ImGui.SameLine();

                if (ImGuiEx.IconButton(FontAwesomeIcon.Trash))
                {
                    if (ImGuiEx.Ctrl)
                    {
                        new TickScheduler(() => C.SeenCharacters.Remove(x));
                        C.ProfilesL.Each(z => z.Characters.Remove(x.Key));
                    }
                }
                ImGuiEx.Tooltip($"Hold CTRL and click to delete information about {x.Value}. This will also undo profile assignment to the character but and as soon as you relog back onto it, {x.Value} will be registered in a plugin again.");

                ImGui.PopID();
            }

            foreach (var x in C.Blacklist)
            {
                var name = C.SeenCharacters.TryGetValue(x, out var n) ? n : $"{x:X16}";
                if (Filters[1].Length > 0 && !name.ContainsAny(StringComparison.OrdinalIgnoreCase, Filters[1])) continue;
                ImGui.PushID(x.ToString());
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGuiEx.TextV(ImGuiColors.DalamudGrey3, $"{name}");
                ImGui.TableNextColumn();

                ImGui.TableNextColumn();
                if (ImGuiEx.IconButton(FontAwesomeIcon.ArrowCircleUp))
                {
                    var item = x;
                    new TickScheduler(() => C.Blacklist.Remove(item));
                }
                ImGuiEx.Tooltip("Unblacklist this character");
                ImGui.PopID();
            }

            ImGui.EndTable();
        }
    }
}
