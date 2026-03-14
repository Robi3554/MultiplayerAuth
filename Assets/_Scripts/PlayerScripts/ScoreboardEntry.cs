using TMPro;
using UnityEngine;

public class ScoreboardEntry : MonoBehaviour
{
    [SerializeField] private TMP_Text usernameText;
    [SerializeField] private TMP_Text killsText;
    [SerializeField] private TMP_Text deathsText;
    [SerializeField] private TMP_Text healthText;

    private RectTransform rt;
    
    private PlayerStats playerStats;

    void Start()
    {
        rt = GetComponent<RectTransform>();
        Vector2 size = rt.sizeDelta;
        size.x = 1565;
        rt.sizeDelta = size;
    }

    public void Initialize(PlayerStats stats)
    {
        playerStats = stats;
        UpdateDisplay();
    }

    public void UpdateDisplay()
    {
        if (playerStats == null) return;

        usernameText.text = playerStats.username.Value;
        killsText.text = playerStats.kills.Value.ToString();
        deathsText.text = playerStats.deaths.Value.ToString();
        healthText.text = playerStats.health.Value.ToString();
    }
}