using FishNet.Connection;
using FishNet.Object;
using System.Collections;
using TMPro;
using UnityEngine;

public class DeathScreenManager : NetworkBehaviour
{
    public static DeathScreenManager Instance { get; private set; }
    
    [SerializeField] private GameObject deathScreenPrefab;
    [SerializeField] private float respawnTime = 5f;
    private GameObject currentDeathScreen;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
            Destroy(gameObject);
    }

    /// <summary>
    /// Called by PlayerManager via TargetRpc to show death screen on client
    /// </summary>
    [TargetRpc]
    public void ShowDeathScreen(NetworkConnection target)
    {
        if (currentDeathScreen != null)
            Destroy(currentDeathScreen);
        
        currentDeathScreen = Instantiate(deathScreenPrefab);
        TextMeshProUGUI respawnTimerText = currentDeathScreen.transform.Find("Text/RespawnTimer").GetComponent<TextMeshProUGUI>();
        
        //start countdown coroutine(UI only)
        StartCoroutine(CountdownCoroutine(respawnTimerText));
    }

    private IEnumerator CountdownCoroutine(TextMeshProUGUI timerText)
    {
        float timeRemaining = respawnTime;
        
        while (timeRemaining > 0)
        {
            int secondsDisplay = Mathf.CeilToInt(timeRemaining);
            timerText.text = $"Respawning in {secondsDisplay}";
            yield return new WaitForSeconds(0.1f);
            timeRemaining -= 0.1f;
        }
        
        timerText.text = "Respawning in 0";
        yield return new WaitForSeconds(0.5f);
        
        //destroy death screen
        Destroy(currentDeathScreen);
    }
}
