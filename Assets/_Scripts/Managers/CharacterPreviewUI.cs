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
/// <see cref="CharacterDefinition"/> ScriptableObjects, so player list chips
/// and the preview backdrop stay in sync without code changes per character.
/// </summary>
public class CharacterPreviewUI : MonoBehaviour
{
    [Header("Character Roster (data-driven)")]
    [SerializeField] private List<CharacterDefinition> characterDefinitions = new();

    [Header("Preview Settings")]
    [SerializeField] private Vector3 previewPosition = new Vector3(1000f, 0f, 0f);
    [SerializeField] private Vector3 characterOffset = new Vector3(0f, -0.9f, 2.5f);
    [SerializeField] private Vector3 characterRotation = new Vector3(0f, 180f, 0f);
    [SerializeField] private float characterScale = 0.6f;
    [SerializeField] private Vector3 cameraRotation = new Vector3(5f, 0f, 0f);
    [SerializeField] private int renderTextureWidth = 512;
    [SerializeField] private int renderTextureHeight = 768;
    [SerializeField] private Color backgroundColor = new Color(0.05f, 0.05f, 0.08f, 1f);

    [Header("Backdrop / Pedestal")]
    [Tooltip("If true, spawns a radial-gradient quad behind the character and a thin pedestal disc under its feet, both tinted with the character's accent color.")]
    [SerializeField] private bool spawn3DBackdrop = true;
    [SerializeField] private float backdropDistance = 4f;
    [SerializeField] private Vector2 backdropSize = new Vector2(7f, 7f);
    [SerializeField] private float pedestalRadius = 1.3f;
    [SerializeField] private float pedestalThickness = 0.06f;

    [Header("UI References (auto-created if null)")]
    [SerializeField] private RawImage previewImage;
    [SerializeField] private Button leftArrowButton;
    [SerializeField] private Button rightArrowButton;
    [SerializeField] private TMPro.TMP_Text characterNameText;
    [Tooltip("Optional: tagline text shown beneath the character name.")]
    [SerializeField] private TMPro.TMP_Text characterTaglineText;

    private Camera previewCamera;
    private RenderTexture renderTexture;
    private GameObject currentPreviewInstance;
    private GameObject backdropQuad;
    private Renderer backdropRenderer;
    private GameObject pedestalDisc;
    private Renderer pedestalRenderer;
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

        // Publish definitions so any UI that resolves a NetworkObject reference can find display data.
        CharacterRegistry.Register(characterDefinitions);

        // Create camera early so builder can wire the RawImage in its own Awake
        SetupPreviewCamera();

        if (spawn3DBackdrop)
            CreateBackdropAndPedestal();
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
        if (backdropQuad != null)
            Destroy(backdropQuad);
        if (pedestalDisc != null)
            Destroy(pedestalDisc);
        if (renderTexture != null)
        {
            renderTexture.Release();
            Destroy(renderTexture);
        }
        if (previewCamera != null)
            Destroy(previewCamera.gameObject);
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
        previewLight.intensity = 1.2f;
        previewLight.cullingMask = 1 << previewLayer;

        // Also add a fill light from the opposite side
        var fillObj = new GameObject("PreviewFillLight");
        fillObj.transform.SetParent(camObj.transform);
        fillObj.transform.localPosition = new Vector3(-1f, 1f, 0.5f);
        fillObj.transform.localRotation = Quaternion.Euler(15f, 150f, 0f);
        fillObj.layer = previewLayer;
        var fillLight = fillObj.AddComponent<Light>();
        fillLight.type = LightType.Directional;
        fillLight.intensity = 0.6f;
        fillLight.color = new Color(0.7f, 0.8f, 1f);
        fillLight.cullingMask = 1 << previewLayer;

