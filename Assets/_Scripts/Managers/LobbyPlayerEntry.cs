using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays a single player's lobby info: character chip, username, team badge,
/// ready indicator. Used by <see cref="LobbyUI"/> in the player list.
///
/// The legacy prefab fields (usernameText, teamText, teamColorBar, readyCheckmark)
/// are preserved for inspector compatibility, but visual polish — rounded
/// background, character chip with short label, ready check styling — is applied
/// procedurally so we don't need to edit the prefab YAML by hand.
/// </summary>
public class LobbyPlayerEntry : MonoBehaviour
{
    [Header("Existing prefab references")]
    [SerializeField] private TMP_Text usernameText;
    [SerializeField] private TMP_Text teamText;
    [SerializeField] private Image teamColorBar;
    [SerializeField] private GameObject readyCheckmark;

    private static readonly Color RebelsColor = new Color(0.92f, 0.32f, 0.34f, 1f);
    private static readonly Color AIColor = new Color(0.32f, 0.55f, 0.95f, 1f);
    private static readonly Color NoneColor = new Color(0.55f, 0.55f, 0.62f, 1f);
    private static readonly Color RowFill = new Color(0.10f, 0.12f, 0.18f, 0.85f);
    private static readonly Color RowBorder = new Color(0.30f, 0.40f, 0.65f, 0.55f);
    private static readonly Color ReadyGlow = new Color(0.28f, 0.85f, 0.42f, 1f);
    private static readonly Color UsernameColor = new Color(0.95f, 0.96f, 1f, 1f);

    private bool _polished;
    private Image _rootBg;
    private Outline _rootOutline;
    private Image _characterChipBg;
    private TMP_Text _characterChipLabel;
    private TMP_Text _readyCheckText;
    private LayoutElement _layoutElement;

    public void Setup(LobbyManager.LobbyPlayerData data)
    {
        EnsurePolish();

        // Username
        if (usernameText != null)
        {
            usernameText.text = data.Username;
            usernameText.fontStyle = FontStyles.Bold;
            usernameText.color = UsernameColor;
        }

        // Team badge text + colored side bar
        Color teamCol = TeamColor(data.Team);
        if (teamText != null)
        {
            teamText.text = TeamLabel(data.Team);
            teamText.color = teamCol;
            teamText.fontStyle = FontStyles.Bold | FontStyles.UpperCase;
            teamText.characterSpacing = 4f;
        }
        if (teamColorBar != null)
        {
            teamColorBar.sprite = LobbyVisuals.GetVerticalGradient(teamCol, teamCol * 0.55f);
            teamColorBar.color = Color.white;
            teamColorBar.type = Image.Type.Simple;
        }

        // Character chip — driven by CharacterRegistry lookup
        var def = CharacterRegistry.GetByPrefab(data.Character);
        if (_characterChipBg != null && _characterChipLabel != null)
        {
            if (def != null)
            {
                _characterChipBg.sprite = LobbyVisuals.GetRoundedRect(8, 1, def.accentColor * 0.85f, def.accentColor);
                _characterChipBg.color = Color.white;
                _characterChipLabel.text = def.shortLabel;
                _characterChipLabel.color = Color.white;
            }
            else
            {
                _characterChipBg.sprite = LobbyVisuals.GetRoundedRect(8, 1, new Color(0.18f, 0.20f, 0.26f, 0.9f), new Color(0.35f, 0.40f, 0.50f, 0.6f));
                _characterChipBg.color = Color.white;
                _characterChipLabel.text = "...";
                _characterChipLabel.color = new Color(0.7f, 0.72f, 0.78f, 1f);
            }
        }

        // Ready indicator
        if (readyCheckmark != null)
        {
            readyCheckmark.SetActive(data.IsReady);
            if (_readyCheckText != null)
            {
                _readyCheckText.text = data.IsReady ? "<b>\u2713</b>" : string.Empty;
                _readyCheckText.color = ReadyGlow;
            }
        }

        // Subtle ready halo on the row outline
        if (_rootOutline != null)
        {
            Color outlineCol = data.IsReady ? ReadyGlow : (def != null ? def.accentColor * 0.7f : RowBorder);
            outlineCol.a = data.IsReady ? 0.85f : 0.45f;
            _rootOutline.effectColor = outlineCol;
        }
    }

    // ─── Polish (one-time procedural styling) ──────────────────────────

    private void EnsurePolish()
    {
        if (_polished) return;
        _polished = true;

        var rootRT = (RectTransform)transform;

        // Force the row to fill the column width in the player list's VerticalLayoutGroup
        rootRT.anchorMin = new Vector2(0f, rootRT.anchorMin.y);
        rootRT.anchorMax = new Vector2(1f, rootRT.anchorMax.y);

        // Preferred row height
        _layoutElement = GetComponent<LayoutElement>();
        if (_layoutElement == null) _layoutElement = gameObject.AddComponent<LayoutElement>();
        _layoutElement.preferredHeight = 48f;
        _layoutElement.minHeight = 44f;
        _layoutElement.flexibleWidth = 1f;

        // Reconfigure the existing HorizontalLayoutGroup for proper spacing
        var hlg = GetComponent<HorizontalLayoutGroup>();
        if (hlg == null) hlg = gameObject.AddComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(8, 6, 5, 5);
        hlg.spacing = 10;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childAlignment = TextAnchor.MiddleLeft;

        // Rounded background image with subtle outline + drop shadow
        _rootBg = GetComponent<Image>();
        if (_rootBg == null) _rootBg = gameObject.AddComponent<Image>();
        _rootBg.sprite = LobbyVisuals.GetRoundedRect(10, 1, RowFill, RowBorder);
        _rootBg.type = Image.Type.Sliced;
        _rootBg.color = Color.white;
        _rootBg.raycastTarget = false;

        _rootOutline = GetComponent<Outline>();
        if (_rootOutline == null) _rootOutline = gameObject.AddComponent<Outline>();
        _rootOutline.effectColor = RowBorder;
        _rootOutline.effectDistance = new Vector2(1f, -1f);

        if (GetComponent<Shadow>() == null)
        {
            var shadow = gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.55f);
            shadow.effectDistance = new Vector2(0f, -2f);
        }

