using Dalamud.Game.ClientState.Objects.Types;
using ECommons.GameHelpers;
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
    public static TitleData[] GetTitleData(string name = null, uint? world = null)
    {
        try
        {
            name ??= Svc.ClientState.LocalPlayer.Name.ToString();
            world ??= Svc.ClientState.LocalPlayer.HomeWorld.Id;
            return Svc.PluginInterface.GetIpcSubscriber<string, uint, TitleData[]>("Honorific.GetCharacterTitleList").InvokeFunc(name, world.Value);
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
                if (GetTitleData().TryGetFirst(x => x.Title == title, out var t))
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
