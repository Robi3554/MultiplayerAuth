using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class meleeCollision : MonoBehaviour
{
    private CapsuleCollider playerCollider;
    [SerializeField] private PredictionMelee sword;

    private void Awake()
    {
        playerCollider = GetComponentInParent<CapsuleCollider>();

    }
    private void Start()
    {
        
    }
    public void ExecuteHurtbox()
    {
        Vector3 boxCenter = transform.position + transform.forward * 1.5f; //offset
        Vector3 halfExtents = new Vector3(1f, 1f, 1f); // half the total size of the box
        Quaternion orientation = transform.rotation;

        //this returns an array of EVERY collider inside the box 
        Collider[] hitColliders = Physics.OverlapBox(boxCenter, halfExtents, orientation);

        foreach (var hit in hitColliders)
        {
            if (hit != playerCollider)
            {
                sword.DealDamage(hit);
                Debug.Log("Hit: " + hit.name);
            }
        }
        sword.EndSlashWindow();
    }
}
