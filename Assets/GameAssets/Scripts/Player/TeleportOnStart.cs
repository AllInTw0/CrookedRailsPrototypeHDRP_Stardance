using UnityEngine;

public class TeleportOnStart : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        PlayerMovement.active.rb.position = transform.position;
    }

}
