using FishNet.Object;
using UnityEngine;

public abstract class PowerupEffect : ScriptableObject
{
    public abstract void TriggerEffect(NetworkObject obj);
}
