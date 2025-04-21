using Dalamud.Game.ClientState.Objects.Enums; //For IPlayerCharacter
using Dalamud.Game.ClientState.Objects.SubKinds;
using ECommons.GameHelpers;
using System.Text.RegularExpressions;


namespace DynamicBridge.Gui;
public class GuiPlayers
{
    internal static IEnumerable<IPlayerCharacter> GetNearbyPlayers()
    {
        if(Svc.Objects == null)
        {
            PluginLog.LogError("ObjectTable is null!");
            return Enumerable.Empty<IPlayerCharacter>(); // Return an empty collection to prevent crashes
        }

        return Svc.Objects
            .Where(x => x != null && x.IsValid()) // Ensure x is not null before calling IsValid()
            .OfType<IPlayerCharacter>()
            .Where(x => x.ObjectIndex < 240);
    }

    private static int GetFlagPriority(StatusFlags flag)
    {
        if(flag.HasFlag(StatusFlags.Friend)) return 1;
        if(flag.HasFlag(StatusFlags.PartyMember)) return 2;
        if(flag.HasFlag(StatusFlags.AllianceMember)) return 3;
        return 999;
    }

    public static List<(string Name, int Priority, float Distance)> SimpleNearbyPlayers()
    {
        List<(string Name, int Priority, float Distance)> result = [];

        var players = GetNearbyPlayers()
            .OrderBy(x => x.Name.TextValue, StringComparer.OrdinalIgnoreCase);

        var you = players.FirstOrDefault(x => x.GameObjectId == Svc.ClientState.LocalPlayer?.GameObjectId);

        foreach(var player in players)
        {
            if(player.GameObjectId == Svc.ClientState.LocalPlayer?.GameObjectId) continue;
            var priority = GetFlagPriority(player.StatusFlags);
            var distance = Vector3.Distance(you.Position, player.Position);
            result.Add((player.GetNameWithWorld(), priority, distance)); // Tuple with Name & Priority
        }

        return result;
    }

    private static string newPlayerName = "";
    private static string errorMessage = "";
    private static int orderPrio = 1;
    private static void ValidatePlayerName()
    {
        // Regular Expression for Validation
        var pattern = @"^[A-Za-z'-]+ [A-Za-z'-]+@[A-Za-z'-]+$";
        if(Regex.IsMatch(newPlayerName, pattern) || string.IsNullOrWhiteSpace(newPlayerName))
        {
            errorMessage = ""; // Valid input, clear error message
            if(C.selectedPlayers.Any(p => p.Name == newPlayerName))
            {
                errorMessage = "Name already in Saved Names";
            }
        }
        else
        {
            errorMessage = "Invalid format. Use: FirstName LastName@HomeWorld";
        }
    }

    public static void Draw()
    {
        ImGui.Text("Add Player:");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - 120f); // Input field takes most of the width
        if(ImGui.InputTextWithHint("##newPlayer", "FirstName LastName@HomeWorld", ref newPlayerName, 80, ImGuiInputTextFlags.EnterReturnsTrue))
        {
            // If valid and Enter is pressed, add to the list
            if(string.IsNullOrEmpty(errorMessage) && !string.IsNullOrWhiteSpace(newPlayerName) && !C.selectedPlayers.Any(p => p.Name == newPlayerName))
            {
                C.selectedPlayers.Add((newPlayerName, 150f));
                newPlayerName = ""; // Clear input after adding
            }
        }
        ValidatePlayerName();
        // Show error message if validation fails
        if(!string.IsNullOrEmpty(errorMessage))
        {
            ImGui.TextColored(new Vector4(1f, 0f, 0f, 1f), errorMessage); // Red text
        }
        else
        {
            ImGui.SameLine();
            if(ImGui.Button("Add##CustomPlayer"))
            {
                PluginLog.Information($"Adding {newPlayerName} to list");
                if(!string.IsNullOrWhiteSpace(newPlayerName) && !C.selectedPlayers.Any(p => p.Name == newPlayerName))
                {
                    C.selectedPlayers.Add((newPlayerName, 150f));
                    newPlayerName = ""; // Clear input after adding
                }
            }
            ImGui.Spacing();
        }

        var totalWidth = ImGui.GetContentRegionAvail().X;
        var secondColumnWidth = totalWidth * 0.2f;

        ImGui.Columns(2, "SplitColumns", true);
        ImGui.SetColumnWidth(0, totalWidth - secondColumnWidth);
        var nearbyPlayers = SimpleNearbyPlayers();

        List<(string Name, float Distance)> updatedPlayers = [];
        List<string> removedPlayers = [];

