using UnityEngine;

public class AfterImageFade : MonoBehaviour
{
    public float fadeTime = 0.2f;
    private float timer;
    private Material[] materials;

    void Start()
    {
        MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>();
        materials = new Material[renderers.Length];

        for (int i = 0; i < renderers.Length; i++)
            materials[i] = renderers[i].material;
    }

    void Update()
    {
        timer += Time.deltaTime;
        float alpha = Mathf.Lerp(0.5f, 0f, timer / fadeTime);

        foreach (var mat in materials)
        {
            if (mat.HasProperty("_BaseColor"))
            {
                Color c = mat.GetColor("_BaseColor");
                c.a = alpha;
                mat.SetColor("_BaseColor", c);
            }
        }

        if (timer >= fadeTime)
            Destroy(gameObject);
    }
}
