using UnityEngine;

public class TutorialSwitch : MonoBehaviour
{
    public GameObject[] panels;

    private int currentIndex = 0;

    private void ShowPanel(int index)
    {
        for (int i = 0; i < panels.Length; i++)
        {
            panels[i].SetActive(i == index);
        }
    }

    public void Next()
    {
        currentIndex = (currentIndex + 1) % panels.Length;
        ShowPanel(currentIndex);
    }

    public void Previous()
    {
        currentIndex = (currentIndex - 1 + panels.Length) % panels.Length;
        ShowPanel(currentIndex);
    }
}
