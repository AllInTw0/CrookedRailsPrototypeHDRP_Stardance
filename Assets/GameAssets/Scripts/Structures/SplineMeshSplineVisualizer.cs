using System.Collections.Generic;
using UnityEngine;

public class SplineMeshSplineVisualizer : SplineVisualizer
{
    [Header("Spline Mesh")]
    [SerializeField]
    private int splineMeshIndex;
    [SerializeField]
    private Vector2 minMaxScale;
    [SerializeField]
    private bool meshHasCollision;
    public override void Visualize(List<PathPoint> path)
    {
        if(Spline.active == null)
        {
            Debug.Log("No Spline script");
            return;
        }
        SplineMeshInfo splineMeshInfoCopy = Spline.active.splineMeshList[splineMeshIndex];
        splineMeshInfoCopy.scale *= Random.Range(minMaxScale.x, minMaxScale.y);
        GameObject mesh = Spline.active.GenerateMeshAlongPath(path, splineMeshInfoCopy, addCollision : meshHasCollision);
        mesh.transform.SetParent(transform);
    }
}
