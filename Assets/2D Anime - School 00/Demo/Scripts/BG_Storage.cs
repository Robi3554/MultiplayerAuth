using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BackgroundDatabase",
    menuName = "Game/UI/Background Database", order = 0)]
public class BG_Storage : ScriptableObject
{
    [SerializeField] private Sprite[] _sprites;

    public int Count => _sprites?.Length ?? 0;

    public Sprite Get(int index)
    {
        if (Count == 0) return null;
        if (index < 0) index = (index % Count + Count) % Count;
        return _sprites[index % Count];
    }

    public IReadOnlyList<Sprite> Sprites => _sprites;
}
