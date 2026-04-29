using System.Collections.Generic;
using FishNet.Object;
using UnityEngine;

/// <summary>
/// Static lookup populated by <see cref="CharacterPreviewUI"/> at Awake. Lets
/// any UI (lobby player entries, late-join overlay, in-game nameplates) resolve
/// a character's display data from a prefab reference without having to drag
/// the preview component into every prefab.
/// </summary>
public static class CharacterRegistry
{
    private static readonly List<CharacterDefinition> _all = new();
    private static readonly Dictionary<int, CharacterDefinition> _byPrefabId = new();

    /// <summary>All registered character definitions in carousel order.</summary>
    public static IReadOnlyList<CharacterDefinition> All => _all;

    /// <summary>True after at least one definition has been registered.</summary>
    public static bool HasAny => _all.Count > 0;

    /// <summary>
    /// Replaces the registry with the given definitions. Safe to call multiple times
    /// (e.g. on scene reloads); the registry is process-wide static state.
    /// </summary>
    public static void Register(IList<CharacterDefinition> defs)
    {
        _all.Clear();
        _byPrefabId.Clear();
        if (defs == null) return;

        for (int i = 0; i < defs.Count; i++)
        {
            CharacterDefinition def = defs[i];
            if (def == null) continue;
            _all.Add(def);
            if (def.prefab != null)
                _byPrefabId[def.prefab.GetInstanceID()] = def;
        }
    }

    /// <summary>
    /// Resolves a definition from a prefab NetworkObject. Returns null if the prefab
    /// hasn't been registered (e.g. character not yet selected by the player).
    /// </summary>
    public static CharacterDefinition GetByPrefab(NetworkObject prefab)
    {
        if (prefab == null) return null;
        return _byPrefabId.TryGetValue(prefab.GetInstanceID(), out CharacterDefinition def) ? def : null;
    }

    /// <summary>
    /// Resolves a definition by its index in the registered list. Returns null on out-of-range.
    /// </summary>
    public static CharacterDefinition GetByIndex(int index)
    {
        if (index < 0 || index >= _all.Count) return null;
        return _all[index];
    }

    /// <summary>Default fallback color when a character has not been selected yet.</summary>
    public static Color UnknownAccent => new Color(0.45f, 0.45f, 0.5f);
}
