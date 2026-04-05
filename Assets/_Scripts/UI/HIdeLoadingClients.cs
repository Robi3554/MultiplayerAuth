using System.Collections;
using UnityEngine;

public class HIdeLoadingClients : MonoBehaviour
{
    private IEnumerator Start()
    {
        yield return new WaitForSeconds(0.5f);

        if (LoadingManager.Instance != null)
            LoadingManager.Instance.Hide();
        else
            Debug.Log("Loadgin Manager not found!");
    }
}
