using DynamicBridge.IPC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicBridge.Configuration;
public class Migrator
{
    public Migrator()
    {
        Svc.Framework.Update += DoGlamourerMigration;
    }

    void DoGlamourerMigration(object a)
    {
        if(C.EnableGlamourer)
        {
            var entries = GlamourerManager.GetDesigns();
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

    void MigrateProfile(Profile p, DesignListEntry[] entries)
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
                            PluginLog.Information($">> Profile {p.Name}: changing {x.Glamourer[i]} -> {value.Identifier}");
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
}
