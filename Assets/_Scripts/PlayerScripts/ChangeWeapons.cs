using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

public class ChangeWeapons : MonoBehaviour
{
    [SerializeField]
    private List<GameObject> weapons = new List<GameObject>();

    private int currentWeaponIndex;
    private int changeWeaponInput;

    void Start()
    {
        GetWeapons(gameObject);
        changeWeaponInput = currentWeaponIndex;
    }

    void Update()
    {
        
    }

    public void OnWeaponChange(InputAction.CallbackContext context)
    {
        if (!context.performed)
            return;

        string keyName = context.control.name;

        if (int.TryParse(keyName, out int weaponNumber))
        {
            changeWeaponInput = weaponNumber;
            SwitchWeapons();
        }
    }

    private void SwitchWeapons()
    {
        if (changeWeaponInput == currentWeaponIndex)
            return;

        weapons[currentWeaponIndex - 1].SetActive(false);
        currentWeaponIndex = changeWeaponInput;
        weapons[currentWeaponIndex - 1].SetActive(true);
    }

    private void GetWeapons(GameObject parent)
    {
        foreach(Transform weapon in parent.transform)
        {
            if (weapon.CompareTag("Weapon"))
                weapons.Add(weapon.gameObject);
        }

        foreach(GameObject w in weapons)
        {
            w.SetActive(false);
        }

        weapons[0].SetActive(true);
        currentWeaponIndex = 1;
    }
}
