using System;
using System.Collections.Generic;
using System.IO;
using FishNet.Connection;
using UnityEngine;

public class AnalyticsManager : MonoBehaviour
{
    private const string TimeFormat = "yyyy-MM-dd_HH-mm-ss";

    public static AnalyticsManager Instance { get; private set; }

    [SerializeField] private bool logMatchSummary = true;
    [SerializeField] private bool writeMatchSummaryToFile = true;

    private readonly Dictionary<int, PlayerAnalytics> players = new Dictionary<int, PlayerAnalytics>();
    private readonly Dictionary<string, int> pickupCounts = new Dictionary<string, int>();
    private readonly List<float> completedGameDurations = new List<float>();

    private bool matchActive;
    private float matchStartRealtime;
    private DateTime matchStartUtc;
    private string matchId;
    private int matchSequence;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        EnsureInstance();
    }

    public static AnalyticsManager EnsureInstance()
    {
        if (Instance != null)
            return Instance;

        var go = new GameObject(nameof(AnalyticsManager));
        DontDestroyOnLoad(go);
        Instance = go.AddComponent<AnalyticsManager>();
        return Instance;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void StartMatch(string gameMode)
    {
        matchSequence++;
        matchId = $"{DateTime.UtcNow.ToString(TimeFormat)}_{matchSequence}";
        matchStartUtc = DateTime.UtcNow;
        matchStartRealtime = Time.realtimeSinceStartup;
        matchActive = true;

        players.Clear();
        pickupCounts.Clear();

        Debug.Log($"[Analytics] Match {matchId} started. Mode={gameMode}");
    }

    public void EndMatch(string reason, string winner)
    {
        if (!matchActive)
            return;

        float durationSeconds = Mathf.Max(0f, Time.realtimeSinceStartup - matchStartRealtime);
        completedGameDurations.Add(durationSeconds);
        matchActive = false;

        string summary = BuildMatchSummary(reason, winner, durationSeconds);

        if (logMatchSummary)
            Debug.Log(summary);

        if (writeMatchSummaryToFile)
            WriteSummaryFile(summary);
    }

    public void RegisterPlayer(NetworkConnection connection, PlayerStats stats)
    {
        if (connection == null)
            return;

        int clientId = connection.ClientId;
        PlayerAnalytics player = GetPlayer(clientId);
        player.Username = stats != null ? stats.username.Value : string.Empty;
        player.Team = stats != null ? stats.team.Value.ToString() : Team.None.ToString();
    }

    public void RecordClientPerformance(int clientId, float averageFps, int fpsSampleCount, long averageLatencyMs, int latencySampleCount)
    {
        if (fpsSampleCount <= 0 && latencySampleCount <= 0)
            return;

        PlayerAnalytics player = GetPlayer(clientId);
        if (fpsSampleCount > 0)
        {
            player.FpsSampleSum += averageFps * fpsSampleCount;
            player.FpsSampleCount += fpsSampleCount;
        }

        if (latencySampleCount > 0)
        {
            player.LatencySampleSum += averageLatencyMs * latencySampleCount;
            player.LatencySampleCount += latencySampleCount;
        }
    }

    public void RecordWeaponUsage(int clientId, int weaponId, string weaponName, float secondsUsed)
    {
        if (weaponId < 0 || secondsUsed <= 0f)
            return;

        PlayerAnalytics player = GetPlayer(clientId);
        if (!player.Weapons.TryGetValue(weaponId, out WeaponAnalytics weapon))
        {
            weapon = new WeaponAnalytics(weaponId, weaponName);
            player.Weapons[weaponId] = weapon;
        }

        if (!string.IsNullOrWhiteSpace(weaponName))
            weapon.Name = weaponName;

        weapon.Seconds += secondsUsed;
    }

    public void RecordHealthPickup(string pickupId, int pickerClientId)
    {
        if (string.IsNullOrWhiteSpace(pickupId))
            pickupId = "UnassignedPickup";

        if (!pickupCounts.ContainsKey(pickupId))
            pickupCounts[pickupId] = 0;

        pickupCounts[pickupId]++;

        PlayerAnalytics player = GetPlayer(pickerClientId);
        player.TotalPickups++;
    }

    private PlayerAnalytics GetPlayer(int clientId)
    {
        if (!players.TryGetValue(clientId, out PlayerAnalytics player))
        {
            player = new PlayerAnalytics(clientId);
            players[clientId] = player;
        }

        return player;
    }

    private string BuildMatchSummary(string reason, string winner, float durationSeconds)
    {
        var lines = new List<string>
        {
            $"[Analytics] Match {matchId} ended. Reason={reason}, Winner={winner}, Duration={FormatDuration(durationSeconds)}, StartedUtc={matchStartUtc:O}",
            $"[Analytics] Average completed game time: {FormatDuration(GetAverageGameDuration())}",
            "[Analytics] Players:"
        };

        foreach (PlayerAnalytics player in players.Values)
        {
            lines.Add($"  Client {player.ClientId} ({player.Username}, {player.Team}) AvgFPS={player.AverageFps:F1}, AvgLatencyMs={player.AverageLatencyMs:F0}, Pickups={player.TotalPickups}");

            if (player.Weapons.Count == 0)
            {
                lines.Add("    Weapons: no usage recorded");
            }
            else
            {
                foreach (WeaponAnalytics weapon in player.Weapons.Values)
                    lines.Add($"    {weapon.DisplayName}: {FormatDuration(weapon.Seconds)}");
            }
        }

        lines.Add("[Analytics] Health pack pickup counts:");
        foreach (KeyValuePair<string, int> pickup in pickupCounts)
            lines.Add($"  {pickup.Key}: {pickup.Value}");

        return string.Join(Environment.NewLine, lines);
    }

    private void WriteSummaryFile(string summary)
    {
        try
        {
            string directory = Path.Combine(Application.persistentDataPath, "Analytics");
            Directory.CreateDirectory(directory);

            string path = Path.Combine(directory, $"match_{matchId}.txt");
            File.WriteAllText(path, summary);
            Debug.Log($"[Analytics] Match summary written to {path}");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[Analytics] Failed to write match summary: {ex.Message}");
        }
    }

    private float GetAverageGameDuration()
    {
        if (completedGameDurations.Count == 0)
            return 0f;

        float sum = 0f;
        foreach (float duration in completedGameDurations)
            sum += duration;

        return sum / completedGameDurations.Count;
    }

    private static string FormatDuration(float seconds)
    {
        TimeSpan time = TimeSpan.FromSeconds(Mathf.Max(0f, seconds));
        return $"{(int)time.TotalMinutes:00}:{time.Seconds:00}";
    }

    private sealed class PlayerAnalytics
    {
        public readonly int ClientId;
        public readonly Dictionary<int, WeaponAnalytics> Weapons = new Dictionary<int, WeaponAnalytics>();

        public string Username;
        public string Team;
        public float FpsSampleSum;
        public int FpsSampleCount;
        public long LatencySampleSum;
        public int LatencySampleCount;
        public int TotalPickups;

        public PlayerAnalytics(int clientId)
        {
            ClientId = clientId;
        }

        public float AverageFps => FpsSampleCount > 0 ? FpsSampleSum / FpsSampleCount : 0f;
        public float AverageLatencyMs => LatencySampleCount > 0 ? LatencySampleSum / (float)LatencySampleCount : 0f;
    }

    private sealed class WeaponAnalytics
    {
        public readonly int Id;
        public string Name;
        public float Seconds;

        public WeaponAnalytics(int id, string name)
        {
            Id = id;
            Name = name;
        }

        public string DisplayName => string.IsNullOrWhiteSpace(Name) ? $"Weapon {Id}" : $"{Name} (id {Id})";
    }
}
