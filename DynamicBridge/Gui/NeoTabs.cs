using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DynamicBridge.Gui;
public class NeoTabs
{
    internal Tab[] Tabs;
    Dictionary<int, float> Paddings = [];

    public NeoTabs(Tab[] tabs)
    {
        this.Tabs = tabs;
    }

    public void Draw()
    {
        if(ImGui.BeginTable($"NeoTabs", Tabs.Length, ImGuiTableFlags.NoSavedSettings | ImGuiTableFlags.SizingStretchSame | ImGuiTableFlags.Borders))
        {
            for (int i = 0; i < Tabs.Length; i++)
            {
                ImGui.TableSetupColumn($"NeoTab{i}");
            }
            ImGui.TableNextRow();
            for (int i = 0; i < Tabs.Length; i++)
            {
                ImGui.TableNextColumn();
                var tab = Tabs[i];
                ImGui.Dummy(new Vector2(ImGui.GetColumnWidth() / 2f - tab.CalcLabelWidth() / 2f - ImGui.GetStyle().CellPadding.X, 1));
                ImGui.SameLine();
                tab.DrawHeader();
            }
            ImGui.EndTable();
        }
    }

    public class Tab
    {
        public string Name;
        public string Icon = null;
        public Action Content;
        public bool ContentExecOnClick = false;

        public Tab(string name, Action content)
        {
            this.Name = name;
            this.Content = content;
        }

        public Tab(string name, string icon, Action content)
        {
            this.Name = name ?? throw new ArgumentNullException(nameof(name));
            this.Icon = icon;
            this.Content = content ?? throw new ArgumentNullException(nameof(content));
        }

        internal float CalcLabelWidth()
        {
            var ret = 0f;
            ImGui.PushFont(UiBuilder.IconFont);
            ret += ImGui.CalcTextSize($"{Icon}").X;
            ImGui.PopFont();
            ret += ImGui.CalcTextSize($"{Name}").X;
            return ret;
        }

        internal void DrawHeader()
        {
            if(Icon != null)
            {
                ImGui.PushFont(UiBuilder.IconFont);
                ImGuiEx.Text($"{Icon}");
                ImGui.PopFont();
                ImGui.SameLine();
            }
            ImGuiEx.Text($"{Name}");
        }

        internal void Draw()
        {
            try
            {
                Content();
            }
            catch(Exception e)
            {
                e.Log();
            }
        }
    }
}
