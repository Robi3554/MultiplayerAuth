using TMPro;
using UnityEngine;

public class AmmoCounter : MonoBehaviour
{
    [SerializeField]
    private TMP_Text ammoText;

    public int maxAmmo;
    public int currentAmmo;

    void Start()
    {
        currentAmmo = maxAmmo;
    }

    void Update()
    {
        ammoText.text = currentAmmo.ToString() + '/' + maxAmmo.ToString();
    }
}
