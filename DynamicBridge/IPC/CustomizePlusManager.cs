using Dalamud.Game.ClientState.Objects.Types;
using ECommons.GameHelpers;
using Newtonsoft.Json;

namespace DynamicBridge.IPC;
public static class CustomizePlusManager
{
    public static bool WasSet = false;
    public static List<MiniCPlusProfile> GetProfiles()
    {
        try
        {
            var ret = new List<MiniCPlusProfile>();
            foreach(var x in Svc.PluginInterface.GetIpcSubscriber<string[]>("CustomizePlus.GetProfiles").InvokeFunc())
            {
                var element = JsonConvert.DeserializeObject<MiniCPlusProfile>(x);
                element.JsonData = x;
                ret.Add(element);
            }
            return ret;
        }
        catch (Exception e)
        {
            e.Log();
            return [];
        }
    }

    public static void SetProfile(string profileName, string charName = null)
    {
        WasSet = true;
        try
        {
            if (GetProfiles().TryGetFirst(x => x.ProfileName == profileName && (charName == null || x.CharacterName == charName), out var profile))
            {
                Svc.PluginInterface.GetIpcSubscriber<string, Character?, object>("CustomizePlus.SetProfileToCharacter").InvokeAction(profile.JsonData, Player.Object);
            }
        }
        catch(Exception e)
        {
            e.Log();
        }
    }

    public static void RevertProfile()
    {
        WasSet = false;
        try
        {
            Svc.PluginInterface.GetIpcSubscriber<Character?, object>("CustomizePlus.RevertCharacter").InvokeAction(Player.Object);
        }
        catch (Exception e)
        {
            e.Log();
        }
    }
}
