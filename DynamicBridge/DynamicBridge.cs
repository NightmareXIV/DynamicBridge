﻿using DynamicBridge.Configuration;
using DynamicBridge.Core;
using DynamicBridge.Gui;
using DynamicBridge.IPC;
using DynamicBridge.IPC.Customize;
using DynamicBridge.IPC.Glamourer;
using DynamicBridge.IPC.Honorific;
using DynamicBridge.IPC.Moodles;
using DynamicBridge.IPC.Penumbra;
using ECommons.Automation;
using ECommons.Automation.LegacyTaskManager;
using ECommons.ChatMethods;
using ECommons.Configuration;
using ECommons.Events;
using ECommons.ExcelServices;
using ECommons.EzEventManager;
using ECommons.GameHelpers;
using ECommons.SimpleGui;
using ECommons.Throttlers;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using Glamourer.Api.Enums;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace DynamicBridge;

public unsafe class DynamicBridge : IDalamudPlugin
{
    public static DynamicBridge P;
    public static Config C;
    public AgentMap* AgentMapInst;
    public WeatherManager WeatherManager;
    public List<ApplyRule> LastRule = [];
    public HashSet<Guid> MoodleCleanupQueue = [];
    public bool ForceUpdate = false;
    public bool SoftForceUpdate = false;
    public string MyOldDesign = null;
    public Random Random => Random.Shared;
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
    public PenumbraManager PenumbraManager;
    public MoodlesManager MoodlesManager;
    public IpcTester IpcTester;
    public HonorificManager HonorificManager;

