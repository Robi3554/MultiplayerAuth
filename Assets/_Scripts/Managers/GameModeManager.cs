using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Connection;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Collections;

public class GameModeManager : NetworkBehaviour
{
    public static GameModeManager Instance { get; private set; }

    [Header("Game Mode Settings")]
    [SerializeField] private int killsToWin = 20;
    [SerializeField] private float gameRestartDelay = 5f;

    [Header("UI")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TMP_Text winnerText;

    // SyncVar to track if game is active
    private readonly SyncVar<bool> isGameActive = new SyncVar<bool>(true);
    private readonly SyncVar<string> winnerName = new SyncVar<string>("");

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        isGameActive.Value = true;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        // Subscribe to winner announcement
        winnerName.OnChange += OnWinnerChanged;
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
        winnerName.OnChange -= OnWinnerChanged;
    }

    /// <summary>
    /// Called when a player gets a kill - check for win condition
    /// </summary>
    [Server]
    public void OnPlayerKill(PlayerStats player)
    {
        if (!isGameActive.Value) return;

        Debug.Log($"[GameMode] {player.username.Value} now has {player.kills.Value} kills");

        if (LobbyData.ResolvedGameMode == GameMode.TeamDeathmatch)
        {
            // TDM: check if the player's team total kills reached the limit
            int teamKills = GetTeamKills(player.team.Value);
            Debug.Log($"[GameMode] Team {player.team.Value} total kills: {teamKills}/{killsToWin}");
            if (teamKills >= killsToWin)
            {
                TeamWon(player.team.Value);
            }
        }
        else
        {
            // FFA: check if individual player reached kill limit
            if (player.kills.Value >= killsToWin)
            {
                PlayerWon(player);
            }
        }
    }

    [Server]
    private int GetTeamKills(Team team)
    {
        int total = 0;
        foreach (var kvp in PlayerManager.Instance.players)
        {
            if (kvp.Value.stats != null && kvp.Value.stats.team.Value == team)
                total += kvp.Value.stats.kills.Value;
        }
        return total;
    }

    [Server]
    private void TeamWon(Team winningTeam)
    {
        isGameActive.Value = false;
        string teamName = winningTeam == Team.Rebels ? "Rebels" : "AI";
        winnerName.Value = $"Team {teamName}";

        Debug.Log($"[GameMode] {teamName} won the game!");
        RpcAnnounceWinner($"Team {teamName}");
        StartCoroutine(RestartCountdown());
    }

    [Server]
    private void PlayerWon(PlayerStats winner)
    {
        isGameActive.Value = false;
        winnerName.Value = winner.username.Value;

        Debug.Log($"[GameMode] {winner.username.Value} won the game!");

        // Announce winner to all clients
        RpcAnnounceWinner(winner.username.Value);

        // Restart game after delay
        StartCoroutine(RestartCountdown());
    }

    [ObserversRpc]
    private void RpcAnnounceWinner(string playerName)
    {
        Debug.Log($"[GameMode] Winner announced: {playerName}");
        
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            if (winnerText != null)
                winnerText.text = $"{playerName} WINS!\n\nGame restarting...";
        }
    }

    [Server]
    private IEnumerator RestartCountdown()
    {
        for (int i = (int)gameRestartDelay; i > 0; i--)
        {
            RpcUpdateCountdown(i);
            yield return new WaitForSeconds(1f);
        }
        
        RestartGame();
    }

    [ObserversRpc]
    private void RpcUpdateCountdown(int seconds)
    {
        if (winnerText != null)
        {
            winnerText.text = $"{winnerName.Value} WINS!\n\nRestarting in {seconds}...";
        }
    }

    [Server]
    private void RestartGame()
    {
        Debug.Log("[GameMode] Restarting game...");

        // Use PlayerManager to reset all players
        if (PlayerManager.Instance != null)
        {
            PlayerManager.Instance.ResetAllPlayers();
        }
        else
        {
            Debug.LogError("[GameMode] PlayerManager.Instance is null!");
        }

        // Reactivate game
        isGameActive.Value = true;
        winnerName.Value = "";

        // Hide game over panel for all clients
        RpcHideGameOverPanel();
    }

    [ObserversRpc]
    private void RpcHideGameOverPanel()
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
    }

    private void OnWinnerChanged(string previous, string current, bool asServer)
    {
        Debug.Log($"[GameMode] Winner changed: {current}");
    }

    // Public getter for game state
    public bool IsGameActive() => isGameActive.Value;
}
