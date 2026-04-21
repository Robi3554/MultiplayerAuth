using UnityEngine;

public class ParticleDeath : MonoBehaviour
{
    public float seconds = 3f;

    void Start()
    {
        Debug.Log("Destroy visual effect!");
        Destroy(gameObject, seconds);
    }
}
