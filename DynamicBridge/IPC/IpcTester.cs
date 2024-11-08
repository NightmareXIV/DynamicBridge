using Dalamud.Plugin.Ipc.Exceptions;
using ECommons.EzIpcManager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicBridge.IPC;
public class IpcTester
{
    private EzIPCDisposalToken[] Tokens;

    public bool Throw = false;
    public bool ThrowIpcError = false;
    [EzIPC("JustFunction")] public readonly Func<bool> JustFunctionNormalCall;
    [EzIPC("JustFunction", wrapper: SafeWrapper.None)] public Func<bool> JustFunctionNoWrapper;
    [EzIPC("JustFunction", wrapper: SafeWrapper.IPCException)] public Func<bool> JustFunctionIpcException;
    [EzIPC("JustFunction", wrapper: SafeWrapper.Inherit)] public Func<bool> JustFunctionInherit;
    [EzIPC("ThrowsException")] public Func<bool> ThrowsExceptionNormalCall { get; init; }
    [EzIPC("ThrowsException", wrapper: SafeWrapper.None)] public Func<bool> ThrowsExceptionNoWrapper { get; set; }
    [EzIPC("ThrowsException", wrapper: SafeWrapper.IPCException)] public Func<bool> ThrowsExceptionIpcException { get; private set; }
    [EzIPC("ThrowsException", wrapper: SafeWrapper.Inherit)] public Func<bool> ThrowsExceptionInherit;
    [EzIPC("NonExistingFunction")] public Func<bool> NonExistingFunctionNormalCall { get; init; }
    [EzIPC("NonExistingFunction", wrapper: SafeWrapper.None)] public Func<bool> NonExistingFunctionNoWrapper { get; set; }
    [EzIPC("NonExistingFunction", wrapper: SafeWrapper.IPCException)] public Func<bool> NonExistingFunctionIpcException { get; private set; }
    [EzIPC("NonExistingFunction", wrapper: SafeWrapper.Inherit)] public Func<bool> NonExistingFunctionInherit;

    public IpcTester(SafeWrapper w)
    {
        Tokens = EzIPC.Init(this, safeWrapper: w);
        EzIPC.OnSafeInvocationException += HandleException;
    }

    public void HandleException(Exception e) => e.LogInternal();

    public void Unregister()
    {
        EzIPC.OnSafeInvocationException -= HandleException;
        Tokens.Each(x => x.Dispose());
    }

    [EzIPC]
    private bool JustFunction()
    {
        return Random.Shared.Next(2) == 0;
    }

    [EzIPC]
    private bool ThrowsException()
    {
        if(Throw) throw new InvalidOperationException();
        if(ThrowIpcError) throw new IpcNotReadyError("test");
        return Random.Shared.Next(2) == 0;
    }
}
