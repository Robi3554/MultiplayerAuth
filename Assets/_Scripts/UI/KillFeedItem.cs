using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class KillFeedItem : MonoBehaviour
{
    [SerializeField] private TMP_Text killerText;
    [SerializeField] private TMP_Text victimText;
    [SerializeField] private Image weaponIcon;

    public void Set(string killer, string victim, Sprite icon)
    {
        killerText.text = killer;
        victimText.text = victim;

        weaponIcon.sprite = icon;
        weaponIcon.enabled = icon != null;
    }
}