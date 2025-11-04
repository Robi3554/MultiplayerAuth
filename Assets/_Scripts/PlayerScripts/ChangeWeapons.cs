using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public class ChangeWeapons : NetworkBehaviour
{
    [SerializeField] private List<GameObject> weapons = new List<GameObject>();
    [SerializeField] private RigBuilder modelRigBuilder;
    
    private readonly SyncVar<int> _currentWeaponIndex = new SyncVar<int>(1);
    private int _newWeaponIndex;

    void Start()
    {
        GetWeapons(gameObject);
        
        if (IsClientStarted)
        {
            _currentWeaponIndex.OnChange += SwitchWeapon;
            SwitchWeapon(-1, _currentWeaponIndex.Value, IsServerOnlyStarted);
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

        weapons[_currentWeaponIndex.Value].SetActive(true);
    }

    public void OnChangeWeaponSlot(InputAction.CallbackContext context)
    {
        if (!context.performed || !IsOwner)
            return;

        var keyName = context.control.name;
        var scrollValue = context.ReadValue<float>();
        
        if (int.TryParse(keyName, out int weaponNumber))
        {
            _newWeaponIndex = weaponNumber;
        }
        else
        {
            if (scrollValue > 0f)
            {
                _newWeaponIndex++;
            }
            else if (scrollValue < 0f)
            {
                _newWeaponIndex--;
            }

            if (_newWeaponIndex > weapons.Count - 1)
                _newWeaponIndex = 0;
            else if (_newWeaponIndex < 0)
                _newWeaponIndex = weapons.Count - 1;

        }
        
        if (_newWeaponIndex == _currentWeaponIndex.Value)
            return;
        
        OnWeaponChangeServer(_newWeaponIndex);
    }
    
    [ServerRpc]
    private void OnWeaponChangeServer(int index)
    {
        _currentWeaponIndex.Value = index;
    }
    
    /*
    [ObserversRpc]
    private void OnWeaponChangeClient()
    {
        var activeWeaponIndex = _currentWeaponIndex.Value;
        
        SwitchActiveWeaponPrefab(activeWeaponIndex);
        UpdateRigLayers(activeWeaponIndex);
    }
    */
    
    private void SwitchWeapon(int prev, int newActiveIndex, bool asServer)
    {
        SwitchActiveWeaponPrefab(newActiveIndex);
        UpdateRigLayers(newActiveIndex);
    }

    private void SwitchActiveWeaponPrefab(int activeWeaponIndex)
    {
        for (int i = 0; i < weapons.Count; i++)
        {
            if (i == activeWeaponIndex)
            {
                weapons[i].SetActive(true);
            }
            else
            {
                weapons[i].SetActive(false);
            }
        }
    }
    
    private void UpdateRigLayers(int activeWeaponIndex)      //tine cont ca RigLayer-ul sa fie de format "RigLayer_<Weapon name>"
    {                                   //se poate imbunatati cu dictionare daca vrem sa facem mai eficient. pt 3 arme/player e ok
        var currentWeaponRigName = "RigLayer_" + weapons[activeWeaponIndex].name;
        
        foreach (var layer in modelRigBuilder.layers.Where(layer => layer.rig))
        {
            layer.active = layer.rig.name.Equals(currentWeaponRigName);
        }
        
        modelRigBuilder.Build();
    }

    /*
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
    
    private void UpdateWeaponModelVisual(int activeWeaponIndex) // this gets called for items that are fused to the hand for animation purposes(e.g. sword)
    {
        var currentWeaponModelName = weapons[activeWeaponIndex].name + "_Model"; // tineti formatul <WeaponsModelName>_Model (e.g. Sword_Model)
        
        foreach (var weapon in weaponsInHand)
        {
            weapon.SetActive(weapon.name.Equals(currentWeaponModelName));
        }
    }
    */
}
