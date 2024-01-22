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
        ImGuiEx.LineCentered("about1", () =>
        {
            ImGuiEx.Text($"Made by NightmareXIV in collaboration with AsunaTsuki");
        });
        ImGuiEx.LineCentered("about2", () =>
        {
            if (ImGui.Button("Join Discord for support and changelogs"))
            {
                ShellStart("https://discord.gg/m8NRt4X8Gf");
            }
        });
        ImGuiEx.LineCentered("about3", () =>
        {
            if (ImGui.Button("Read instructions and FAQ"))
            {
                ShellStart("https://github.com/NightmareXIV/DynamicBridge/tree/main/docs");
            }
        });
    }
}
