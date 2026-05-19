using System;
using System.Collections.Generic;
using UnityEngine;

public class MovingPlatformEntry
{
    public Transform transform;
    public Transform rotationTransform;
    public bool onlyUpdateYRot;
    public Rigidbody rb;
    
    public Transform platform;
    public Quaternion platformLastRot;
    
    public Vector3 lastLocalPos;
    public Vector3 lastWorldPos;

    public Vector3 velocity;
    public void UpdateValues()
    {
        lastLocalPos = platform.InverseTransformPoint(transform.position);
        lastWorldPos = transform.position;
        platformLastRot = platform.rotation;
    }
}
public class MovingPlatformManager : MonoBehaviour
{
    public static MovingPlatformManager active;
    //Run Time
    private List<MovingPlatformEntry> entries = new List<MovingPlatformEntry>();

    private void Start()
    {
        active = this;
    }

    private void LateUpdate()
    {
        //Debug.Log("Entries: " + entries.Count);
        for (int i = 0; i < entries.Count; i++)
        {
            MovingPlatformEntry entry = entries[i];

            if (entry.transform == null || entry.rotationTransform == null)
            {
                entries.RemoveAt(i);
                i--;
                continue;
            }

            try
            {
                //Position
                Vector3 vector = entry.platform.TransformPoint(entry.lastLocalPos) - entry.lastWorldPos;
                if (entry.rb != null)
                    entry.velocity = vector / Time.deltaTime;
                entry.transform.position += vector;

                //Rotation
                Quaternion diffrence = entry.platform.rotation * Quaternion.Inverse(entry.platformLastRot);
                if (entry.onlyUpdateYRot)
                    entry.rotationTransform.Rotate(0f, diffrence.eulerAngles.y, 0f);
                else
                    entry.rotationTransform.Rotate(diffrence.eulerAngles);

                entry.UpdateValues();
            }
            catch
            {
                entries.RemoveAt(i);
                i--;
            }
        }
    }

    public void AddEntry(Transform entryTransform, Transform entryRotationTransform, Transform entryPlatform, bool entryOnlyUpdateYRot = false)
    {
        if(FindEntry(entryTransform) != -1)
            return;
        
        MovingPlatformEntry entry = new MovingPlatformEntry()
        {
            transform = entryTransform,
            rotationTransform = entryRotationTransform,
            platform = entryPlatform,
            onlyUpdateYRot = entryOnlyUpdateYRot
        };
        entry.UpdateValues();
        entries.Add(entry);
    }
    public void AddEntry(Rigidbody entryRigidBody, Transform entryRotationTransform, Transform entryPlatform, bool entryOnlyUpdateYRot = false)
    {
        if(FindEntry(entryRigidBody.transform) != -1)
            return;

        entryRigidBody.interpolation = RigidbodyInterpolation.None;
        MovingPlatformEntry entry = new MovingPlatformEntry()
        {
            transform = entryRigidBody.transform,
            rotationTransform = entryRotationTransform,
            rb = entryRigidBody,
            platform = entryPlatform,
            onlyUpdateYRot = entryOnlyUpdateYRot
        };
        entry.UpdateValues();
        entries.Add(entry);
    }

    public void RemoveEntry(Transform entryTransform)
    {
        int i = FindEntry(entryTransform);
        if (i != -1)
        {
            if (entries[i].rb != null)
            {
                entries[i].rb.linearVelocity += entries[i].velocity;
                entries[i].rb.interpolation = RigidbodyInterpolation.Interpolate;
            }
            entries.RemoveAt(i);
        }
    }

    private int FindEntry(Transform entryTransform)
    {
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i].transform == entryTransform)
            {
                return i;
            }
        }
        return -1;
    }
}
