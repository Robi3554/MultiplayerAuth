using UnityEngine;
using UnityEngine.UI;

public class WeaponHUD : MonoBehaviour
{
    [SerializeField] private Image _iconImage;

    private void OnEnable()
    {
        ChangeWeapons.OnLocalWeaponChanged += UpdateIcon;
    }

    private void OnDisable()
    {
        ChangeWeapons.OnLocalWeaponChanged -= UpdateIcon;
    }

    private void UpdateIcon(Sprite newSprite)
    {
        if (newSprite != null)
        {
            _iconImage.sprite = newSprite;
            _iconImage.enabled = true;
        }
        else
        {
            _iconImage.enabled = false; 
        }
    }
}