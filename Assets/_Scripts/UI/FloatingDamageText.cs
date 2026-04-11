using UnityEngine;

public class FloatingDamageText : MonoBehaviour
{
    private RectTransform rt;

    [SerializeField] private float moveSpeed = 30f;
    [SerializeField] private float lifetime = 1f;

    private Vector2 moveDirection;

    private void Awake()
    {
        rt = GetComponent<RectTransform>();

        float randomX = Random.Range(-0.7f, 0.7f);
        float randomY = Random.Range(0.8f, 1f);

        moveDirection = new Vector2(randomX, randomY).normalized;
    }

    private void Update()
    {
        rt.anchoredPosition += moveDirection * moveSpeed * Time.deltaTime;

        lifetime -= Time.deltaTime;
        if (lifetime <= 0f)
        {
            Destroy(gameObject);
        }
    }
}
