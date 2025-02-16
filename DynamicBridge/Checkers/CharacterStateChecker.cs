using ECommons.GameHelpers;
using ECommons.Throttlers;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicBridge.Core
{
    public static class CharacterStateChecker
    {
        private static readonly Dictionary<CharacterState, Func<bool>> States = new()
        {
            [CharacterState.Swimming] = () => Svc.Condition[ConditionFlag.Swimming] && Utils.IsMoving,
            [CharacterState.Floating] = () => Svc.Condition[ConditionFlag.Swimming] && !Utils.IsMoving,
            [CharacterState.Mounted_on_the_ground] = () => Svc.Condition[ConditionFlag.Mounted] && !Svc.Condition[ConditionFlag.InFlight] && !Svc.Condition[ConditionFlag.Diving],
            [CharacterState.Flying_underwater] = () => Svc.Condition[ConditionFlag.Mounted] && Svc.Condition[ConditionFlag.Diving],
            [CharacterState.Flying_in_the_air] = () => Svc.Condition[ConditionFlag.Mounted] && Svc.Condition[ConditionFlag.InFlight],
            [CharacterState.Diving] = () => Svc.Condition[ConditionFlag.Diving] && !Svc.Condition[ConditionFlag.Mounted],
            [CharacterState.Wading_in_water] = () => !Svc.Condition[ConditionFlag.Diving] && !Svc.Condition[ConditionFlag.Swimming] && Utils.IsInWater,
            [CharacterState.Watching_cutscene] = () => Svc.Condition[ConditionFlag.OccupiedInCutSceneEvent]
                || Svc.Condition[ConditionFlag.WatchingCutscene78],
            [CharacterState.In_combat] = () => Svc.Condition[ConditionFlag.InCombat],
            [CharacterState.Dead] = () => Player.Available && Player.Object.IsDead,
            [CharacterState.Crafting] = () => Svc.Condition[ConditionFlag.Crafting]
        };

        public static bool Check(this CharacterState state)
        {
            if(States.TryGetValue(state, out var func))
            {
                return func();
            }
            if(EzThrottler.Throttle("ErrorReport", 10000)) DuoLog.Error($"Cound not find checker for state {state}. Please report this error with logs.");
            return false;
        }
    }
}
