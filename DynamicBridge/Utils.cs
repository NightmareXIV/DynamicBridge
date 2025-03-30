using Dalamud.Memory;
using DynamicBridge.Configuration;
using ECommons.ExcelServices;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Client.Game;

using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using Lumina.Excel.Sheets;
using System.Globalization;
using Action = System.Action;

namespace DynamicBridge
{
    public static unsafe class Utils
    {
        public const string IconWarning = "\uf071";
        public static bool IsMoving => P.AgentMapInst->IsPlayerMoving;
        public static bool IsInWater => Player.Available && Player.Object.IsInWater();
        public static ImGuiInputTextFlags CensorFlags => C.NoNames ? ImGuiInputTextFlags.Password : ImGuiInputTextFlags.None;
        public static Vector2 CellPadding => ImGui.GetStyle().CellPadding + new Vector2(0, 2);
        public const float IndentSpacing = 5f;

        public static uint GetAdjustedEmote()
        {
            var em = Player.Character->EmoteController.EmoteId;
            if(Data.EmoteGroups.TryGetFirst(x => x.Contains(em), out var array))
            {
                return array[0];
            }
            return em;
        }

        public static string[] SplitDirectories(this string path)
        {
            var ret = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if(ret.Length == 0) return [""];
            return ret;
        }

        public static void DrawFolder(IEnumerable<(string[] Folder, Action Draw)> values)
        {
            var folder = new Folder(null, []);
            foreach(var x in values)
            {
                folder.AddItem(x.Folder, new(x.Draw));
            }
            folder.Draw();
        }

        public static bool CollectionSelectable<T>(Vector4? color, string label, T value, ICollection<T> collection, bool delayedOperation = false)
        {
            bool Draw(ref bool x) => ImGuiEx.SelectableNode(color ?? ImGuiEx.Vector4FromRGB(0xDDDDDD), label, ref x, collection.Contains(value) ? ImGuiTreeNodeFlags.Bullet : ImGuiTreeNodeFlags.Leaf);
            return ImGuiEx.CollectionCore(Draw, value, collection, false, delayedOperation);
        }

        public static Job GetUpgradedJobIfNeeded(this Job current)
        {
            if(C.UnifyJobs) return current.GetUpgradedJob();
            return current;
        }

        public static List<string> TrimPathes(IEnumerable<string> origPathes)
        {
            var pathes = new List<string>();
            foreach(var path in origPathes)
            {
                var nameParts = path.Split('/');
                if(nameParts.Length > 1)
                {
                    var pathParts = nameParts[..^1];
                    pathes.Add(pathParts.Join("/"));
                }
            }
            return pathes;
        }

        public static List<PathInfo> BuildPathes(List<string> rawPathes)
        {
            var ret = new List<PathInfo>();
            try
            {
                var pathes = Utils.TrimPathes(rawPathes);
                pathes.Sort();
                foreach(var x in pathes)
                {
                    var parts = x.Split('/');
                    if(parts.Length == 0) continue;
                    for(var i = 1; i <= parts.Length; i++)
                    {
                        var part = parts[..i].Join("/");
                        var info = new PathInfo(part, i - 1);
                        if(!ret.Contains(info)) ret.Add(info);
                    }
                }
            }
            catch(Exception e)
            {
                e.LogInternal();
            }
            return ret;
        }

        public static void ResetCaches()
        {
            P.GlamourerManager.ResetCache();
            P.MoodlesManager.ResetCache();
            P.CustomizePlusManager.ResetCache();
        }

        public static IEnumerable<string> HonorificFiltered(this Preset preset)
        {
            var name = Utils.GetCharaNameFromCID(Player.CID);
            var hlist = P.HonorificManager.GetTitleData(C.HonotificUnfiltered ? null : [Player.CID]);
            foreach(var x in preset.Honorific)
            {
                if(hlist.Any(h => h.Title == x))
                {
                    yield return x;
                }
            }
        }

