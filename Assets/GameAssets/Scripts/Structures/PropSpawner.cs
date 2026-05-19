using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Util;

public class PropSpawner : StructureGenerator
{
    [SerializeField]
    private List<ProbabilityListElement<GameObject>> propProbabilityList = new List<ProbabilityListElement<GameObject>>();
    [SerializeField]
    private LayerMask overlapCheckLayerMask;
    [SerializeField]
    private int maxPropCount;
    [SerializeField]
    private int maxSpawnTries;

    public override IEnumerator Generate(StructureMaster structureMaster)
    {
        int spawnedPropCount = 0;
        int spawnTries = 0;

        ProbabilityList<GameObject> probabilityList = new ProbabilityList<GameObject>(propProbabilityList);

        PropSpawnBoundingBox[] propSpawnArray = transform.GetComponentsInChildren<PropSpawnBoundingBox>();

        while (spawnTries < maxSpawnTries && spawnedPropCount < maxPropCount && probabilityList.HasItemsLeft())
        {
            //Spawn prop
            GameObject chosenProp = probabilityList.PickNext(false);
            BoxCollider propBoundingBox = chosenProp.GetComponent<BoxCollider>();

            //Get list of boundingBoxes the prop fits in
            List<PropSpawnBoundingBox> propSpawnList = new List<PropSpawnBoundingBox>(propSpawnArray);
            for (int i = 0; i < propSpawnList.Count; i++)
            {
                if (propSpawnList[i].DoseBoundingBoxFit(propBoundingBox) == false || DoseBoxFitInBox(propSpawnList[i].boundingBox, propBoundingBox) == false)
                {
                    //Debug.Log("Dosent fit");
                    propSpawnList.RemoveAt(i);
                    i--;
                } 
            }

            if(propSpawnList.Count == 0)
            {
                Debug.LogWarning(chosenProp + " dosent fit");
            }
            else
            {
                PropSpawnBoundingBox targetBoundingBoxScript = propSpawnList[Random.Range(0, propSpawnList.Count)];
                BoxCollider targetBoundingBox = targetBoundingBoxScript.boundingBox;

                Vector2 prop2DBounds = new Vector2(propBoundingBox.size.x, propBoundingBox.size.z);
                if (prop2DBounds.y > prop2DBounds.x) prop2DBounds = new Vector2(prop2DBounds.y, prop2DBounds.x);
                float prop2DHypotenuse = prop2DBounds.magnitude;
                Vector2 target2DBounds = new Vector2(targetBoundingBox.size.x, targetBoundingBox.size.z);
                if (target2DBounds.y > target2DBounds.x) target2DBounds = new Vector2(target2DBounds.y, target2DBounds.x);

                //Max rotOffset that fits box
                float rotLimit = Mathf.Rad2Deg * Mathf.Asin(Mathf.Min(target2DBounds.x, target2DBounds.y) / prop2DHypotenuse); // īsākā katete / hipotenūzu
                float propHypotenuseRot = Mathf.Rad2Deg * Mathf.Asin(Mathf.Min(prop2DBounds.x, prop2DBounds.y) / prop2DHypotenuse); // īsākā katete / hipotenūzu
                rotLimit -= propHypotenuseRot;


                Vector2 maxOffset;
                if (rotLimit is float.NaN) //If rot limit is NaN then there is no limit
                {
                    //Debug.Log("Rot is nan");
                    rotLimit = 180f;
                }


                float randomRot = Random.Range(-rotLimit, rotLimit);
                float calcRot = randomRot;
                while (calcRot >= 90f || calcRot <= -90f)
                {
                    if (calcRot < 0) calcRot += 90f;
                    else calcRot -= 90f;
                }

                Vector2 maxOffset1 = new Vector2(target2DBounds.x - Mathf.Cos((propHypotenuseRot + Mathf.Abs(calcRot)) * Mathf.Deg2Rad) * prop2DHypotenuse, target2DBounds.y - Mathf.Sin((propHypotenuseRot + Mathf.Abs(calcRot)) * Mathf.Deg2Rad) * prop2DHypotenuse);
                Vector2 maxOffset2 = new Vector2(target2DBounds.x - Mathf.Cos((propHypotenuseRot - Mathf.Abs(calcRot)) * Mathf.Deg2Rad) * prop2DHypotenuse, target2DBounds.y - Mathf.Sin((propHypotenuseRot - Mathf.Abs(calcRot)) * Mathf.Deg2Rad) * prop2DHypotenuse);
                maxOffset = new Vector2(Mathf.Min(maxOffset1.x, maxOffset2.x) * 0.5f, Mathf.Min(maxOffset1.y, maxOffset2.y) * 0.5f);
                //Debug.Log("rotLimit: " + rotLimit + ", randomRot: " + randomRot + ", calcRot: " + calcRot + ", maxOffset: " + maxOffset);

                Vector2 offset = new Vector2(Random.Range(-maxOffset.x, maxOffset.x), Random.Range(-maxOffset.y, maxOffset.y));

                //Spawn prop
                GameObject propCopy = Instantiate(chosenProp, targetBoundingBox.transform);

                //A little spaghety but it works
                void SetPos(bool case1, bool case2)
                {
                    if (case1 == false)
                    {
                        propCopy.transform.localPosition = new Vector3(offset.x, 0, offset.y);
                        propCopy.transform.localRotation = Quaternion.Euler(0, randomRot + 90, 0);

                        Debug.DrawLine(targetBoundingBox.transform.position, propCopy.transform.position, case2 ? Color.green : Color.red, 60f);
                    }
                    else
                    {
                        if(case2)
                            propCopy.transform.localPosition = new Vector3(offset.x, 0, offset.y);
                        else
                            propCopy.transform.localPosition = new Vector3(offset.y, 0, offset.x);
                        propCopy.transform.localRotation = Quaternion.Euler(0, randomRot, 0);

                        Debug.DrawLine(targetBoundingBox.transform.position, propCopy.transform.position, case2 ? Color.white : Color.black,60f);
                    }
                }
                if(targetBoundingBox.size.x >= targetBoundingBox.size.z)
                {
                    SetPos(propBoundingBox.size.x >= propBoundingBox.size.z,true);
                }
                else
                {
                    SetPos(propBoundingBox.size.x <= propBoundingBox.size.z,false);
                }


                if (DoesPropOverlap(propCopy.GetComponent<BoxCollider>()))
                {
                    yield return new WaitForFixedUpdate();
                    DestroyImmediate(propCopy);
                }
                else
                {
                    probabilityList.IncreasePickCount();

                    spawnedPropCount++;
                    targetBoundingBoxScript.AddBoundingBox(propBoundingBox);
                }
            }

            spawnTries++;
            yield return new WaitForFixedUpdate();
        }

        yield break;
    }

    public bool DoseBoxFitInBox(BoxCollider bounds, BoxCollider box)
    {
        if (box.size.y > bounds.size.y) return false;

        if (Mathf.Min(box.size.x, box.size.z) > Mathf.Min(bounds.size.x, bounds.size.z)) return false;
        if (Mathf.Max(box.size.x, box.size.z) > Mathf.Max(bounds.size.x, bounds.size.z)) return false;

        return true;
    }
    public bool DoesPropOverlap(BoxCollider propCollider)
    {
        Collider[] overlapingColiderArray = Util.PhysicsBoxColliderOverlap(propCollider, overlapCheckLayerMask);
        foreach (Collider collider in overlapingColiderArray)
        {
            if (collider != propCollider)
            {
                return true;
            }
        }
        return false;
    }
}
