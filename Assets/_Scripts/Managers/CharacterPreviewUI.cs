using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using FishNet.Object;

/// <summary>
/// Manages a live 3D character preview in the lobby.
/// Renders the selected character prefab via a dedicated camera + RenderTexture
/// onto a RawImage in the UI. Supports cycling with left/right arrows.
///
/// Character data (display name, short label, accent color) is sourced from
/// <see cref="CharacterDefinition"/> ScriptableObjects in <c>Assets/Resources/Characters/</c>.
/// They are auto-discovered at runtime via <see cref="Resources.LoadAll"/>, so the
/// scene does not need any manual inspector wiring after a code-side roster change.
/// The inspector list (if assigned) takes precedence — useful for limiting/ordering
/// the carousel without touching the assets folder.
/// </summary>
public class CharacterPreviewUI : MonoBehaviour
{
    [Header("Character Roster (data-driven)")]
    [Tooltip("Optional explicit roster. Leave empty to auto-load every CharacterDefinition under Assets/Resources/Characters/.")]
    [SerializeField] private List<CharacterDefinition> characterDefinitions = new();

    [Tooltip("Folder under Assets/Resources/ to load CharacterDefinition assets from.")]
    [SerializeField] private string resourcesFolder = "Characters";

    [Header("Preview Settings")]
    [SerializeField] private Vector3 previewPosition = new Vector3(1000f, 0f, 0f);
    [SerializeField] private Vector3 characterOffset = new Vector3(0f, -0.9f, 2.5f);
    [SerializeField] private Vector3 characterRotation = new Vector3(0f, 180f, 0f);
    [SerializeField] private float characterScale = 0.6f;
    [SerializeField] private Vector3 cameraRotation = new Vector3(5f, 0f, 0f);
    [SerializeField] private int renderTextureWidth = 512;
    [SerializeField] private int renderTextureHeight = 768;
    [SerializeField] private Color backgroundColor = new Color(0.05f, 0.05f, 0.08f, 1f);

    [Header("UI References (auto-created if null)")]
    [SerializeField] private RawImage previewImage;
    [SerializeField] private Button leftArrowButton;
    [SerializeField] private Button rightArrowButton;
    [SerializeField] private TMPro.TMP_Text characterNameText;
    [Tooltip("Optional: tagline text shown beneath the character name.")]
    [SerializeField] private TMPro.TMP_Text characterTaglineText;

    [Tooltip("Optional: a UI Image whose color is retinted with the active character's accent (e.g. a radial glow behind the preview).")]
    [SerializeField] private Image accentGlowImage;
    [SerializeField, Range(0f, 1f)] private float accentGlowAlpha = 0.65f;

    [Tooltip("Optional: an Outline component (typically on the character name text) whose color is retinted to a darker shade of the accent.")]
    [SerializeField] private Outline accentNameOutline;

    private Camera previewCamera;
    private RenderTexture renderTexture;
    private GameObject currentPreviewInstance;
    private int currentIndex;
    private int previewLayer;
    private bool _arrowButtonsSetExternally;

    public IReadOnlyList<CharacterDefinition> CharacterDefinitions => characterDefinitions;
    public int CurrentIndex => currentIndex;

    /// <summary>Currently selected character's prefab (back-compat with LobbyUI.CmdSetCharacter).</summary>
    public NetworkObject CurrentCharacter
    {
        get
        {
            CharacterDefinition def = CurrentDefinition;
            return def != null ? def.prefab : null;
        }
    }

    /// <summary>Currently selected character definition (display name, accent, etc.).</summary>
    public CharacterDefinition CurrentDefinition =>
        characterDefinitions != null && characterDefinitions.Count > 0 && currentIndex >= 0 && currentIndex < characterDefinitions.Count
            ? characterDefinitions[currentIndex]
            : null;

    /// <summary>Fired when the player cycles to a different character.</summary>
    public event System.Action<NetworkObject> OnCharacterChanged;

    /// <summary>Fired when the player cycles to a different character (definition variant). Fires after OnCharacterChanged.</summary>
    public event System.Action<CharacterDefinition> OnDefinitionChanged;

