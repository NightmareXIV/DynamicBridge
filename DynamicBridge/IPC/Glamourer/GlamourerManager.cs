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

namespace DynamicBridge.IPC.Glamourer
{
    public unsafe class GlamourerManager
    {
        public GlamourerReflector Reflector;
        public GlamourerCommands Commands;

        [EzIPC] Func<Character, string> GetAllCustomizationFromCharacter;
        [EzIPC] Action<string, Character> ApplyAllOnceToCharacter;
        [EzIPC] Action<Character> RevertCharacter;
        [EzIPC] Action<Guid, Character> ApplyByGuidOnceToCharacter;
        [EzIPC] Func<GlamourerDesignInfo[]> GetDesignList;

        public GlamourerManager()
        {
            EzIPC.Init(this, "Glamourer");
            Reflector = new();
            Commands = new();
        }

        public void RevertToAutomation()
        {
            Commands.RevertToAutomation();
        }

        public void ApplyByGuid(Guid guid)
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

        GlamourerDesignInfo[] GetDesignListIPC()
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

        public string GetMyCustomization()
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

        public void SetMyCustomization(string customization)
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

        public void ApplyToSelf(GlamourerDesignInfo design)
        {
            try
            {
                ApplyByGuid(design.Identifier);
            }
            catch (Exception e)
            {
                Notify.Error(e.Message);
                e.Log();
            }
        }

        public void Revert()
        {
            try
            {
                RevertCharacter(Player.Object);
            }
            catch (Exception e)
            {
                Notify.Error(e.Message);
                e.Log();
            }
        }

        GlamourerDesignInfo[] CachedDesignInfo = [];
        ulong ValidCacheFrame = 0;
        public GlamourerDesignInfo[] GetDesigns()
        {
            var fc = CSFramework.Instance()->FrameCounter;
            if (fc != ValidCacheFrame)
            {
                ValidCacheFrame = fc;
                CachedDesignInfo = GetDesignListIPC();
            }
            return CachedDesignInfo;
        }

        Dictionary<string, string> TransformNameCache = [];
        public string TransformName(string originalName)
        {
            if(TransformNameCache.TryGetValue(originalName, out var ret))
            {
                return ret;
            }
            if (Guid.TryParse(originalName, out Guid guid))
            {
                if (GetDesigns().TryGetFirst(x => x.Identifier == guid, out var entry))
                {
                    if (C.GlamourerFullPath)
                    {
                        return CacheAndReturn(Reflector.GetPathForDesignByGuid(guid) ?? entry.Name);
                    }
                    return CacheAndReturn(entry.Name);
                }
            }
            return CacheAndReturn(originalName);

            string CacheAndReturn(string name)
            {
                TransformNameCache[originalName] = name;
                return TransformNameCache[originalName];
            }
        }

        public void ResetNameCache() => TransformNameCache.Clear();
    }
}
