using System.Collections.Generic;
using FishNet.Object;
using UnityEngine;

/// <summary>
/// Static data storage for passing lobby state across scene transitions.
/// Written by LobbyManager (server) before transitioning to the game scene.
/// Read by PlayerSpawner, PlayerManager, and GameModeManager in the game scene.
/// </summary>
public static class LobbyData
{
    public static GameMode ResolvedGameMode = GameMode.FreeForAll;
    public static Dictionary<int, Team> PlayerTeams = new Dictionary<int, Team>();
    public static Dictionary<int, NetworkObject> PlayerCharacters = new();

    public static void Clear()
    {
        ResolvedGameMode = GameMode.FreeForAll;
        PlayerTeams.Clear();
    }

    /// <summary>
    /// Auto-assigns a late joiner to the team with fewer players.
    /// </summary>
    public static Team GetAutoAssignTeam()
    {
        int rebels = 0;
        int ai = 0;
        foreach (var kvp in PlayerTeams)
        {
            if (kvp.Value == Team.Rebels) rebels++;
            else if (kvp.Value == Team.AI) ai++;
        }
        return rebels <= ai ? Team.Rebels : Team.AI;
    }
}