    private void Awake()
    {
        // Use a dedicated layer so the preview camera only sees preview models.
        // "CharacterPreview" is preferred; fall back to an unused high layer.
        previewLayer = LayerMask.NameToLayer("CharacterPreview");
        if (previewLayer < 0)
            previewLayer = 31; // last layer, least likely to conflict

        // Auto-load roster from Resources if the inspector list is empty.
        if (characterDefinitions == null || characterDefinitions.Count == 0)
        {
            characterDefinitions = LoadDefinitionsFromResources();
            if (characterDefinitions.Count == 0)
            {
                Debug.LogError(
                    $"[CharacterPreview] No CharacterDefinition assets found at Assets/Resources/{resourcesFolder}/. " +
                    "Place at least one CharacterDefinition.asset there or assign the inspector list manually.");
            }
            else
            {
                Debug.Log($"[CharacterPreview] Auto-loaded {characterDefinitions.Count} CharacterDefinition(s) from Resources/{resourcesFolder}.");
            }
        }

        // Strip any null entries before publishing so consumers never trip on them.
        characterDefinitions.RemoveAll(d => d == null);

        // Publish so any UI that resolves a NetworkObject reference can find display data.
        CharacterRegistry.Register(characterDefinitions);

        SetupPreviewCamera();
    }

    private void Start()
    {
        // Ensure RawImage has the RenderTexture bound — covers all Awake() orderings
        if (previewImage != null && renderTexture != null)
            previewImage.texture = renderTexture;

        // Only register listeners here if SetArrowButtons() was NOT called by LobbyLayoutBuilder.
        // Registering twice would cause each click to skip a character and send two RPCs.
        if (!_arrowButtonsSetExternally)
        {
            if (leftArrowButton != null)
                leftArrowButton.onClick.AddListener(PreviousCharacter);
            if (rightArrowButton != null)
                rightArrowButton.onClick.AddListener(NextCharacter);
        }

        if (characterDefinitions != null && characterDefinitions.Count > 0)
            ShowCharacter(0);
    }

    private void OnDestroy()
    {
        if (currentPreviewInstance != null)
            Destroy(currentPreviewInstance);
        if (renderTexture != null)
        {
            renderTexture.Release();
            Destroy(renderTexture);
        }
        if (previewCamera != null)
            Destroy(previewCamera.gameObject);
    }

    /// <summary>
    /// Loads every CharacterDefinition asset under <c>Assets/Resources/{resourcesFolder}/</c>.
    /// Sorts deterministically by display name so the carousel order is stable across builds.
    /// </summary>
    private List<CharacterDefinition> LoadDefinitionsFromResources()
    {
        CharacterDefinition[] loaded = Resources.LoadAll<CharacterDefinition>(resourcesFolder);
        var list = new List<CharacterDefinition>(loaded);
        list.Sort((a, b) =>
        {
            if (a == null) return 1;
            if (b == null) return -1;
            return string.Compare(a.displayName ?? a.name, b.displayName ?? b.name, System.StringComparison.OrdinalIgnoreCase);
        });
        return list;
    }