        public static IEnumerable<string> CustomizeFiltered(this Preset preset)
        {
            var name = Utils.GetCharaNameFromCID(Player.CID);
            var clist = P.CustomizePlusManager.GetProfiles([Player.NameWithWorld]);
            foreach(var x in preset.Customize)
            {
                if(clist.Any(h => h.UniqueId.ToString() == x))
                {
                    yield return x;
                }
            }
        }

        public static void Banner(string id, IEnumerable<string> text, Vector4? colBg = null, Vector4? colText = null)
        {
            if(ImGui.BeginTable($"##TableBanner{id}", 1, ImGuiTableFlags.SizingStretchSame | ImGuiTableFlags.Borders))
            {
                ImGui.TableHeader($"##header");
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.TableSetBgColor(ImGuiTableBgTarget.CellBg, (colBg ?? ImGuiEx.Vector4FromRGBA(0x222222aa)).ToUint());
                var i = 0;
                foreach(var t in text)
                {
                    ImGuiEx.LineCentered($"Banner{id}-{i++}", () => ImGuiEx.Text(colText ?? ImGuiColors.DalamudWhite, t));
                }
                ImGui.EndTable();
            }
        }

        public static bool BannerCombo(string id, string text, Action draw, Vector4? colBg = null)
        {
            if(colBg != null)
            {
                ImGui.PushStyleColor(ImGuiCol.FrameBg, colBg.Value);
                ImGui.PushStyleColor(ImGuiCol.FrameBgActive, colBg.Value);
                ImGui.PushStyleColor(ImGuiCol.FrameBgHovered, colBg.Value);
            }

            var ret = ImGui.BeginCombo($"##{id}", text, C.ComboSize);
            if(ret)
            {
                if(colBg != null) ImGui.PopStyleColor(3);
                draw();
                ImGui.EndCombo();
            }
            else
            {
                if(colBg != null) ImGui.PopStyleColor(3);
            }
            return ret;
        }

        public static string GetCharaNameFromCID(ulong CID)
        {
            if(C.SeenCharacters.TryGetValue(CID, out var name)) return name;
            return $"Unknown character {CID:X16}";
        }

        public static void SetCharacter(this Profile profile, ulong player)
        {
            foreach(var c in C.ProfilesL)
            {
                c.Characters.Remove(player);
            }
            profile.Characters.Add(player);
        }

        public static List<uint> GetCurrentGear()
        {
            var ret = new List<uint>();
            var im = InventoryManager.Instance();
            var cont = im->GetInventoryContainer(InventoryType.EquippedItems);
            for(var i = 0; i < cont->Size; i++)
            {
                var item = cont->GetInventorySlot(i);
                ret.Add((uint)(item->ItemId + (item->Flags.HasFlag(InventoryItem.ItemFlags.HighQuality) ? 1000000 : 0)));
            }
            return ret;
        }

