using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class Item : Interactable
{
    //Variables
    [Header("Item")]
    public ItemSO itemInfo;

    [SerializeField] 
    private List<MeshRenderer> meshRenderers;
    [SerializeField] 
    public Collider interactableCollider;

    public int count = 1;

    //Run time
    [HideInInspector]
    public bool falling;
    private float targetY;
    private float distance;
    private float velocity;
    private float rotVelocity;

    private Rigidbody rb;
    private bool physicsEnabled;
    private bool dropSilently;
    private void Awake()
    {
        if (TryGetComponent(out Rigidbody rigidbody))
        {
            rb = rigidbody;
        }
        else
        {
            rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
        }
    }
    void Start()
    {
        DropFromPos(transform.position);
    }

    public override bool Interact()
    {
        bool success = PlayerInventory.active.TryEquipping(this);
        if (success)
        {
            falling = false;
            DisablePhysics();  
        }
        Debug.Log("Item Equipped Successfully: " + success);

        base.Interact();
        return success;
    }

    public override string GetName()
    {
        if (itemInfo.maxCount > 1)
        {
            return objectName + " [" + count + "]";
        }
        //else
        return objectName;
    }
    private void Update()
    {
        UpdateItem();
    }

    public void UpdateItem()
    {
        if (physicsEnabled)
        {
            falling = false;
            if(rb.linearVelocity.magnitude <= 0.1f)
            {
                DropFromPos(transform.position);
            }
        }
        else
        {
            rb.isKinematic = true;
        }
        if (falling)
        {
            velocity += 10f * Time.deltaTime;
            transform.position -= new Vector3(0, velocity * Time.deltaTime, 0f);
            transform.Rotate(0f, rotVelocity * Time.deltaTime, 0f);
            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(0, transform.eulerAngles.y, 0), (distance - (transform.position.y - targetY)) / distance);
            if (transform.position.y < targetY)
            {
                falling = false;
                transform.position = new Vector3(transform.position.x, targetY, transform.position.z);
                transform.rotation = Quaternion.Euler(0, transform.eulerAngles.y, 0);

                if (dropSilently == false)
                    SoundManager.active.PlayAtPos(transform.position, "Item - Drop");
                else
                    dropSilently = false;
            }
        }
    }
    public void DropFromPos(Vector3 startPos, bool hasSound = true)
    {
        transform.position = startPos;
        falling = true;
        velocity = 0;
        rotVelocity = Random.Range(-35f, 35f);
        if (hasSound == false) dropSilently = true;
        if (Physics.Raycast(startPos + new Vector3(0f,0.05f,0f), Vector3.down, out RaycastHit hit, 100f, ItemManager.itemDropLayerMask))
        {
            targetY = hit.point.y;
            distance = transform.position.y - targetY;

            if (hit.transform.CompareTag("Moving"))
            {
                MovingPlatformManager.active.AddEntry(transform,transform,hit.transform);
            }

            DisablePhysics();
        }
        else
        {
            falling = false;
            Debug.Log("No Ground Found");
        }
    }
    public void BecomeInvisible()
    {
        transform.position = new Vector3(0f, -50f, 0f); //Look below
        foreach (var mesh in meshRenderers)
        {
            mesh.enabled = false;
        }
        //This is done so triggers can detect if an item has exited it when picked up by the player
        //interactableCollider.enabled = false;
        falling = false;
    }
    public void BecomeVisible()
    {
        foreach (var mesh in meshRenderers)
        {
            mesh.enabled = true;
        }
        interactableCollider.enabled = true;
    }
    public void EnablePhysics(Vector3 velocity)
    {
        if (interactableCollider.enabled == false) return;

        MovingPlatformManager.active.RemoveEntry(transform);
        rb.isKinematic = false;
        falling = false;
        physicsEnabled = true;
        rb.linearVelocity = velocity;
        rb.angularVelocity = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f));
    }
    public void AddExplosionForce(Vector3 explosionPos, float force, float range)
    {
        if(physicsEnabled == false)
        {
            EnablePhysics(Vector3.zero);
        }
        rb.AddExplosionForce(force, explosionPos, range);
    }
    public void DisablePhysics()
    {
        rb.isKinematic = true;
        physicsEnabled = false;
    }
    public bool IsPhysicsEnabled()
    {
        return physicsEnabled;
    }
    public static Item SpawnItem(ItemSO itemSO, Vector3 pos, Quaternion rot, int stackCount = 1, bool dropSound = true)
    {
        
        Transform copy = Instantiate(itemSO.prefab,pos,rot).transform;

        Item item = copy.GetComponent<Item>();
        item.DropFromPos(pos, dropSound);
        item.count = Mathf.Clamp(stackCount, 1, item.itemInfo.maxCount);

        return item;
    }
}
