using System;
using System.Collections.Generic;
using System.Linq;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.InputSystem;

public class ChangeWeapons : NetworkBehaviour
{
    [SerializeField] private List<GameObject> weapons = new List<GameObject>();
    [SerializeField] private RigBuilder modelRigBuilder;

    public static event Action<Sprite> OnLocalWeaponChanged;

    private readonly SyncVar<int> _currentWeaponIndex = new SyncVar<int>(1);
    private int _newWeaponIndex;

    internal bool canChange = true;

    void Start()
    {
        GetWeapons(gameObject);

        if (IsClientStarted)
        {
            _currentWeaponIndex.OnChange += SwitchWeapon;
            SwitchWeapon(-1, _currentWeaponIndex.Value, IsServerOnlyStarted);
        }
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        if (IsOwner)
        {
            SwitchWeapon(-1, _currentWeaponIndex.Value, false);
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

        if(weapons.Count > 0 && _currentWeaponIndex.Value < weapons.Count)
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
            _newWeaponIndex = weaponNumber - 1;
        }
        else if (keyName == "left")
        {
            _newWeaponIndex--;
            if (_newWeaponIndex < 0) _newWeaponIndex = weapons.Count - 1;
        }
        else if (keyName == "right")
        {
            _newWeaponIndex++;
            if (_newWeaponIndex > weapons.Count - 1) _newWeaponIndex = 0;
        }
        else
        {
            if (scrollValue > 0f) _newWeaponIndex++;
            else if (scrollValue < 0f) _newWeaponIndex--;

            if (_newWeaponIndex > weapons.Count - 1) _newWeaponIndex = 0;
            else if (_newWeaponIndex < 0) _newWeaponIndex = weapons.Count - 1;
        }

        _newWeaponIndex = Mathf.Clamp(_newWeaponIndex, 0, weapons.Count - 1);

        if (_newWeaponIndex == _currentWeaponIndex.Value)
            return;

        OnWeaponChangeServer(_newWeaponIndex);
    }

    [ServerRpc]
    private void OnWeaponChangeServer(int index)
    {
        _currentWeaponIndex.Value = index;
    }

    private void SwitchWeapon(int prev, int newActiveIndex, bool asServer)
    {
        if (!canChange)
            return;

        // Safety check
        if (newActiveIndex < 0 || newActiveIndex >= weapons.Count) return;

        SwitchActiveWeaponPrefab(newActiveIndex);
        UpdateRigLayers(newActiveIndex);

        if (IsOwner)
        {
            if (weapons[newActiveIndex].TryGetComponent<WeaponInfo>(out var info))
            {
                OnLocalWeaponChanged?.Invoke(info.HUDIcon);
            }
            else
            {
                OnLocalWeaponChanged?.Invoke(null);
            }
        }
    }

    private void SwitchActiveWeaponPrefab(int activeWeaponIndex)
    {
        for (int i = 0; i < weapons.Count; i++)
        {
            weapons[i].SetActive(i == activeWeaponIndex);
        }
    }

    private void UpdateRigLayers(int activeWeaponIndex)
    {
        if (activeWeaponIndex < 0 || activeWeaponIndex >= weapons.Count) return;

        var currentWeaponRigName = "RigLayer_" + weapons[activeWeaponIndex].name;

        foreach (var layer in modelRigBuilder.layers.Where(layer => layer.rig))
        {
            layer.active = layer.rig.name.Equals(currentWeaponRigName);
        }

        modelRigBuilder.Build();
    }
}