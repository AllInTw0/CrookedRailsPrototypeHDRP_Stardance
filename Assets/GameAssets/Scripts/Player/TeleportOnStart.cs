using System;
using Unity.Netcode;
using UnityEngine;

public class TeleportOnStart : NetworkBehaviour
{
    public static TeleportOnStart active;

    private void Awake()
    {
        active = this;
    }

    public override void OnNetworkSpawn()
    {
        Teleport();
    }

    public void Teleport()
    {
        if(Player.localPlayer)
            Player.localPlayer.movement.Teleport(transform.position);
    }
}
