namespace DynamicBridge.Configuration;

public class Profile
{
    [NonSerialized] internal string GUID = Guid.NewGuid().ToString();
    public string Name = "";
    public List<ApplyRule> Rules = [];
    public List<ApplyRuleFolder> RulesFolders = [];
    public List<Preset> Presets = [];
    public List<PresetFolder> PresetsFolders = [];
    public string ForcedPreset = null;
    public List<Profile> Subprofiles = [];
    public HashSet<ulong> Characters = [];
    public List<string> Pathes = [];
    public List<string> CustomizePathes = [];
    public List<string> MoodlesPathes = [];
    public Preset FallbackPreset = new();

    internal bool IsGlobal => C.GlobalProfile == this;

    internal string CensoredName => C.NoNames ? GUID : Name;

    public IEnumerable<Preset> GetPresetsUnion(bool includeGlobal = true)
    {
        foreach(var x in GetPresetsListUnion(includeGlobal))
        {
            for(var i = 0; i < x.Count; i++)
            {
                var z = x[i];
                yield return z;
            }
        }
    }

    public IEnumerable<List<Preset>> GetPresetsListUnion(bool includeGlobal = true)
    {
        yield return Presets;
        foreach(var x in PresetsFolders) yield return x.Presets;
        if(!IsGlobal && includeGlobal)
        {
            yield return C.GlobalProfile.Presets;
            for(var i = 0; i < C.GlobalProfile.PresetsFolders.Count; i++)
            {
                var x = C.GlobalProfile.PresetsFolders[i];
                yield return x.Presets;
            }
        }
    }

    public IEnumerable<ApplyRule> GetRulesUnion(bool onlyEnabled = false)
    {
        foreach(var x in GetRulesListUnion(onlyEnabled))
        {
            for(var i = 0; i < x.Count; i++)
            {
                var z = x[i];
                yield return z;
            }
        }
    }

    public IEnumerable<List<ApplyRule>> GetRulesListUnion(bool onlyEnabled = false)
    {
        yield return Rules;
        for(var i = 0; i < RulesFolders.Count; i++)
        {
            var x = RulesFolders[i];
            if(!onlyEnabled || x.Enabled)
            {
                yield return x.Rules;
            }
        }
    }
}
