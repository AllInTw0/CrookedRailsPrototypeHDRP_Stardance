using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ObjectPool
{
    public GameObject prefab;
    public int maxCapacity;
    public int defaultCapacity;
    private List<GameObject> poolList;
    public ObjectPool(int maxCapacity, int defaultCapacity)
    {
        Init(maxCapacity, defaultCapacity);
    }
    public void Init(int maxCapacity, int defaultCapacity)
    {
        poolList = new List<GameObject>();

        this.maxCapacity = maxCapacity;

        for (int i = 0; i < defaultCapacity; i++)
        {
            poolList.Add(InstantiatePrefab());
            poolList[i].transform.position = new Vector3(0, -40f, 0f);
        }
    }

    private GameObject InstantiatePrefab()
    {
        GameObject copy = Object.Instantiate(prefab);
        return copy;
    }

    public GameObject Get()
    {
        if(poolList.Count > 0)
        {
            GameObject pooledObject = poolList[poolList.Count - 1];
            poolList.RemoveAt(poolList.Count - 1);
            return pooledObject;
        }
        else
        {
            return InstantiatePrefab();
        }
    }

    public void Add(GameObject gameObject)
    {
        if(poolList.Count < maxCapacity)
        {
            poolList.Add(gameObject);
            gameObject.transform.position = new Vector3(0, -40f, 0f);
        }
        else
        {
            Debug.Log("Reached max capacity: " + gameObject + " maxCapacity: " + maxCapacity);
            Object.Destroy(gameObject);
        }
    }
}
