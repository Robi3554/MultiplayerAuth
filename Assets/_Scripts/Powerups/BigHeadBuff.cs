using FishNet.Object;
using UnityEngine;

[CreateAssetMenu(menuName = "Powerups/Big Head Buff")]
public class BigHeadBuff : PowerupEffect
{
    public float damage; //will need to increase the damage the character with big head deals, but later
    public float headSizeMultiplier;

    public override void TriggerEffect(NetworkObject obj)
    {
        obj.GetComponent<PlayerStats>().HeadSizeChange(obj, headSizeMultiplier);
    }
}
