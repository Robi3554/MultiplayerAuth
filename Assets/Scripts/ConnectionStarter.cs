using System;
using FishNet.Transporting.Tugboat;
using UnityEngine;

public class ConnectionStarter : MonoBehaviour
{
    private Tugboat _tugboat;
    
    private void Start()
    {
        if (TryGetComponent(out Tugboat _t))
        {
            _tugboat = _t;
        }
        else
        {
            Debug.LogError("Could not find Tugboat");
            return;
        }
        
        if (ParrelSync.ClonesManager.IsClone())
        {
            _tugboat.StartConnection(false);

        }
        else
        {
            _tugboat.StartConnection(true);
            _tugboat.StartConnection(false);

        }
    }
}
