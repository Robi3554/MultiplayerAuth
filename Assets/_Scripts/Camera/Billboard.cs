using FishNet.Object;
using UnityEngine;

public class Billboard : MonoBehaviour // No need for NetworkBehaviour
{
    private Transform _cameraTransform;

    void Start()
    {
        // Find the main camera in the scene.
        // Camera.main can be slow; a more optimized solution would be to use a singleton
        // or reference manager, but this works for most cases.
        if (Camera.main != null)
        {
            _cameraTransform = Camera.main.transform;
        }
    }

    // LateUpdate runs after all other updates, which is ideal for camera-related logic.
    void LateUpdate()
    {
        // Retry finding the camera if it wasn't available at Start
        // (e.g. during FishNet scene transitions Camera.main may not exist yet)
        if (_cameraTransform == null)
        {
            if (Camera.main != null)
                _cameraTransform = Camera.main.transform;
            else
                return;
        }

        // This makes the object's rotation match the camera's rotation.
        // It's the simplest and most reliable method for a UI billboard.
        transform.rotation = _cameraTransform.rotation;
    }
}