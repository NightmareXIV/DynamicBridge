using DynamicBridge.Configuration;
using DynamicBridge.Core;
using DynamicBridge.IPC.Glamourer;
using Lumina.Excel.Sheets;
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
        [GlamourerNoRuleBehavior.RevertToNormal] = "Revert to game state",
        [GlamourerNoRuleBehavior.RevertToAutomation] = "Revert to Glamourer's automation",
        [GlamourerNoRuleBehavior.StoreRestore] = "[Beta] Restore appearance as it was before applying rule",
    };

    public static Dictionary<ImGuiComboFlags, string> ComboFlagNames = new()
    {
        [ImGuiComboFlags.HeightSmall] = "Small",
        [ImGuiComboFlags.HeightRegular] = "Standard",
        [ImGuiComboFlags.HeightLarge] = "Large",
        [ImGuiComboFlags.HeightLargest] = "Maximum possible",
    };

    public static void Draw()
    {
        ImGui.Checkbox($"Enable Plugin", ref C.Enable);
        if(ImGuiGroup.BeginGroupBox("General"))
        {
            ImGuiEx.CheckboxInverted("Hide tutorial", ref C.ShowTutorial);
            ImGui.Checkbox($"Allow applying negative conditions", ref C.AllowNegativeConditions);
            ImGuiEx.HelpMarker("If you will enable this option, you will be able to mark any condition with cross marker. If any condition marked with cross within the rule is matching, that entire rule is ignored.");
            ImGui.Checkbox("Display full path in profile editor, where available", ref C.GlamourerFullPath);
            ImGuiEx.SetNextItemWidthScaled(150f);
            ImGui.Checkbox("Reapply rules and presets on change in Glamourer dropdowns", ref C.AutoApplyOnChange);
            ImGuiEx.EnumCombo("Dropdown menu size", ref C.ComboSize, ComboFlagNames.ContainsKey, ComboFlagNames);
            if(ImGui.Checkbox($"Force update appearance on job and gearset changes", ref C.UpdateJobGSChange))
            {
                if(C.UpdateJobGSChange)
                {
                    P.Memory.EquipGearsetHook.Enable();
                }
                else
                {
                    P.Memory.EquipGearsetHook.Disable();
                }
            }

            ImGuiEx.HelpMarker("Please ensure \"Revert Manual Changes on Zone Change\" is unchecked in Glamourer Behavior Settings");
            ImGui.Checkbox($"Attempt to preserve rules", ref C.Sticky);
            if (C.Sticky)
            {
                ImGuiEx.Spacing();
                ImGuiEx.SetNextItemWidthScaled(200f);
                ImGuiEx.EnumCombo($"Randomize on Login", ref C.RandomChoosenType);
                if (C.RandomChoosenType == RandomTypes.Timer) {
                    ImGui.SameLine();
                    ImGui.Text("|");
                    ImGui.SameLine();
                    ImGui.Text("How often should it randomize everything in minutes:");
                    ImGui.SameLine();
                    ImGuiEx.SetNextItemWidthScaled(200f);
                    if (ImGui.InputDouble("", ref C.UserInputRandomizerTime))
                    {
                        double ReloadSpeed = 1;
                        if (!C.ForceUpdateOnRandomize) 
                        {
                            ReloadSpeed = 0.1;
                        }
                        C.UserInputRandomizerTime = Math.Max(ReloadSpeed, C.UserInputRandomizerTime);
                    };
                    ImGuiEx.Spacing();ImGuiEx.Spacing();
                    ImGui.Checkbox("Force update on randomize", ref C.ForceUpdateOnRandomize);
                }
                ImGuiEx.Spacing();
                ImGui.Checkbox($"Attempt to preserve presets", ref C.StickyPresets);
                ImGuiEx.Spacing();
                ImGui.Checkbox($"Attempt to preserve glamourer", ref C.StickyGlamourer);
                ImGuiEx.Spacing();
                ImGui.Checkbox($"Attempt to preserve customize", ref C.StickyCustomize);
                ImGuiEx.Spacing();
                ImGui.Checkbox($"Attempt to preserve honorific   ", ref C.StickyHonorific); //Cheaty spaces to make it all line up
                ImGuiEx.Spacing();
                ImGui.Checkbox($"Attempt to preserve penumbra", ref C.StickyPenumbra);
            }

            ImGui.Checkbox($"Don't force update on territory change if applied rules don't change", ref C.DontChangeOnTerritoryChange); // Concise and clear wording?
            ImGuiEx.HelpMarker("Please ensure \"Revert Manual Changes on Zone Change\" is unchecked in Glamourer Behavior Settings");

            /*ImGui.Checkbox($"Force update appearance on manual gear changes", ref C.UpdateGearChange);
            ImGuiEx.HelpMarker("This option impacts performance", EColor.OrangeBright, FontAwesomeIcon.ExclamationTriangle.ToIconString());*/
            ImGui.Separator();
            ImGui.Checkbox($"[Beta] Enable Incognito Mode (WORK IN PROGRESS, ONLY HIDES IN SOME PLACES YET)", ref C.NoNames);
            ImGuiEx.HelpMarker($"Replaces your character name with random animal name and your world name with random fantasy world name. Same name will always generate same counterparts, for you but not for other people. Additionally, hides text in input fields and shows temporary profile/preset ID instead of name.");
            ImGuiEx.HelpMarker($"Warning! No names will be censored in Log and Debug tabs and Dalamud.log! \nWarning! If you share configuration file AND censored name, original name CAN BE RESTORED. If you need to send configuration file and ensure that you remain anonymous, click Regenerate Censor Seed button, send configuration and click the button again.", ImGuiColors.DalamudOrange);
            ImGuiEx.Spacing();
            ImGui.Checkbox($"Use only replacement words with same first letter as original when possible.", ref C.LesserCensor);
            ImGuiEx.Spacing();
            if(ImGui.Button("Regenerate Censor Seed"))
            {
                C.CensorSeed = Guid.NewGuid().ToString();
            }
            ImGuiEx.HelpMarker($"Censored names will change upon pressing this button.");

            ImGuiEx.CheckboxInverted($"Split base classes and jobs", ref C.UnifyJobs);

            ImGui.Checkbox("Autofill empty preset name with first selected plugin's option name upon selecting it", ref C.AutofillFromGlam);
            ImGuiGroup.EndGroupBox();
        }

        if(ImGuiGroup.BeginGroupBox("Configure rule conditions"))
        {
            ImGuiEx.TextWrapped($"Enable extra conditions or disable unused for convenience and performance boost.");
            ImGuiEx.EzTableColumns("extras", [
                () => ImGui.Checkbox($"State", ref C.Cond_State),
                () => ImGui.Checkbox($"Biome", ref C.Cond_Biome),
                () => ImGui.Checkbox($"Weather", ref C.Cond_Weather),
                () => ImGui.Checkbox($"Time", ref C.Cond_Time),
                () => ImGui.Checkbox($"Zone group", ref C.Cond_ZoneGroup),
                () => ImGui.Checkbox($"Zone", ref C.Cond_Zone),
                () => ImGui.Checkbox($"House", ref C.Cond_House),
                () => ImGui.Checkbox($"Emote", ref C.Cond_Emote),
                () => ImGui.Checkbox($"Job", ref C.Cond_Job),
                () => ImGui.Checkbox($"World", ref C.Cond_World),
                () => ImGui.Checkbox($"Gearset", ref C.Cond_Gearset),
                () => ImGui.Checkbox($"Current Race", ref C.Cond_Race_Bonus),
                () => ImGui.Checkbox($"Nearby Players", ref C.Cond_Players),
            ],
                (int)(ImGui.GetContentRegionAvail().X / 180f), ImGuiTableFlags.BordersInner);
            if (C.Cond_Time)
            {
                ImGui.Separator();
                ImGui.Checkbox("Enable Precise Time", ref C.Cond_Time_Precise);
                if (C.Cond_Time_Precise)
                {
                    ImGui.SameLine();
                    if(ImGui.Button("Convert all Simple Times to Precise Times") && ImGui.GetIO().KeyCtrl)
                    {
                        ConvertTimeRules();
                    }
                    ImGuiEx.Tooltip("Hold CTRL+Click, deletes all current Precise Times");
                }
            }
            else 
            {
                C.Cond_Time_Precise = false;
            }
            ImGuiGroup.EndGroupBox();
        }
        bool Cond_Race_Bonus_Window = C.Cond_Race_Bonus && !C.Cond_Race;
        if (Cond_Race_Bonus_Window)
        {
            // Measure the longest text line
            Vector2 textSize1 = ImGui.CalcTextSize("ARE YOU SURE YOU WANT TO ENABLE THE RACE CONDITION FOR RULES?");
            Vector2 textSize2 = ImGui.CalcTextSize("It is VERY easy to be stuck in infinite loops with this condition");
            Vector2 textSize3 = ImGui.CalcTextSize("By selecting YES, I yeild my right to ask for help if I can't fix the loop");
            float width = Math.Max(textSize1.X, Math.Max(textSize2.X, textSize3.X)) + 40;
            float height = textSize1.Y + textSize2.Y + textSize3.Y + textSize3.Y + 100;

            ImGui.SetNextWindowSize(new Vector2(width, height), ImGuiCond.Always);

            if (ImGui.Begin("ARE YOU SURE", ref Cond_Race_Bonus_Window, ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.AlwaysAutoResize))
            {
                float windowWidth = ImGui.GetWindowSize().X;

                float time = (float)ImGui.GetTime();
                bool flash = (int)(time * 2) % 2 == 0;
                Vector4 textColor = flash ? new Vector4(1f, 0f, 0f, 1f) : new Vector4(1f, 1f, 1f, 1f);

                ImGui.SetCursorPosX((windowWidth - textSize1.X) * 0.5f);
                ImGui.TextColored(textColor, "ARE YOU SURE YOU WANT TO ENABLE THE RACE CONDITION FOR RULES?");

                ImGui.SetCursorPosX((windowWidth - textSize2.X) * 0.5f);
                ImGui.Text("It is VERY easy to be stuck in infinite loops with this condition");

                ImGui.SetCursorPosX((windowWidth - textSize3.X) * 0.5f);
                ImGui.Text("By selecting YES, I yeild my right to ask for help if I can't fix out the loop");

                ImGui.NewLine();

                float buttonWidth = 80f;
                float buttonSpacing = 10f;
                float totalButtonWidth = (buttonWidth * 2) + buttonSpacing;

                ImGui.SetCursorPosX((windowWidth - totalButtonWidth) * 0.5f);
                if (ImGui.Button("NO", new Vector2(buttonWidth, 30)))
                {
                    C.Cond_Race_Bonus = false;
                    C.Cond_Race = false;
                    Cond_Race_Bonus_Window = false;
                }

                ImGui.SameLine();

                if (ImGui.Button("YES", new Vector2(buttonWidth, 30)))
                {
                    C.Cond_Race_Bonus = true;
                    C.Cond_Race = true;
                    Cond_Race_Bonus_Window = false;
                }
            }
            ImGui.End();
        }

        if(!C.Cond_Race_Bonus){C.Cond_Race=false;}

        if(ImGuiGroup.BeginGroupBox("Integrations"))
        {
            ImGuiEx.Text($"Here you can individually enable/disable plugin integrations and configure appropriate related settings.");
            //glam

            ImGui.Checkbox("Glamourer", ref C.EnableGlamourer);
            DrawPluginCheck("Glamourer", "1.2.2.2");
            ImGuiEx.Spacing();
            ImGuiEx.TextV($"DynamicBridge behavior when no Glamourer rule is found:");
            ImGui.SameLine();
            ImGuiEx.Spacing();
            ImGuiEx.SetNextItemWidthScaled(200f);
            ImGuiEx.EnumCombo("##dbglamdef", ref C.GlamNoRuleBehaviour, GlamourerNoRuleBehaviorNames);
            if(C.ManageGlamourerAutomation)
            {
                if(C.GlamNoRuleBehaviour != GlamourerNoRuleBehavior.RevertToAutomation)
                {
                    ImGuiEx.HelpMarker("Revert to Automation is recommended if you are using Glamourer automation.", ImGuiColors.DalamudRed, FontAwesomeIcon.ExclamationTriangle.ToIconString());
                }
            }
            ImGuiEx.Spacing();
            ImGui.Checkbox("Allow DynamicBridge to manage Glamourer's automation setting", ref C.ManageGlamourerAutomation);
            ImGuiEx.HelpMarker("If this setting is enabled, Glamourer's global automation setting will be automatically disabled upon applying any rule and will be automatically enabled when no rules are found.");
            if(P.GlamourerManager.Reflector.GetAutomationGlobalState() && P.GlamourerManager.Reflector.GetAutomationStatusForChara())
            {
                if(!C.ManageGlamourerAutomation)
                {
                    ImGuiEx.HelpMarker("You MUST enable this setting or disable Glamourer's automation, otherwise either Glamourer's or DynamicBridge's automation will not work correctly.", ImGuiColors.DalamudRed, FontAwesomeIcon.ExclamationTriangle.ToIconString());
                }
            }
            ImGuiEx.Spacing();
            ImGui.Checkbox("Revert character before restoring automation", ref C.RevertBeforeAutomationRestore);
            ImGuiEx.Spacing();
            ImGui.Checkbox("Revert character before applying rule", ref C.RevertGlamourerBeforeApply);


            ImGui.Separator();

            //customize

            ImGui.Checkbox("Customize+", ref C.EnableCustomize);
            DrawPluginCheck("CustomizePlus", "2.0.2.3");

            //honorific

            ImGui.Checkbox("Honorific", ref C.EnableHonorific);
            DrawPluginCheck("Honorific", "1.4.2.0");
            ImGuiEx.Spacing();
            ImGui.Checkbox($"Allow selecting titles registered for other characters", ref C.HonotificUnfiltered);

            //penumbra
            ImGui.Checkbox("Penumbra", ref C.EnablePenumbra);
            DrawPluginCheck("Penumbra", "1.0.1.0");

            //moodles
            ImGui.Checkbox("Moodles", ref C.EnableMoodles);
            DrawPluginCheck("Moodles", "1.0.0.15");

            ImGuiGroup.EndGroupBox();
        }

        if(ImGuiGroup.BeginGroupBox("About"))
        {
            GuiAbout.Draw();
            ImGuiGroup.EndGroupBox();
        }
    }

    private static void ConvertTimeRules()
    {
        foreach (Profile profile in C.ProfilesL)
        {
            foreach (ApplyRule rule in profile.Rules)
            {
                rule.Precise_Times = [
                    new TimelineSegment(0f / 24, 5f / 24, rule.Not.Times.Contains(ETime.Night) ? 2 : rule.Times.Contains(ETime.Night) ? 1 : 0),
                    new TimelineSegment(5f / 24, 7f / 24, rule.Not.Times.Contains(ETime.Dawn) ? 2 : rule.Times.Contains(ETime.Dawn) ? 1 : 0),
                    new TimelineSegment(7f / 24, 12f / 24, rule.Not.Times.Contains(ETime.Morning) ? 2 : rule.Times.Contains(ETime.Morning) ? 1 : 0),
                    new TimelineSegment(12f / 24, 17f / 24, rule.Not.Times.Contains(ETime.Day) ? 2 : rule.Times.Contains(ETime.Day) ? 1 : 0),
                    new TimelineSegment(17f / 24, 19f / 24, rule.Not.Times.Contains(ETime.Dusk) ? 2 : rule.Times.Contains(ETime.Dusk) ? 1 : 0),
                    new TimelineSegment(19f / 24, 22f / 24, rule.Not.Times.Contains(ETime.Evening) ? 2 : rule.Times.Contains(ETime.Evening) ? 1 : 0),
                    new TimelineSegment(22f / 24, 24f / 24, rule.Not.Times.Contains(ETime.Night) ? 2 : rule.Times.Contains(ETime.Night) ? 1 : 0)
                ];
            }
        }
    }

    private static void DrawPluginCheck(string name, string minVersion = "0.0.0.0")
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
