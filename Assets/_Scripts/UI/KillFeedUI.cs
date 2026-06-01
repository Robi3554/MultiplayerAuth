using UnityEngine;
using TMPro;

public class KillFeedUI : MonoBehaviour
{
    public Transform feedParent;
    public GameObject feedItemPrefab;
    public float itemLifetime = 5f;

    public void AddFeedItem(string killer, string victim, int weaponId)
    {
        GameObject obj = Instantiate(feedItemPrefab, feedParent);
        obj.SetActive(true);

        KillFeedItem item = obj.GetComponent<KillFeedItem>();

        Sprite weaponIcon = WeaponData.Instance.GetIcon(weaponId);

        item.Set(killer, victim, weaponIcon);

        Destroy(obj, 5f);
    }
}
