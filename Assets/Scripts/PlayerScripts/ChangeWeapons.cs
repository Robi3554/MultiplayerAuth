using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

public class ChangeWeapons : MonoBehaviour
{
    private List<GameObject> weapons = new List<GameObject>();

    private float currentWeaponIndex;
    private float changeWeaponInput;

    void Start()
    {
        GetWeapons(gameObject);
    }

    void Update()
    {
        
    }

    public void OnWeaponChange(InputAction.CallbackContext context)
    {
        if(context.performed)
        {
            if (context.control == Keyboard.current.digit1Key)
                changeWeaponInput = 1;
            if (context.control == Keyboard.current.digit2Key)
                changeWeaponInput = 2;

            SwitchWeapons();
        }
    }

    private void SwitchWeapons()
    {
        float weaponToDisableIndex = currentWeaponIndex;
        currentWeaponIndex = changeWeaponInput;

        weapons[(int)weaponToDisableIndex - 1].SetActive(false);
        weapons[(int)currentWeaponIndex - 1].SetActive(true);
    }

    private void GetWeapons(GameObject parent)
    {
        foreach(Transform weapon in parent.transform)
        {
            if (weapon.CompareTag("Weapon"))
                weapons.Add(weapon.gameObject);
        }

        currentWeaponIndex = 1;
    }
}
