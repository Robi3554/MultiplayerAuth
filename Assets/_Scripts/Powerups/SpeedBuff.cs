using FishNet.Object;
using UnityEngine;

[CreateAssetMenu(menuName = "Powerups/Speed Buff")]
public class SpeedBuff : PowerupEffect
{
    public float speed;

    public override void TriggerEffect(NetworkObject obj)
    {
        obj.GetComponent<PredictionMoving>().moveRate += speed;
    }
}
