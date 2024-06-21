using DynamicBridge.Configuration;
using DynamicBridge.IPC.Glamourer;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DynamicBridge.Gui;
public static class GuiSettings
{
    public static Dictionary<GlamourerNoRuleBehavior, string> GlamourerNoRuleBehaviorNames = new()
    {
        [GlamourerNoRuleBehavior.RevertToNormal] = Lang.GuiSettings_RevertToGameState,
        [GlamourerNoRuleBehavior.RevertToAutomation] = Lang.GuiSettings_RevertToGlamourerSAutomation,
        [GlamourerNoRuleBehavior.StoreRestore] = Lang.GuiSettings_BetaRestoreAppearanceAsItWasBeforeApplyingRule,
    };

    public static Dictionary<ImGuiComboFlags, string> ComboFlagNames = new()
    {
        [ImGuiComboFlags.HeightSmall] = Lang.GuiSettings_Small,
        [ImGuiComboFlags.HeightRegular] = Lang.GuiSettings_Standard,
        [ImGuiComboFlags.HeightLarge] = Lang.GuiSettings_Large,
        [ImGuiComboFlags.HeightLargest] = Lang.GuiSettings_MaximumPossible,
    };

    public static void Draw()
    {
        ImGui.Checkbox(Lang.Draw_EnablePlugin, ref C.Enable);
        if (ImGuiGroup.BeginGroupBox(Lang.Draw_General))
        {
            ImGuiEx.CheckboxInverted(Lang.Draw_HideTutorial, ref C.ShowTutorial);
            ImGui.Checkbox(Lang.Draw_AllowApplyingNegativeConditions, ref C.AllowNegativeConditions);
            ImGuiEx.HelpMarker(Lang.DenyCondHelp);
            ImGui.Checkbox(Lang.FullPath, ref C.GlamourerFullPath);
            ImGuiEx.SetNextItemWidthScaled(150f);
            ImGuiEx.EnumCombo(Lang.Draw_DropdownMenuSize, ref C.ComboSize, ComboFlagNames.ContainsKey, ComboFlagNames);
            if(ImGui.Checkbox(Lang.ForceUpdateSetting, ref C.UpdateJobGSChange))
            {
                if (C.UpdateJobGSChange)
                {
                    P.Memory.EquipGearsetHook.Enable();
                }
                else
                {
                    P.Memory.EquipGearsetHook.Disable();
                }
            }
            /*ImGui.Checkbox($"Force update appearance on manual gear changes", ref C.UpdateGearChange);
            ImGuiEx.HelpMarker("This option impacts performance", EColor.OrangeBright, FontAwesomeIcon.ExclamationTriangle.ToIconString());*/
            ImGui.Separator();
            ImGui.Checkbox(Lang.IncognitoEnable, ref C.NoNames);
            ImGuiEx.HelpMarker(Lang.IncognitoHelp);
            ImGuiEx.HelpMarker(Lang.IncognitoWarning, ImGuiColors.DalamudOrange);
            ImGuiEx.Spacing();
            ImGui.Checkbox(Lang.IncognitoReplacement, ref C.LesserCensor);
            ImGuiEx.Spacing();
            if (ImGui.Button(Lang.IncognitoCensor))
            {
                C.CensorSeed = Guid.NewGuid().ToString();
            }
            ImGuiEx.HelpMarker(Lang.CensorHelp);

            ImGuiEx.CheckboxInverted(Lang.SplitBaseClassesAndJobs, ref C.UnifyJobs);
            
            ImGui.Checkbox(Lang.AutofillEmptyNames, ref C.AutofillFromGlam);
            ImGuiGroup.EndGroupBox();
        }

        if(ImGuiGroup.BeginGroupBox(Lang.ConfigureRuleConditions))
        {
            ImGuiEx.TextWrapped(Lang.ExtraHelp);
            ImGuiEx.EzTableColumns("extras", [
                () => ImGui.Checkbox(Lang.RuleState, ref C.Cond_State),
                () => ImGui.Checkbox(Lang.RuleBiome, ref C.Cond_Biome),
                () => ImGui.Checkbox(Lang.RuleWeather, ref C.Cond_Weather),
                () => ImGui.Checkbox(Lang.RuleTime, ref C.Cond_Time),
                () => ImGui.Checkbox(Lang.ZoneGroup, ref C.Cond_ZoneGroup),
                () => ImGui.Checkbox(Lang.RuleZone, ref C.Cond_Zone),
                () => ImGui.Checkbox(Lang.RuleHouse, ref C.Cond_House),
                () => ImGui.Checkbox(Lang.RuleEmote, ref C.Cond_Emote),
                () => ImGui.Checkbox(Lang.RuleJob, ref C.Cond_Job),
                () => ImGui.Checkbox(Lang.RuleWorld, ref C.Cond_World),
                () => ImGui.Checkbox(Lang.RuleGearset, ref C.Cond_Gearset),
            ],
                (int)(ImGui.GetContentRegionAvail().X / 180f), ImGuiTableFlags.BordersInner);
            ImGuiGroup.EndGroupBox();
        }

        if (ImGuiGroup.BeginGroupBox(Lang.Integrations))
        {
            ImGuiEx.Text(Lang.IntegrationsHelp);
            //glam

            ImGui.Checkbox("Glamourer", ref C.EnableGlamourer);
            DrawPluginCheck("Glamourer", "1.2.2.2");
            ImGuiEx.Spacing();
            ImGuiEx.TextV(Lang.DynamicBridgeBehaviorWhenNoGlamourerRuleIsFound);
            ImGui.SameLine();
            ImGuiEx.Spacing();
            ImGuiEx.SetNextItemWidthScaled(200f);
            ImGuiEx.EnumCombo("##dbglamdef", ref C.GlamNoRuleBehaviour, GlamourerNoRuleBehaviorNames);
            if (C.ManageGlamourerAutomation)
            {
                if (C.GlamNoRuleBehaviour != GlamourerNoRuleBehavior.RevertToAutomation)
                {
                    ImGuiEx.HelpMarker(Lang.RevertTooltip, ImGuiColors.DalamudRed, FontAwesomeIcon.ExclamationTriangle.ToIconString());
                }
            }
            ImGuiEx.Spacing();
            ImGui.Checkbox(Lang.ReflectAutomation, ref C.ManageGlamourerAutomation);
            ImGuiEx.HelpMarker(Lang.ReflectAutomationHelp);
            if (P.GlamourerManager.Reflector.GetAutomationGlobalState() && P.GlamourerManager.Reflector.GetAutomationStatusForChara())
            {
                if (!C.ManageGlamourerAutomation)
                {
                    ImGuiEx.HelpMarker(Lang.AutomationWarning, ImGuiColors.DalamudRed, FontAwesomeIcon.ExclamationTriangle.ToIconString());
                }
            }
            ImGuiEx.Spacing();
            ImGui.Checkbox(Lang.RevertCharacterBeforeRestoringAutomation, ref C.RevertBeforeAutomationRestore);
            ImGuiEx.Spacing();
            ImGui.Checkbox(Lang.RevertCharacterBeforeApplyingRule, ref C.RevertGlamourerBeforeApply);


            ImGui.Separator();

            //customize

            ImGui.Checkbox("Customize+", ref C.EnableCustomize);
            DrawPluginCheck("CustomizePlus", "2.0.2.3");

            //honorific

            ImGui.Checkbox("Honorific", ref C.EnableHonorific);
            DrawPluginCheck("Honorific", "1.4.2.0");
            ImGuiEx.Spacing();
            ImGui.Checkbox(Lang.AllowSelectingTitlesRegisteredForOtherCharacters, ref C.HonotificUnfiltered);

            //penumbra
            ImGui.Checkbox("Penumbra", ref C.EnablePenumbra);
            DrawPluginCheck("Penumbra", "1.0.1.0");

            //moodles
            ImGui.Checkbox("Moodles", ref C.EnableMoodles);
            DrawPluginCheck("Moodles", "1.0.0.15");

            ImGuiGroup.EndGroupBox();
        }

        if (ImGuiGroup.BeginGroupBox(Lang.About))
        {
            GuiAbout.Draw();
            ImGuiGroup.EndGroupBox();
        }
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
            ImGuiEx.Text(Lang.NotInstalled);
        }
        else
        {
            if(plugin.Version < Version.Parse(minVersion))
            {
                ImGui.PushFont(UiBuilder.IconFont);
                ImGuiEx.Text(EColor.RedBright, "\uf00d");
                ImGui.PopFont();
                ImGui.SameLine();
                ImGuiEx.Text(Lang.UnsupportedVersion);
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
