using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays a single player's lobby info (username, team, ready state).
/// Instantiated per player by LobbyUI.
/// </summary>
public class LobbyPlayerEntry : MonoBehaviour
{
    [SerializeField] private TMP_Text usernameText;
    [SerializeField] private TMP_Text teamText;
    [SerializeField] private Image teamColorBar;
    [SerializeField] private GameObject readyCheckmark;

    private static readonly Color RebelsColor = new Color(0.9f, 0.3f, 0.3f); // Red
    private static readonly Color AIColor = new Color(0.3f, 0.5f, 0.9f);     // Blue
    private static readonly Color NoneColor = new Color(0.5f, 0.5f, 0.5f);   // Gray

    public void Setup(LobbyManager.LobbyPlayerData data)
    {
        usernameText.text = data.Username;

        switch (data.Team)
        {
            case Team.Rebels:
                teamText.text = "Rebels";
                teamColorBar.color = RebelsColor;
                break;
            case Team.AI:
                teamText.text = "AI";
                teamColorBar.color = AIColor;
                break;
            default:
                teamText.text = "-";
                teamColorBar.color = NoneColor;
                break;
        }

        readyCheckmark.SetActive(data.IsReady);
    }
}
