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
    [SerializeField] private int teamKillsToWin = 20;
    [SerializeField] private float gameRestartDelay = 5f;

    [Header("UI")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TMP_Text winnerText;
    public readonly SyncVar<GameMode> gameMode = new SyncVar<GameMode>();
    internal readonly SyncVar<int> myTeamKills = new SyncVar<int>();
    internal readonly SyncVar<int> oppositeTeamKills = new SyncVar<int>();

    // SyncVar to track if game is active
    internal readonly SyncVar<bool> isGameActive = new SyncVar<bool>(true);
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
        gameMode.Value = LobbyData.ResolvedGameMode;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
    }

    /// <summary>
    /// Called when a player gets a kill - check for win condition
    /// </summary>
    [Server]
    public void OnPlayerKill(PlayerStats player)
    {
        if (!isGameActive.Value) return;

        if (LobbyData.ResolvedGameMode == GameMode.TeamDeathmatch)
        {
            // TDM: check if the player's team total kills reached the limit
            myTeamKills.Value = GetTeamKills(player.team.Value);
            oppositeTeamKills.Value = GetOppositeTeamKills(player.team.Value);
            if (myTeamKills.Value >= teamKillsToWin)
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
    private int GetOppositeTeamKills(Team myTeam)
    {
        Team oppositeTeam = (myTeam == Team.Rebels) ? Team.AI : Team.Rebels;
        return GetTeamKills(oppositeTeam);
    }

    [Server]
    private void TeamWon(Team winningTeam)
    {
        isGameActive.Value = false;
        string teamName = winningTeam == Team.Rebels ? "Rebels" : "AI";
        winnerName.Value = $"Team {teamName}";

        RpcAnnounceWinner($"Team {teamName}");
        StartCoroutine(RestartCountdown());
    }

    [Server]
    private void PlayerWon(PlayerStats winner)
    {
        isGameActive.Value = false;
        winnerName.Value = winner.username.Value;

        // Announce winner to all clients
        RpcAnnounceWinner(winner.username.Value);

        // Restart game after delay
        StartCoroutine(RestartCountdown());
    }

    [ObserversRpc]
    private void RpcAnnounceWinner(string playerName)
    {
        
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
            winnerText.text = $"{winnerName.Value} WINS!\n\nReturning to lobby in {seconds}...";
        }
    }

    [Server]
    private void RestartGame()
    {
        if (LobbyManager.Instance != null)
        {
            LobbyManager.Instance.ReturnToLobby();
        }
        else
        {
            Debug.LogError("[GameModeManager] LobbyManager.Instance is null — cannot return to lobby. " +
                           "Ensure LobbyManager is spawned as a Global NetworkObject before the game starts.");
        }
    }

    [ObserversRpc]
    private void RpcHideGameOverPanel()
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
    }

    // Public getter for game state
    public bool IsGameActive() => isGameActive.Value;
}
