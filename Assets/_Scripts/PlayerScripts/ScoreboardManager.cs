using FishNet.Object;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class ScoreboardManager : NetworkBehaviour
{
    public static ScoreboardManager Instance { get; private set; }

    [SerializeField] private GameObject scoreboardUI;
    [SerializeField] private Transform scoreboardContent;
    [SerializeField] private GameObject scoreboardEntryPrefab;

    private Dictionary<PlayerStats, ScoreboardEntry> scoreboardEntries = new Dictionary<PlayerStats, ScoreboardEntry>();
    private bool isScoreboardVisible = false;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    /// <summary>
    /// Called by PlayerInput via the Scoreboard action (Tab key or Gamepad Select).
    /// </summary>
    public void OnScoreboard(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        isScoreboardVisible = !isScoreboardVisible;
        scoreboardUI.SetActive(isScoreboardVisible);
    }

    private void Update()
    {
        // Update all entries when visible
        if (isScoreboardVisible)
        {
            foreach (var entry in scoreboardEntries.Values)
            {
                entry.UpdateDisplay();
            }
        }
    }

    public void RegisterPlayer(PlayerStats stats)
    {
        if (scoreboardEntries.ContainsKey(stats))
            return;

        GameObject entryObj = Instantiate(scoreboardEntryPrefab, scoreboardContent);
        ScoreboardEntry entry = entryObj.GetComponent<ScoreboardEntry>();
        entry.Initialize(stats);

        scoreboardEntries[stats] = entry;
    }

    public void UnregisterPlayer(PlayerStats stats)
    {
        if (scoreboardEntries.TryGetValue(stats, out var entry))
        {
            Destroy(entry.gameObject);
            scoreboardEntries.Remove(stats);
        }
    }
}