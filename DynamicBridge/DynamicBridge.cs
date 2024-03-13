using DynamicBridge.Configuration;
using DynamicBridge.Gui;
using DynamicBridge.IPC.Customize;
using DynamicBridge.IPC.Glamourer;
using DynamicBridge.IPC.Honorific;
using ECommons.Automation;
using ECommons.ChatMethods;
using ECommons.Configuration;
using ECommons.Events;
using ECommons.ExcelServices;
using ECommons.EzEventManager;
using ECommons.GameHelpers;
using ECommons.SimpleGui;
using ECommons.Throttlers;
using FFXIVClientStructs.FFXIV.Client.Game.Housing;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace DynamicBridge
{
    public unsafe class DynamicBridge : IDalamudPlugin
    {
        public static DynamicBridge P;
        public static Config C;
        public AgentMap* AgentMapInst;
        public WeatherManager WeatherManager;
        public List<ApplyRule> LastRule = [];
        public bool ForceUpdate = false;
        public bool SoftForceUpdate = false;
        public string MyOldDesign = null;
        public Random Random = new();
        public TaskManager TaskManager;
        public const int DelayMS = 100;
        public static ApplyRule StaticRule = new();
        public static Migrator Migrator;
        public uint LastJob = 0;
        //public int LastGS = -1;
        public Memory Memory;
        public List<uint> LastItems = [];

        public GlamourerManager GlamourerManager;
        public CustomizePlusManager CustomizePlusManager;

        public DynamicBridge(DalamudPluginInterface pi)
        {
            P = this;
            ECommonsMain.Init(pi, this, Module.DalamudReflector);
            new TickScheduler(() =>
            {
                C = EzConfig.Init<Config>();
                var ver = this.GetType().Assembly.GetName().Version.ToString();
                if(C.LastVersion != ver)
                {
                    try
                    {
                        using (FileStream fs = new FileStream(Path.Combine(Svc.PluginInterface.ConfigDirectory.FullName, $"Backup_{DateTimeOffset.Now.ToUnixTimeMilliseconds()}.zip"), FileMode.Create))
                        using (ZipArchive arch = new ZipArchive(fs, ZipArchiveMode.Create))
                        {
                            arch.CreateEntryFromFile(EzConfig.DefaultConfigurationFileName, EzConfig.DefaultSerializationFactory.DefaultConfigFileName);
                        }
                        C.LastVersion = ver;
                        DuoLog.Information($"Because plugin version was changed, a backup of your current configuraton has been created.");
                    }
                    catch(Exception e)
                    {
                        e.Log();
                    }
                }
                EzConfigGui.Init(UI.DrawMain);
                EzCmd.Add("/db", OnCommand, "open the plugin settings\n/db apply - reapply rules immediately\n/db static <name> - mark preset as static\n/db dynamic - cancel static preset and use dynamic rules");
                AgentMapInst = AgentMap.Instance();
                WeatherManager = new();
                new EzFrameworkUpdate(OnUpdate);
                new EzLogout(Logout);
                new EzTerritoryChanged(TerritoryChanged);
                TaskManager = new()
                {
                    TimeLimitMS = 2000,
                    AbortOnTimeout = true,
                    TimeoutSilently = false,
                };
                Migrator = new();
                GlamourerManager = new();
                CustomizePlusManager = new();
                ProperOnLogin.RegisterInteractable(OnLogin, true);
                Memory = new();
            });
        }

        private void OnLogin()
        {
            C.SeenCharacters[Player.CID] = Player.NameWithWorld;
            if (C.EnableGlamourer)
            {
                if (P.GlamourerManager.Reflector.GetAutomationGlobalState() && P.GlamourerManager.Reflector.GetAutomationStatusForChara())
                {
                    if(C.ManageGlamourerAutomation)
                    {
                        //do nothing
                    }
                    else
                    {
                        ChatPrinter.Orange("[DynamicBridge] Glamourer automation is enabled but DynamicBridge is not configured to work together with it. This will cause issues. Either disable Glamourer automation or configure DynamicBridge accordingly (/db - settings).");
                    }
                }
            }

            Utils.UpdateGearsetCache();

            LastJob = Player.Object.ClassJob.Id;
            //LastGS = RaptureGearsetModule.Instance()->CurrentGearsetIndex;
        }

        private void OnCommand(string command, string arguments)
        {
            if(arguments.EqualsIgnoreCaseAny("debug"))
            {
                C.Debug = !C.Debug;
            }
            else if (arguments.EqualsIgnoreCaseAny("apply", "a"))
            {
                ForceUpdate = true;
            }
            else if (arguments.StartsWithAny(StringComparison.OrdinalIgnoreCase, "static", "s"))
            {
                Safe(() =>
                {
                    var name = arguments[(arguments.IndexOf(" ")+1)..];
                    var profile = Utils.Profile();
                    if (profile != null && profile.GetPresetsUnion().TryGetFirst(x => x.Name == name, out var p))
                    {
                        profile.GetPresetsUnion().Each(x => x.IsStatic = false);
                        p.IsStatic = true;
                        Notify.Success($"{name} was made static.");
                        P.ForceUpdate = true;
                    }
                    else
                    {
                        Notify.Error($"Could not find preset {name}.");
                    }
                });
            }
            else if (arguments.EqualsIgnoreCaseAny("dynamic", "d"))
            {
                Safe(() =>
                {
                    var profile = Utils.Profile();
                    if (profile != null)
                    {
                        profile.Presets.Each(x => x.IsStatic = false);
                        Notify.Success($"Using dynamic rules now.");
                        P.ForceUpdate = true;
                    }
                    else
                    {
                        Notify.Error($"Character blacklisted or not logged in.");
                    }
                });
            }
            else
            {
                EzConfigGui.Window.IsOpen ^= true;
            }
        }

        void Logout()
        {
            MyOldDesign = null;
            if (C.EnableCustomize) TaskManager.Enqueue(() => CustomizePlusManager.RestoreState());
            LastJob = 0;
            LastItems = [];
            //LastGS = -1;
        }

        private void OnUpdate()
        {
            if (Player.Interactable)
            {
                if (C.UpdateJobGSChange)
                {
                    if (LastJob != Player.Object.ClassJob.Id)
                    {
                        LastJob = Player.Object.ClassJob.Id;
                        ForceUpdate = true;
                    }
                    /*if (LastGS != RaptureGearsetModule.Instance()->CurrentGearsetIndex)
                    {
                        LastGS = RaptureGearsetModule.Instance()->CurrentGearsetIndex;
                        ForceUpdate = true;
                    }*/
                }
                if(C.UpdateGearChange)
                {
                    var items = Utils.GetCurrentGear();
                    if (!LastItems.SequenceEqual(items))
                    {
                        LastItems = items;
                        P.TaskManager.Enqueue(() => ForceUpdate = true);
                    }
                }

                var profile = Utils.GetProfileByCID(Player.CID);
                if (!TaskManager.IsBusy && profile != null)
                {
                    List<ApplyRule> newRule = [];
                    if (C.Enable)
                    {
                        if (profile.IsStaticExists())
                        {
                            newRule = [StaticRule];
                            StaticRule.SelectedPresets = [profile.GetStaticPreset().Name];
                        }
                        else
                        {
                            foreach (var x in profile.Rules)
                            {
                                if (
                                    x.Enabled
                                    &&
                                    (!C.Cond_State || ((x.States.Count == 0 || x.States.Any(s => s.Check()))
                                    && (!C.AllowNegativeConditions || !x.Not.States.Any(s => s.Check()))))
                                    &&
                                    (!C.Cond_ZoneGroup || ((x.SpecialTerritories.Count == 0 || x.SpecialTerritories.Any(s => s.Check()))
                                    && (!C.AllowNegativeConditions || !x.Not.SpecialTerritories.Any(s => s.Check()))))
                                    &&
                                    (!C.Cond_Biome || ((x.Biomes.Count == 0 || x.Biomes.Any(s => s.Check()))
                                    && (!C.AllowNegativeConditions || !x.Not.Biomes.Any(s => s.Check()))))
                                    &&
                                    (!C.Cond_Weather || ((x.Weathers.Count == 0 || x.Weathers.Contains(WeatherManager.GetWeather()))
                                    && (!C.AllowNegativeConditions || !x.Not.Weathers.Contains(WeatherManager.GetWeather()))))
                                    &&
                                    (!C.Cond_Zone || ((x.Territories.Count == 0 || x.Territories.Contains(Svc.ClientState.TerritoryType))
                                    && (!C.AllowNegativeConditions || !x.Not.Territories.Contains(Svc.ClientState.TerritoryType))))
                                    &&
                                    (!C.Cond_House || ((x.Houses.Count == 0 || x.Houses.Contains(HousingManager.Instance()->GetCurrentHouseId()))
                                    && (!C.AllowNegativeConditions || !x.Not.Houses.Contains(HousingManager.Instance()->GetCurrentHouseId()))))
                                    &&
                                    (!C.Cond_Emote || ((x.Emotes.Count == 0 || x.Emotes.Contains(Player.Character->EmoteController.EmoteId))
                                    && (!C.AllowNegativeConditions || !x.Not.Emotes.Contains(Player.Character->EmoteController.EmoteId))))
                                    &&
                                    (!C.Cond_Job || ((x.Jobs.Count == 0 || x.Jobs.Contains(Player.Job.GetUpgradedJob()))
                                    && (!C.AllowNegativeConditions || !x.Not.Jobs.Contains(Player.Job.GetUpgradedJob()))))
                                    &&
                                    (!C.Cond_Time || ((x.Times.Count == 0 || x.Times.Contains(ETimeChecker.GetEorzeanTimeInterval()))
                                    && (!C.AllowNegativeConditions || !x.Not.Times.Contains(ETimeChecker.GetEorzeanTimeInterval()))))
                                    &&
                                    (!C.Cond_World || ((x.Worlds.Count == 0 || x.Worlds.Contains(Player.Object.CurrentWorld.Id))
                                    && (!C.AllowNegativeConditions || !x.Not.Worlds.Contains(Player.Object.CurrentWorld.Id))))
                                    &&
                                    (!C.Cond_Gearset || ((x.Gearsets.Count == 0 || x.Gearsets.Contains(RaptureGearsetModule.Instance()->CurrentGearsetIndex))
                                    && (!C.AllowNegativeConditions || !x.Not.Gearsets.Contains(RaptureGearsetModule.Instance()->CurrentGearsetIndex))))
                                    )
                                {
                                    newRule.Add(x);
                                    if (!x.Passthrough) break;
                                }
                            }
                        }
                    }
                    if (ForceUpdate || !Utils.GuidEquals(newRule, LastRule) || (SoftForceUpdate && newRule.Count > 0))
                    {
                        PluginLog.Debug($"Old rule: {LastRule.Print()}, new rule: {newRule.Print()} | {Utils.GuidEquals(newRule, LastRule)} | F:{ForceUpdate}");
                        LastRule = newRule;
                        ForceUpdate = false;
                        SoftForceUpdate = false;
                        var DoNullGlamourer = true;
                        var DoNullCustomize = true;
                        var DoNullHonorific = true;
                        for (int i = 0; i < newRule.Count; i++)
                        {
                            var rule = newRule[i];
                            var isLast = i == newRule.Count - 1;
                            var isFirst = i == 0;
                            if (rule != null && rule.SelectedPresets.Count > 0)
                            {
                                var index = Random.Next(0, rule.SelectedPresets.Count);
                                var preset = profile.GetPresetsUnion().FirstOrDefault(s => s.Name == rule.SelectedPresets[index]);
                                if (preset != null)
                                {
                                    if (C.EnableGlamourer)
                                    {
                                        if (preset.Glamourer.Count > 0 || preset.ComplexGlamourer.Count > 0)
                                        {
                                            var selectedIndex = Random.Next(0, preset.Glamourer.Count + preset.ComplexGlamourer.Count);
                                            var designs = new List<string>();
                                            if (selectedIndex < preset.Glamourer.Count)
                                            {
                                                designs.Add(preset.Glamourer[selectedIndex]);
                                            }
                                            else
                                            {
                                                var complexEntry = C.ComplexGlamourerEntries.FirstOrDefault(x => x.Name == preset.ComplexGlamourer[selectedIndex - preset.Glamourer.Count]);
                                                if (complexEntry != null)
                                                {
                                                    foreach (var e in complexEntry.Designs)
                                                    {
                                                        designs.Add(e);
                                                    }
                                                }
                                            }
                                            var isNull = true;
                                            foreach (var name in designs)
                                            {
                                                var design = Utils.GetDesignByGUID(name);
                                                if (design != null)
                                                {
                                                    DoNullGlamourer = false;
                                                    if(isFirst) MyOldDesign ??= GlamourerManager.GetMyCustomization();
                                                    //TaskManager.DelayNext(60, true);
                                                    if (isFirst)
                                                    {
                                                        if (C.ManageGlamourerAutomation)
                                                        {
                                                            TaskManager.Enqueue(() => GlamourerManager.Reflector.SetAutomationGlobalState(false), "GlamourerReflector.SetAutomationGlobalState = false");
                                                        }
                                                        if (C.RevertGlamourerBeforeApply)
                                                        {
                                                            TaskManager.Enqueue(GlamourerManager.Revert, "Revert character");
                                                        }
                                                    }
                                                    isNull = false;
                                                    TaskManager.Enqueue(Utils.WaitUntilInteractable);
                                                    TaskManager.Enqueue(() => GlamourerManager.ApplyToSelf(design.Value), $"ApplyToSelf({design})");
                                                    PluginLog.Debug($"Applying design {design}");
                                                }
                                            }
                                            /*if (isNull)
                                            {
                                                NullGlamourer();
                                                PluginLog.Debug($"Restoring state because design was null");
                                            }*/
                                        }
                                        /*else
                                        {
                                            NullGlamourer();
                                            PluginLog.Debug($"Restoring state because design was null");
                                        }*/
                                    }
                                    if (C.EnableHonorific)
                                    {
                                        var hfiltered = preset.HonorificFiltered().ToArray();
                                        if (hfiltered.Length > 0)
                                        {
                                            DoNullHonorific = false;
                                            var randomTitle = hfiltered[Random.Next(hfiltered.Length)];
                                            TaskManager.Enqueue(Utils.WaitUntilInteractable);
                                            TaskManager.Enqueue(() => HonorificManager.SetTitle(randomTitle));
                                        }
                                        /*else
                                        {
                                            NullHonorific();
                                        }*/
                                    }
                                    if (C.EnableCustomize)
                                    {
                                        var cfiltered = preset.CustomizeFiltered().ToArray();
                                        if (cfiltered.Length > 0)
                                        {
                                            DoNullCustomize = false;
                                            var randomCusProfile = cfiltered[Random.Next(cfiltered.Length)];
                                            TaskManager.Enqueue(Utils.WaitUntilInteractable);
                                            TaskManager.Enqueue(() => CustomizePlusManager.SetProfile(randomCusProfile, Player.Name));
                                        }
                                        /*else
                                        {
                                            NullCustomize();
                                        }*/
                                    }
                                }
                                /*else
                                {
                                    Null();
                                    PluginLog.Debug($"Restoring state because preset was null");
                                }*/
                            }
                            /*else
                            {
                                Null();
                                PluginLog.Debug($"Restoring state because no rule was found");
                            }*/
                        }

                        if (DoNullGlamourer) NullGlamourer();
                        if (DoNullCustomize) NullCustomize();
                        if (DoNullHonorific) NullHonorific();

                        void Null()
                        {
                            NullGlamourer();
                            NullHonorific();
                            NullCustomize();
                        }

                        void NullHonorific()
                        {
                            if (!C.EnableHonorific) return;
                            TaskManager.Enqueue(Utils.WaitUntilInteractable);
                            if (HonorificManager.WasSet) TaskManager.Enqueue(() => HonorificManager.SetTitle());
                        }

                        void NullCustomize()
                        {
                            if (!C.EnableCustomize) return;
                            TaskManager.Enqueue(Utils.WaitUntilInteractable);
                            TaskManager.Enqueue(() => CustomizePlusManager.RestoreState());
                        }

                        void NullGlamourer()
                        {
                            if (!C.EnableGlamourer) return;
                            if (C.ManageGlamourerAutomation)
                            {
                                TaskManager.Enqueue(() => GlamourerManager.Reflector.SetAutomationGlobalState(true), "GlamourerReflector.SetAutomationGlobalState = true");
                            }
                            if (C.GlamNoRuleBehaviour == GlamourerNoRuleBehavior.StoreRestore)
                            {
                                if (MyOldDesign != null)
                                {
                                    //TaskManager.DelayNext(DelayMS);
                                    TaskManager.Enqueue(Utils.WaitUntilInteractable);
                                    TaskManager.Enqueue(() =>
                                    {
                                        GlamourerManager.SetMyCustomization(MyOldDesign);
                                        MyOldDesign = null;
                                    }, "Set my customization to old design");
                                    PluginLog.Debug($"Saved design found, restoring");
                                }
                                else
                                {
                                    //TaskManager.DelayNext(DelayMS);
                                    TaskManager.Enqueue(Utils.WaitUntilInteractable);
                                    TaskManager.Enqueue(GlamourerManager.Revert);
                                    PluginLog.Debug($"No saved design found, reverting");
                                }
                            }
                            else if (C.GlamNoRuleBehaviour == GlamourerNoRuleBehavior.RevertToAutomation)
                            {
                                if (C.RevertBeforeAutomationRestore)
                                {
                                    TaskManager.Enqueue(Utils.WaitUntilInteractable);
                                    TaskManager.Enqueue(GlamourerManager.Revert);
                                }
                                TaskManager.Enqueue(Utils.WaitUntilInteractable);
                                TaskManager.Enqueue(GlamourerManager.RevertToAutomation);
                                PluginLog.Debug($"Reverting to automation");
                            }
                            else if (C.GlamNoRuleBehaviour == GlamourerNoRuleBehavior.RevertToNormal)
                            {
                                TaskManager.Enqueue(Utils.WaitUntilInteractable);
                                TaskManager.Enqueue(GlamourerManager.Revert);
                                PluginLog.Debug($"Reverting to game state");
                            }
                        }
                    }
                }

                if (Svc.Condition[ConditionFlag.LoggingOut])
                {
                    if (EzThrottler.Throttle("LogoutUpdateGS", 30000)) Utils.UpdateGearsetCache();
                }
            }
            else
            {
                if (!Svc.ClientState.IsLoggedIn)
                {
                    ForceUpdate = true;
                }
            }
        }

        void TerritoryChanged(ushort id)
        {
            SoftForceUpdate = true;
        }

        public void Dispose()
        {
            Memory.Dispose();
            ECommonsMain.Dispose();
            P = null; 
            C = null;
        }
    }
}
