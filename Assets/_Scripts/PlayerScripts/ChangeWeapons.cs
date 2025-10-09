using System;
using System.Collections.Generic;
using FishNet.Object;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.InputSystem;

public class ChangeWeapons : NetworkBehaviour
{
    [SerializeField]
    private List<GameObject> weapons = new List<GameObject>();
    [SerializeField]
    private GameObject ModelRig;
    private RigBuilder rigBuilder;
    private int currentWeaponIndex;
    private int changeWeaponInput;

    void Start()
    {
        GetWeapons(gameObject);
        changeWeaponInput = currentWeaponIndex;
        rigBuilder = ModelRig.GetComponent<RigBuilder>();
    }

    public void OnWeaponChange(InputAction.CallbackContext context)
    {
        if (!context.performed || !IsOwner)
            return;

        string keyName = context.control.name;
        float scrollValue = context.ReadValue<float>();
        OnWeaponChangeServer(keyName,scrollValue);
    }
    [ServerRpc]
    private void OnWeaponChangeServer(string keyName, float scrollValue)
    {
        OnWeaponChangeClient(keyName, scrollValue);
    }
    [ObserversRpc]
    private void OnWeaponChangeClient(string keyName, float scrollValue)
    { 
        if (int.TryParse(keyName, out int weaponNumber))
        {
            changeWeaponInput = weaponNumber;
            SwitchWeapons();
        }
        else
        {
            if (scrollValue > 0f)
            {
                changeWeaponInput++;
            }
            else if (scrollValue < 0f)
            {
                changeWeaponInput--;
            }

            if (changeWeaponInput > weapons.Count)
                changeWeaponInput = 1;
            else if (changeWeaponInput < 1)
                changeWeaponInput = weapons.Count;

            SwitchWeapons();
        }
        UpdateRigLayers();
    }

    private void UpdateRigLayers()      //tine cont ca RigLayer-ul sa fie de format "RigLayer_<Weapon name>"
    {                                   //se poate imbunatati cu dictionare daca vrem sa facem mai eficient. pt 3 arme/player e ok
        string currentWeaponRigName = "RigLayer_" + weapons[currentWeaponIndex - 1].name;
        Debug.Log(currentWeaponRigName);
        for (int i = 0; i < rigBuilder.layers.Count; i++)
        {
            var layer = rigBuilder.layers[i];
            if (layer.rig.name == currentWeaponRigName)
            {
                layer.active = true;
            }
            else if (layer.rig != null)
            {
                layer.active = false;
            }
            rigBuilder.layers[i] = layer;
        }
        rigBuilder.Build();
    }

    private void SwitchWeapons()
    {
        if (changeWeaponInput == currentWeaponIndex)
            return;

        weapons[currentWeaponIndex - 1].SetActive(false);
        currentWeaponIndex = changeWeaponInput;
        var newWeapon = weapons[currentWeaponIndex - 1];
        newWeapon.SetActive(true);

        if (IsOwner)
        {
            var weaponScript = newWeapon.GetComponent<RaycastShoot>();
            if (weaponScript != null)
            {
                weaponScript.InitializeWeapon();
            }

            RequestWeaponOwnershipServerRpc(currentWeaponIndex - 1);
        }
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

    [ServerRpc]
    private void RequestWeaponOwnershipServerRpc(int newWeaponIndex)
    {
        var newWeaponObj = weapons[newWeaponIndex].GetComponent<NetworkObject>();
        if (newWeaponObj == null)
        {
            Debug.Log("No Netwrok Object Found!");
            return;
        }

        newWeaponObj.GiveOwnership(Owner);
    }
}
