using FishNet.Object;
using UnityEngine;

/// <summary>
/// Designer-driven character entry for the lobby. One asset per playable character.
/// Used by <see cref="CharacterPreviewUI"/> to drive the carousel and by
/// <see cref="CharacterRegistry"/> to resolve display data on the player list.
/// </summary>
[CreateAssetMenu(fileName = "Character", menuName = "Multiplayer/Character Definition")]
public class CharacterDefinition : ScriptableObject
{
    [Tooltip("Networked player prefab spawned when this character is selected.")]
    public NetworkObject prefab;

    [Tooltip("Full display name shown in the center panel, e.g. 'Arts student'.")]
    public string displayName = "Character";

    [Tooltip("3-5 char label shown in player list chips, e.g. 'Arts'.")]
    public string shortLabel = "???";

    [Tooltip("Accent color used for chips, glow, selected states and the preview backdrop.")]
    [ColorUsage(showAlpha: false, hdr: false)]
    public Color accentColor = new Color(0.35f, 0.75f, 1f);

    [Tooltip("Optional flavor text shown under the display name in the lobby.")]
    [TextArea(1, 2)]
    public string tagline;
}