    /// <summary>
    /// Set up the off-screen camera that renders the character model.
    /// </summary>
    private void SetupPreviewCamera()
    {
        renderTexture = new RenderTexture(renderTextureWidth, renderTextureHeight, 24, RenderTextureFormat.ARGB32);
        renderTexture.antiAliasing = 4;

        var camObj = new GameObject("LobbyPreviewCamera");
        camObj.transform.position = previewPosition;
        camObj.transform.rotation = Quaternion.Euler(cameraRotation);

        previewCamera = camObj.AddComponent<Camera>();
        previewCamera.targetTexture = renderTexture;
        previewCamera.clearFlags = CameraClearFlags.SolidColor;
        previewCamera.backgroundColor = backgroundColor;
        previewCamera.cullingMask = 1 << previewLayer;
        previewCamera.nearClipPlane = 0.1f;
        previewCamera.farClipPlane = 50f;
        previewCamera.fieldOfView = 30f;

        // URP requires this additional data component to render properly
        var urpCamData = camObj.AddComponent<UniversalAdditionalCameraData>();
        urpCamData.renderType = CameraRenderType.Base;
        urpCamData.renderShadows = false;
        urpCamData.renderPostProcessing = false;

        // Add a directional light dedicated to the preview
        var lightObj = new GameObject("PreviewLight");
        lightObj.transform.SetParent(camObj.transform);
        lightObj.transform.localPosition = new Vector3(0.5f, 2f, -1f);
        lightObj.transform.localRotation = Quaternion.Euler(30f, -15f, 0f);
        lightObj.layer = previewLayer;
        var previewLight = lightObj.AddComponent<Light>();
        previewLight.type = LightType.Directional;
        previewLight.intensity = 1.4f;
        previewLight.cullingMask = 1 << previewLayer;

        // Also add a fill light from the opposite side
        var fillObj = new GameObject("PreviewFillLight");
        fillObj.transform.SetParent(camObj.transform);
        fillObj.transform.localPosition = new Vector3(-1f, 1f, 0.5f);
        fillObj.transform.localRotation = Quaternion.Euler(15f, 150f, 0f);
        fillObj.layer = previewLayer;
        var fillLight = fillObj.AddComponent<Light>();
        fillLight.type = LightType.Directional;
        fillLight.intensity = 0.7f;
        fillLight.color = new Color(0.7f, 0.8f, 1f);
        fillLight.cullingMask = 1 << previewLayer;

        if (previewImage != null)
            previewImage.texture = renderTexture;
    }

    /// <summary>
    /// Assign the RawImage at runtime (used by LobbyLayoutBuilder).
    /// </summary>
    public void SetPreviewImage(RawImage image)
    {
        previewImage = image;
        if (renderTexture != null)
            previewImage.texture = renderTexture;
    }

    /// <summary>
    /// Assign arrow buttons at runtime.
    /// </summary>
    public void SetArrowButtons(Button left, Button right)
    {
        leftArrowButton = left;
        rightArrowButton = right;
        leftArrowButton.onClick.AddListener(PreviousCharacter);
        rightArrowButton.onClick.AddListener(NextCharacter);
        _arrowButtonsSetExternally = true;
    }

    /// <summary>
    /// Assign character name text at runtime.
    /// </summary>
    public void SetCharacterNameText(TMPro.TMP_Text text)
    {
        characterNameText = text;
    }

    /// <summary>
    /// Assign the optional tagline text at runtime.
    /// </summary>
    public void SetCharacterTaglineText(TMPro.TMP_Text text)
    {
        characterTaglineText = text;
    }

    /// <summary>
    /// Assign the optional radial glow image whose color is retinted per character.
    /// </summary>
    public void SetAccentGlow(Image image)
    {
        accentGlowImage = image;
    }

    /// <summary>
    /// Assign the optional Outline component on the name text that gets retinted per character.
    /// </summary>
    public void SetAccentNameOutline(Outline outline)
    {
        accentNameOutline = outline;
    }

    public void NextCharacter()
    {
        if (characterDefinitions == null || characterDefinitions.Count == 0) return;
        ShowCharacter((currentIndex + 1) % characterDefinitions.Count);
    }

    public void PreviousCharacter()
    {
        if (characterDefinitions == null || characterDefinitions.Count == 0) return;
        ShowCharacter((currentIndex - 1 + characterDefinitions.Count) % characterDefinitions.Count);
    }

