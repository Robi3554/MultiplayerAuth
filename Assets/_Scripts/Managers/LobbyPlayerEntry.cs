using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays a single player's lobby info: character chip, username, team badge,
/// ready indicator. Used by <see cref="LobbyUI"/> in the player list.
///
/// Styling matches the Fortnite-ish lobby look: chunky solid panel, thick
/// outline, bright character chip on the left, big bold uppercase typography,
/// vivid yellow ready check. The legacy prefab fields are preserved for
/// inspector compatibility but visual polish is applied procedurally.
/// </summary>
public class LobbyPlayerEntry : MonoBehaviour
{
    [Header("Existing prefab references")]
    [SerializeField] private TMP_Text usernameText;
    [SerializeField] private TMP_Text teamText;
    [SerializeField] private Image teamColorBar;
    [SerializeField] private GameObject readyCheckmark;

    private static readonly Color RebelsColor = new Color(0.95f, 0.30f, 0.34f, 1f);
    private static readonly Color AIColor = new Color(0.30f, 0.55f, 1.00f, 1f);
    private static readonly Color NoneColor = new Color(0.55f, 0.58f, 0.70f, 1f);
    private static readonly Color RowFill = new Color(0.18f, 0.27f, 0.62f, 1f);
    private static readonly Color RowFillReady = new Color(0.20f, 0.36f, 0.78f, 1f);
    private static readonly Color RowOutline = new Color(0.02f, 0.05f, 0.14f, 1f);
    private static readonly Color AccentYellow = new Color(1.00f, 0.82f, 0.18f, 1f);
    private static readonly Color UsernameColor = new Color(0.99f, 1.00f, 1.00f, 1f);

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
            usernameText.fontStyle = FontStyles.Bold | FontStyles.UpperCase;
            usernameText.color = UsernameColor;
            usernameText.characterSpacing = 3f;
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
            // Solid color strip — flat, no gradient (matches the lobby's flat-color style)
            teamColorBar.sprite = LobbyVisuals.GetWhitePixel();
            teamColorBar.color = teamCol;
            teamColorBar.type = Image.Type.Simple;
        }

        // Character chip — driven by CharacterRegistry lookup
        var def = CharacterRegistry.GetByPrefab(data.Character);
        if (_characterChipBg != null && _characterChipLabel != null)
        {
            if (def != null)
            {
                Color chipFill = def.accentColor;
                Color chipBorder = chipFill * 0.45f; chipBorder.a = 1f;
                _characterChipBg.sprite = LobbyVisuals.GetRoundedRect(8, 2, chipFill, chipBorder);
                _characterChipBg.color = Color.white;
                _characterChipLabel.text = def.shortLabel;
                _characterChipLabel.color = Color.white;
            }
            else
            {
                _characterChipBg.sprite = LobbyVisuals.GetRoundedRect(8, 2,
                    new Color(0.16f, 0.22f, 0.42f, 1f), RowOutline);
                _characterChipBg.color = Color.white;
                _characterChipLabel.text = "...";
                _characterChipLabel.color = new Color(0.78f, 0.85f, 1f, 1f);
            }
        }

        // Ready indicator (yellow check mark when ready)
        if (readyCheckmark != null)
        {
            readyCheckmark.SetActive(data.IsReady);
            if (_readyCheckText != null)
            {
                _readyCheckText.text = data.IsReady ? "\u2713" : string.Empty;
                _readyCheckText.color = AccentYellow;
            }
        }

        // Brighten the row fill + outline when this player is ready
        if (_rootBg != null)
            _rootBg.sprite = LobbyVisuals.GetRoundedRect(10, 3, data.IsReady ? RowFillReady : RowFill, RowOutline);

        if (_rootOutline != null)
        {
            Color outlineCol = data.IsReady ? AccentYellow : (def != null ? def.accentColor : RowOutline);
            outlineCol.a = data.IsReady ? 0.95f : 0.55f;
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
        _layoutElement.preferredHeight = 54f;
        _layoutElement.minHeight = 50f;
        _layoutElement.flexibleWidth = 1f;

        // Reconfigure the existing HorizontalLayoutGroup
        var hlg = GetComponent<HorizontalLayoutGroup>();
        if (hlg == null) hlg = gameObject.AddComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(8, 0, 6, 6);
        hlg.spacing = 10;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childAlignment = TextAnchor.MiddleLeft;

        // Chunky rounded background with thick outline + drop shadow
        _rootBg = GetComponent<Image>();
        if (_rootBg == null) _rootBg = gameObject.AddComponent<Image>();
        _rootBg.sprite = LobbyVisuals.GetRoundedRect(10, 3, RowFill, RowOutline);
        _rootBg.type = Image.Type.Sliced;
        _rootBg.color = Color.white;
        _rootBg.raycastTarget = false;

        _rootOutline = GetComponent<Outline>();
        if (_rootOutline == null) _rootOutline = gameObject.AddComponent<Outline>();
        _rootOutline.effectColor = RowOutline;
        _rootOutline.effectDistance = new Vector2(1.5f, -1.5f);

        if (GetComponent<Shadow>() == null)
        {
            var shadow = gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.6f);
            shadow.effectDistance = new Vector2(0f, -3f);
        }

        // Build the character chip (left-most child)
        BuildCharacterChip();

        // Layout elements for the existing children so the row reads as: [Chip] [Username]                  [Team] [Check] [Stripe]
        ConfigureChild(usernameText != null ? usernameText.gameObject : null,
            preferredWidth: 0f, flexibleWidth: 1f);
        if (usernameText != null)
        {
            usernameText.alignment = TextAlignmentOptions.MidlineLeft;
            usernameText.fontSize = 18;
            usernameText.enableAutoSizing = true;
            usernameText.fontSizeMin = 12;
            usernameText.fontSizeMax = 22;
            usernameText.color = UsernameColor;
            usernameText.fontStyle = FontStyles.Bold | FontStyles.UpperCase;
            usernameText.characterSpacing = 3f;
            usernameText.margin = new Vector4(2, 0, 4, 0);
            // Subtle dark outline so the username pops against the saturated panel
            var usernameOutline = usernameText.gameObject.GetComponent<Outline>();
            if (usernameOutline == null) usernameOutline = usernameText.gameObject.AddComponent<Outline>();
            usernameOutline.effectColor = new Color(0f, 0f, 0f, 0.7f);
            usernameOutline.effectDistance = new Vector2(1f, -1f);
        }

        ConfigureChild(teamText != null ? teamText.gameObject : null,
            preferredWidth: 86f, flexibleWidth: 0f);
        if (teamText != null)
        {
            teamText.alignment = TextAlignmentOptions.MidlineRight;
            teamText.fontSize = 15;
            teamText.enableAutoSizing = false;
            teamText.fontStyle = FontStyles.Bold | FontStyles.UpperCase;
            teamText.characterSpacing = 4f;
            var teamOutline = teamText.gameObject.GetComponent<Outline>();
            if (teamOutline == null) teamOutline = teamText.gameObject.AddComponent<Outline>();
            teamOutline.effectColor = new Color(0f, 0f, 0f, 0.7f);
            teamOutline.effectDistance = new Vector2(1f, -1f);
        }

        ConfigureChild(readyCheckmark, preferredWidth: 36f, flexibleWidth: 0f);
        if (readyCheckmark != null)
        {
            _readyCheckText = readyCheckmark.GetComponent<TMP_Text>();
            if (_readyCheckText != null)
            {
                _readyCheckText.alignment = TextAlignmentOptions.Center;
                _readyCheckText.fontSize = 30;
                _readyCheckText.enableAutoSizing = false;
                _readyCheckText.fontStyle = FontStyles.Bold;
                _readyCheckText.text = "\u2713";
                _readyCheckText.color = AccentYellow;
                var readyOutline = _readyCheckText.gameObject.GetComponent<Outline>();
                if (readyOutline == null) readyOutline = _readyCheckText.gameObject.AddComponent<Outline>();
                readyOutline.effectColor = new Color(0f, 0f, 0f, 0.85f);
                readyOutline.effectDistance = new Vector2(1.4f, -1.4f);
            }
        }

        // Convert team color bar into a thick vertical color strip on the right edge
        if (teamColorBar != null)
        {
            ConfigureChild(teamColorBar.gameObject, preferredWidth: 8f, flexibleWidth: 0f);
            var barRT = (RectTransform)teamColorBar.transform;
            barRT.sizeDelta = new Vector2(8f, barRT.sizeDelta.y);
            teamColorBar.transform.SetAsLastSibling();
        }
    }

    private void BuildCharacterChip()
    {
        var chip = new GameObject("CharacterChip", typeof(RectTransform));
        chip.transform.SetParent(transform, false);
        chip.transform.SetAsFirstSibling();

        var le = chip.AddComponent<LayoutElement>();
        le.preferredWidth = 76f;
        le.minWidth = 70f;
        le.preferredHeight = 36f;
        le.flexibleWidth = 0f;

        _characterChipBg = chip.AddComponent<Image>();
        _characterChipBg.sprite = LobbyVisuals.GetRoundedRect(8, 2,
            new Color(0.16f, 0.22f, 0.42f, 1f), RowOutline);
        _characterChipBg.type = Image.Type.Sliced;
        _characterChipBg.color = Color.white;
        _characterChipBg.raycastTarget = false;

        // Chip drop shadow for chunkiness
        var shadow = chip.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.55f);
        shadow.effectDistance = new Vector2(0f, -2f);

        var labelObj = new GameObject("Label", typeof(RectTransform));
        labelObj.transform.SetParent(chip.transform, false);
        var labelRT = (RectTransform)labelObj.transform;
        labelRT.anchorMin = Vector2.zero;
        labelRT.anchorMax = Vector2.one;
        labelRT.offsetMin = new Vector2(2, 1);
        labelRT.offsetMax = new Vector2(-2, -1);

        _characterChipLabel = labelObj.AddComponent<TextMeshProUGUI>();
        _characterChipLabel.text = "...";
        _characterChipLabel.fontSize = 15;
        _characterChipLabel.color = Color.white;
        _characterChipLabel.alignment = TextAlignmentOptions.Center;
        _characterChipLabel.fontStyle = FontStyles.Bold | FontStyles.UpperCase;
        _characterChipLabel.characterSpacing = 2f;
        _characterChipLabel.raycastTarget = false;

        var labelOutline = labelObj.AddComponent<Outline>();
        labelOutline.effectColor = new Color(0f, 0f, 0f, 0.8f);
        labelOutline.effectDistance = new Vector2(1.2f, -1.2f);

        // Inherit font from the username text if available
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
        Team.AI => "A.I.",
        _ => "—",
    };
}