        if(ImGui.BeginTable("PlayerTable", 4,
            ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders | ImGuiTableFlags.SizingStretchSame))
        {
            // Column Setup
            ImGui.TableSetupColumn("", ImGuiTableColumnFlags.WidthFixed, 80f); // Remove Button (Fixed Width)
            ImGui.TableSetupColumn("Player"); // Auto-size based on longest name
            ImGui.TableSetupColumn("Max Distance to Apply Rules", ImGuiTableColumnFlags.WidthStretch); // Stretches to fill remaining space
            ImGui.TableSetupColumn("Current Distance"); // Auto-size based on content
            ImGui.TableHeadersRow();

            if(C.selectedPlayers.Count > 0)
            {
                foreach(var playerData in C.selectedPlayers)
                {
                    var playerFullName = playerData.Name;
                    var ruleDistance = playerData.Distance;

                    // Find current distance from nearbyPlayers
                    var nearbyPlayer = nearbyPlayers.FirstOrDefault(p => p.Name == playerFullName);
                    var currentDistance = nearbyPlayer != default ? nearbyPlayer.Distance : -1f;
                    var isNearby = currentDistance >= 0;

                    // Row Coloring (Green if currentDistance < ruleDistance)
                    if(isNearby && currentDistance < ruleDistance)
                    {
                        ImGui.TableNextRow(ImGuiTableRowFlags.None, 0);
                        ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg0,
                            ImGui.ColorConvertFloat4ToU32(new Vector4(0.2f, 0.6f, 0.2f, 0.5f))); // Light Green
                    }
                    else
                    {
                        ImGui.TableNextRow();
                    }

                    // Column 0: Remove Button (Fixed Size)
                    ImGui.TableSetColumnIndex(0);
                    if(ImGui.Button($"Remove##{playerFullName}"))
                    {
                        removedPlayers.Add(playerFullName);
                    }

                    // Column 1: Player Name (Auto-width)
                    ImGui.TableSetColumnIndex(1);
                    ImGui.Text(playerFullName);

                    // Column 2: Rule Distance (Slider, Stretchable)
                    ImGui.TableSetColumnIndex(2);
                    ImGuiEx.SetNextItemFullWidth();
                    if(ImGui.SliderFloat($"##distance_{playerFullName}", ref ruleDistance, 0f, 150f, "%.1f"))
                    {
                        updatedPlayers.Add((playerFullName, ruleDistance)); // Store the update separately
                    }

                    // Column 3: Current Distance (Auto-width)
                    ImGui.TableSetColumnIndex(3);
                    ImGui.Text(isNearby ? $"{currentDistance:F1}" : "Not Loaded");
                }
            }

            ImGui.EndTable();

            // Apply updates AFTER iteration to avoid modifying the list while looping
            foreach(var updatedPlayer in updatedPlayers)
            {
                var index = C.selectedPlayers.FindIndex(p => p.Name == updatedPlayer.Name);
                if(index != -1)
                {
                    C.selectedPlayers[index] = updatedPlayer;
                }
            }

            var removedPlayerNames = new HashSet<string>(removedPlayers);
            C.selectedPlayers = C.selectedPlayers.Where(p => !removedPlayerNames.Contains(p.Name)).ToList();
        }


        ImGui.NextColumn();

        // Right Column - Nearby Players List
        ImGui.Text("Nearby Players");

        // Filter nearby players based on input
        var filteredPlayers = string.IsNullOrEmpty(newPlayerName)
            ? nearbyPlayers
            : nearbyPlayers.Where(p => p.Name.Contains(newPlayerName, StringComparison.OrdinalIgnoreCase)).ToList();

        if(orderPrio == 0)
        {
            if(ImGui.Button("Order by Name"))
            {
                filteredPlayers.OrderBy(x => x.Name);
                orderPrio = 1;
            }
        }
        if(orderPrio == 1)
        {
            if(ImGui.Button("Order by Friends/Party"))
            {
                filteredPlayers.OrderBy(x => x.Priority).ThenBy(x => x.Name);
                orderPrio = 2;
            }
        }
        if(orderPrio == 2)
        {
            if(ImGui.Button("Order by Distance"))
            {
                filteredPlayers.OrderBy(x => x.Distance);
                orderPrio = 0;
            }
        }
        foreach(var player in filteredPlayers)
        {
            if(!C.selectedPlayers.Any(p => p.Name == player.Name))
            {
                if(ImGui.Button($"Add##{player.Name}"))
                {
                    C.selectedPlayers.Add((player.Name, 150f));
                }
                ImGui.SameLine();
                ImGui.Text(player.Name);
            }
        }

        ImGui.Columns(1);
    }
}