    /// <summary>
    /// Show the character at the given index in the preview area.
    /// </summary>
    public void ShowCharacter(int index)
    {
        if (characterDefinitions == null || characterDefinitions.Count == 0)
        {
            Debug.LogWarning("[CharacterPreview] ShowCharacter called but no CharacterDefinitions are available.");
            return;
        }
        currentIndex = Mathf.Clamp(index, 0, characterDefinitions.Count - 1);

        CharacterDefinition def = characterDefinitions[currentIndex];
        if (def == null || def.prefab == null)
        {
            Debug.LogWarning($"[CharacterPreview] CharacterDefinition at index {currentIndex} is null or has no prefab! " +
                             "Check the asset and re-assign its prefab field.");
            return;
        }

        // Ensure RenderTexture is bound to the RawImage (lazy bind)
        if (previewImage != null && renderTexture != null && previewImage.texture != renderTexture)
        {
            previewImage.texture = renderTexture;
            Debug.Log("[CharacterPreview] Late-bound RenderTexture to RawImage.");
        }

        // Destroy previous preview instance
        if (currentPreviewInstance != null)
            Destroy(currentPreviewInstance);

        // Instantiate at preview position (far from gameplay area)
        Vector3 spawnPos = previewPosition + characterOffset;
        currentPreviewInstance = Instantiate(def.prefab.gameObject, spawnPos, Quaternion.Euler(characterRotation));
        currentPreviewInstance.name = $"Preview_{def.prefab.name}";
        currentPreviewInstance.transform.localScale = Vector3.one * characterScale;

        // Strip all network/gameplay components immediately — must use DestroyImmediate
        // because FishNet's NetworkObject.OnDestroy can interfere with deferred Destroy.
        StripNonVisualComponents(currentPreviewInstance);

        // Set layer recursively so only the preview camera sees it
        SetLayerRecursively(currentPreviewInstance, previewLayer);

        // Play idle animation if an Animator exists
        var animator = currentPreviewInstance.GetComponentInChildren<Animator>();
        if (animator != null)
        {
            animator.enabled = true;
            animator.Play("Idle", 0, 0f);
        }

        // Tint the preview camera's clear color with a subtle hint of the accent so the
        // background "feels" the character without needing 3D backdrop geometry.
        if (previewCamera != null)
        {
            Color tinted = Color.Lerp(backgroundColor, def.accentColor, 0.18f);
            previewCamera.backgroundColor = tinted;
        }

        if (characterNameText != null)
        {
            characterNameText.text = !string.IsNullOrWhiteSpace(def.displayName) ? def.displayName : def.name;
            characterNameText.color = def.accentColor;
        }
        if (characterTaglineText != null)
            characterTaglineText.text = def.tagline ?? string.Empty;

        // Retint the optional UI accent surfaces. Doing this here (instead of via a
        // C# event subscription from the layout builder) means the references are
        // preserved through scene save / "bake" workflows.
        if (accentGlowImage != null)
        {
            accentGlowImage.color = new Color(def.accentColor.r, def.accentColor.g, def.accentColor.b, accentGlowAlpha);
        }
        if (accentNameOutline != null)
        {
            Color o = def.accentColor * 0.45f;
            o.a = 0.85f;
            accentNameOutline.effectColor = o;
        }

        Debug.Log($"[CharacterPreview] Showing '{def.displayName}' (prefab '{def.prefab.name}'), accent={def.accentColor}");

        OnCharacterChanged?.Invoke(def.prefab);
        OnDefinitionChanged?.Invoke(def);
    }

    /// <summary>
    /// Remove all non-rendering components so the preview is purely visual.
    /// Keeps: Transform, MeshFilter, MeshRenderer, SkinnedMeshRenderer, Animator, LODGroup.
    /// </summary>
    private static void StripNonVisualComponents(GameObject obj)
    {
        // Collect all non-visual components, then destroy them with DestroyImmediate.
        // DestroyImmediate is required because FishNet's NetworkObject.OnDestroy (deferred)
        // can destroy the entire GameObject before the frame ends.
        var toDestroy = new List<Component>();
        var allComponents = obj.GetComponentsInChildren<Component>(true);
        foreach (var comp in allComponents)
        {
            if (comp == null) continue;
            if (comp is Transform) continue;
            if (comp is MeshFilter) continue;
            if (comp is Renderer) continue;    // covers MeshRenderer, SkinnedMeshRenderer, etc.
            if (comp is Animator) continue;
            if (comp is LODGroup) continue;
            toDestroy.Add(comp);
        }

        // Destroy in reverse order so child components are removed before parents.
        // This prevents FishNet NetworkBehaviour from cascading destruction.
        for (int i = toDestroy.Count - 1; i >= 0; i--)
        {
            if (toDestroy[i] != null)
                DestroyImmediate(toDestroy[i]);
        }
    }

    private static void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
            SetLayerRecursively(child.gameObject, layer);
    }
}
