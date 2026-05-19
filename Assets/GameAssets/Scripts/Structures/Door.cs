using System.Collections.Generic;
using UnityEngine;


public class Door : MonoBehaviour
{
    public enum DoorType
    {
        Hinge,
        Sliding
    }
    [System.Serializable]
    public struct DoorEntry
    {
        public Transform doorTransform;
        public DoorType doorType;
        public Vector3 startVector;
        public Vector3 endVector;
    }

    //Variables
    [Header("Door")]
    public List<DoorEntry> doorList = new List<DoorEntry>();
    public float doorSpeed = 0.5f;

    //Run time
    private bool open;
    private float time;

    private void Update()
    {
        UpdateDoor();
    }
    public void UpdateDoor()
    {
        if (open)
            time += doorSpeed * Time.deltaTime;
        else
            time -= doorSpeed * Time.deltaTime;

        time = Mathf.Clamp01(time);

        foreach (DoorEntry door in doorList)
        {
            if (door.doorType == DoorType.Sliding)
            {
                door.doorTransform.localPosition = Vector3.Lerp(door.startVector, door.endVector, time);
            }
            else
            {
                door.doorTransform.localRotation = Quaternion.Slerp(Quaternion.Euler(door.startVector), Quaternion.Euler(door.endVector), time);
            }
        }
    }

    public void Open()
    {
        if (open == false)
            SoundManager.active.PlayAtPos(transform.position, "Door - Sliding");
        open = true;
    }
    public void Close()
    {
        if (open == true)
            SoundManager.active.PlayAtPos(transform.position, "Door - Sliding");
        open = false;
    }
    public void Toggle()
    {
        open = !open;
    }
}
