using UnityEngine;
using TMPro;

public class KillFeedUI : MonoBehaviour
{
    public Transform feedParent;
    public GameObject feedItemPrefab;
    public float itemLifetime = 5f;

    public void AddFeedItem(string killer, string victim)
    {
        GameObject obj = Instantiate(feedItemPrefab, feedParent);
        TMP_Text text = obj.GetComponent<TMP_Text>();

        text.text = $"{killer} killed {victim}";

        Destroy(obj, itemLifetime);
    }
}
