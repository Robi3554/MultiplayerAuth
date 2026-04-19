using FishNet.Component.Animating;
using FishNet.Object;
using UnityEngine;
using UnityEngine.InputSystem;

public class EmoteSystem : NetworkBehaviour
{
    [SerializeField]
    private Animator animator; 
    [SerializeField]
    private NetworkAnimator netAnimator;
    private PlayerStats _playerStats;

    private void Awake()
    {
        _playerStats = GetComponent<PlayerStats>();
    }

    public void OnEmote(InputAction.CallbackContext context)
    {
        if (_playerStats != null && _playerStats.isRespawning.Value)
            return;

        string emoteName = context.control.name;
        ChangeEmote(emoteName);
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        if (_playerStats != null && _playerStats.isRespawning.Value)
            return;
        
        ChangeEmote("");
    }
    
    private void ChangeEmote(string emoteName)
    {
            emoteName = emoteName.ToUpper();
        switch (emoteName)
        {
            case "F1":
                animator.SetInteger("EmoteNumber", 1);
                netAnimator.SetTrigger("EmoteTrigger");
                break;
            case "F2":
                animator.SetInteger("EmoteNumber", 2);
                netAnimator.SetTrigger("EmoteTrigger");
                break;
            case "F3":
                animator.SetInteger("EmoteNumber", 3);
                netAnimator.SetTrigger("EmoteTrigger");
                break;
            default:
                animator.SetInteger("EmoteNumber", 0);
                break;
        }
    }
}
