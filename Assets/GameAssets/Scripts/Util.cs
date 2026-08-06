using System.Collections.Generic;
using UnityEngine;

public static class Util
{
    public static Collider[] PhysicsBoxColliderOverlap(BoxCollider boxCollider, LayerMask layerMask = default)
    {
        return Physics.OverlapBox(boxCollider.transform.TransformPoint(boxCollider.center), boxCollider.size * 0.5f, boxCollider.transform.rotation, layerMask);
    }

    public static float O1RangeToVector2Range(float value, Vector2 range)
    {
        float amplitude = range.y - range.x;
        return range.x + amplitude * value;
    }

    public static int SeedToRandomIndex(float seed, int maxIndex)
    {
        seed *= seed;
        seed = seed.ToString().GetHashCode() * 0.00005f;
        float noise = Mathf.Clamp01(Mathf.PerlinNoise1D(seed));
        Debug.Log("Noise: " + noise + " seed: " + seed + " maxIndex: " + maxIndex + " return: " + Mathf.RoundToInt(noise * maxIndex));
        return Mathf.RoundToInt(noise * maxIndex);
    }
    public static bool SolveTrajectory(float startVelocity, Vector3 startPos, Vector3 targetPos, bool angleMode, out float angle)
    {
        //Solve trajectory
        float distance = Vector3.Distance(startPos, targetPos);
        float distance2D = Vector2.Distance(new Vector2(startPos.x, startPos.z), new Vector2(targetPos.x, targetPos.z));

        float yOffset = targetPos.y - startPos.y;

        //https://en.wikipedia.org/wiki/Projectile_motion
        //angle = arctan((v^2 +- root(v^4 - g*(g*x^2 + 2*y*v^2)))/(g*x))
        float g = -9.81f;
        if (angleMode)
            g = -g;
        float root = Mathf.Sqrt(Mathf.Pow(startVelocity, 4) - g * (g * distance2D * distance2D + 2 * yOffset * startVelocity * startVelocity));
        if (angleMode)
            root = -root;
        angle = Mathf.Atan((Mathf.Pow(startVelocity, 2) + root) / (g * distance2D)) * Mathf.Rad2Deg;
        if (angleMode)
            angle = -angle;

        if (angle is float.NaN)
            //Cant reach
            return false;
        
        return true;
    }

    public static float SolveTrajectoryMinAngle(Vector3 startPos, Vector3 targetPos)
    {
        //Minimum angle
        //angle = arctan(y/x + root(y*y/x*x + 1))
        
        float distance2D = Vector2.Distance(new Vector2(startPos.x, startPos.z), new Vector2(targetPos.x, targetPos.z));
        float yOffset = targetPos.y - startPos.y;

        float angle = Mathf.Atan(yOffset / distance2D + Mathf.Sqrt((yOffset * yOffset) / (distance2D * distance2D) + 1)) * Mathf.Rad2Deg;
        
        return angle;
    }

    public static float SolveVelocityForAngle(float angle, Vector3 startPos, Vector3 targetPos)
    {
        //velocity = root((x^2 * g)/xsin2angle - 2ycos^2angle)
        float distance2D = Vector2.Distance(new Vector2(startPos.x, startPos.z), new Vector2(targetPos.x, targetPos.z));
        float yOffset = targetPos.y - startPos.y;
        float g = 9.81f;

        float vel0 = Mathf.Sqrt((distance2D * distance2D * g) /
                                (distance2D * Mathf.Sin(2 * angle * Mathf.Deg2Rad) - 2 * yOffset * Mathf.Pow(Mathf.Cos(angle * Mathf.Deg2Rad), 2)));

        return vel0;
    }
    //OLD CODE FROM CASUAL INDUSTRIALIZATION GAME
    public static Camera CreateCamera(int cullingMask = -1, CameraClearFlags clearFlags = CameraClearFlags.Skybox, bool orthographic = false)
    {
        GameObject cam_object = new GameObject();

        Camera cam = cam_object.AddComponent<Camera>();

        if (cullingMask != -1)
            cam.cullingMask = cullingMask;

        cam.clearFlags = clearFlags;

        if (clearFlags == CameraClearFlags.Color)
            cam.backgroundColor = new Color(0, 0, 0, 0);

        cam.orthographic = orthographic;

        return cam;
    }
    public static void ChangeObjectsLayer(GameObject _object, LayerMask layer)
    {
        var object_list = _object.GetComponentsInChildren<Transform>();
        foreach (Transform _t in object_list)
        {
            _t.gameObject.layer = Mathf.FloorToInt(Mathf.Log(layer, 2));
        }
    }
    public static void ToggleObjectsMeshRenderers(GameObject _object, bool enable = false)
    {
        var renderer_list = _object.GetComponentsInChildren<MeshRenderer>();
        foreach (MeshRenderer _mesh in renderer_list)
        {
            _mesh.enabled = enable;
        }
    }

