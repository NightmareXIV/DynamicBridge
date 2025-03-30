using Dalamud.Interface.Components;
using FFXIVClientStructs.FFXIV.Client.Game;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicBridge.Gui
{
    public static unsafe class HouseReg
    {
        public static void Draw()
        {
            ImGuiEx.TextWrapped($"Here you can register a house. After registration, you will be able to select it as a condition in Dynamic Rules tab.");
            var CurrentHouse = HousingManager.Instance()->GetCurrentIndoorHouseId();
            if(CurrentHouse > 0)
            {
                ImGuiEx.Text($"Current house: {Censor.Hide($"{CurrentHouse:X16}")}");
                if(!C.Houses.TryGetFirst(x => x.ID == CurrentHouse, out var record))
                {
                    if(ImGuiComponents.IconButtonWithText(FontAwesomeIcon.Home, "Register this house"))
                    {
                        C.Houses.Add(new() { ID = CurrentHouse, Name = Utils.GetHouseDefaultName() });
                    }
                }
            }
            else
            {
                ImGuiEx.Text($"You are not in house");
            }
            if(ImGui.BeginTable("##houses", 3, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.Borders))
            {
                ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("ID");
                ImGui.TableSetupColumn(" ");
                ImGui.TableHeadersRow();
                foreach(var x in C.Houses)
                {
                    ImGui.PushID(x.GUID);
                    var col = x.ID == CurrentHouse;
                    if(col) ImGui.PushStyleColor(ImGuiCol.Text, EColor.GreenBright);

                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();

                    ImGuiEx.SetNextItemFullWidth();
                    ImGui.InputText("##name", ref x.Name, 100, Utils.CensorFlags);

                    ImGui.TableNextColumn();
                    ImGuiEx.Text($"{Censor.Hide($"{x.ID:X16}")}");

                    ImGui.TableNextColumn();
                    if(ImGuiEx.IconButton(FontAwesomeIcon.Trash, enabled: ImGuiEx.Ctrl))
                    {
                        new TickScheduler(() => C.Houses.RemoveAll(z => z.GUID == x.GUID));
                    }
                    ImGuiEx.Tooltip($"Hold CTRL+Click to delete");

                    if(col) ImGui.PopStyleColor();
                    ImGui.PopID();
                }
                ImGui.EndTable();
            }
        }
    }
}
