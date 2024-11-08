using OtterGui.Widgets;
using OtterGuiInternal.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicBridge.Gui;
public class TristateCheckboxEx : TristateCheckbox
{
    protected override void RenderSymbol(sbyte value, Vector2 position, float size)
    {
        switch(value)
        {
            case -1:
                SymbolHelpers.RenderCross(ImGui.GetWindowDrawList(), position, CrossColor, size);
                break;
            case 1:
                SymbolHelpers.RenderCheckmark(ImGui.GetWindowDrawList(), position, CheckColor, size);
                break;
            default:
                //SymbolHelpers.RenderDot(ImGui.GetWindowDrawList(), position, DotColor, size);
                break;
        }
    }
}
