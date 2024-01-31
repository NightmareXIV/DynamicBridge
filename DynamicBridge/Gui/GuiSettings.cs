using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicBridge.Gui;
public static class GuiSettings
{
    public static void Draw()
    {
        ImGuiEx.Text($"Plugin integrations:");
        ImGui.Checkbox("Glamourer", ref C.EnableGlamourer);
        DrawPluginCheck("Glamourer", "1.1.0.4");
        ImGui.Checkbox($"Revert character before applying new rule", ref C.GlamourerResetBeforeApply);
        ImGui.Separator();
        ImGui.Checkbox("Customize+", ref C.EnableCustomize);
        DrawPluginCheck("CustomizePlus", "2.0.0.8");
        ImGui.Separator();
        ImGui.Checkbox("Palette+", ref C.EnablePalette);
        DrawPluginCheck("PalettePlus", "0.0.3.14");
        ImGui.Separator();
        ImGui.Checkbox("Honorific", ref C.EnableHonorific);
        DrawPluginCheck("Honorific", "1.4.2.0");
        ImGui.Separator();
        GuiAbout.Draw();
    }

    static void DrawPluginCheck(string name, string minVersion = "0.0.0.0")
    {
        ImGui.SameLine();
        var plugin = Svc.PluginInterface.InstalledPlugins.FirstOrDefault(x => x.InternalName == name && x.IsLoaded);
        if(plugin == null)
        {
            ImGui.PushFont(UiBuilder.IconFont);
            ImGuiEx.Text(EColor.RedBright, "\uf00d");
            ImGui.PopFont();
            ImGui.SameLine();
            ImGuiEx.Text($"not installed");
        }
        else
        {
            if(plugin.Version < Version.Parse(minVersion))
            {
                ImGui.PushFont(UiBuilder.IconFont);
                ImGuiEx.Text(EColor.RedBright, "\uf00d");
                ImGui.PopFont();
                ImGui.SameLine();
                ImGuiEx.Text($"unsupported version");
            }
            else
            {
                ImGui.PushFont(UiBuilder.IconFont);
                ImGuiEx.Text(EColor.GreenBright, FontAwesomeIcon.Check.ToIconString());
                ImGui.PopFont();
            }
        }
    }
}
