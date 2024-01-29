using Dalamud.Game.ClientState.Objects.Types;
using ECommons.GameHelpers;
using Newtonsoft.Json;

namespace DynamicBridge.IPC;
public static class CustomizePlusManager
{
    public static bool WasSet = false;
    public static CustomizePlusProfile[] GetProfiles(string chara = null)
    {
        try
        {
            var ret = Svc.PluginInterface.GetIpcSubscriber<CustomizePlusProfile[]>("CustomizePlus.GetRegisteredProfileList").InvokeFunc();
            if(chara != null)
            {
                ret = ret.Where(x => x.characterName == chara).ToArray();
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
            if (GetProfiles().TryGetFirst(x => x.Name == profileName && (charName == null || x.characterName == charName), out var profile))
            {
                Svc.PluginInterface.GetIpcSubscriber<Guid, Character?, object>("CustomizePlus.SetProfileToCharacterByUniqueId").InvokeAction(profile.ID, Player.Object);
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
