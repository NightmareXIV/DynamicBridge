using Dalamud.Interface.Components;
using FFXIVClientStructs.FFXIV.Client.Game;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicBridge.Gui
{
    public unsafe static class HouseReg
    {
        public static void Draw()
        {
            ImGuiEx.TextWrapped(Lang.HouseRegHelp);
            var CurrentHouse = HousingManager.Instance()->GetCurrentHouseId();
            if (CurrentHouse > 0)
            {
                ImGuiEx.Text(Lang.HouseReg_Draw_CurrentHouse.Params(Censor.Hide($"{CurrentHouse:X16}")));
                if (!C.Houses.TryGetFirst(x => x.ID == CurrentHouse, out var record))
                {
                    if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.Home, Lang.HouseReg_Draw_RegisterThisHouse))
                    {
                        C.Houses.Add(new() { ID = CurrentHouse, Name = Utils.GetHouseDefaultName() });
                    }
                }
            }
            else
            {
                ImGuiEx.Text(Lang.HouseReg_Draw_YouAreNotInHouse);
            }
            if (ImGui.BeginTable("##houses", 3, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.Borders))
            {
                ImGui.TableSetupColumn(Lang.NameColumn, ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn(Lang.IDColumn);
                ImGui.TableSetupColumn(" ");
                ImGui.TableHeadersRow();
                foreach (var x in C.Houses)
                {
                    ImGui.PushID(x.GUID);
                    var col = x.ID == CurrentHouse;
                    if (col) ImGui.PushStyleColor(ImGuiCol.Text, EColor.GreenBright);

                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();

                    ImGuiEx.SetNextItemFullWidth();
                    ImGui.InputText("##name", ref x.Name, 100, Utils.CensorFlags);

                    ImGui.TableNextColumn();
                    ImGuiEx.Text($"{Censor.Hide($"{x.ID:X16}")}");

                    ImGui.TableNextColumn();
                    if (ImGuiEx.IconButton(FontAwesomeIcon.Trash, enabled:ImGuiEx.Ctrl))
                    {
                        new TickScheduler(() => C.Houses.RemoveAll(z => z.GUID == x.GUID));
                    }
                    ImGuiEx.Tooltip(Lang.HoldCTRLClickToDelete);

                    if (col) ImGui.PopStyleColor();
                    ImGui.PopID();
                }
                ImGui.EndTable();
            }
        }
    }
}
