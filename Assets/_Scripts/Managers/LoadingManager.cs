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
