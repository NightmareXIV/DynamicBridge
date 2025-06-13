namespace DynamicBridge.IPC.Glamourer;
public class GlamourerCommands
{
    public void ApplyByGuid(Guid guid)
    {
        Svc.Commands.ProcessCommand($"/glamour apply {guid}|<me>");
    }

    public void Revert()
    {
        Svc.Commands.ProcessCommand("/glamour revert <me>");
    }

    public void RevertToAutomation()
    {
        Svc.Commands.ProcessCommand("/glamour reapplyautomation <me>");
    }
}
