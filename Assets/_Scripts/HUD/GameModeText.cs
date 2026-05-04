using System.Collections;
using FishNet.Object;
using TMPro;
using UnityEngine;

public class GameModeText : NetworkBehaviour
{
    [SerializeField] private TMP_Text gameModeText;

    [SerializeField] private TMP_Text myTeamKills;
    [SerializeField] private TMP_Text oppositeTeamKills;

    public override void OnStartClient()
    {
        base.OnStartClient();
        StartCoroutine(InitWhenReady());
    }

    private void OnGameModeChanged(GameMode oldValue, GameMode newValue, bool asServer)
    {
        if (newValue == GameMode.TeamDeathmatch)
            gameModeText.text = "Team Deathmatch";
        else if (newValue == GameMode.FreeForAll)
            gameModeText.text = "Free for All";
    }

    private void OnKillsTeamChanged(int oldValue, int newValue, bool asServer)
    {
        UpdateKillsUI();
    }

    private void UpdateKillsUI()
    {
        var gm = GameModeManager.Instance;

        if (gm.gameMode.Value == GameMode.FreeForAll)
            return;

        myTeamKills.text = gm.myTeamKills.Value.ToString();
        oppositeTeamKills.text = gm.oppositeTeamKills.Value.ToString();
    }

    private IEnumerator InitWhenReady()
    {
        while(GameModeManager.Instance == null) 
            yield return null;

        var gm = GameModeManager.Instance;

        while (gm.gameMode == null)
            yield return null;

        gm.gameMode.OnChange += OnGameModeChanged;
        gm.myTeamKills.OnChange += OnKillsTeamChanged;
        gm.oppositeTeamKills.OnChange += OnKillsTeamChanged;

        OnGameModeChanged(default, gm.gameMode.Value, false);
        UpdateKillsUI();
    }
}
