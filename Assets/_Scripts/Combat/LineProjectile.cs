using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class LineProjectile : MonoBehaviour
{
    private LineRenderer lr;

    private Vector3 startPos;
    private Vector3 endPos;

    private float speed;

    public void Initialize(float speed, Vector3 startPos, Vector3 endPos)
    {
        lr = GetComponent<LineRenderer>();
        if (lr == null)
        {
            Debug.LogWarning("Shot Line Prefab has no LineRenderer!");
            Destroy(gameObject);
        }

        lr.positionCount = 2;
        lr.enabled = true;

        this.speed = speed;
        this.startPos = startPos;
        this.endPos = endPos;
        StartCoroutine(MoveProjectile());
    }

    private IEnumerator MoveProjectile()
    {
        float distance = Vector3.Distance(startPos, endPos);
        float travelTime = distance / speed;
        float elapsed = 0f;

        Vector3 direction = (endPos - startPos).normalized;

        while (elapsed < travelTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / travelTime;
            Vector3 currentPos = Vector3.Lerp(startPos, endPos, t);

            lr.SetPosition(0, currentPos);
            lr.SetPosition(1, currentPos - (direction * 0.5f));

            yield return null;
        }

        lr.enabled = false;
        Destroy(gameObject);
    }
}
