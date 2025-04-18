using FFXIVClientStructs.FFXIV.Client.Game;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicBridge.Core;
public static unsafe class ETimeChecker
{
    internal static long* ET = &CSFramework.Instance()->ClientTime.EorzeaTime;

    public static readonly Dictionary<ETime, string> Names = new()
    {
        [ETime.Day] = "12pm - 5pm",
        [ETime.Night] = "10pm - 5am",
        [ETime.Dawn] = "5am - 7am",
        [ETime.Dusk] = "5pm - 7pm",
        [ETime.Morning] = "7am - 12pm",
        [ETime.Evening] = "7pm - 10pm",
    };

    public static ETime GetEorzeanTimeInterval() => GetTimeInterval(*ET);

    public static ETime GetTimeInterval(long time)
    {
        var date = DateTimeOffset.FromUnixTimeSeconds(time);
        if(date.Hour < 5) return ETime.Night;
        if(date.Hour < 7) return ETime.Dawn;
        if(date.Hour < 12) return ETime.Morning;
        if(date.Hour < 17) return ETime.Day;
        if(date.Hour < 19) return ETime.Dusk;
        if(date.Hour < 22) return ETime.Evening;
        return ETime.Night;
    }

    public static float GetEorzeanTime() => GetTime(*ET);
    public static float GetTime(long time)
    {
        var date = DateTimeOffset.FromUnixTimeSeconds(time);
        // PluginLog.Information(((date.Hour*60+date.Minute)/(float)(24*60)).ToString());
        return (date.Hour * 60 + date.Minute) / (float)(24 * 60);
    }
}
