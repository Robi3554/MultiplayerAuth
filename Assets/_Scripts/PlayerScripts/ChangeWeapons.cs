using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.InputSystem;

public class ChangeWeapons : MonoBehaviour
{
    [SerializeField]
    private RigBuilder characterRigBuilder;
    [SerializeField]
    private List<GameObject> weapons = new List<GameObject>();

    private int currentWeaponIndex = 1;

    void Start()
    {
        GetWeapons(gameObject);

        if (characterRigBuilder.layers.Count < weapons.Count)
        {
            Debug.Log("There are fewer weapon rig layers assigned than existing weapons");
        }
    }

    void Update()
    {
        
    }

    public void OnWeaponChange(InputAction.CallbackContext context)
    {
        if (!context.performed)
            return;

        string keyName = context.control.name;

        if (int.TryParse(keyName, out int keyboardNumber))
        {
            SwitchWeapon(keyboardNumber);
        }
    }

    private void SwitchWeapon(int newWeaponIndex)
    {
        if (newWeaponIndex == currentWeaponIndex)
            return;

        weapons[currentWeaponIndex].SetActive(false);
        characterRigBuilder.layers[currentWeaponIndex].active = false;
        
        weapons[newWeaponIndex].SetActive(true);
        characterRigBuilder.layers[newWeaponIndex].active = true;

        currentWeaponIndex = newWeaponIndex;
    }

    private void GetWeapons(GameObject parent)
    {
        foreach(Transform weapon in parent.transform)
        {
            if (weapon.CompareTag("Weapon"))
                weapons.Add(weapon.gameObject);
        }

        for (int i = 0; i < weapons.Count; i++)
        {
            weapons[i].SetActive(false);
            characterRigBuilder.layers[currentWeaponIndex].active = false;
        }

        weapons[currentWeaponIndex].SetActive(true);
        characterRigBuilder.layers[currentWeaponIndex].active = true;
    }
}
