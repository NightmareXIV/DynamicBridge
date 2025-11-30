using Lumina.Excel.Sheets;

namespace DynamicBridge;

public class OnlineStatusManager
{
    public Dictionary<uint, string> OnlineStatuses = [];
    public Dictionary<uint, uint> IconOverrides = []; // Maps status ID to icon source ID

    // Blacklist statuses that shouldn't appear in the dropdown
    private static readonly HashSet<uint> BlacklistedStatuses = [1,2,3,4,5,6,7,8,9,10,19,20,24,28,40,41,42,44,45,46,47];

    public OnlineStatusManager()
    {
        // Add status 0 as "Online" and map it to use icon from status 47
        OnlineStatuses[0] = "Online";
        IconOverrides[0] = 47;

        foreach(var x in Svc.Data.GetExcelSheet<OnlineStatus>())
        {
            if(x.RowId == 0) continue;
            if(BlacklistedStatuses.Contains(x.RowId)) continue;
            var name = x.Name.ExtractText();
            if(!name.IsNullOrEmpty())
            {
                OnlineStatuses[x.RowId] = name;
            }
        }
    }
}
