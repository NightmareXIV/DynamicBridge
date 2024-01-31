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
        public ApplyRule LastRule = null;
        public bool ForceUpdate = false;
        public bool SoftForceUpdate = false;
        public string MyOldDesign = null;
        public Random Random = new();
        public TaskManager TaskManager;
        public const int DelayMS = 100;
        public static ApplyRule StaticRule = new();

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
                    if (profile != null && profile.Presets.TryGetFirst(x => x.Name == name, out var p))
                    {
                        profile.Presets.Each(x => x.IsStatic = false);
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
                    ApplyRule newRule = null;
                    if (profile.IsStaticExists())
                    {
                        newRule = StaticRule;
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
                                &&
                                (x.SpecialTerritories.Count == 0 || x.SpecialTerritories.Any(s => s.Check()))
                                &&
                                (x.Biomes.Count == 0 || x.Biomes.Any(s => s.Check()))
                                &&
                                (x.Weathers.Count == 0 || x.Weathers.Contains(WeatherManager.GetWeather()))
                                &&
                                (x.Territories.Count == 0 || x.Territories.Contains(Svc.ClientState.TerritoryType))
                                &&
                                (x.Houses.Count == 0 || x.Houses.Contains(HousingManager.Instance()->GetCurrentHouseId()))
                                &&
                                (x.Emotes.Count == 0 || x.Emotes.Contains(Player.Character->EmoteController.EmoteId))
                                &&
                                (x.Jobs.Count == 0 || x.Jobs.Contains(Player.Job.GetUpgradedJob()))
                                &&
                                (x.Times.Count == 0 || x.Times.Contains(ETimeChecker.GetEorzeanTimeInterval()))
                                )
                            {
                                newRule = x;
                                break;
                            }
                        }
                    }
                    if (ForceUpdate || newRule?.GUID != LastRule?.GUID || (SoftForceUpdate && newRule != null))
                    {
                        PluginLog.Debug($"Old rule: {LastRule}, new rule: {newRule} | {newRule?.GUID != LastRule?.GUID} | F:{ForceUpdate}");
                        LastRule = newRule;
                        ForceUpdate = false;
                        SoftForceUpdate = false;
                        if (newRule != null && newRule.SelectedPresets.Count > 0)
                        {
                            var index = Random.Next(0, newRule.SelectedPresets.Count);
                            var preset = profile.Presets.FirstOrDefault(s => s.Name == newRule.SelectedPresets[index]);
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
                                            var design = Utils.GetDesignByName(name);
                                            if (design != null)
                                            {
                                                if(isNull && C.GlamourerResetBeforeApply)
                                                {
                                                    TaskManager.Enqueue(GlamourerManager.Revert);
                                                } 
                                                isNull = false;
                                                MyOldDesign ??= GlamourerManager.GetMyCustomization();
                                                //TaskManager.DelayNext(60, true);
                                                TaskManager.Enqueue(Utils.WaitUntilInteractable);
                                                TaskManager.Enqueue(() => design.Value.ApplyToSelf(), $"ApplyToSelf({design})");
                                                PluginLog.Debug($"Applying design {design}");
                                            }
                                        }
                                        if (isNull)
                                        {
                                            NullGlamourer();
                                            PluginLog.Debug($"Restoring state because design was null");
                                        }
                                    }
                                    else
                                    {
                                        NullGlamourer();
                                        PluginLog.Debug($"Restoring state because design was null");
                                    }
                                }
                                if (C.EnableHonorific)
                                {
                                    if (preset.Honorific.Count > 0)
                                    {
                                        var randomTitle = preset.Honorific[Random.Next(preset.Honorific.Count)];
                                        TaskManager.Enqueue(Utils.WaitUntilInteractable);
                                        TaskManager.Enqueue(() => HonorificManager.SetTitle(randomTitle));
                                    }
                                    else
                                    {
                                        NullHonorific();
                                    }
                                }
                                if (C.EnablePalette)
                                {
                                    if (preset.Palette.Count > 0)
                                    {
                                        var randomPalette = preset.Palette[Random.Next(preset.Palette.Count)];
                                        TaskManager.Enqueue(Utils.WaitUntilInteractable);
                                        TaskManager.Enqueue(() => PalettePlusManager.SetPalette(randomPalette));
                                    }
                                    else
                                    {
                                        NullPalette();
                                    }
                                }
                                if (C.EnableCustomize)
                                {
                                    if (preset.Customize.Count > 0)
                                    {
                                        var randomCusProfile = preset.Customize[Random.Next(preset.Customize.Count)];
                                        TaskManager.Enqueue(Utils.WaitUntilInteractable);
                                        TaskManager.Enqueue(() => CustomizePlusManager.SetProfile(randomCusProfile, Player.Name));
                                    }
                                    else
                                    {
                                        NullCustomize();
                                    }
                                }
                            }
                            else
                            {
                                Null();
                                PluginLog.Debug($"Restoring state because preset was null");
                            }
                        }
                        else
                        {
                            Null();
                            PluginLog.Debug($"Restoring state because no rule was found");
                        }

                        void Null()
                        {
                            NullGlamourer();
                            NullHonorific();
                            NullPalette();
                            NullCustomize();
                        }

                        void NullHonorific()
                        {
                            if (!C.EnableHonorific) return;
                            TaskManager.Enqueue(Utils.WaitUntilInteractable);
                            if (HonorificManager.WasSet) TaskManager.Enqueue(() => HonorificManager.SetTitle());
                        }

                        void NullPalette()
                        {
                            if (!C.EnablePalette) return;
                            TaskManager.Enqueue(Utils.WaitUntilInteractable);
                            if (PalettePlusManager.WasSet) TaskManager.Enqueue(() => PalettePlusManager.RevertPalette());
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
