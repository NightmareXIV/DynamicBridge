using Dalamud.Utility.Signatures;
using ECommons.EzHookManager;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicBridge;
public unsafe class Memory : IDisposable
{
    public delegate nint RaptureGearsetModule_EquipGearsetInternal(nint a1, uint a2, byte a3);
    [EzHook("40 55 53 56 57 41 57 48 8D AC 24 ?? ?? ?? ?? 48 81 EC ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 85 ?? ?? ?? ?? 4C 63 FA", false)]
    public EzHook<RaptureGearsetModule_EquipGearsetInternal> EquipGearsetHook;

    //public byte* IsLPInWater;

    public Memory()
    {
        EzSignatureHelper.Initialize(this);
        var addr = Svc.SigScanner.GetStaticAddressFromSig("F6 05 ?? ?? ?? ?? ?? 74 19");
        //IsLPInWater = (byte*)(addr + 2);
        if(C.UpdateJobGSChange)
        {
            EquipGearsetHook.Enable();
        }
    }

    private nint EquipGearsetDetour(nint a1, uint a2, byte a3)
    {
        try
        {
            P.ForceUpdate = true;
            InternalLog.Information($"Gearset equip: {a2}/{a3}");
        }
        catch(Exception e)
        {
            e.Log();
        }
        return EquipGearsetHook.Original(a1, a2, a3);
    }

    public void Dispose()
    {
        //...
    }
}
