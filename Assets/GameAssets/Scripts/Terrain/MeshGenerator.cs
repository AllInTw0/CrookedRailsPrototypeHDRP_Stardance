using System;
using UnityEngine;

public static class MeshGenerator
{
    public static MeshData GenerateTerrainMesh(float[,] heightMap, Color[,] colorMap, float vertSpacing)
    {
        int vertCountBorder = heightMap.GetLength(0);

        Vector3[] verticies = new Vector3[vertCountBorder * vertCountBorder];
        Color[] colors = new Color[vertCountBorder * vertCountBorder]; 
        int[] triangles = new int[(vertCountBorder - 1) * (vertCountBorder - 1) * 6];

        int triangleIndex = 0;
        for (int x = 0; x < vertCountBorder; x++)
        {
            for (int y = 0; y < vertCountBorder; y++)
            {
                int vertIndex = x + vertCountBorder * y;
                verticies[vertIndex] = new Vector3((x-1) * vertSpacing, heightMap[x, y], (y - 1) * vertSpacing);
                colors[vertIndex] = colorMap[x, y];
                
                if( x != vertCountBorder -1 && y != vertCountBorder - 1)
                {
                    if(x % 2 == 0 && y % 2 == 0 || x % 2 == 1 && y % 2 == 1)
                    {
                        triangles[triangleIndex] = vertIndex + vertCountBorder;
                        triangles[triangleIndex + 1] = vertIndex + 1;
                        triangles[triangleIndex + 2] = vertIndex;

                        triangles[triangleIndex + 3] = vertIndex + vertCountBorder;
                        triangles[triangleIndex + 4] = vertIndex + 1 + vertCountBorder;
                        triangles[triangleIndex + 5] = vertIndex + 1;
                    }
                    else
                    {
                        triangles[triangleIndex] = vertIndex + 1 + vertCountBorder;
                        triangles[triangleIndex + 1] = vertIndex + 1;
                        triangles[triangleIndex + 2] = vertIndex;

                        triangles[triangleIndex + 3] = vertIndex + vertCountBorder;
                        triangles[triangleIndex + 4] = vertIndex + 1 + vertCountBorder;
                        triangles[triangleIndex + 5] = vertIndex;
                    }

                    triangleIndex += 6;
                }
            }
        }

        return new MeshData(verticies, colors, triangles, vertCountBorder);
    }
}
public class MeshData
{
    public int verticiesPerLine;
    public Vector3[] verticies;
    public int[] triangles;
    public Color[] colors;
    //public Color[] colors;
    public Mesh mesh;
    public GameObject meshObject;
    public MeshData(Vector3[] verticies, Color[] colors, int[] triangles, int verticiesPerLine)
    {
        this.verticies = verticies;
        this.triangles = triangles;
        this.verticiesPerLine = verticiesPerLine;
        this.colors = colors;
    }
    public void CreateMeshObject(Vector2Int worldPos)
    {
        meshObject = new GameObject("Terrain");
        meshObject.transform.SetParent(TerrainGeneration.active.transform);
        meshObject.layer = TerrainGeneration.active.terrainLayer;
        MeshFilter meshFilter = meshObject.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = meshObject.AddComponent<MeshRenderer>();
        meshRenderer.renderingLayerMask = TerrainGeneration.active.terrainRenderingLayerMask;
        MeshCollider meshCollider = meshObject.AddComponent<MeshCollider>();
        meshFilter.sharedMesh = mesh;
        meshCollider.sharedMesh = mesh;

        meshRenderer.material = TerrainGeneration.active.terrainMaterial;

        meshObject.transform.position = new Vector3(worldPos.x, 0, worldPos.y);

    }
    public void CreateMesh()
    {
        mesh = new Mesh();
        Vector3[] normals = CalculateNormals();

        int vertCountBorder = verticiesPerLine - 2;
        int vertCount = vertCountBorder * vertCountBorder;
        Vector3[] verticiesInBounds = new Vector3[vertCount];
        int[] trianglesInBounds = new int[(vertCountBorder - 1) * (vertCountBorder - 1) * 6];
        Vector3[] normalsInBounds = new Vector3[vertCount];
        Color[] colorsInBounds = new Color[vertCount];
        
        int vertIndex = 0;
        int triangleIndex = 0;
        for (int x = 0; x < verticiesPerLine; x++)
        {
            for (int y = 0; y < verticiesPerLine; y++)
            {
                if (x > 0 && x < verticiesPerLine-1 && y > 0 && y < verticiesPerLine - 1)
                {
                    int index = x + y * verticiesPerLine;

                    verticiesInBounds[vertIndex] = verticies[index];
                    normalsInBounds[vertIndex] = normals[index];
                    colorsInBounds[vertIndex] = colors[index];
                    
                    if (x != verticiesPerLine - 2 && y != verticiesPerLine - 2)
                    {
                        if (x % 2 == 0 && y % 2 == 0 || x % 2 == 1 && y % 2 == 1)
                        {
                            trianglesInBounds[triangleIndex] = vertIndex;
                            trianglesInBounds[triangleIndex + 1] = vertIndex + 1;
                            trianglesInBounds[triangleIndex + 2] = vertIndex + vertCountBorder;

                            trianglesInBounds[triangleIndex + 3] = vertIndex + 1;
                            trianglesInBounds[triangleIndex + 4] = vertIndex + 1 + vertCountBorder;
                            trianglesInBounds[triangleIndex + 5] = vertIndex + vertCountBorder;
                        }
                        else
                        {
                            trianglesInBounds[triangleIndex] = vertIndex;
                            trianglesInBounds[triangleIndex + 1] = vertIndex + 1;
                            trianglesInBounds[triangleIndex + 2] = vertIndex + 1 + vertCountBorder; ;

                            trianglesInBounds[triangleIndex + 3] = vertIndex;
                            trianglesInBounds[triangleIndex + 4] = vertIndex + 1 + vertCountBorder;
                            trianglesInBounds[triangleIndex + 5] = vertIndex + vertCountBorder;
                        }

                        triangleIndex += 6;
                    }
                    vertIndex++;
                }
            }
        }

        mesh.vertices = verticiesInBounds;
        mesh.triangles = trianglesInBounds;
        mesh.normals = normalsInBounds;
        mesh.colors = colorsInBounds;
    }
    public void Destroy()
    {
        if (meshObject)
            ThreadManager.AddMainThreadJob(delegate{GameObject.Destroy(meshObject);});
    }
    private Vector3[] CalculateNormals()
    {
        Vector3[] vertexNormals = new Vector3[verticies.Length];
        for (int i = 0; i < triangles.Length; i+= 3)
        {
            Vector3 normal = TriangleNormalFromPos(verticies[triangles[i]], verticies[triangles[i + 1]], verticies[triangles[i + 2]]);
            vertexNormals[triangles[i]] += normal;
            vertexNormals[triangles[i + 1]] += normal;
            vertexNormals[triangles[i + 2]] += normal;
        }

        for (int i = 0; i < vertexNormals.Length; i++)
        {
            vertexNormals[i] = vertexNormals[i].normalized;
        }
        return vertexNormals;
    }
    private Vector3 TriangleNormalFromPos(Vector3 posA,Vector3 posB, Vector3 posC)
    {
        Vector3 sideAB = posB - posA;
        Vector3 sideAC = posC - posA;
        return Vector3.Cross(sideAB, sideAC).normalized;
    }
}
