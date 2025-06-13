namespace DynamicBridge;

[Serializable]
public class HousingRecord
{
    [NonSerialized] internal string GUID = Guid.NewGuid().ToString();
    public long ID;
    public string Name;
}
