using System.Collections;
using FishNet;
using FishNet.Managing.Scened;
using UnityEngine;

public class LoadingManager : MonoBehaviour
{
    public static LoadingManager Instance;

    [SerializeField] private GameObject loadingScreen;
    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        loadingScreen.SetActive(false);
    }

    private void OnEnable()
    {
        InstanceFinder.SceneManager.OnLoadStart += HandleLoadStart;
        InstanceFinder.SceneManager.OnLoadEnd += HandleLoadEnd;
    }

    private void OnDisable()
    {
        InstanceFinder.SceneManager.OnLoadStart -= HandleLoadStart;
        InstanceFinder.SceneManager.OnLoadEnd -= HandleLoadEnd;
    }

    private void HandleLoadStart(SceneLoadStartEventArgs args)
    {
        Show();
    }

    private void HandleLoadEnd(SceneLoadEndEventArgs args)
    {
        StartCoroutine(HideAfterLoad());
    }

    private IEnumerator HideAfterLoad()
    {
        yield return new WaitForSeconds(0.5f);

        Hide();
    }

    public void Show()
    {
        loadingScreen.SetActive(true);
    }

    public void Hide()
    {
        loadingScreen.SetActive(false);
    }
}
