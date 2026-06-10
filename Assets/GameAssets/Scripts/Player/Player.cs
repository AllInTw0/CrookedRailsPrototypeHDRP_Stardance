using Unity.Netcode;
using UnityEngine;

public class Player : NetworkBehaviour
{
    public PlayerMovement movement;
    void Start()
    {
        movement = GetComponent<PlayerMovement>();
        movement.Teleport(FindFirstObjectByType<TeleportOnStart>().transform.position);
    }
    
}
