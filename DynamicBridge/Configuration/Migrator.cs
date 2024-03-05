using DynamicBridge.IPC.Customize;
using DynamicBridge.IPC.Glamourer;

namespace DynamicBridge.Configuration;
public class Migrator
{
    public Migrator()
    {
        Svc.Framework.Update += DoGlamourerMigration;
        Svc.Framework.Update += DoCustomizeMigration;
    }

    void DoGlamourerMigration(object a)
    {
        if(C.EnableGlamourer)
        {
            var entries = P.GlamourerManager.GetDesigns();
            if (entries.Any())
            {
                PluginLog.Information($"Begin Glamourer name to guid migration");
                MigrateProfile(C.GlobalProfile, entries);
                foreach(var x in C.Profiles)
                {
                    MigrateProfile(x.Value, entries);
                }
                PluginLog.Information($"Finished Glamourer name to guid migration");

                foreach(var x in C.ComplexGlamourerEntries)
                {
                    for (int i = 0; i < x.Designs.Count; i++)
                    {
                        if (!Guid.TryParse(x.Designs[i], out _))
                        {
                            if (entries.TryGetFirst(z => z.Name == x.Designs[i], out var value))
                            {
                                PluginLog.Information($">> Complex Glamourer Entry: changing {x.Designs[i]} -> {value.Identifier}");
                                x.Designs[i] = value.Identifier.ToString();
                            }
                        }
                    }
                }
                Svc.Framework.Update -= DoGlamourerMigration;
            }
        }
    }

    void MigrateProfile(Profile p, GlamourerDesignInfo[] entries)
    {
        try
        {
            foreach (var x in p.GetPresetsUnion())
            {
                for (int i = 0; i < x.Glamourer.Count; i++)
                {
                    if (!Guid.TryParse(x.Glamourer[i], out _))
                    {
                        if (entries.TryGetFirst(z => z.Name == x.Glamourer[i], out var value))
                        {
                            PluginLog.Information($">> Profile {p.Name}: glamourer changing {x.Glamourer[i]} -> {value.Identifier}");
                            x.Glamourer[i] = value.Identifier.ToString();
                        }
                    }
                }
            }
        }
        catch(Exception e)
        {
            e.Log();
        }
    }

    void DoCustomizeMigration(object a)
    {
        if (C.EnableCustomize)
        {
            var entries = CustomizePlusManager.GetProfiles();
            if (entries.Any())
            {
                PluginLog.Information($"Begin Customize+ name to guid migration");
                MigrateProfileCustomize(C.GlobalProfile, entries);
                foreach (var x in C.Profiles)
                {
                    MigrateProfileCustomize(x.Value, entries);
                }
                PluginLog.Information($"Finished Customize+ name to guid migration");
                Svc.Framework.Update -= DoCustomizeMigration;
            }
        }
    }

    void MigrateProfileCustomize(Profile p, CustomizePlusProfile[] entries)
    {
        try
        {
            foreach (var x in p.GetPresetsUnion())
            {
                for (int i = 0; i < x.Customize.Count; i++)
                {
                    if (!Guid.TryParse(x.Customize[i], out _))
                    {
                        if (entries.TryGetFirst(z => z.Name == x.Customize[i], out var value))
                        {
                            PluginLog.Information($">> Profile {p.Name}: customize+ changing {x.Customize[i]} -> {value.ID}");
                            x.Customize[i] = value.ID.ToString();
                        }
                    }
                }
            }
        }
        catch (Exception e)
        {
            e.Log();
        }
    }
}
