using Dalamud.Memory;
using DynamicBridge.Configuration;
using DynamicBridge.Gui;
using DynamicBridge.IPC.Glamourer;
using ECommons.ExcelServices;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using ECommons.Reflection;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Housing;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace DynamicBridge
{
    public unsafe static class Utils
    {
        public const string IconWarning = "\uf071";
        public static bool IsMoving => P.AgentMapInst->IsPlayerMoving == 1;
        public static bool IsInWater => Player.Available && Player.Object.IsInWater();

        public static List<uint> GetCurrentGear()
        {
            var ret = new List<uint>();
            var im = InventoryManager.Instance();
            var cont = im->GetInventoryContainer(InventoryType.EquippedItems);
            for (int i = 0; i < cont->Size; i++)
            {
                var item = cont->GetInventorySlot(i);
                ret.Add((uint)(item->ItemID + (item->Flags.HasFlag(InventoryItem.ItemFlags.HQ) ? 1000000 : 0)));
            }
            return ret;
        }
        
        public static bool GuidEquals(this List<ApplyRule> rule, List<ApplyRule> other)
        {
            //if (rule == null && other == null) return true;
            //if (rule == null || other == null) return false;
            if (rule.Count != other.Count) return false;
            for (int i = 0; i < rule.Count; i++)
            {
                if (rule[i].GUID != other[i].GUID) return false;
            }
            return true;
        }

        public static bool IsStaticExists(this Profile p)
        {
            return p.GetPresetsUnion().Any(x => x.IsStatic);
        }

        public static Preset GetStaticPreset(this Profile p) => p.GetPresetsUnion().FirstOrDefault(x => x.IsStatic);

        public static void SetStatic(this Profile p, Preset preset)
        {
            p.GetPresetsUnion(false).Each(x => x.IsStatic = false);
            preset.IsStatic = true;
        }

        public static Profile Profile(ulong CID, bool returnMain = false)
        {
            if (CID == 0 || C.Blacklist.Contains(CID)) return null;
            if (C.Profiles.TryGetValue(CID, out var ret))
            {
                if (returnMain) return ret;
                return ret.Subprofiles.SafeSelect(ret.Subprofile) ?? ret;
            }
            else
            {
                ret = new Profile();
                if (CID == Player.CID) ret.Name = Player.NameWithWorld;
                C.Profiles[CID] = ret;
                return ret;
            }
        }

        public static Profile Profile(bool returnMain = false) => Profile(Player.CID, returnMain);

        public static string GetHouseDefaultName()
        {
            var h = HousingManager.Instance();
            return $"{Svc.Data.GetExcelSheet<TerritoryType>().GetRow(Svc.ClientState.TerritoryType)?.PlaceNameRegion.Value?.Name?.ExtractText()}, Ward {h->GetCurrentWard()+1}, plot {h->GetCurrentPlot()+1}";
        }

        public static GlamourerDesignInfo? GetDesignByGUID(string GUID)
        {
            var designs = P.GlamourerManager.GetDesigns();
            foreach(var d in designs)
            {
                if (d.Identifier.ToString() == GUID) return d;
            }
            return null;
        }

        public static bool TryFindBytes(this byte[] haystack, byte[] needle, out int pos)
        {
            var len = needle.Length;
            var limit = haystack.Length - len;
            for (var i = 0; i <= limit; i++)
            {
                var k = 0;
                for (; k < len; k++)
                {
                    if (needle[k] != haystack[i + k]) break;
                }
                if (k == len)
                {
                    pos = i;
                    return true;
                }
            }
            pos = default;
            return false;
        }

        public static (List<byte> WeatherList, string EnvbFile) ParseLvb(ushort id) 
        {
            var weathers = new List<byte>();
            var territoryType = Svc.Data.GetExcelSheet<TerritoryType>().GetRow(id);
            if (territoryType == null) return default;
            try
            {
                var file = Svc.Data.GetFile<LvbFile>($"bg/{territoryType.Bg}.lvb");
                if (file?.weatherIds == null || file.weatherIds.Length == 0)
                    return (null, null);
                foreach (var weather in file.weatherIds)
                    if (weather > 0 && weather < 255)
                        weathers.Add((byte)weather);
                weathers.Sort();
                return (weathers, file.envbFile);
            }
            catch (Exception e)
            {
                PluginLog.Error($"Failed to load lvb for {territoryType}\n{e}");
            }
            return default;
        }
        public static bool TryFindBytes(this byte[] haystack, string needle, out int pos)
        {
            return TryFindBytes(haystack, needle.Split(" ").Select(x => byte.Parse(x, NumberStyles.HexNumber)).ToArray(), out pos);
        }

        public static string PrintRange(this IEnumerable<string> s, out string FullList, string noneStr = "Any")
        {
            FullList = null;
            var list = s.ToArray();
            if (list.Length == 0) return noneStr;
            if (list.Length == 1) return list[0].ToString();
            FullList = list.Select(x => x.ToString()).Join("\n");
            return $"{list.Length} selected";
        }

        public static string PrintRange<T>(this IEnumerable<T> s, IEnumerable<T> notS, out string FullList, string noneStr = "Any")
        {
            if (!C.AllowNegativeConditions)
            {
                return PrintRange(s.Select(x => x.ToString().Replace("_", " ")), out FullList, noneStr);
            }
            FullList = null;
            var list = s.Select(x => x.ToString().Replace("_", " ")).ToArray();
            var notList = notS.Select(x => x.ToString().Replace("_", " ")).ToArray();
            if (list.Length == 0 && notList.Length == 0) return noneStr;
            FullList = list.Select(x => x.ToString()).Join("\n");
            if (notList.Length > 0)
            {
                FullList += "\nMeeting any of these condition will make rule invalid:\n";
                FullList += notList.Select(x => x.ToString()).Join("\n");
            }
            return $"{list.Length} | {notList.Length} selected";
        }

        public static List<uint> GetArmor()
        {
            var im = InventoryManager.Instance();
            var cont = im->GetInventoryContainer(InventoryType.EquippedItems);
            var ret = new List<uint>();
            for (int i = 0; i < cont->Size; i++)
            {
                ret.Add(cont->GetInventorySlot(i)->ItemID);
            }
            return ret;
        }

        public static bool? WaitUntilInteractable() => Player.Interactable;

        public static PresetFolder GetFolder(this Preset preset, Profile profile)
        {
            foreach(var x in profile.PresetsFolders)
            {
                if (x.Presets.Any(z => z == preset)) return x;
            }
            return null;
        }

        public static bool IsDisguise()
        {
            if (Gui.Debug.ForceDisguise != null) return Gui.Debug.ForceDisguise.Value;
            return Svc.PluginInterface.SourceRepository.ContainsAny(StringComparison.OrdinalIgnoreCase, "SeaOfStars", "DynamicBridgeStandalone");
        }

        public static IEnumerable<string> ToWorldNames(this IEnumerable<uint> worldIds)
        {
            return worldIds.Select(ExcelWorldHelper.GetName);
        }

        public static void UpdateGearsetCache()
        {
            if (Player.Available)
            {
                var list = new List<GearsetEntry>();
                foreach (var x in RaptureGearsetModule.Instance()->EntriesSpan)
                {
                    if (*x.Name == 0) continue;
                    list.Add(new(x.ID, MemoryHelper.ReadStringNullTerminated((nint)x.Name), x.ClassJob));
                }
                C.GearsetNameCache[Player.NameWithWorld] = list;
            }
        }

        public static IEnumerable<string> ToGearsetNames(this List<int> gearsetIDs, string nameWithWorld)
        {
            if (!C.GearsetNameCache.TryGetValue(nameWithWorld, out var cache)) cache = [];
            for (int i = 0; i < gearsetIDs.Count; i++)
            {
                if (cache.TryGetFirst(x => x.Id == gearsetIDs[i], out var ret))
                {
                    yield return $"{ret}";
                }
                else
                {
                    yield return $"{gearsetIDs[i]} No name found";
                }
            }
        }
    }
}
