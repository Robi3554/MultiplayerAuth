using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class meleeCollision : MonoBehaviour
{
    private CapsuleCollider playerCollider;
    [SerializeField] private PredictionMelee sword;
    private MeshCollider meshCollider;

    private void Awake()
    {
        playerCollider = GetComponentInParent<CapsuleCollider>();
        meshCollider = GetComponent<MeshCollider>();

    }
    private void Start()
    {
        
    }
    public void ExecuteHurtbox()
    {
        Bounds meshBounds = meshCollider.bounds;
        Vector3 boxCenter = meshBounds.center;
        Vector3 halfExtents = meshBounds.extents;

        //this returns an array of EVERY collider overlapping with the mesh collider bounds
        Collider[] hitColliders = Physics.OverlapBox(boxCenter, halfExtents);

        foreach (var hit in hitColliders)
        {
            if (hit != playerCollider && hit != meshCollider)
            {
                sword.DealDamage(hit);
                Debug.Log("Hit: " + hit.name);
            }
        }
        sword.EndSlashWindow();
    }
}
