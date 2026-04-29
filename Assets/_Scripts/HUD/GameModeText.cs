using TMPro;
using UnityEngine;

public class GameModeText : MonoBehaviour
{
    [SerializeField] private TMP_Text gameModeText;
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
}
