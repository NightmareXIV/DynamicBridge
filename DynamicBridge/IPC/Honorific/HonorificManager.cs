using Dalamud.Game.ClientState.Objects.Types;
using ECommons.ExcelServices;
using ECommons.GameHelpers;
using Lumina.Excel.GeneratedSheets;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicBridge.IPC.Honorific;
public static class HonorificManager
{
    public static bool WasSet = false;
    public static List<TitleData> GetTitleData(IEnumerable<ulong> CIDs)
    {
        try
        {
            List<TitleData> ret = [];
            foreach (var c in CIDs)
            {
                var nameWithWorld = Utils.GetCharaNameFromCID(c);
                if (nameWithWorld != null)
                {
                    var parts = nameWithWorld.Split("@");
                    if (parts.Length == 2)
                    {
                        var name = parts[0];
                        var world = ExcelWorldHelper.Get(parts[1]);
                        ret.AddRange(Svc.PluginInterface.GetIpcSubscriber<string, uint, TitleData[]>("Honorific.GetCharacterTitleList").InvokeFunc(name, world.RowId));
                    }
                }
            }
            return ret;
        }
        catch (Exception e)
        {
            e.Log();
            return [];
        }
    }

    public static void SetTitle(string title = null)
    {
        try
        {
            if (title.IsNullOrEmpty())
            {
                WasSet = false;
                Svc.PluginInterface.GetIpcSubscriber<Character, object>("Honorific.ClearCharacterTitle").InvokeAction(Player.Object);
            }
            else
            {
                WasSet = true;
                if (GetTitleData([Player.CID]).TryGetFirst(x => x.Title == title, out var t))
                {
                    Svc.PluginInterface.GetIpcSubscriber<Character, string, object>("Honorific.SetCharacterTitle").InvokeAction(Player.Object, JsonConvert.SerializeObject(t));
                }
                else
                {
                    throw new KeyNotFoundException($"Could not find title preset {title}");
                }
            }
        }
        catch (Exception e)
        {
            e.Log();
        }
    }
}
