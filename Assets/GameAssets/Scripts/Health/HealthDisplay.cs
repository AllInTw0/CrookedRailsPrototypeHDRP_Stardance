using System.Collections.Generic;
using UnityEngine;

public class HealthDisplay : MonoBehaviour
{
    //Variables
    [SerializeField]
    private Health healthTarget;

    public enum DisplayType
    {
        Position,
        Scale,
        Enable,
    }
    [System.Serializable]
    public class DisplayEntry
    {
        public DisplayType displayType;
        public Transform targetTransform;
        [Header("Position")]
        public Transform startTransform;
        public Transform endTransform;
        [Header("Scale")]
        public Vector3 startSize;
        public Vector3 endSize;
        [Header("Enable")]
        public Vector2 healthRange;
    }
    [SerializeField]
    private List<DisplayEntry> displayEntryList;

    private void Start()
    {
        healthTarget.onTakeDamage.AddListener((float dammage, Vector3 force) =>
        {
            UpdateTransform();
        });
        UpdateTransform();
    }
    private void UpdateTransform()
    {
        foreach (DisplayEntry entry in displayEntryList)
        {
            if (entry.displayType == DisplayType.Position)
            {
                float time = healthTarget.GetHealth() / healthTarget.maxHealth;
                entry.targetTransform.position = Vector3.Lerp(entry.startTransform.position, entry.endTransform.position, time);
            }
            else if (entry.displayType == DisplayType.Scale)
            {
                float time = healthTarget.GetHealth() / healthTarget.maxHealth;
                entry.targetTransform.localScale = Vector3.Lerp(entry.startSize, entry.endSize, time);
            }
            else if(entry.displayType == DisplayType.Enable)
            {
                bool target = healthTarget.GetHealth() >= entry.healthRange.x && healthTarget.GetHealth() <= entry.healthRange.y;
                Debug.Log("Y " + target);
                if(entry.targetTransform.gameObject.activeSelf != target)
                    entry.targetTransform.gameObject.SetActive(target);
            }

        }

    }
}
