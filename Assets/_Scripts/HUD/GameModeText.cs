using TMPro;
using UnityEngine;

public class GameModeText : MonoBehaviour
{
    [SerializeField] private TMP_Text gameModeText;

    [SerializeField] private TMP_Text myTeamKills;
    [SerializeField] private TMP_Text oppositeTeamKills;
    void Start()
    {
        if(LobbyData.ResolvedGameMode == GameMode.TeamDeathmatch)
        {
            gameModeText.text = "Team Deathmatch";
        }
        else if(LobbyData.ResolvedGameMode == GameMode.FreeForAll)
        {
            gameModeText.text = "Free For All";
        }
    }

    void Update()
    {
        if (LobbyData.ResolvedGameMode == GameMode.TeamDeathmatch)
        {
            myTeamKills.text = GameModeManager.Instance.myTeamKills.ToString();
            oppositeTeamKills.text = GameModeManager.Instance.oppositeTeamKills.ToString();
        }
    }
}
