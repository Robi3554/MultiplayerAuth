using UnityEngine;

public class AfterImageFade : MonoBehaviour
{
    private float fadeTime = 0.15f;
    private float timer;
    private MeshRenderer[] renderers;
    private PredictionMoving poolOwner;
    private AfterImageInstance instance;

    public void Initialize(PredictionMoving owner, AfterImageInstance inst)
    {
        poolOwner = owner;
        instance = inst;
        renderers = inst.meshRenderers;
    }

    private void OnEnable()
    {
        timer = 0f;
    }

    private void Update()
    {
        timer += Time.deltaTime;
        float alpha = Mathf.Lerp(0.5f, 0f, timer / fadeTime);

        foreach (var r in renderers)
        {
            if (r.material.HasProperty("_BaseColor"))
            {
                Color c = r.material.GetColor("_BaseColor");
                r.material.SetColor("_BaseColor", new Color(c.r, c.g, c.b, alpha));
            }
        }

        if (timer >= fadeTime)
        {
            gameObject.SetActive(false);
            poolOwner.ReturnToPool(instance);
        }
    }
}
