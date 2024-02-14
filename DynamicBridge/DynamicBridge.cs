using Dalamud.Interface.Internal;
using Dalamud.Plugin;
using DynamicBridge.Configuration;
using DynamicBridge.Gui;
using DynamicBridge.IPC;
using ECommons.Automation;
using ECommons.Configuration;
using ECommons.ExcelServices;
using ECommons.EzEventManager;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using ECommons.SimpleGui;
using ECommons.Throttlers;
using FFXIVClientStructs.FFXIV.Client.Game.Housing;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using System.Collections.Generic;

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

        public DynamicBridge(DalamudPluginInterface pi)
        {
            P = this;
            ECommonsMain.Init(pi, this, Module.DalamudReflector);
            new TickScheduler(() =>
            {
                C = EzConfig.Init<Config>();
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
            });
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
        }

        private void OnUpdate()
        {
            if (Player.Interactable)
            {
                var profile = Utils.Profile(Player.CID);
                if (!TaskManager.IsBusy && profile != null)
                {
                    List<ApplyRule> newRule = [];
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
                                (x.States.Count == 0 || x.States.Any(s => s.Check())) 
                                && (!C.AllowNegativeConditions || !x.Not.States.Any(s => s.Check()))
                                &&
                                (x.SpecialTerritories.Count == 0 || x.SpecialTerritories.Any(s => s.Check())) 
                                && (!C.AllowNegativeConditions || !x.Not.SpecialTerritories.Any(s => s.Check()))
                                &&
                                (x.Biomes.Count == 0 || x.Biomes.Any(s => s.Check())) 
                                && (!C.AllowNegativeConditions || !x.Not.Biomes.Any(s => s.Check()))
                                &&
                                (x.Weathers.Count == 0 || x.Weathers.Contains(WeatherManager.GetWeather())) 
                                && (!C.AllowNegativeConditions || !x.Not.Weathers.Contains(WeatherManager.GetWeather()))
                                &&
                                (x.Territories.Count == 0 || x.Territories.Contains(Svc.ClientState.TerritoryType)) 
                                && (!C.AllowNegativeConditions || !x.Not.Territories.Contains(Svc.ClientState.TerritoryType))
                                &&
                                (x.Houses.Count == 0 || x.Houses.Contains(HousingManager.Instance()->GetCurrentHouseId())) 
                                && (!C.AllowNegativeConditions || !x.Not.Houses.Contains(HousingManager.Instance()->GetCurrentHouseId()))
                                &&
                                (x.Emotes.Count == 0 || x.Emotes.Contains(Player.Character->EmoteController.EmoteId))
                                && (!C.AllowNegativeConditions || !x.Not.Emotes.Contains(Player.Character->EmoteController.EmoteId))
                                &&
                                (x.Jobs.Count == 0 || x.Jobs.Contains(Player.Job.GetUpgradedJob()))
                                && (!C.AllowNegativeConditions || !x.Not.Jobs.Contains(Player.Job.GetUpgradedJob()))
                                &&
                                (x.Times.Count == 0 || x.Times.Contains(ETimeChecker.GetEorzeanTimeInterval()))
                                && (!C.AllowNegativeConditions || !x.Not.Times.Contains(ETimeChecker.GetEorzeanTimeInterval()))
                                )
                            {
                                newRule.Add(x);
                                if(!x.Passthrough) break;
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
                                                    isNull = false;
                                                    if(isFirst) MyOldDesign ??= GlamourerManager.GetMyCustomization();
                                                    //TaskManager.DelayNext(60, true);
                                                    if (isFirst && C.ManageGlamourerAutomation)
                                                    {
                                                        TaskManager.Enqueue(() => GlamourerReflector.SetAutomationGlobalState(false), "GlamourerReflector.SetAutomationGlobalState = false");
                                                    }
                                                    TaskManager.Enqueue(Utils.WaitUntilInteractable);
                                                    TaskManager.Enqueue(() => design.Value.ApplyToSelf(), $"ApplyToSelf({design})");
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
                                        if (preset.Honorific.Count > 0)
                                        {
                                            DoNullHonorific = false;
                                            var randomTitle = preset.Honorific[Random.Next(preset.Honorific.Count)];
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
                                        if (preset.Customize.Count > 0)
                                        {
                                            DoNullCustomize = false;
                                            var randomCusProfile = preset.Customize[Random.Next(preset.Customize.Count)];
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
                                TaskManager.Enqueue(() => GlamourerReflector.SetAutomationGlobalState(true), "GlamourerReflector.SetAutomationGlobalState = true");
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
                                TaskManager.Enqueue(GlamourerManager.RevertToAutomation);
                                PluginLog.Debug($"Reverting to automation");
                            }
                            else if (C.GlamNoRuleBehaviour == GlamourerNoRuleBehavior.RevertToNormal)
                            {
                                TaskManager.Enqueue(GlamourerManager.Revert);
                                PluginLog.Debug($"Reverting to game state");
                            }
                        }
                    }
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
            ECommonsMain.Dispose();
            P = null; 
            C = null;
        }
    }
}
