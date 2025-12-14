using UnityEngine;


public class GamerGirlAnimatorProxy : MonoBehaviour
{

    [SerializeField] private PredictionMelee meleeWeapon;
    [SerializeField] private Animator animator;
    [SerializeField] private  GameObject meleeCollider;
    private static readonly int IsSlashingHash = Animator.StringToHash("IsSlashing");
    private void OnSlashStart()
    {
        animator.SetBool(IsSlashingHash, true);
    }

    private void OnSlashEnd()
    {
        animator.SetBool(IsSlashingHash, false);
        meleeWeapon.OnAnimationComplete();
    }
    private void enableMeleeCollider()
    {
        meleeCollider.GetComponent<MeshCollider>().enabled = true;
    }

    private void disableMeleeCollider()
    {
        meleeCollider.GetComponent<MeshCollider>().enabled = false;
    }
    private void playSlashVfx()
    {
        meleeWeapon.PlayObserverWeaponVfx();
    }
}
