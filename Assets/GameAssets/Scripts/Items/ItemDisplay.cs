using UnityEngine;
using System.Collections.Generic;
public class ItemDisplay : MonoBehaviour
{
    [SerializeField]
    private List<GameObject> prefabList;
    [SerializeField]
    private Vector2 spacing;
    [SerializeField]
    private Vector3 rotation;
    [SerializeField]
    private float posRandomness;
    [SerializeField]
    private float sizeRandomness;
    [SerializeField]
    private float rotRandomness;
    [SerializeField]
    private int bottomRowCount;

    [SerializeField]
    private Interactable linkedInteractable;
    //Run time
    private List<GameObject> objectList = new List<GameObject>();
    public void SetTarget(int targetCount)
    {
        while (objectList.Count != targetCount)
        {
            if(objectList.Count < targetCount)
            {
                //Add
                GameObject copy = Instantiate(prefabList[Random.Range(0, prefabList.Count)], transform);

                int row = 0;
                int rowObjCount = 0;
                while (objectList.Count >= rowObjCount + bottomRowCount - row)
                {
                    rowObjCount += bottomRowCount - row;
                    row++;
                }

                copy.transform.localPosition = new Vector3((objectList.Count - rowObjCount + row * 0.5f) * spacing.x, row * spacing.y) + new Vector3(Random.Range(-posRandomness, posRandomness), Random.Range(-posRandomness, posRandomness), Random.Range(-posRandomness, posRandomness));
                copy.transform.localRotation = Quaternion.Euler(rotation + new Vector3(Random.Range(-rotRandomness,rotRandomness), Random.Range(-rotRandomness, rotRandomness), Random.Range(-rotRandomness, rotRandomness)));
                copy.transform.localScale = Vector3.one + new Vector3(Random.Range(-sizeRandomness, sizeRandomness), Random.Range(-sizeRandomness, sizeRandomness), Random.Range(-sizeRandomness, sizeRandomness));
                objectList.Add(copy);
            }
            else
            {
                //Remove
                Destroy(objectList[^1]);
                objectList.RemoveAt(objectList.Count - 1);
            }
        }
        if (objectList.Count > 0)
            linkedInteractable.iconPosition = objectList[^1].transform;
        else
            linkedInteractable.iconPosition = transform;
    }
}
