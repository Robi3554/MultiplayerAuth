using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class WeaponHUD : MonoBehaviour
{
    [SerializeField] private Image _iconImage;
    [SerializeField] private Image _cooldownOverlayImage;

    private Coroutine _cooldownRoutine;

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

            _cooldownOverlayImage.sprite = newSprite;
            _cooldownOverlayImage.enabled = true;
            _cooldownOverlayImage.fillAmount = 0;
        }
        else
        {
            _iconImage.enabled = false; 
            _cooldownOverlayImage.enabled = false;
        }
    }

    public void StartCooldown(float duration)
    {
        if (_cooldownRoutine != null)
            StopCoroutine(_cooldownRoutine);

        _cooldownRoutine = StartCoroutine(CooldownRoutine(duration));
    }

    private IEnumerator CooldownRoutine(float duration)
    {
        float timer = duration;
        _cooldownOverlayImage.fillAmount = 1f;

        while (timer > 0f)
        {
            timer -= Time.deltaTime;
            _cooldownOverlayImage.fillAmount = timer / duration;
            yield return null;
        }

        _cooldownOverlayImage.fillAmount = 0f;
        _cooldownRoutine = null;
    }
}