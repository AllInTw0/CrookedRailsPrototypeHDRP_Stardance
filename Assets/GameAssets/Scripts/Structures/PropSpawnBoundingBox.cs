using UnityEngine;

public class PropSpawnBoundingBox : MonoBehaviour
{
    public BoxCollider boundingBox;

    private float areaLeft;
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
    }
    public bool DoseBoundingBoxFit(BoxCollider boxCollider)
    {
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
