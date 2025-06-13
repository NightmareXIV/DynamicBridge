namespace DynamicBridge.Configuration;
public class MoodleInfo
{
    public Guid Guid;
    public bool Cancel = false;

    public MoodleInfo(Guid guid, bool cancel)
    {
        Guid = guid;
        Cancel = cancel;
    }
}
