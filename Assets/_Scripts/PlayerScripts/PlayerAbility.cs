using System.Collections;
using FishNet.Object;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PlayerAbility : NetworkBehaviour
{
    [SerializeField]
    private Image abilityOverlay;

    [SerializeField]
    private float abilityCooldown;

    private bool abilityPressed = false;

    private Coroutine _cooldownRoutine;

    void Start()
    {
        StartCooldown(abilityCooldown);
    }

    public void OnAbilityPress(InputAction.CallbackContext context)
    {
        if (!context.performed || abilityPressed) return;

        ActivateAbilty();
    }

    void Update()
    {
        
    }

    protected virtual void ActivateAbilty()
    {
        Debug.Log("Start timer");
        StartCooldown(abilityCooldown);
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
        abilityOverlay.fillAmount = 1f;
        abilityPressed = true;

        while (timer > 0f)
        {
            timer -= Time.deltaTime;
            abilityOverlay.fillAmount = timer / duration;
            yield return null;
        }

        abilityOverlay.fillAmount = 0f;
        _cooldownRoutine = null;
        abilityPressed = false;
    }
}
