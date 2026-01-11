using FishNet.Object;
using UnityEngine;

[CreateAssetMenu(menuName = "Powerups/Big Head Buff")]
public class BigHeadBuff : PowerupEffect
{
    public int damageMultiplier;
    public float headSizeMultiplier;

    public override void TriggerEffect(NetworkObject obj)
    {
        obj.GetComponent<PlayerStats>().HeadSizeChange(obj, headSizeMultiplier);
        obj.GetComponent<PlayerStats>().ChangeMult(damageMultiplier);
    }
}
