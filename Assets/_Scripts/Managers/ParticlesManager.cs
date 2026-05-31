using FishNet.Object;
using UnityEngine;

public class ParticlesManager : NetworkBehaviour
{
    public static ParticlesManager Instance;


    [SerializeField] private GameObject explosion;
    [SerializeField] private GameObject smallExplosion;

    private void Awake()
    {
        if (Instance!= null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    [ObserversRpc]
    public void PlayEffect(Vector3 pos, EffectType effect)
    {
        GameObject prefab = null;

        switch (effect)
        {
            case EffectType.Explosion:
                prefab = explosion;
                break;
            case EffectType.SmallExplosion:
                prefab = smallExplosion;
                break;
        }

        Instantiate(prefab, pos, Quaternion.identity);
    }
}
