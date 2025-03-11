using ECommons.DalamudServices;
using Glamourer.Api.Enums;
using Glamourer.Api.Helpers;
using Glamourer.Api.IpcSubscribers;
using SharpDX.Win32;
using System;
using System.Collections.Generic;

//I have no idea what I'm doing, I'm just stealing code from everwhere.... But either way, I don't think this is actually very useful. Too many Customize changes.
public class EzStateChanged : IDisposable
{
    internal static List<EzStateChanged> Registered = [];
    internal EventSubscriber<nint, StateChangeType> Subscriber;

    public EzStateChanged(Action<nint, StateChangeType> handler)
    {
        if (handler == null) throw new ArgumentNullException(nameof(handler));
        // Create the subscriber for StateChangedWithType
        Subscriber = StateChangedWithType.Subscriber(Svc.PluginInterface, handler);

        // Register instance
        Registered.Add(this);
    }

    public void Dispose()
    {
        Subscriber.Dispose();
        Registered.Remove(this);
    }
}
