namespace DynamicBridge.Configuration;
[Serializable]
public class ComplexGlamourerEntry
{
    [NonSerialized] internal string GUID = Guid.NewGuid().ToString();
    public string Name = "";
    public List<string> Designs = [];
}
