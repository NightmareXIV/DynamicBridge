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
    private static readonly string Content = @"
Welcome to DynamicBridge plugin!
This plugin allows you to dynamically change your Glamourer, Customize+ and Honorific presets based on various rules. You can use this plugin for simple means such as switching your appearance manually or you can create very advanced and precise rule sets.
---
If you have previously used older version of the plugin, your configuration has been converted to use more convenient profile system. In case something went wrong, a backup of your old configuration file has been taken.
Instead of enforcing per-character profiles, you can now create profiles and assign them to characters. This enables you to use one profile for multiple characters as well as easily changing profiles used for your characters.
---
+Quick start
I will describe minimal amount of steps you will need to take to begin using this plugin.
First, assuming you have never created profiles before, log in to your character, navigate to ""Dynamic Rules"" or ""Presets"" tab and press the ""+"" button:
image=1
This will create new profile and associate it with your current character. Now you can create rules and presets to your liking.
+Rules and Presets
DynamicBridge constantly monitors your character states and checks rules top to bottom. Once correct rule is found, a random preset among selected ones will be applied to your character and no further rules will be processed. Currently applying rule will be highlighted in green. 
Preset is a set of Glamourer, Customize+ and Honorific customizations that will be applied to your character. 
Overview of a header of Rules and Presets section:
image=2

fai=Plus
is used to create new, empty dynamic rule or preset and add it to the end of the list. 

fai=FolderPlus
is used to create new folder in Presets section. Folders are used only to visually separate your presets.

fai=Paste
is used to paste an existing rule or preset from clipboard, if you have previously copied any.

fai=Tshirt
is used to reapply rules and presets to your character. You should use it after you have modified your rules or presets if you want these changes to be immediately reflected.

fai=Unlink
is used to unlink profile from current character. The profile itself is not deleted and can be linked to your character once again. This button is changed to ""Link profile"" when you are editing profile that is not assigned to your current character.

Middle section indicates which profile is currently selected for editing and whether it's linked to your character or not. Click on the middle section to select a profile that you want to edit.
---
+Dynamic Rules
You may go to Settings tab to enable extra conditions or disable unused conditions. Please note: the more conditions you have enabled, the more processing time plugin will be taking. The same goes for rules - the more rules you have, the higher processing time is. Fortunately, you will have to go well over few hundreds of rules before you start to notice the effect.

fai=Check
Indicates whether rule is enabled or not. Disabled rules are completely ignored.

fai=ArrowsUpDownLeftRight
Grab this button and drag the rule up or down to reorder it.

fai=f103
Enabling passthrough indicator will prevent rule engine from stopping upon matching with that rule. When 2 or more rules are matching, their presets are applied sequentially one after another.

fai=Copy
Copy this rule to clipboard for future use. You may save it and share with others.
+Presets
You may go to Settings tab to disable plugins that you don't use.

fai=Circle
Use this button to set preset as static. While preset is set as static, rules and base preset are completely ignored and your appearance will always be set to be according to the preset.

If you choose to set any values in base preset, they will be used when there is no other value in corresponding plugin section.
";
    public static void Draw()
    {
        ImGuiEx.CheckboxInverted("Hide tutorial", ref C.ShowTutorial);
        var array = Content.ReplaceLineEndings().Split(Environment.NewLine);
        for(var i = 0; i < array.Length; i++)
        {
            var s = array[i];
            if(s.StartsWith("+"))
            {
                if(ImGui.TreeNode(s[1..]))
                {
                    do
                    {
                        DrawLine(array[i + 1]);
                        i++;
                    }
                    while(i + 1 < array.Length && !array[i + 1].StartsWith("+"));
                    ImGui.TreePop();
                }
                else
                {
                    do
                    {
                        i++;
                    }
                    while(i + 1 < array.Length && !array[i + 1].StartsWith("+"));
                }
            }
            else
            {
                DrawLine(s);
            }
        }
    }

    private static void DrawLine(string s)
    {
        if(s.StartsWith("fai="))
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
        else if(s.StartsWith("image="))
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
