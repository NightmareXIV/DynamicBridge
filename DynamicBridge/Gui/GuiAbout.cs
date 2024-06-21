using Dalamud.Interface.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicBridge.Gui;
public static class GuiAbout
{
    public static void Draw()
    {
        if (!Utils.IsDisguise())
        {
            ImGuiEx.LineCentered("about1", () =>
            {
                ImGuiEx.Text(Lang.MadeByNightmareXIVInCollaborationWithAsunaTsuki);
            });
            ImGuiEx.LineCentered("about2", () =>
            {
                if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.Comments, Lang.JoinDiscordForSupportAndChangelogs))
                {
                    ShellStart("https://discord.gg/m8NRt4X8Gf");
                }
            });
            ImGuiEx.LineCentered("about3", () =>
            {
                if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.QuestionCircle, Lang.ReadInstructionsAndFAQ))
                {
                    ShellStart("https://github.com/NightmareXIV/DynamicBridge/tree/main/docs");
                }
            });
        }
        else
        {
            ImGuiEx.LineCentered("about4", () =>
            {
                if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.Bug, Lang.ReportIssueRequestFeatureOrAskAQuestionOnGitHub))
                {
                    ShellStart("https://github.com/Limiana/DynamicBridgeStandalone/issues");
                }
            }); 
        }
    }
}
