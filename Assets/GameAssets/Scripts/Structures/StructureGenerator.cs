using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class StructureGenerator : NetworkBehaviour
{
    public virtual IEnumerator Generate(StructureMaster structureMaster)
    {
        return null;
    }
    public void StopGenerating()
    {
        // StopCoroutine(nameof(Generate));
        StopAllCoroutines();
    }
    public void GenerateTest()
    {
        StartCoroutine(Generate(null));
    }
}