    private DateTime RandomizerTimer;
    private bool RandomizedRecently = false;
    public DynamicBridge(IDalamudPluginInterface pi)
    {
        P = this;
        ECommonsMain.Init(pi, this, Module.DalamudReflector);
        RandomizerTimer = DateTime.UtcNow;
        new TickScheduler(() =>
        {
            C = EzConfig.Init<Config>();
            var ver = GetType().Assembly.GetName().Version.ToString();
            if(C.LastVersion != ver)
            {
                try
                {
                    using(var fs = new FileStream(Path.Combine(Svc.PluginInterface.ConfigDirectory.FullName, $"Backup_{DateTimeOffset.Now.ToUnixTimeMilliseconds()}.zip"), FileMode.Create))
                    using(var arch = new ZipArchive(fs, ZipArchiveMode.Create))
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
            EzCmd.Add("/db", OnCommand, "open the plugin settings\n" +
                "/db apply → reapply rules immediately\n" +
                "/db static <name> → mark preset as static\n" +
                "/db dynamic → cancel static preset and use dynamic rules\n" +
                "/db characterprofile <name> → changes profile of currently active character to provided profile");
            AgentMapInst = AgentMap.Instance();
            WeatherManager = new();
            new EzFrameworkUpdate(OnUpdate);
            new EzLogout(Logout);
            new EzTerritoryChanged(TerritoryChanged);
            new EzStateChanged(StateChanged);
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
            PenumbraManager = new();
            MoodlesManager = new();
            HonorificManager = new();
        });
    }

    private void OnLogin()
    {
        C.SeenCharacters[Player.CID] = Player.NameWithWorld;
        if(C.EnableGlamourer)
        {
            if(P.GlamourerManager.Reflector.GetAutomationGlobalState() && P.GlamourerManager.Reflector.GetAutomationStatusForChara())
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

        LastJob = (uint)Player.Job;
        //LastGS = RaptureGearsetModule.Instance()->CurrentGearsetIndex;

        if(C.RandomChoosenType == RandomTypes.OnLogin && !RandomizedRecently) {
            RandomizedRecently = true;
            Randomizer();
        }
    }
    
    private void Randomizer() {
        var profile = Utils.GetProfileByCID(Player.CID);
        if (profile != null) {
            if (C.StickyPresets && C.Sticky) {
                foreach (var rule in profile.Rules) 
                {
                rule.StickyRandom = Random.Shared.Next(0, rule.SelectedPresets.Count);
            }
            }
            foreach (var preset in profile.Presets) {
                if (C.StickyCustomize && C.Sticky)
                {
                preset.StickyRandomC = Random.Shared.Next(0, preset.CustomizeFiltered().ToArray().Length);
                }
                if (C.StickyGlamourer && C.Sticky)
                {
                preset.StickyRandomG = Random.Shared.Next(0, preset.Glamourer.Count + preset.ComplexGlamourer.Count);
                }
                if (C.StickyHonorific && C.Sticky)
                {
                preset.StickyRandomH = Random.Shared.Next(0, preset.HonorificFiltered().ToArray().Length);
                }
                if (C.StickyPenumbra && C.Sticky)
                {
                preset.StickyRandomP = Random.Shared.Next(0, preset.Penumbra.Count);
            }
        }
        }
        ForceUpdate = C.ForceUpdateOnRandomize && C.Sticky && (C.StickyPresets||C.StickyCustomize||C.StickyGlamourer||C.StickyHonorific||C.StickyPenumbra) && (C.UserInputRandomizerTime >= 0.75);
        RandomizedRecently = false;
    }

    private void OnCommand(string command, string arguments)
    {
        if(arguments.EqualsIgnoreCaseAny("debug"))
        {
            C.Debug = !C.Debug;
        }
        else if(arguments.EqualsIgnoreCaseAny("apply", "a"))
        {
            ForceUpdate = true;
        }
        else if(arguments.StartsWithAny(StringComparison.OrdinalIgnoreCase, "static", "s"))
        {
            Safe(() =>
            {
                var name = arguments[(arguments.IndexOf(" ") + 1)..];
                var profile = Utils.Profile();
                if(profile != null && profile.GetPresetsUnion().TryGetFirst(x => x.Name == name, out var p))
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
        else if(arguments.EqualsIgnoreCaseAny("dynamic", "d"))
        {
            Safe(() =>
            {
                var profile = Utils.Profile();
                if(profile != null)
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
        else if(arguments.StartsWithAny(StringComparison.OrdinalIgnoreCase, "characterprofile", "chp"))
        {
            Safe(() =>
            {
                var name = arguments[(arguments.IndexOf(" ") + 1)..];
                var profile = C.ProfilesL.FirstOrDefault(p => p.Name == name);
                var currentProfile = C.ProfilesL.FirstOrDefault(z => z.Characters.Contains(Player.CID));

                if(profile != null)
                {
                    if(C.SeenCharacters.ContainsKey(Player.CID) && !C.Blacklist.Contains(Player.CID))
                    {
                        if (profile.IsStaticExists() && (currentProfile == null || currentProfile.IsStaticExists()))
                        {
                            P.ForceUpdate = true;
                        }
                        profile.SetCharacter(Player.CID);

                    }
                    else
                    {
                        Notify.Error("Could not find valid Character based on your current Player ID.");
                    }
                }
                else
                {
                    Notify.Error($"Could not find profile {name}.");
                }
            });
        }
        else
        {
            EzConfigGui.Window.IsOpen ^= true;
        }
    }

    private void Logout()
    {
        MyOldDesign = null;
        if(C.EnableCustomize) TaskManager.Enqueue(() => CustomizePlusManager.RestoreState());
        LastJob = 0;
        LastItems = [];
        if(C.EnablePenumbra)
        {
            //PenumbraManager.UnsetAssignmentIfNeeded();
        }
        //LastGS = -1;
    }

    private void OnUpdate()
    {
        if(Player.Interactable)
        {
            if(C.UpdateJobGSChange)
            {
                if(LastJob != Player.Object.ClassJob.RowId)
                {
                    LastJob = Player.Object.ClassJob.RowId;
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
                if(!LastItems.SequenceEqual(items))
                {
                    LastItems = items;
                    P.TaskManager.Enqueue(() => ForceUpdate = true);
                }
            }

            if (!RandomizedRecently && ((DateTime.UtcNow - RandomizerTimer).TotalMinutes >= C.UserInputRandomizerTime) && (C.RandomChoosenType == RandomTypes.Timer))
            {
                RandomizedRecently = true;
                RandomizerTimer = DateTime.UtcNow;
                Randomizer();
            }

            var profile = Utils.GetProfileByCID(Player.CID);
            if(!TaskManager.IsBusy && profile != null)
            {
                List<ApplyRule> newRule = [];
                if(C.Enable)
                {
                    if(profile.IsStaticExists())
                    {
                        newRule = [StaticRule];
                        StaticRule.SelectedPresets = [profile.GetStaticPreset().Name];
                    }
                    else
                    {
                        foreach(var x in profile.Rules)
                        {
                            if (C.Cond_Race)
                            {
                                x.valid = CheckValidRace(profile.Presets, x, "Customize,Race");
                                if (x.Enabled)
                                {
                                    x.Enabled = x.valid;
                                }
                            }
                            if(
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
                                (!C.Cond_House || ((x.Houses.Count == 0 || x.Houses.Contains(HousingManager.Instance()->GetCurrentIndoorHouseId()))
                                && (!C.AllowNegativeConditions || !x.Not.Houses.Contains(HousingManager.Instance()->GetCurrentIndoorHouseId()))))
                                &&
                                (!C.Cond_Emote || ((x.Emotes.Count == 0 || x.Emotes.Contains(Utils.GetAdjustedEmote()))
                                && (!C.AllowNegativeConditions || !x.Not.Emotes.Contains(Utils.GetAdjustedEmote()))))
                                &&
                                (!C.Cond_Job || ((x.Jobs.Count == 0 || x.Jobs.Contains(Player.Job.GetUpgradedJobIfNeeded()))
                                && (!C.AllowNegativeConditions || !x.Not.Jobs.Contains(Player.Job.GetUpgradedJobIfNeeded()))))
                                &&
                                (!C.Cond_Time || ((x.Times.Count == 0 || x.Times.Contains(ETimeChecker.GetEorzeanTimeInterval()))
                                && (!C.AllowNegativeConditions || !x.Not.Times.Contains(ETimeChecker.GetEorzeanTimeInterval()))))
                                &&
                                (!C.Cond_World || ((x.Worlds.Count == 0 || x.Worlds.Contains(Player.Object.CurrentWorld.RowId))
                                && (!C.AllowNegativeConditions || !x.Not.Worlds.Contains(Player.Object.CurrentWorld.RowId))))
                                &&
                                (!C.Cond_Gearset || ((x.Gearsets.Count == 0 || x.Gearsets.Contains(RaptureGearsetModule.Instance()->CurrentGearsetIndex))
                                && (!C.AllowNegativeConditions || !x.Not.Gearsets.Contains(RaptureGearsetModule.Instance()->CurrentGearsetIndex))))
                                &&
                                (!C.Cond_Race || ((x.Races.Count == 0 || x.Races.Any(s => s == (Races)(int)GlamourerManager.GetMyState()["Customize"]["Race"]["Value"]))
                                && (!C.AllowNegativeConditions || !x.Not.Races.Any(s => s == (Races)(int)GlamourerManager.GetMyState()["Customize"]["Race"]["Value"]))))
                                && (!C.Cond_Players || (x.Players.Count == 0 || x.Players.Any(rp => GuiPlayers.SimpleNearbyPlayers().Any(sp => rp == sp.Name && C.selectedPlayers.Any(sel => sel.Name == sp.Name && (sel.Distance >= sp.Distance || sel.Distance >= 150f))))) 
                                && (!C.AllowNegativeConditions || !x.Not.Players.Any(rp => GuiPlayers.SimpleNearbyPlayers().Any(sp => rp == sp.Name && C.selectedPlayers.Any(sel => sel.Name == sp.Name && (sel.Distance >= sp.Distance || sel.Distance >= 150f))))))
                                )
                            {
                                newRule.Add(x);
                                if(!x.Passthrough) break;
                            }
                        }
                    }
                }
                bool DontChangeOnTerritoryChange = C.DontChangeOnTerritoryChange; // true: Don't change if rules are same on territory change, false (defualt): Use old method 
                if(ForceUpdate || !Utils.GuidEquals(newRule, LastRule) || (SoftForceUpdate && newRule.Count > 0 && !DontChangeOnTerritoryChange))
                {
                    PluginLog.Debug($"Old rule: {LastRule.Print()}, new rule: {newRule.Print()} | {Utils.GuidEquals(newRule, LastRule)} | F:{ForceUpdate}");
                    LastRule = newRule;
                    ForceUpdate = false;
                    SoftForceUpdate = false;
                    if(C.EnableMoodles) MoodlesManager.ResetCache();
                    var DoNullGlamourer = true;
                    var DoNullCustomize = true;
                    var DoNullHonorific = true;
                    var DoNullPenumbra = true;
                    HashSet<Guid> moodleCleanup = [];
                    for(var i = 0; i < newRule.Count; i++)
                    {
                        var rule = newRule[i];
                        var isLast = i == newRule.Count - 1;
                        var isFirst = i == 0;
                        if(rule != null && rule.SelectedPresets.Count > 0)
                        {
                            var index = Random.Next(0, rule.SelectedPresets.Count);
                            if (C.StickyPresets && C.Sticky)
                            {
                                index = rule.StickyRandom;
                            }
                            var preset = profile.GetPresetsUnion().FirstOrDefault(s => s.Name == rule.SelectedPresets[index]);
                            if(preset != null)
                            {
                                if(C.EnablePenumbra)
                                {
                                    ApplyPresetPenumbra(preset, ref DoNullPenumbra);
                                }
                                if(C.EnableGlamourer)
                                {
                                    ApplyPresetGlamourer(preset, isFirst, ref DoNullGlamourer);
                                }
                                if(C.EnableHonorific)
                                {
                                    ApplyPresetHonorific(preset, ref DoNullHonorific);
                                }
                                if(C.EnableCustomize)
                                {
                                    ApplyPresetCustomize(preset, ref DoNullCustomize);
                                }
                                if(C.EnableMoodles)
                                {
                                    ApplyPresetMoodles(preset, moodleCleanup);
                                }
                            }
                        }
                    }

                    if(DoNullPenumbra)
                    {
                        ApplyPresetPenumbra(profile.FallbackPreset, ref DoNullPenumbra);
                        if(DoNullPenumbra) NullPenumbra();
                    }
                    if(DoNullGlamourer)
                    {
                        ApplyPresetGlamourer(profile.FallbackPreset, true, ref DoNullGlamourer);
                        if(DoNullGlamourer) NullGlamourer();
                    }
                    if(DoNullCustomize)
                    {
                        ApplyPresetCustomize(profile.FallbackPreset, ref DoNullCustomize);
                        if(DoNullCustomize) NullCustomize();
                    }
                    if(DoNullHonorific)
                    {
                        ApplyPresetHonorific(profile.FallbackPreset, ref DoNullHonorific);
                        if(DoNullHonorific) NullHonorific();
                    }
                    foreach(var x in MoodleCleanupQueue)
                    {
                        if(!moodleCleanup.Contains(x))
                        {
                            if(MoodlesManager.GetMoodles().Any(z => z.ID == x))
                            {
                                MoodlesManager.RemoveMoodle(x);
                            }
                            else if(MoodlesManager.GetPresets().Any(z => z.ID == x))
                            {
                                MoodlesManager.RemovePreset(x);
                            }
                        }
                    }
                    MoodleCleanupQueue = moodleCleanup;

                    void NullPenumbra()
                    {
                        if(!C.EnablePenumbra) return;
                        PenumbraManager.UnsetAssignmentIfNeeded();
                    }

                    void NullHonorific()
                    {
                        if(!C.EnableHonorific) return;
                        TaskManager.Enqueue(Utils.WaitUntilInteractable);
                        if(HonorificManager.WasSet) TaskManager.Enqueue(() => HonorificManager.SetTitle());
                    }

                    void NullCustomize()
                    {
                        if(!C.EnableCustomize) return;
                        TaskManager.Enqueue(Utils.WaitUntilInteractable);
                        TaskManager.Enqueue(() => CustomizePlusManager.RestoreState());
                    }

                    void NullGlamourer()
                    {
                        if(!C.EnableGlamourer) return;
                        if(C.ManageGlamourerAutomation)
                        {
                            TaskManager.Enqueue(() => GlamourerManager.Reflector.SetAutomationGlobalState(true), "GlamourerReflector.SetAutomationGlobalState = true");
                        }
                        if(C.GlamNoRuleBehaviour == GlamourerNoRuleBehavior.StoreRestore)
                        {
                            if(MyOldDesign != null)
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
                        else if(C.GlamNoRuleBehaviour == GlamourerNoRuleBehavior.RevertToAutomation)
                        {
                            if(C.RevertBeforeAutomationRestore)
                            {
                                TaskManager.Enqueue(Utils.WaitUntilInteractable);
                                TaskManager.Enqueue(GlamourerManager.Revert);
                            }
                            TaskManager.Enqueue(Utils.WaitUntilInteractable);
                            TaskManager.Enqueue(GlamourerManager.RevertToAutomation);
                            PluginLog.Debug($"Reverting to automation");
                        }
                        else if(C.GlamNoRuleBehaviour == GlamourerNoRuleBehavior.RevertToNormal)
                        {
                            TaskManager.Enqueue(Utils.WaitUntilInteractable);
                            TaskManager.Enqueue(GlamourerManager.Revert);
                            PluginLog.Debug($"Reverting to game state");
                        }
                    }
                }
            }

            if(Svc.Condition[ConditionFlag.LoggingOut])
            {
                if(EzThrottler.Throttle("LogoutUpdateGS", 30000)) Utils.UpdateGearsetCache();
                if(C.EnablePenumbra) PenumbraManager.UnsetAssignmentIfNeeded();
            }
        }
        else
        {
            if(!Svc.ClientState.IsLoggedIn)
            {
                ForceUpdate = true;
            }
        }
    }

    private void TerritoryChanged(ushort id)
    {
        SoftForceUpdate = true;
    }

    private void StateChanged(nint objectId, StateChangeType changeType)
    {
        // PluginLog.Debug($"STATE CHANGED!!!!! Type: {changeType}");
    }

    private void ApplyPresetPenumbra(Preset preset, ref bool DoNullPenumbra)
    {
        if(preset.PenumbraType == SpecialPenumbraAssignment.Remove_Individual_Assignment)
        {
            PenumbraManager.SetAssignment("");
            DoNullPenumbra = false;
        }
        else if(preset.PenumbraType == SpecialPenumbraAssignment.Use_No_Mods)
        {
            PenumbraManager.SetAssignment("None");
            DoNullPenumbra = false;
        }
        else if(preset.Penumbra.Count > 0)
        {
            int index = Random.Next(preset.Penumbra.Count);
            if (C.StickyPenumbra && C.Sticky) {index = preset.StickyRandomP;}
            var randomAssignment = preset.Penumbra[index];
            PenumbraManager.SetAssignment(randomAssignment);
            DoNullPenumbra = false;
        }
    }

    private void ApplyPresetGlamourer(Preset preset, bool isFirst, ref bool DoNullGlamourer)
    {
        if(preset.Glamourer.Count > 0 || preset.ComplexGlamourer.Count > 0)
        {
            var selectedIndex = Random.Shared.Next(0, preset.Glamourer.Count + preset.ComplexGlamourer.Count);
            if (C.StickyGlamourer && C.Sticky) {selectedIndex = preset.StickyRandomG;}
            var designs = new List<string>();
            if(selectedIndex < preset.Glamourer.Count)
            {
                designs.Add(preset.Glamourer[selectedIndex]);
            }
            else
            {
                var complexEntry = C.ComplexGlamourerEntries.FirstOrDefault(x => x.Name == preset.ComplexGlamourer[selectedIndex - preset.Glamourer.Count]);
                if(complexEntry != null)
                {
                    foreach(var e in complexEntry.Designs)
                    {
                        designs.Add(e);
                    }
                }
            }
            foreach(var name in designs)
            {
                var design = Utils.GetDesignByGUID(name);
                if(design != null)
                {
                    DoNullGlamourer = false;
                    if(isFirst) MyOldDesign ??= GlamourerManager.GetMyCustomization();
                    //TaskManager.DelayNext(60, true);
                    if(isFirst)
                    {
                        if(C.ManageGlamourerAutomation)
                        {
                            TaskManager.Enqueue(() => GlamourerManager.Reflector.SetAutomationGlobalState(false), "GlamourerReflector.SetAutomationGlobalState = false");
                        }
                        if(C.RevertGlamourerBeforeApply)
                        {
                            TaskManager.Enqueue(GlamourerManager.Revert, "Revert character");
                        }
                    }
                    TaskManager.Enqueue(Utils.WaitUntilInteractable);
                    TaskManager.Enqueue(() => GlamourerManager.ApplyToSelf(design.Value), $"ApplyToSelf({design})");
                    PluginLog.Debug($"Applying design {design}");
                }
            }
        }
    }

    private void ApplyPresetHonorific(Preset preset, ref bool DoNullHonorific)
    {
        var hfiltered = preset.HonorificFiltered().ToArray();
        if(hfiltered.Length > 0)
        {
            DoNullHonorific = false;
            var index = Random.Next(hfiltered.Length);
            if (C.StickyHonorific && C.Sticky) {index = preset.StickyRandomH;};
            var randomTitle = hfiltered[index];
            TaskManager.Enqueue(Utils.WaitUntilInteractable);
            TaskManager.Enqueue(() => HonorificManager.SetTitle(randomTitle));
        }
    }

    private void ApplyPresetCustomize(Preset preset, ref bool DoNullCustomize)
    {
        var cfiltered = preset.CustomizeFiltered().ToArray();
        if(cfiltered.Length > 0)
        {
            DoNullCustomize = false;
            var index = Random.Next(cfiltered.Length);
            if (C.StickyCustomize && C.Sticky) {index = preset.StickyRandomC;};
            var randomCusProfile = cfiltered[index];
            TaskManager.Enqueue(Utils.WaitUntilInteractable);
            TaskManager.Enqueue(() => CustomizePlusManager.SetProfile(randomCusProfile, Player.NameWithWorld));
        }
    }

    private void ApplyPresetMoodles(Preset preset, HashSet<Guid> moodleCleanup)
    {
        MoodlesManager.ResetCache();
        foreach(var x in preset.Moodles)
        {
            if(MoodlesManager.GetMoodles().TryGetFirst(z => z.ID == x.Guid, out var m))
            {
                PluginLog.Debug($"Applying Moodle {m}");
                MoodlesManager.ApplyMoodle(x.Guid);
                if(x.Cancel) moodleCleanup.Add(x.Guid);
            }
            else if(MoodlesManager.GetPresets().TryGetFirst(z => z.ID == x.Guid, out var mp))
            {
                PluginLog.Debug($"Applying Moodle preset {mp}");
                MoodlesManager.ApplyPreset(x.Guid);
                if(x.Cancel) moodleCleanup.Add(x.Guid);
            }
        }
    }

    public void Dispose()
    {
        Memory.Dispose();
        ECommonsMain.Dispose();
        P = null;
        C = null;
    }

    private static bool CheckValidRace(List<Preset> presets, ApplyRule rule, string path)
    {
        bool valid = true;
        List<Races> racesToCheck = rule.Races;
        List<Races> notRacesToCheck = rule.Not.Races;
        foreach(string preset_name in rule.SelectedPresets)
        {
            foreach(Preset preset in presets)
            {
                if(preset_name == preset.Name)
                {
                    foreach(string design_name in preset.Glamourer)
                    {
                        var design = (GlamourerDesignInfo)Utils.GetDesignByGUID(design_name);
                        var guid = design.Identifier;
                        string targetFile = Path.Combine(Svc.PluginInterface.ConfigDirectory.Parent!.FullName,"Glamourer","Designs",$"{guid}.json");
                        if (File.Exists(targetFile))
                        {
                            string jsonContent = File.ReadAllText(targetFile);

                            var designObject = Newtonsoft.Json.Linq.JObject.Parse(jsonContent);
                            Newtonsoft.Json.Linq.JToken current = designObject;
                            string[] keys = path.Split(',');
                            foreach (var key in keys)
                            {
                                current = current?[key];
                            }
                            Races design_race = (Races)(int)current["Value"];

                            bool applied = false;

                            foreach (var property in ((Newtonsoft.Json.Linq.JObject)designObject[keys[0]]).Properties())
                            {
                                if(property.Name != "ModelId" && property.Name != "BodyType")
                                {
                                    if ((string)designObject[keys[0]][property.Name]["Apply"] == "True")
                                    {
                                        applied = true;
                                        break;
                                    }
                                }
                            }

                            if (applied && !racesToCheck.Contains(design_race))
                            {
                                valid = false;
                            }
                            if (applied && notRacesToCheck.Contains(design_race))
                            {
                                valid = false;
                            }
                            PluginLog.Information($"Valid: {valid} | Design: {design.Name} | Race: {design_race} | Applied: {applied} | racesToCheck: {racesToCheck.FirstOrNull()},{!racesToCheck.Contains(design_race)} | notRacesToCheck: {notRacesToCheck.FirstOrNull()},{notRacesToCheck.Contains(design_race)} | Guid: {guid}");
                        }
                        else
                        {
                            PluginLog.Warning($"Design file '{guid}.json' not found.");
                        }
                    }
                    break;
                }
            }
        }
        return valid;
    }
}
