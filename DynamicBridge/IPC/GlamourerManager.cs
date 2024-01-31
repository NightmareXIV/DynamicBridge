using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Ipc.Exceptions;
using ECommons.GameHelpers;
using ImGuizmoNET;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicBridge.IPC
{
    public static class GlamourerManager
    {
        public const string LabelGetAllCustomizationFromCharacter = "Glamourer.GetAllCustomizationFromCharacter";
        public const string LabelApplyAllToCharacter = "Glamourer.ApplyAllToCharacter";

        public const string LabelRevertCharacter = "Glamourer.RevertCharacter";

        public const string LabelApplyByGuidAll = "Glamourer.ApplyByGuid";
        public const string LabelApplyByGuidAllToCharacter = "Glamourer.ApplyByGuidToCharacter";

        public const string LabelGetDesignList = "Glamourer.GetDesignList";

        public static void ApplyByGuid(Guid guid)
        {
            try
            {
                Svc.PluginInterface.GetIpcSubscriber<Guid, Character, object>(LabelApplyByGuidAllToCharacter).InvokeAction(guid, Player.Object);
            }
            catch (Exception ex)
            {
                ex.Log();
            }
        }

        public static DesignListEntry[] GetDesignListIPC()
        {
            try
            {
                return Svc.PluginInterface.GetIpcSubscriber<DesignListEntry[]>(LabelGetDesignList).InvokeFunc();
            }
            catch (Exception ex)
            {
                ex.Log();
            }
            return [];
        }

        public static string GetMyCustomization()
        {
            try
            {
                return Svc.PluginInterface.GetIpcSubscriber<Character, string>(LabelGetAllCustomizationFromCharacter).InvokeFunc(Player.Object);
            }
            catch (Exception e)
            {
                e.Log();
                return null;
            }
        }

        public static void SetMyCustomization(string customization)
        {
            try
            {
                Svc.PluginInterface.GetIpcSubscriber<string, Character, object>(LabelApplyAllToCharacter).InvokeAction(customization, Player.Object);
            }
            catch (Exception e)
            {
                e.Log();
            }
        }

        public static void ApplyToSelf(this DesignListEntry design)
        {
            try
            {
                ApplyByGuid(design.Identifier);
                /*var result = Svc.Commands.ProcessCommand($"/glamour apply {design.Identifier} | <me>");
                if (!result) throw new Exception("Glamourer not found");*/
            }
            catch (Exception e)
            {
                Notify.Error(e.Message);
                e.Log();
            }
        }

        public static void Revert()
        {
            try
            {
                /*var result = Svc.Commands.ProcessCommand($"/glamour revert <me>");
                if (!result) throw new Exception("Glamourer not found");*/
                Svc.PluginInterface.GetIpcSubscriber<Character, object>(LabelRevertCharacter).InvokeAction(Player.Object);
            }
            catch (Exception e)
            {
                Notify.Error(e.Message);
                e.Log();
            }
        }

        public static void ResetCache()
        {
            GetDesigns();
        }

        public static DesignListEntry[] GetDesigns()
        {
            return GetDesignListIPC();
        }
    }
}
