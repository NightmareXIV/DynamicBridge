using DynamicBridge.Configuration;

namespace DynamicBridge;

/// <summary>
/// Creates a same-named preset for every Glamourer design.
/// </summary>
public static class GlamourerPresetImporter
{
    /// <summary>
    /// Creates a preset for each Glamourer design, named after the design, skipping designs that already have a same-named preset.
    /// Presets are placed into a preset folder whose name matches the design's Glamourer folder when one exists, otherwise into <paramref name="fallbackTarget"/>.
    /// </summary>
    /// <param name="currentProfile">Profile to add presets into.</param>
    /// <param name="fallbackTarget">Preset list to use when no matching preset folder exists (focused folder or main presets).</param>
    public static void Import(Profile currentProfile, List<Preset> fallbackTarget)
    {
        try
        {
            var designs = P.GlamourerManager.GetDesigns();
            if(designs.Length == 0)
            {
                Notify.Warning("No Glamourer designs found");
                return;
            }
            var existingNames = new HashSet<string>(currentProfile.GetPresetsUnion().Select(x => x.Name.Trim()), StringComparer.OrdinalIgnoreCase);
            var added = 0;
            var skipped = 0;
            foreach(var design in designs.OrderBy(x => P.GlamourerManager.TransformName(x.Identifier.ToString())))
            {
                var name = design.Name.Trim();
                if(name.Length == 0) continue;
                var fullPath = P.GlamourerManager.GetFullPath(design.Identifier.ToString());
                if(currentProfile.Pathes.Count > 0 && !fullPath.StartsWithAny(currentProfile.Pathes)) continue;
                if(!existingNames.Add(name))
                {
                    skipped++;
                    continue;
                }
                var target = FindTargetFolder(currentProfile, fullPath) ?? fallbackTarget;
                target.Add(new Preset()
                {
                    Name = name,
                    Glamourer = [design.Identifier.ToString()],
                });
                added++;
            }
            if(added > 0)
            {
                Notify.Success($"Created {added} preset{(added == 1 ? "" : "s")} from Glamourer designs" + (skipped > 0 ? $", skipped {skipped} already existing" : ""));
            }
            else
            {
                Notify.Info(skipped > 0 ? $"All {skipped} Glamourer designs already have a same-named preset" : "No Glamourer designs to import");
            }
        }
        catch(Exception e)
        {
            e.Log();
            Notify.Error(e.Message);
        }
    }

    /// <summary>
    /// Finds a preset folder named after the design's full Glamourer folder path, then its nearest parent folder, then any ancestor.
    /// </summary>
    private static List<Preset> FindTargetFolder(Profile currentProfile, string fullPath)
    {
        var directories = fullPath.SplitDirectories()[0..^1];
        if(directories.Length == 0) return null;
        var candidates = new List<string> { string.Join("/", directories) };
        for(var i = directories.Length - 1; i >= 0; i--) candidates.Add(directories[i]);
        foreach(var candidate in candidates)
        {
            if(currentProfile.PresetsFolders.TryGetFirst(f => f.Name.Trim().Equals(candidate, StringComparison.OrdinalIgnoreCase), out var folder))
            {
                return folder.Presets;
            }
        }
        return null;
    }
}
