using FishNet.Object;
using FishNet.Object.Prediction;
using FishNet.Transporting;
using UnityEngine;

public class PickUpObject : NetworkBehaviour
{
    [SerializeField] private bool yRotation = true;
    [SerializeField] private bool yMove = true;
    [SerializeField] private float minHeight = 0.5f;
    [SerializeField] private float maxHeight = 1.5f;
    [SerializeField] private float rotateSpeed= 100f;
    private bool itemPickedUp = false; 
    public override void OnStartClient()
    {
        base.OnStartClient();
        if (!IsOwner)
        {
            return; // idk ce sa pun aici sincer
        }
    }

     private void OnTriggerEnter(Collider other){
        if (!itemPickedUp)
        { 
            Debug.Log("item collided with");
            if (other.gameObject.layer == LayerMask.NameToLayer("Player"))
            {
                itemPickedUp = true;
                ItemPickUp(other);
            }   
        }
    }
    virtual protected void ItemPickUp(Collider other)
    {
        Debug.Log("Default item pickup does nothing");
    }
    // Update is called once per frame
    void Update()
    {
        if (yMove) { // item oscilates up and down
            float t = Mathf.PingPong(Time.time, 1f); // Goes from 0 to 1 and back
            float easedT = Mathf.SmoothStep(0f, 1f, t); // Eases at ends
            float y = Mathf.Lerp(minHeight, maxHeight, easedT);
            transform.position = new Vector3(transform.position.x, y, transform.position.z);
        }
        if (yRotation)
        { // item rotates around the y axis
            Vector3 rotation = Vector3.zero;
            rotation += Vector3.forward;
            transform.Rotate(rotation * rotateSpeed * Time.deltaTime);
        }
    }
}
