using ECommons.GameHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicBridge.Gui;
public static class GuiCharacters
{
    private static string[] Filters = ["", "", "", ""];
    public static void Draw()
    {
        ImGuiEx.SetNextItemFullWidth();
        ImGuiEx.InputWithRightButtonsArea(() =>
        {
            ImGui.InputTextWithHint($"##Filter1", "Search character name...", ref Filters[1], 100, Utils.CensorFlags);
        }, () =>
        {
            if(ImGuiEx.IconButton(FontAwesomeIcon.UserPlus))
            {
                ImGui.OpenPopup("NewChara");
            }
            ImGuiEx.Tooltip("Register new character manually");
        });

        if(ImGui.BeginPopup("NewChara"))
        {
            ImGui.SetNextItemWidth(150f);
            ImGui.InputTextWithHint("##name2", "Character Name@World", ref NewChara, 50);
            ImGui.SetNextItemWidth(150f);
            ImGui.InputTextWithHint("##cid", "Character/Content ID", ref NewCID, 50);
            if(ImGuiEx.IconButtonWithText(FontAwesomeIcon.UserPlus, "Add new character"))
            {
                if(NewChara.Length > 2 && NewChara.Split(" ").Length == 2 && NewChara.Contains('@') && ulong.TryParse(NewCID, out var cid) && cid > 0)
                {
                    if(C.SeenCharacters.ContainsKey(cid))
                    {
                        Notify.Error("This character ID is already present");
                    }
                    else if(C.SeenCharacters.Values.Select(x => x.ToLower()).Contains(NewChara.ToLower()))
                    {
                        Notify.Error("This character name is already present");
                    }
                    else
                    {
                        C.SeenCharacters[cid] = NewChara;
                        NewChara = "";
                        NewCID = "";
                        ImGui.CloseCurrentPopup();
                        Notify.Success("Character successfully added");
                    }
                }
                else
                {
                    Notify.Error("Invalid name or Character/Contenr ID");
                }
            }
            ImGui.EndPopup();
        }

        ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, Utils.CellPadding);
        if(ImGui.BeginTable($"##characters", 4, ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders | ImGuiTableFlags.SizingFixedFit))
        {
            ImGui.TableSetupColumn("Name");
            ImGui.TableSetupColumn("CID");
            ImGui.TableSetupColumn("Assigned profile", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn(" ");
            ImGui.TableHeadersRow();

            foreach(var x in C.SeenCharacters)
            {
                if(C.Blacklist.Contains(x.Key)) continue;
                if(Filters[1].Length > 0 && !x.Value.ContainsAny(StringComparison.OrdinalIgnoreCase, Filters[1])) continue;

                ImGui.PushID(x.Key.ToString());
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGuiEx.TextV(Player.CID == x.Key ? ImGuiColors.HealerGreen : null, $"{Censor.Character(x.Value)}");
                ImGui.TableNextColumn();
                if(!C.NoNames)
                {
                    ImGuiEx.TextCopy($"{x.Key}");
                }
                else
                {
                    ImGuiEx.Text("Hidden by settings");
                }
                ImGui.TableNextColumn();

                var currentProfile = C.ProfilesL.FirstOrDefault(z => z.Characters.Contains(x.Key));
                ImGuiEx.SetNextItemFullWidth();
                if(ImGui.BeginCombo($"selProfile", currentProfile?.CensoredName ?? "- No profile -", C.ComboSize))
                {
                    if(ImGui.Selectable("- No profile -"))
                    {
                        C.ProfilesL.Each(z => z.Characters.Remove(x.Key));
                    }
                    ImGui.SetNextItemWidth(350f);
                    ImGui.InputTextWithHint($"##selProfileFltr", "Filter...", ref Filters[2], 100, Utils.CensorFlags);
                    foreach(var profile in C.ProfilesL)
                    {
                        if(Filters[2].Length > 0 && !profile.Name.Contains(Filters[2], StringComparison.OrdinalIgnoreCase)) continue;
                        if(currentProfile == profile && ImGui.IsWindowAppearing()) ImGui.SetScrollHereY();
                        if(ImGui.Selectable($"{profile.CensoredName}##{profile.GUID}", currentProfile == profile))
                        {
                            if(profile.IsStaticExists() && (currentProfile == null || currentProfile.IsStaticExists()))
                            {
                                P.ForceUpdate = true;
                            }
                            profile.SetCharacter(x.Key);
                        }
                    }
                    ImGui.EndCombo();
                }

                ImGui.TableNextColumn();

                if(ImGuiEx.IconButton(FontAwesomeIcon.Ban))
                {
                    C.Blacklist.Add(x.Key);
                    C.ProfilesL.Each(z => z.Characters.Remove(x.Key));
                }
                ImGuiEx.Tooltip($"Blacklist {Censor.Character(x.Value)}. This will prevent it from showing up for profile assignments. This will also undo profile assignments for {Censor.Character(x.Value)}. ");
                ImGui.SameLine();

                if(ImGuiEx.IconButton(FontAwesomeIcon.Trash, enabled: ImGuiEx.Ctrl))
                {
                    new TickScheduler(() => C.SeenCharacters.Remove(x));
                    C.ProfilesL.Each(z => z.Characters.Remove(x.Key));
                }
                ImGuiEx.Tooltip($"Hold CTRL and click to delete information about {x.Value}. This will also undo profile assignment to the character but and as soon as you relog back onto it, {x.Value} will be registered in a plugin again.");

                ImGui.PopID();
            }

            foreach(var x in C.Blacklist)
            {
                var name = C.SeenCharacters.TryGetValue(x, out var n) ? n : $"{x:X16}";
                if(Filters[1].Length > 0 && !name.ContainsAny(StringComparison.OrdinalIgnoreCase, Filters[1])) continue;
                ImGui.PushID(x.ToString());
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGuiEx.TextV(ImGuiColors.DalamudGrey3, $"{Censor.Character(name)}");
                ImGui.TableNextColumn();

                ImGui.TableNextColumn();
                if(ImGuiEx.IconButton(FontAwesomeIcon.ArrowCircleUp))
                {
                    var item = x;
                    new TickScheduler(() => C.Blacklist.Remove(item));
                }
                ImGuiEx.Tooltip("Unblacklist this character");
                ImGui.PopID();
            }

            ImGui.EndTable();
        }
        ImGui.PopStyleVar();
    }

    private static string NewChara = "";
    private static string NewCID = "";
}