        // Build the character chip (left-most child, before everything else)
        BuildCharacterChip();

        // Configure layout elements for existing children so the layout is balanced.
        ConfigureChild(usernameText != null ? usernameText.gameObject : null,
            preferredWidth: 0f, flexibleWidth: 1f);
        if (usernameText != null)
        {
            usernameText.alignment = TextAlignmentOptions.MidlineLeft;
            usernameText.fontSize = 18;
            usernameText.enableAutoSizing = true;
            usernameText.fontSizeMin = 12;
            usernameText.fontSizeMax = 20;
            usernameText.color = UsernameColor;
            usernameText.fontStyle = FontStyles.Bold;
            usernameText.margin = new Vector4(2, 0, 4, 0);
        }

        ConfigureChild(teamText != null ? teamText.gameObject : null,
            preferredWidth: 80f, flexibleWidth: 0f);
        if (teamText != null)
        {
            teamText.alignment = TextAlignmentOptions.MidlineRight;
            teamText.fontSize = 14;
            teamText.enableAutoSizing = false;
            teamText.fontStyle = FontStyles.Bold | FontStyles.UpperCase;
            teamText.characterSpacing = 4f;
        }

        ConfigureChild(readyCheckmark, preferredWidth: 32f, flexibleWidth: 0f);
        if (readyCheckmark != null)
        {
            // Replace the legacy "R" with a proper styled check glyph
            _readyCheckText = readyCheckmark.GetComponent<TMP_Text>();
            if (_readyCheckText != null)
            {
                _readyCheckText.alignment = TextAlignmentOptions.Center;
                _readyCheckText.fontSize = 26;
                _readyCheckText.enableAutoSizing = false;
                _readyCheckText.fontStyle = FontStyles.Bold;
                _readyCheckText.text = "\u2713"; // ✓
                _readyCheckText.color = ReadyGlow;
            }
        }

        // Convert the team color bar from a square slot into a thin vertical strip on the right.
        if (teamColorBar != null)
        {
            // Make the bar narrow regardless of layout-group sizing
            ConfigureChild(teamColorBar.gameObject, preferredWidth: 6f, flexibleWidth: 0f);
            var barRT = (RectTransform)teamColorBar.transform;
            barRT.sizeDelta = new Vector2(6f, barRT.sizeDelta.y);
            // Move to the very right of the row regardless of original sibling order
            teamColorBar.transform.SetAsLastSibling();
        }
    }

    private void BuildCharacterChip()
    {
        var chip = new GameObject("CharacterChip", typeof(RectTransform));
        chip.transform.SetParent(transform, false);
        chip.transform.SetAsFirstSibling();

        var le = chip.AddComponent<LayoutElement>();
        le.preferredWidth = 70f;
        le.minWidth = 64f;
        le.preferredHeight = 32f;
        le.flexibleWidth = 0f;

        _characterChipBg = chip.AddComponent<Image>();
        _characterChipBg.sprite = LobbyVisuals.GetRoundedRect(8, 1,
            new Color(0.18f, 0.20f, 0.26f, 0.9f), new Color(0.35f, 0.40f, 0.50f, 0.6f));
        _characterChipBg.type = Image.Type.Sliced;
        _characterChipBg.color = Color.white;
        _characterChipBg.raycastTarget = false;

        var labelObj = new GameObject("Label", typeof(RectTransform));
        labelObj.transform.SetParent(chip.transform, false);
        var labelRT = (RectTransform)labelObj.transform;
        labelRT.anchorMin = Vector2.zero;
        labelRT.anchorMax = Vector2.one;
        labelRT.offsetMin = new Vector2(2, 1);
        labelRT.offsetMax = new Vector2(-2, -1);

        _characterChipLabel = labelObj.AddComponent<TextMeshProUGUI>();
        _characterChipLabel.text = "...";
        _characterChipLabel.fontSize = 14;
        _characterChipLabel.color = Color.white;
        _characterChipLabel.alignment = TextAlignmentOptions.Center;
        _characterChipLabel.fontStyle = FontStyles.Bold | FontStyles.UpperCase;
        _characterChipLabel.characterSpacing = 1.5f;
        _characterChipLabel.raycastTarget = false;

        // Inherit font from the username text if available, so the chip matches the row typography.
        if (usernameText != null && usernameText.font != null)
            _characterChipLabel.font = usernameText.font;
    }

    private static void ConfigureChild(GameObject child, float preferredWidth, float flexibleWidth)
    {
        if (child == null) return;
        var le = child.GetComponent<LayoutElement>();
        if (le == null) le = child.AddComponent<LayoutElement>();
        le.preferredWidth = preferredWidth;
        le.flexibleWidth = flexibleWidth;
    }

    private static Color TeamColor(Team team) => team switch
    {
        Team.Rebels => RebelsColor,
        Team.AI => AIColor,
        _ => NoneColor,
    };

    private static string TeamLabel(Team team) => team switch
    {
        Team.Rebels => "Rebels",
        Team.AI => "AI",
        _ => "—",
    };
}
