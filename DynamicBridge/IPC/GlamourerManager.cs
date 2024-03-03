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
        public const string GlamourerGetAllCustomization = "Glamourer.GetAllCustomizationFromCharacter";
        public const string GlamourerApplyToChar = "Glamourer.ApplyAllOnceToCharacter";
        public const string GlamourerRevert = "Glamourer.RevertCharacter";
        public const string GlamourerApplyToCharByGuid = "Glamourer.ApplyByGuidOnceToCharacter";
        public const string GlamourerGetDesignList = "Glamourer.GetDesignList";

        public static void RevertToAutomation()
        {
            GlamourerManager2.RevertToAutomation();
        }

        public static void ApplyByGuid(Guid guid)
        {
            try
            {
                Svc.PluginInterface.GetIpcSubscriber<Guid, Character, object>(GlamourerApplyToCharByGuid).InvokeAction(guid, Player.Object);
            }
            catch (Exception ex)
            {
                ex.Log();
            }
        }

        static DesignListEntry[] GetDesignListIPC()
        {
            try
            {
                return Svc.PluginInterface.GetIpcSubscriber<DesignListEntry[]>(GlamourerGetDesignList).InvokeFunc();
            }
            catch (Exception ex)
            {
                InternalLog.Error(ex.ToString());
            }
            return [];
        }

        public static string GetMyCustomization()
        {
            try
            {
                return Svc.PluginInterface.GetIpcSubscriber<Character, string>(GlamourerGetAllCustomization).InvokeFunc(Player.Object);
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
                Svc.PluginInterface.GetIpcSubscriber<string, Character, object>(GlamourerApplyToChar).InvokeAction(customization, Player.Object);
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
                Svc.PluginInterface.GetIpcSubscriber<Character, object>(GlamourerRevert).InvokeAction(Player.Object);
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

        public static string TransformName(string originalName)
        {
            if(Guid.TryParse(originalName, out Guid guid))
            {
                if(GetDesigns().TryGetFirst(x => x.Identifier == guid, out var entry))
                {
                    if (C.GlamourerFullPath)
                    {
                        return GlamourerReflector.GetPathForDesignByGuid(guid) ?? entry.Name;
                    }
                    return entry.Name;
                }
            }
            return originalName;
        }
    }
}
