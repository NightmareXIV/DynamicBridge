using Dalamud.Game.ClientState.Objects.Types;
using ECommons.GameHelpers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicBridge.IPC;
public static class PalettePlusManager
{
    public static bool WasSet = false;

    public static List<MicroPalette> GetPalettes()
    {
        try
        {
            var ret = new List<MicroPalette>();
            foreach(var palette in Svc.PluginInterface.GetIpcSubscriber<string[]>("PalettePlus.GetSavedPalettes").InvokeFunc()) 
            {
                var decoded = JsonConvert.DeserializeObject<MicroPalette>(palette);
                decoded.JsonData = palette;
                ret.Add(decoded);
            }
            return ret;
        }
        catch(Exception e)
        {
            e.Log();
            return [];
        }
    }

    public static void SetPalette(string name)
    {
        try
        {
            WasSet = true;
            if (GetPalettes().TryGetFirst(x => x.Name == name, out var palette))
            {
                Svc.PluginInterface.GetIpcSubscriber<Character, string, object>("PalettePlus.SetCharaPalette").InvokeAction(Player.Object, palette.JsonData);
            }
        }
        catch (Exception ex)
        {
            ex.Log();
        }
    }

    public static void RevertPalette()
    {
        try
        {
            WasSet = false;
            Svc.PluginInterface.GetIpcSubscriber<Character, object>("PalettePlus.RemoveCharaPalette").InvokeAction(Player.Object);
            Svc.PluginInterface.GetIpcSubscriber<Character, object>("PalettePlus.RedrawChara").InvokeAction(Player.Object);
        }
        catch (Exception ex)
        {
            ex.Log();
        }
    }
}
