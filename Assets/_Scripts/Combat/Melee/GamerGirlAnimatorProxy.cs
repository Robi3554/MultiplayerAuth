using UnityEngine;


public class GamerGirlAnimatorProxy : MonoBehaviour
{

    [SerializeField] private PredictionMelee melleWeapon;
    [SerializeField] private Animator animator;
    private static readonly int IsSlashingHash = Animator.StringToHash("IsSlashing");
    private void OnSlashStart()
    {
        animator.SetBool(IsSlashingHash, true);
    }

    private void OnSlashEnd()
    {
        animator.SetBool(IsSlashingHash, false);
    }

    private void callDamageFunction()
    {
        melleWeapon.PerformSlash();
    } 

}
