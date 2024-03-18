using ImGuizmoNET;
using OtterGui;
using OtterGui.Raii;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.ComponentModel.Design.ObjectSelectorEditor;

namespace DynamicBridge.Gui;
public class NeoTabs
{
    public void DrawHeaderLine()
    {
        var withSpacing = ImGui.GetFrameHeightWithSpacing();
        using var style = ImRaii.PushStyle(ImGuiStyleVar.FrameRounding, 0).Push(ImGuiStyleVar.ItemSpacing, Vector2.Zero);
        var buttonSize = new Vector2((ImGui.GetContentRegionAvail().X - withSpacing * 0f) / 4f, ImGui.GetFrameHeight());

        using var _ = ImRaii.Group();
    }
}
