using System;
using Unity.Netcode;
using UnityEngine;

public class TeleportOnStart : NetworkBehaviour
{
    public static TeleportOnStart active;

    private void Start()
    {
        active = this;
    }

    public void Teleport()
    {
        if(Player.localPlayer)
            Player.localPlayer.movement.Teleport(transform.position);
    }
}
