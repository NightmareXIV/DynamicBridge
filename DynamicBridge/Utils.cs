using DynamicBridge.Configuration;
using DynamicBridge.IPC;
using ECommons.ExcelServices;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Client.Game.Housing;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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

        public static int GetPreviousPreset(this Profile profile, int index, bool isStatic)
        {
            for (int i = index - 1; i >= 0; i--)
            {
                var preset = profile.Presets[i];
                if (preset.IsStaticCategory == isStatic) return i;
            }
            return -1;
        }
        public static int GetNextPreset(this Profile profile, int index, bool isStatic)
        {
            for (int i = index + 1; i < profile.Presets.Count; i++)
            {
                var preset = profile.Presets[i];
                if (preset.IsStaticCategory == isStatic) return i;
            }
            return -1;
        }

        public static bool IsStaticExists(this Profile p)
        {
            return p.Presets.Any(x => x.IsStatic);
        }

        public static Preset GetStaticPreset(this Profile p) => p.Presets.FirstOrDefault(x => x.IsStatic);

        public static void SetStatic(this Profile p, Preset preset)
        {
            p.Presets.Each(x => x.IsStatic = false);
            preset.IsStatic = true;
        }

        public static Profile Profile(ulong CID)
        {
            if (CID == 0 || C.Blacklist.Contains(CID)) return null;
            if (C.Profiles.TryGetValue(CID, out var ret))
            {
                return ret;
            }
            else
            {
                ret = new Profile();
                if (CID == Player.CID) ret.Name = Player.NameWithWorld;
                C.Profiles[CID] = ret;
                return ret;
            }
        }

        public static Profile Profile() => Profile(Player.CID);

        public static string GetHouseDefaultName()
        {
            var h = HousingManager.Instance();
            return $"{Svc.Data.GetExcelSheet<TerritoryType>().GetRow(Svc.ClientState.TerritoryType)?.PlaceNameRegion.Value?.Name?.ExtractText()}, Ward {h->GetCurrentWard()+1}, plot {h->GetCurrentPlot()+1}";
        }

        public static DesignListEntry? GetDesignByName(string name)
        {
            var designs = GlamourerManager.GetDesigns();
            foreach(var d in designs)
            {
                if (d.Name == name) return d;
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
            if(list.Length == 1) return list[0].ToString();
            FullList = list.Select(x => x.ToString()).Join("\n");
            return $"{list.Length} selected";
        }
    }
}