        public static bool GuidEquals(this List<ApplyRule> rule, List<ApplyRule> other)
        {
            //if (rule == null && other == null) return true;
            //if (rule == null || other == null) return false;
            if(rule.Count != other.Count) return false;
            for(var i = 0; i < rule.Count; i++)
            {
                if(rule[i].GUID != other[i].GUID) return false;
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

        public static Profile GetProfileByCID(ulong CID)
        {
            if(CID == 0 || C.Blacklist.Contains(CID)) return null;
            if(C.ProfilesL.TryGetFirst(x => x.Characters.Contains(CID), out var profile))
            {
                return profile;
            }
            return null;
        }

        public static Profile Profile(bool returnMain = false) => GetProfileByCID(Player.CID);

        public static string GetHouseDefaultName()
        {
            var h = HousingManager.Instance();
            return $"{Svc.Data.GetExcelSheet<TerritoryType>().GetRowOrDefault(Svc.ClientState.TerritoryType)?.PlaceNameRegion.ValueNullable?.Name.ExtractText()}, Ward {h->GetCurrentWard() + 1}, plot {h->GetCurrentPlot() + 1}";
        }

        public static GlamourerDesignInfo? GetDesignByGUID(string GUID)
        {
            var designs = P.GlamourerManager.GetDesigns();
            foreach(var d in designs)
            {
                if(d.Identifier.ToString() == GUID) return d;
            }
            return null;
        }

        public static bool TryFindBytes(this byte[] haystack, byte[] needle, out int pos)
        {
            var len = needle.Length;
            var limit = haystack.Length - len;
            for(var i = 0; i <= limit; i++)
            {
                var k = 0;
                for(; k < len; k++)
                {
                    if(needle[k] != haystack[i + k]) break;
                }
                if(k == len)
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
            var territoryType = Svc.Data.GetExcelSheet<TerritoryType>().GetRowOrDefault(id);
            if(territoryType == null) return default;
            try
            {
                var file = Svc.Data.GetFile<LvbFile>($"bg/{territoryType?.Bg}.lvb");
                if(file?.weatherIds == null || file.weatherIds.Length == 0)
                    return (null, null);
                foreach(var weather in file.weatherIds)
                    if(weather > 0 && weather < 255)
                        weathers.Add((byte)weather);
                weathers.Sort();
                return (weathers, file.envbFile);
            }
            catch(Exception e)
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
            if(list.Length == 0) return noneStr;
            if(list.Length == 1) return list[0].ToString();
            FullList = list.Select(x => x.ToString()).Join("\n");
            return $"{list.Length} selected";
        }

        public static string GetName(this MoodleInfo info) => GetName(info, out _);

        public static string GetName(this MoodleInfo info, out bool success)
        {
            success = true;
            foreach(var x in P.MoodlesManager.GetMoodles())
            {
                if(x.ID == info.Guid) return x.FullPath.Split("/")[^1];
            }
            foreach(var x in P.MoodlesManager.GetPresets())
            {
                if(x.ID == info.Guid) return x.FullPath.Split("/")[^1];
            }
            success = false;
            return info.Guid.ToString();
        }

        public static string PrintRange<T>(this IEnumerable<T> s, IEnumerable<T> notS, out string FullList, string noneStr = "Any")
        {
            if(!C.AllowNegativeConditions)
            {
                return PrintRange(s.Select(x => x.ToString().Replace("_", " ")), out FullList, noneStr);
            }
            FullList = null;
            var list = s.Select(x => x.ToString().Replace("_", " ")).ToArray();
            var notList = notS.Select(x => x.ToString().Replace("_", " ")).ToArray();
            if(list.Length == 0 && notList.Length == 0) return noneStr;
            FullList = list.Select(x => x.ToString()).Join("\n");
            if(notList.Length > 0)
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
            for(var i = 0; i < cont->Size; i++)
            {
                ret.Add(cont->GetInventorySlot(i)->ItemId);
            }
            return ret;
        }

        public static bool? WaitUntilInteractable() => Player.Interactable;

        public static PresetFolder GetFolder(this Preset preset, Profile profile)
        {
            foreach(var x in profile.PresetsFolders)
            {
                if(x.Presets.Any(z => z == preset)) return x;
            }
            return null;
        }

        public static bool IsDisguise()
        {
            if(Gui.Debug.ForceDisguise != null) return Gui.Debug.ForceDisguise.Value;
            return Svc.PluginInterface.SourceRepository.ContainsAny(StringComparison.OrdinalIgnoreCase, "SeaOfStars", "DynamicBridgeStandalone");
        }

        public static IEnumerable<string> ToWorldNames(this IEnumerable<uint> worldIds)
        {
            return worldIds.Select(ExcelWorldHelper.GetName);
        }

        public static void UpdateGearsetCache()
        {
            if(Player.Available)
            {
                var list = new List<GearsetEntry>();
                foreach(var x in RaptureGearsetModule.Instance()->Entries)
                {
                    if(x.Name[0] == 0) continue;
                    list.Add(new(x.Id, GenericHelpers.Read(x.Name), x.ClassJob));
                }
                C.GearsetNameCacheCID[Player.CID] = list;
            }
        }

        public static IEnumerable<string> ToGearsetNames(this List<int> gearsetIDs, ulong CID)
        {
            if(!C.GearsetNameCacheCID.TryGetValue(CID, out var cache)) cache = [];
            for(var i = 0; i < gearsetIDs.Count; i++)
            {
                if(cache.TryGetFirst(x => x.Id == gearsetIDs[i], out var ret))
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
