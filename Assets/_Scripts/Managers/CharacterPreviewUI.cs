using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using FishNet.Object;

/// <summary>
/// Manages a live 3D character preview in the lobby.
/// Renders the selected character prefab via a dedicated camera + RenderTexture
/// onto a RawImage in the UI. Supports cycling with left/right arrows.
/// </summary>
public class CharacterPreviewUI : MonoBehaviour
{
    [Header("Character Options")]
    [SerializeField] private List<NetworkObject> characterOptions;

    [Header("Preview Settings")]
    [SerializeField] private Vector3 previewPosition = new Vector3(1000f, 0f, 0f);
    [SerializeField] private Vector3 characterOffset = new Vector3(0f, -0.9f, 2.5f);
    [SerializeField] private Vector3 characterRotation = new Vector3(0f, 180f, 0f);
    [SerializeField] private Vector3 cameraRotation = new Vector3(5f, 0f, 0f);
    [SerializeField] private int renderTextureWidth = 512;
    [SerializeField] private int renderTextureHeight = 768;
    [SerializeField] private Color backgroundColor = new Color(0.08f, 0.08f, 0.12f, 0f);

    [Header("UI References (auto-created if null)")]
    [SerializeField] private RawImage previewImage;
    [SerializeField] private Button leftArrowButton;
    [SerializeField] private Button rightArrowButton;
    [SerializeField] private TMPro.TMP_Text characterNameText;

    private Camera previewCamera;
    private RenderTexture renderTexture;
    private GameObject currentPreviewInstance;
    private int currentIndex;
    private int previewLayer;

    public List<NetworkObject> CharacterOptions => characterOptions;
    public int CurrentIndex => currentIndex;
    public NetworkObject CurrentCharacter => characterOptions != null && characterOptions.Count > 0
        ? characterOptions[currentIndex]
        : null;

    /// <summary>Fired when the player cycles to a different character.</summary>
    public event System.Action<NetworkObject> OnCharacterChanged;

    private void Awake()
    {
        previewLayer = LayerMask.NameToLayer("UI");
        if (previewLayer < 0) previewLayer = 5; // fallback to UI layer
    }

    private void Start()
    {
        SetupPreviewCamera();

        if (leftArrowButton != null)
            leftArrowButton.onClick.AddListener(PreviousCharacter);
        if (rightArrowButton != null)
            rightArrowButton.onClick.AddListener(NextCharacter);

        if (characterOptions != null && characterOptions.Count > 0)
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
    /// Set up the off-screen camera that renders the character model.
    /// </summary>
    private void SetupPreviewCamera()
    {
        renderTexture = new RenderTexture(renderTextureWidth, renderTextureHeight, 24);
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

        // Add a subtle light for the preview
        var lightObj = new GameObject("PreviewLight");
        lightObj.transform.SetParent(camObj.transform);
        lightObj.transform.localPosition = new Vector3(0.5f, 2f, -1f);
        lightObj.transform.localRotation = Quaternion.Euler(30f, -15f, 0f);
        var previewLight = lightObj.AddComponent<Light>();
        previewLight.type = LightType.Directional;
        previewLight.intensity = 1.2f;
        previewLight.cullingMask = 1 << previewLayer;

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
    }

    /// <summary>
    /// Assign character name text at runtime.
    /// </summary>
    public void SetCharacterNameText(TMPro.TMP_Text text)
    {
        characterNameText = text;
    }

    public void NextCharacter()
    {
        if (characterOptions == null || characterOptions.Count == 0) return;
        ShowCharacter((currentIndex + 1) % characterOptions.Count);
    }

    public void PreviousCharacter()
    {
        if (characterOptions == null || characterOptions.Count == 0) return;
        ShowCharacter((currentIndex - 1 + characterOptions.Count) % characterOptions.Count);
    }

    /// <summary>
    /// Show the character at the given index in the preview area.
    /// </summary>
    public void ShowCharacter(int index)
    {
        if (characterOptions == null || characterOptions.Count == 0) return;
        currentIndex = Mathf.Clamp(index, 0, characterOptions.Count - 1);

        // Destroy previous preview instance
        if (currentPreviewInstance != null)
            Destroy(currentPreviewInstance);

        var prefab = characterOptions[currentIndex];
        if (prefab == null) return;

        // Instantiate at preview position (far from gameplay area)
        Vector3 spawnPos = previewPosition + characterOffset;
        currentPreviewInstance = Instantiate(prefab.gameObject, spawnPos, Quaternion.Euler(characterRotation));

        // Strip all network/gameplay components — this is a visual-only preview
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

        // Update name label
        if (characterNameText != null)
            characterNameText.text = prefab.name;

        OnCharacterChanged?.Invoke(prefab);
    }

    /// <summary>
    /// Remove all non-rendering components so the preview is purely visual.
    /// Keeps: Transform, MeshFilter, MeshRenderer, SkinnedMeshRenderer, Animator, LODGroup.
    /// </summary>
    private static void StripNonVisualComponents(GameObject obj)
    {
        var allComponents = obj.GetComponentsInChildren<Component>(true);
        foreach (var comp in allComponents)
        {
            if (comp == null) continue;
            if (comp is Transform) continue;
            if (comp is MeshFilter) continue;
            if (comp is MeshRenderer) continue;
            if (comp is SkinnedMeshRenderer) continue;
            if (comp is Animator) continue;
            if (comp is LODGroup) continue;
            // Destroy everything else (NetworkBehaviour, Rigidbody, Colliders, scripts, etc.)
            Destroy(comp);
        }
    }

    private static void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
            SetLayerRecursively(child.gameObject, layer);
    }
}
