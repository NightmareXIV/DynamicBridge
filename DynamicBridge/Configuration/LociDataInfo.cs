namespace DynamicBridge.Configuration;

// Possibly add more here later.
public class LociDataInfo
{
    public Guid Guid;
    public bool Cancel = false;

    public LociDataInfo(Guid guid, bool cancel)
    {
        Guid = guid;
        Cancel = cancel;
    }
}
