using FishNet.Object;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public class ScoreboardManager : MonoBehaviour
{
    public static ScoreboardManager Instance { get; private set; }

    [SerializeField] private GameObject scoreboardUI;
    [SerializeField] private Transform scoreboardContent;
    [SerializeField] private GameObject scoreboardEntryPrefab;

    private static Dictionary<PlayerStats, ScoreboardEntry> scoreboardEntries = new Dictionary<PlayerStats, ScoreboardEntry>();
    private bool isScoreboardVisible = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;

            foreach (var key in scoreboardEntries.Keys.ToList())
            {
                if (scoreboardEntries[key] != null)
                    continue;

                GameObject entryObj = Instantiate(scoreboardEntryPrefab, scoreboardContent);
                ScoreboardEntry entry = entryObj.GetComponent<ScoreboardEntry>();
                entry.Initialize(key);

                scoreboardEntries[key] = entry;
            }
        }
        else
            Destroy(gameObject);
    }

    /// <summary>
    /// Polls Tab key and Gamepad Select (simulated by OnScreenButton on mobile).
    /// </summary>
    private void Update()
    {
        bool tabPressed = Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame;
        bool selectPressed = Gamepad.current != null && Gamepad.current.selectButton.wasPressedThisFrame;

        if (tabPressed || selectPressed)
        {
            isScoreboardVisible = !isScoreboardVisible;
            scoreboardUI.SetActive(isScoreboardVisible);
        }

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
        
        Debug.Log("Registering player with ScoreboardManager: " + stats.username.Value);

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

    public static void AddPlayerToInitialList(PlayerStats stats)
    {
        scoreboardEntries[stats] = null;
    }
}