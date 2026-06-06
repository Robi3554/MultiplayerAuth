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

    public IWeaponInfo CurrentWeaponInfo { get; private set; }

    public static event Action<Sprite> OnLocalWeaponChanged;

    private readonly SyncVar<int> _currentWeaponIndex = new SyncVar<int>(1);

    public int CurrentWeaponIndex => _currentWeaponIndex.Value;

    private int _newWeaponIndex;

    internal bool canChange = true;

    private int serverAnalyticsActiveWeaponId = -1;
    private string serverAnalyticsActiveWeaponName;
    private float serverAnalyticsActiveWeaponStartTime;

    void Start()
    {
        GetWeapons(gameObject, true);

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

    public override void OnStartServer()
    {
        base.OnStartServer();
        GetWeapons(gameObject, false);
        BeginServerWeaponAnalytics(_currentWeaponIndex.Value);
    }

    public override void OnStopServer()
    {
        FlushServerWeaponAnalyticsNow();
        base.OnStopServer();
    }

    private void GetWeapons(GameObject parent, bool applyActiveWeapon)
    {
        if (weapons.Count == 0)
        {
            foreach(Transform weapon in parent.transform)
            {
                if (weapon.CompareTag("Weapon"))
                    weapons.Add(weapon.gameObject);
            }
        }

        if (!applyActiveWeapon)
            return;

        foreach(GameObject w in weapons)
            w.SetActive(false);

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
        FlushServerWeaponAnalyticsNow();
        _currentWeaponIndex.Value = index;
        BeginServerWeaponAnalytics(index);
    }

    private void SwitchWeapon(int prev, int newActiveIndex, bool asServer)
    {
        if (!canChange)
            return;

        // Safety check
        if (newActiveIndex < 0 || newActiveIndex >= weapons.Count) return;

        SwitchActiveWeaponPrefab(newActiveIndex);
        UpdateRigLayers(newActiveIndex);

        CurrentWeaponInfo = weapons[newActiveIndex].GetComponent<IWeaponInfo>();

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

    public int GetCurrentWeaponId()
    {
        if (_currentWeaponIndex.Value < 0 || _currentWeaponIndex.Value >= weapons.Count)
            return -1;

        var weaponInfo = weapons[_currentWeaponIndex.Value].GetComponentInChildren<IWeaponInfo>(true);

        return weaponInfo != null ? weaponInfo.WeaponId : -1;
    }

    [Server]
    public void FlushServerWeaponAnalyticsNow()
    {
        if (weapons.Count == 0)
            GetWeapons(gameObject, false);

        if (serverAnalyticsActiveWeaponId < 0)
            return;

        float secondsUsed = Time.realtimeSinceStartup - serverAnalyticsActiveWeaponStartTime;
        if (secondsUsed > 0f)
            AnalyticsManager.EnsureInstance().RecordWeaponUsage(Owner.ClientId, serverAnalyticsActiveWeaponId, serverAnalyticsActiveWeaponName, secondsUsed);

        serverAnalyticsActiveWeaponId = -1;
        serverAnalyticsActiveWeaponName = null;
    }

    [Server]
    private void BeginServerWeaponAnalytics(int weaponIndex)
    {
        if (weapons.Count == 0)
            GetWeapons(gameObject, false);

        serverAnalyticsActiveWeaponId = GetWeaponIdAtIndex(weaponIndex);
        serverAnalyticsActiveWeaponName = GetWeaponNameAtIndex(weaponIndex);
        serverAnalyticsActiveWeaponStartTime = Time.realtimeSinceStartup;
    }

    private int GetWeaponIdAtIndex(int weaponIndex)
    {
        if (weaponIndex < 0 || weaponIndex >= weapons.Count)
            return -1;

        var weaponInfo = weapons[weaponIndex].GetComponentInChildren<IWeaponInfo>(true);
        return weaponInfo != null ? weaponInfo.WeaponId : -1;
    }

    private string GetWeaponNameAtIndex(int weaponIndex)
    {
        if (weaponIndex < 0 || weaponIndex >= weapons.Count)
            return null;

        GameObject weapon = weapons[weaponIndex];
        if (weapon == null)
            return null;

        return weapon.name;
    }
}