    //Code from unity discussions
    //(https://discussions.unity.com/t/test-to-see-if-a-vector3-point-is-within-a-boxcollider/17385)
    public static bool PointInBox(Vector3 point, BoxCollider box)
    {
        point = box.transform.InverseTransformPoint(point) - box.center;

        float halfX = (box.size.x * 0.5f);
        float halfY = (box.size.y * 0.5f);
        float halfZ = (box.size.z * 0.5f);

        if (point.x <= halfX && point.x >= -halfX &&
           point.y <= halfY && point.y >= -halfY &&
           point.z <= halfZ && point.z >= -halfZ)
            return true;
        else
            return false;
    }

    [System.Serializable]
    public class ProbabilityListElement<T>
    {
        public T element;
        public float probability;
        public int maxCount;

        public ProbabilityListElement(T element, float probability, int maxCount)
        {
            this.element = element;
            this.probability = probability;
            this.maxCount = maxCount;
        }

        [HideInInspector]
        public int pickCount;
        [HideInInspector]
        public ProbabilityList<T> probabilityList;
        [HideInInspector]
        public bool enabled = true;
        public void Enable()
        {
            if (enabled == false)
            {
                probabilityList.ModifyProbabilitySum(probability);
                enabled = true;
            }
        }
        public void Disable()
        {
            if (enabled)
            {
                probabilityList.ModifyProbabilitySum(-probability);
                enabled = false;
            }
        }
    }

    public class ProbabilityList<T>
    {
        public List<ProbabilityListElement<T>> probabilityList = new List<ProbabilityListElement<T>>();
        private float probabilitySum = 0f;
        private int lastPicked = -1;
        
        public ProbabilityList(List<ProbabilityListElement<T>> probabilityList)
        {
            //Copy
            probabilityList = new List<ProbabilityListElement<T>>(probabilityList);

            //Get probability sum and reset count just in case
            for (int i = 0; i < probabilityList.Count; i++)
            {
                if(probabilityList[i].maxCount == 0)
                {
                    probabilityList.RemoveAt(i);
                    i--;
                    continue;
                }
                
                probabilityList[i].pickCount = 0;
                probabilitySum += probabilityList[i].probability;
                probabilityList[i].probabilityList = this;
                probabilityList[i].enabled = true;
            }

            //Sort
            bool sorted = false;
            while (sorted == false)
            {
                sorted = true;
                for (int i = 0; i < probabilityList.Count - 1; i++)
                {
                    if (probabilityList[i].probability < probabilityList[i + 1].probability)
                    {
                        var temp = probabilityList[i];
                        probabilityList[i] = probabilityList[i + 1];
                        probabilityList[i + 1] = temp;
                        sorted = false;
                    }
                }
            }

            this.probabilityList = probabilityList;
        }
        public void ModifyProbabilitySum(float addition)
        {
            probabilitySum += addition;
        }
        public T PickNext(bool increasePickCount = true, int seed = -1)
        {
            if (probabilityList.Count == 0) return default(T);

            float randomProbability = seed == -1 ? Random.Range(0f, probabilitySum) : (float)new System.Random(seed).NextDouble() * probabilitySum;
            //`Debug.Log("randomProbability: " + randomProbability+", max: " + probabilitySum);
            for (int i = 0; i < probabilityList.Count; i++)
            {
                if (probabilityList[i].enabled)
                {
                    randomProbability -= probabilityList[i].probability;
                    if (randomProbability <= 0f)
                    {
                        lastPicked = i;
                        T pickedElement = probabilityList[i].element;
                        if (increasePickCount) IncreasePickCount();
                        return pickedElement;
                    }
                }
            }

            Debug.LogWarning("Couldnt pick from probabilityList. probabilitySum incorrect?");
            return default(T);
        }
        public void IncreasePickCount()
        {
            probabilityList[lastPicked].pickCount++;
            if (probabilityList[lastPicked].pickCount >= probabilityList[lastPicked].maxCount && probabilityList[lastPicked].maxCount > 0)
            {
                T element = probabilityList[lastPicked].element;
                probabilitySum -= probabilityList[lastPicked].probability;
                probabilityList.RemoveAt(lastPicked);
                lastPicked = -1;
            }
        }
        public bool HasItemsLeft()
        {
            return probabilityList.Count > 0;
        }
        public void RemoveLastPicked()
        {
            if (lastPicked != -1)
            {
                probabilitySum -= probabilityList[lastPicked].probability;
                probabilityList.RemoveAt(lastPicked);
                lastPicked = -1;
            }
        }
    }
}
