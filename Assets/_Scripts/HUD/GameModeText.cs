using System.Collections;
using FishNet.Object;
using TMPro;
using UnityEngine;

public class GameModeText : NetworkBehaviour
{
    [SerializeField] private TMP_Text gameModeText;

    [SerializeField] private TMP_Text myTeamKills;
    [SerializeField] private TMP_Text oppositeTeamKills;

    [SerializeField] private PlayerStats playerStats;

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

        Team myTeam = playerStats.team.Value;

        if (myTeam == Team.Rebels)
        {
            myTeamKills.text = gm.rebelKills.Value.ToString();
            oppositeTeamKills.text = gm.aiKills.Value.ToString();
        }
        else
        {
            myTeamKills.text = gm.aiKills.Value.ToString();
            oppositeTeamKills.text = gm.rebelKills.Value.ToString();
        }
    }

    private IEnumerator InitWhenReady()
    {
        while(GameModeManager.Instance == null) 
            yield return null;

        var gm = GameModeManager.Instance;

        while (gm.gameMode == null)
            yield return null;

        gm.gameMode.OnChange += OnGameModeChanged;
        gm.rebelKills.OnChange += OnKillsTeamChanged;
        gm.aiKills.OnChange += OnKillsTeamChanged;

        OnGameModeChanged(default, gm.gameMode.Value, false);
        UpdateKillsUI();
    }
}
