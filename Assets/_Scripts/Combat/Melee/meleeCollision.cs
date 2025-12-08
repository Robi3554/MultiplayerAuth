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
    private void OnTriggerEnter(Collider hit){
        if (hit != playerCollider)
        {
            sword.DealDamage(hit);
        }
    }   
}
