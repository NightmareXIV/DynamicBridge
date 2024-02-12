using DynamicBridge.IPC;
using ECommons.GameHelpers;
using Lumina.Excel.GeneratedSheets;
using Newtonsoft.Json.Linq;

namespace DynamicBridge.Gui
{
    public unsafe static class UI
    {

        public static ulong SelectedCID = 0;
        public static ulong CurrentCID => SelectedCID == 0 ? Player.CID : SelectedCID;
        public const string RandomNotice = "Will be randomly selected between:\n";

        public static void DrawMain()
        {
            if (ImGui.IsWindowAppearing())
            {
                GlamourerManager.ResetCache();
                foreach (var x in Svc.Data.GetExcelSheet<Weather>()) ThreadLoadImageHandler.TryGetIconTextureWrap((uint)x.Icon, false, out _);
                foreach (var x in Svc.Data.GetExcelSheet<Emote>()) ThreadLoadImageHandler.TryGetIconTextureWrap(x.Icon, false, out _);
            }
            if (Player.Available && Utils.Profile() != null)
            {
                Utils.Profile().Name = Player.NameWithWorld;
            }
            KoFiButton.DrawRight();
            ImGuiEx.EzTabBar("Tabs", true, [
                //("Settings", Settings, null, true),
                ("Dynamic Rules", Rules.Draw, null, true),
                ("Presets", GuiPresets.DrawUser, null, true),
                ("Global Presets", GuiPresets.DrawGlobal, null, true),
                ("Layered Designs", ComplexGlamourer.Draw, null, true),
                ("House Registration", HouseReg.Draw, null, true),
                ("Settings", GuiSettings.Draw, null, true),
                InternalLog.ImGuiTab(),
                (C.Debug?"Debug":null, Debug.Draw, ImGuiColors.DalamudGrey3, true),
                ]);
        }

        public static void ProfileSelectorCommon()
        {
            if (C.Blacklist.Contains(Player.CID))
            {
                if (ImGui.Button("Unblacklist current character"))
                {
                    C.Blacklist.Remove(Player.CID);
                }
            }
            ImGuiEx.SetNextItemFullWidth();
            if (ImGui.BeginCombo($"##selectProfile", $"{Utils.Profile(UI.CurrentCID)?.Name ?? "Select profile..."}"))
            {
                foreach (var x in C.Profiles)
                {
                    if (C.Blacklist.Contains(x.Key)) continue;
                    ImGui.PushID(x.Value.GUID);
                    if (x.Key == Player.CID) ImGui.PushStyleColor(ImGuiCol.Text, EColor.Green);
                    if (ImGui.Selectable(x.Value.Name, x.Key == CurrentCID))
                    {
                        UI.SelectedCID = Player.CID == x.Key ? 0 : x.Key;
                    }
                    if (x.Key == Player.CID) ImGui.PopStyleColor();
                        if (x.Key == UI.CurrentCID && ImGui.IsWindowAppearing()) ImGui.SetScrollHereY();
                    ImGui.PopID();
                }
                ImGui.EndCombo();
            }
        }
    }
}