        if (previewImage != null)
            previewImage.texture = renderTexture;
    }

    /// <summary>
    /// Build a softly-glowing radial backdrop quad and a thin pedestal disc under
    /// the character. Both live on the preview layer so the gameplay camera never sees them.
    /// </summary>
    private void CreateBackdropAndPedestal()
    {
        // Use a built-in shader. URP's Lit shader expects a Universal RP material; for
        // simplicity and full control we use Sprites/Default-style unlit color which
        // works under URP without errors and lets us tint freely.
        Shader unlit = Shader.Find("Unlit/Texture");
        if (unlit == null) unlit = Shader.Find("Sprites/Default");
        if (unlit == null) unlit = Shader.Find("UI/Default");

        // Backdrop quad (radial gradient, billboard-ish, behind the character)
        backdropQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        backdropQuad.name = "PreviewBackdrop";
        Object.Destroy(backdropQuad.GetComponent<Collider>());
        backdropQuad.transform.position = previewPosition + new Vector3(0f, 0.4f, characterOffset.z + backdropDistance);
        backdropQuad.transform.localScale = new Vector3(backdropSize.x, backdropSize.y, 1f);
        // Face the camera (camera looks down +Z by default with this rig)
        backdropQuad.transform.rotation = Quaternion.LookRotation(Vector3.forward) * Quaternion.Euler(0f, 180f, 0f);
        SetLayerRecursively(backdropQuad, previewLayer);

        backdropRenderer = backdropQuad.GetComponent<Renderer>();
        Material backdropMat = new Material(unlit) { mainTexture = BuildRadialGradientTexture(256) };
        backdropMat.color = new Color(1f, 1f, 1f, 1f);
        backdropRenderer.sharedMaterial = backdropMat;

        // Pedestal disc (thin cylinder under the character)
        pedestalDisc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pedestalDisc.name = "PreviewPedestal";
        Object.Destroy(pedestalDisc.GetComponent<Collider>());
        Vector3 pedestalPos = previewPosition + new Vector3(characterOffset.x, characterOffset.y - 0.02f, characterOffset.z);
        pedestalDisc.transform.position = pedestalPos;
        pedestalDisc.transform.localScale = new Vector3(pedestalRadius * 2f, pedestalThickness, pedestalRadius * 2f);
        SetLayerRecursively(pedestalDisc, previewLayer);

        pedestalRenderer = pedestalDisc.GetComponent<Renderer>();
        Material pedestalMat = new Material(unlit) { mainTexture = BuildPedestalTexture(128) };
        pedestalMat.color = new Color(1f, 1f, 1f, 1f);
        pedestalRenderer.sharedMaterial = pedestalMat;
    }

    /// <summary>
    /// Builds a 256x256 radial gradient texture: bright center, fading to opaque dark at edge.
    /// The renderer's color is multiplied with this so we can retint per character.
    /// </summary>
    private static Texture2D BuildRadialGradientTexture(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        Color center = Color.white;
        Color edge = new Color(0.04f, 0.04f, 0.06f, 1f);
        Vector2 c = new Vector2(size * 0.5f, size * 0.5f);
        float maxDist = size * 0.5f;
        Color[] pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), c) / maxDist;
                d = Mathf.Clamp01(d);
                // Smooth quadratic falloff feels softer than linear
                float t = Mathf.Pow(d, 1.6f);
                pixels[y * size + x] = Color.Lerp(center, edge, t);
            }
        }
        tex.SetPixels(pixels);
        tex.Apply(false, true);
        return tex;
    }

    /// <summary>
    /// Builds a 128x128 pedestal texture: solid dark with a soft accent ring near the top.
    /// </summary>
    private static Texture2D BuildPedestalTexture(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        Color baseDark = new Color(0.08f, 0.08f, 0.10f, 1f);
        Color highlight = new Color(0.22f, 0.22f, 0.28f, 1f);
        Color[] pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            float yt = y / (float)(size - 1);
            // Subtle vertical band: brighter near the top edge of the side strip
            float band = Mathf.SmoothStep(0f, 1f, yt);
            Color c = Color.Lerp(baseDark, highlight, band * 0.5f);
            for (int x = 0; x < size; x++)
                pixels[y * size + x] = c;
        }
        tex.SetPixels(pixels);
        tex.Apply(false, true);
        return tex;
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
        if (characterDefinitions == null || characterDefinitions.Count == 0) return;
        currentIndex = Mathf.Clamp(index, 0, characterDefinitions.Count - 1);

        CharacterDefinition def = characterDefinitions[currentIndex];
        if (def == null || def.prefab == null)
        {
            Debug.LogWarning($"[CharacterPreview] CharacterDefinition at index {currentIndex} is null or has no prefab!");
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

        ApplyAccentToBackdrop(def.accentColor);

        if (characterNameText != null)
            characterNameText.text = def.displayName;
        if (characterTaglineText != null)
            characterTaglineText.text = def.tagline ?? string.Empty;

        Debug.Log($"[CharacterPreview] Showing '{def.displayName}' (prefab '{def.prefab.name}'), accent={def.accentColor}");

        OnCharacterChanged?.Invoke(def.prefab);
        OnDefinitionChanged?.Invoke(def);
    }

    /// <summary>
    /// Tints the radial backdrop and pedestal with a slightly warmer/cooler version
    /// of the character's accent so the preview area "feels" them.
    /// </summary>
    private void ApplyAccentToBackdrop(Color accent)
    {
        if (backdropRenderer != null)
        {
            // Backdrop is mostly the gradient texture; we multiply by a low-saturation version of the accent
            Color tint = Color.Lerp(new Color(0.2f, 0.2f, 0.25f, 1f), accent, 0.55f);
            backdropRenderer.sharedMaterial.color = tint;
        }
        if (pedestalRenderer != null)
        {
            // Pedestal: dark, with a faint accent so the "ring" reads as character-tinted
            Color tint = Color.Lerp(new Color(0.5f, 0.5f, 0.55f, 1f), accent, 0.45f);
            pedestalRenderer.sharedMaterial.color = tint;
        }
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
