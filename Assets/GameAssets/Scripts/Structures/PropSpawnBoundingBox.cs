using UnityEngine;

public class PropSpawnBoundingBox : MonoBehaviour
{
    public BoxCollider boundingBox;
    public bool spawnOnTerrain;
    public int maxPropCount = -1;
    
    private float areaLeft;
    private int propCount;
    private bool calculated;
    private void Awake()
    {
        boundingBox.isTrigger = true;
        if (calculated == false) CalculateArea();
    }
    public void AddBoundingBox(BoxCollider boxCollider)
    {
        if (calculated == false) CalculateArea();
        areaLeft -= boxCollider.size.x * boxCollider.size.z;
        propCount++;
    }
    public bool DoseBoundingBoxFit(BoxCollider boxCollider)
    {
        if(maxPropCount >= 0 && propCount >= maxPropCount) 
            return false;
        if (calculated == false) CalculateArea();
        return areaLeft >= boxCollider.size.x * boxCollider.size.z;
    }
    private void CalculateArea()
    {
        Debug.Log("Area calculated");
        areaLeft = boundingBox.size.x * boundingBox.size.z;
        calculated = true;
    }
}
