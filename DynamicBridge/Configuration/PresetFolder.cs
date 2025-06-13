namespace DynamicBridge.Configuration;
public class PresetFolder
{
    [NonSerialized] internal string GUID = Guid.NewGuid().ToString();
    public string Name = "";
    public List<Preset> Presets = [];
    public bool HiddenFromSelection = false;
}
