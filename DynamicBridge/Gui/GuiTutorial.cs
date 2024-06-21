using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace DynamicBridge.Gui;
public static class GuiTutorial
{
    static readonly string Content = Lang.Tutorial;
    public static void Draw()
    {
        ImGuiEx.CheckboxInverted(Lang.Draw_HideTutorial, ref C.ShowTutorial);
        var array = Content.ReplaceLineEndings().Split(Environment.NewLine);
        for (int i = 0; i < array.Length; i++)
        {
            var s = array[i];
            if (s.StartsWith("+"))
            {
                if (ImGui.TreeNode(s[1..]))
                {
                    do
                    {
                        DrawLine(array[i + 1]);
                        i++;
                    }
                    while (i+1 < array.Length && !array[i + 1].StartsWith("+"));
                    ImGui.TreePop();
                }
                else
                {
                    do
                    {
                        i++;
                    }
                    while (i+1 < array.Length && !array[i + 1].StartsWith("+"));
                }
            }
            else
            {
                DrawLine(s);
            }
        }
    }

    static void DrawLine(string s)
    {
        if (s.StartsWith("fai="))
        {
            var chr = s[4..];
            var success = false;
            foreach(var x in Enum.GetValues<FontAwesomeIcon>())
            {
                if(x.ToString() == chr)
                {
                    ImGui.PushFont(UiBuilder.IconFont);
                    ImGuiEx.Text($"{x.ToIconString()}");
                    ImGui.PopFont();
                    ImGui.SameLine();
                    success = true;
                    break;
                }
            }
            if(!success && uint.TryParse(chr, System.Globalization.NumberStyles.HexNumber, null, out var num))
            {
                ImGui.PushFont(UiBuilder.IconFont);
                ImGuiEx.Text($"{(char)num}");
                ImGui.PopFont();
                ImGui.SameLine();
            }
            else
            {
                //ImGuiEx.Text($"Parse error: {chr}");
            }
        }
        else if (s.StartsWith("image="))
        {
            if(ThreadLoadImageHandler.TryGetTextureWrap($"{Path.Combine(Svc.PluginInterface.AssemblyLocation.DirectoryName, "res", "tutorial", $"{s[6..]}.png")}", out var tex))
            {
                ImGui.Image(tex.ImGuiHandle, new(tex.Width, tex.Height));
            }
        }
        else if(s == "---")
        {
            ImGui.Separator();
        }
        else if(s == "")
        {
            ImGui.Dummy(new Vector2(5));
        }
        else
        {
            ImGuiEx.TextWrapped(s);
        }
    }
}
