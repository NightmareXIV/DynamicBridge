using DynamicBridge.Configuration;
using DynamicBridge.IPC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicBridge.Gui;
public static class GuiSettings
{
    public static Dictionary<GlamourerNoRuleBehavior, string> GlamourerNoRuleBehaviorNames = new()
    {
        [GlamourerNoRuleBehavior.RevertToNormal] = "Revert to game state",
        [GlamourerNoRuleBehavior.RevertToAutomation] = "Revert to Glamourer's automation",
        [GlamourerNoRuleBehavior.StoreRestore] = "[Beta] Restore appearance as it was before applying rule",
    };

    public static void Draw()
    {
        ImGui.Checkbox($"Allow applying negative conditions", ref C.AllowNegativeConditions);
        ImGuiEx.HelpMarker("If you will enable this option, you will be able to mark any condition with dot marker. If any condition marked with dot within the rule is matching, that entire rule is ignored.");
        ImGui.Separator();
        ImGuiEx.Text($"Here you can individually enable/disable plugin integrations and configure appropriate related settings.");
        ImGui.Checkbox("Glamourer", ref C.EnableGlamourer);
        DrawPluginCheck("Glamourer", "1.1.0.4");
        ImGuiEx.TextV($"DynamicBridge behavior when no Glamourer rule is found:");
        ImGui.SameLine();
        ImGuiEx.SetNextItemWidthScaled(200f);
        ImGuiEx.EnumCombo("##dbglamdef", ref C.GlamNoRuleBehaviour, GlamourerNoRuleBehaviorNames);
        if (C.ManageGlamourerAutomation)
        {
            if(C.GlamNoRuleBehaviour != GlamourerNoRuleBehavior.RevertToAutomation)
            {
                ImGuiEx.HelpMarker("Revert to Automation is recommended if you are using Glamourer automation.", ImGuiColors.DalamudRed, FontAwesomeIcon.ExclamationTriangle.ToIconString());
            }
        }
        ImGui.Checkbox("Allow DynamicBridge to manage Glamourer's automation setting", ref C.ManageGlamourerAutomation);
        ImGuiEx.HelpMarker("If this setting is enabled, Glamourer's global automation setting will be automatically disabled upon applying any rule and will be automatically enabled when no rules are found.");
        if (GlamourerReflector.GetAutomationGlobalState())
        {
            if (!C.ManageGlamourerAutomation)
            {
                ImGuiEx.HelpMarker("You MUST enable this setting or disable Glamourer's automation, otherwise either Glamourer's or DynamicBridge's automation will not work correctly.", ImGuiColors.DalamudRed, FontAwesomeIcon.ExclamationTriangle.ToIconString());
            }
        }
        ImGui.Checkbox("Display full path in design selection dropdown", ref C.GlamourerFullPath);
        ImGui.Separator();
        ImGui.Checkbox("Customize+", ref C.EnableCustomize);
        DrawPluginCheck("CustomizePlus", "2.0.0.8");
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
