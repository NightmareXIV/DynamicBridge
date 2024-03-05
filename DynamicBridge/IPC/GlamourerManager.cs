using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Ipc.Exceptions;
using ECommons.EzIpcManager;
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
        [EzIPC] static Func<Character, string> GetAllCustomizationFromCharacter;
        [EzIPC] static Action<string, Character> ApplyAllOnceToCharacter;
        [EzIPC] static Action<Character> RevertCharacter;
        [EzIPC] static Action<Guid, Character> ApplyByGuidOnceToCharacter;
        [EzIPC] static Func<DesignListEntry[]> GetDesignList;

        public static void Init() => EzIPC.Init(typeof(GlamourerManager), "Glamourer");

        public static void RevertToAutomation()
        {
            GlamourerManager2.RevertToAutomation();
        }

        public static void ApplyByGuid(Guid guid)
        {
            try
            {
                ApplyByGuidOnceToCharacter(guid, Player.Object);
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
                return GetDesignList();
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
                return GetAllCustomizationFromCharacter(Player.Object);
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
                ApplyAllOnceToCharacter(customization, Player.Object);
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
                RevertCharacter(Player.Object);
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
