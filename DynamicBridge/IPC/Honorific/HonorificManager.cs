using Dalamud.Game.ClientState.Objects.Types;
using ECommons.ExcelServices;
using ECommons.EzIpcManager;
using ECommons.GameHelpers;
using Lumina.Excel.GeneratedSheets;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicBridge.IPC.Honorific;
public class HonorificManager
{
    [EzIPC] readonly Func<string, uint, TitleData[]> GetCharacterTitleList;
    [EzIPC] readonly Action<ICharacter> ClearCharacterTitle;
    [EzIPC] readonly Action<ICharacter, string> SetCharacterTitle;

    public HonorificManager()
    {
        EzIPC.Init(this, "Honorific", SafeWrapper.AnyException);
    }

    public bool WasSet = false;
    public List<TitleData> GetTitleData(IEnumerable<ulong> CIDs)
    {
        try
        {
            List<TitleData> ret = [];
            CIDs ??= C.SeenCharacters.Keys;
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
                        ret.AddRange(GetCharacterTitleList(name, world.RowId) ?? []);
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

    public void SetTitle(string title = null)
    {
        try
        {
            if (title.IsNullOrEmpty())
            {
                WasSet = false;
                ClearCharacterTitle(Player.Object);
            }
            else
            {
                WasSet = true;
                if (GetTitleData(C.HonotificUnfiltered?null:[Player.CID]).TryGetFirst(x => x.Title == title, out var t))
                {
                    SetCharacterTitle(Player.Object, JsonConvert.SerializeObject(t));
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
