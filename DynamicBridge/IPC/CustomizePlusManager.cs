using Dalamud.Game.ClientState.Objects.Types;
using DynamicBridge.Configuration;
using ECommons.GameHelpers;
using Newtonsoft.Json;

namespace DynamicBridge.IPC;
public static class CustomizePlusManager
{
    public static bool WasSet = false;
    public static Guid SavedProfileID = Guid.Empty;
    public static Guid LastEnabledProfileID = Guid.Empty;
    public static CustomizePlusProfile[] GetProfiles(string chara = null)
    {
        try
        {
            //var ret = Svc.PluginInterface.GetIpcSubscriber<CustomizePlusProfile[]>("CustomizePlus.GetProfileList").InvokeFunc();
            var ret = CustomizePlusReflector.GetProfiles().ToArray();
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

    public static void SetProfile(string profileName, string charName)
    {
        try
        {
            var charaProfiles = GetProfiles().Where(x => x.characterName == charName).ToArray();
            if (!WasSet)
            {
                if(charaProfiles.TryGetFirst(x => x.IsEnabled, out var enabledProfile))
                {
                    SavedProfileID = enabledProfile.ID;
                }
                else
                {
                    SavedProfileID = Guid.Empty;
                }
            }
            if (charaProfiles.TryGetFirst(x => x.Name == profileName, out var profile))
            {
                //Svc.PluginInterface.GetIpcSubscriber<Guid, object>("CustomizePlus.EnableProfileByUniqueId").InvokeAction(profile.ID);
                CustomizePlusReflector.SetEnabled(profile.ID, true);
                LastEnabledProfileID = profile.ID;
            }
        }
        catch(Exception e)
        {
            e.Log();
        }
        WasSet = true;
    }

    public static void RestoreState()
    {
        if (WasSet)
        {
            try
            {
                if (SavedProfileID == Guid.Empty)
                {
                    //Svc.PluginInterface.GetIpcSubscriber<Guid, object>("CustomizePlus.DisableProfileByUniqueId").InvokeAction(LastEnabledProfileID);
                    CustomizePlusReflector.SetEnabled(LastEnabledProfileID, false);
                }
                else
                {
                    //Svc.PluginInterface.GetIpcSubscriber<Guid, object>("CustomizePlus.EnableProfileByUniqueId").InvokeAction(SavedProfileID);
                    CustomizePlusReflector.SetEnabled(SavedProfileID, true);
                }
            }
            catch (Exception e)
            {
                e.Log();
            }
            SavedProfileID = Guid.Empty;
        }
        WasSet = false;
    }
}
