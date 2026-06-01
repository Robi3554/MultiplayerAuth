using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class WeaponEntry
{
    public int id;
    public Sprite icon;
}

public class WeaponData : MonoBehaviour
{
    public static WeaponData Instance;

    public List<WeaponEntry> weapons;

    private void Awake()
    {
        Instance = this;
    }

    public Sprite GetIcon(int id)
    {
        foreach (var w in weapons)
        {
            if (w.id == id)
                return w.icon;
        }
        return null;
    }
}