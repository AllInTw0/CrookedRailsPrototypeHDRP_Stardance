using UnityEngine;

public class WaterTower : MonoBehaviour
{
    //Variables
    [SerializeField]
    private EventInteractable interactable;
    [SerializeField]
    private Health waterLevel;
    [SerializeField]
    private float maxDistance;
    [SerializeField]
    private Transform spoutTransform;
    [SerializeField]
    private Transform waterTransform;
    [SerializeField]
    private Animator animator;
    [SerializeField]
    private MeshRenderer waterMesh;
    [SerializeField]
    private ParticleSystem waterParticle;
    [SerializeField]
    private LineRenderer[] chainArray;
    [SerializeField]
    private Transform spoutChainOriginTransform;

    [SerializeField]
    private float spoutSpeed;
    [SerializeField]
    private float waterSpeed;
    [SerializeField]
    private float lowerTime;
    [SerializeField]
    private float fillSpeed;

    //Run time
    private Vector3 originalWaterSize;
    private float waterSize;

    private bool lowered;
    private float timeSinceLowered;

    private float spoutRotY;


    private void Start()
    {
        originalWaterSize = waterTransform.localScale;
        waterMesh.enabled = false;
        waterParticle.gameObject.SetActive(false);
    }
    private void LateUpdate()
    {
        spoutTransform.rotation = Quaternion.Euler(spoutTransform.localEulerAngles.x, spoutRotY, spoutTransform.localEulerAngles.z);
        for (int i = 0; i < chainArray.Length; i++)
        {
            chainArray[i].SetPosition(0, chainArray[i].transform.position);
            chainArray[i].SetPosition(1, spoutChainOriginTransform.position);
        }
    }
    private void Update()
    {
        if (Tender.active == null)
            return;

        if (lowered)
            timeSinceLowered += Time.deltaTime;

        float distance = Vector2.Distance(new Vector2(spoutTransform.position.x, spoutTransform.position.z), new Vector2(Tender.active.waterHatch.position.x, Tender.active.waterHatch.position.z));
        bool fillWater = false;

        if(distance > maxDistance || lowered == false || Tender.active.hatchOpened == false)
        {
            spoutRotY = Quaternion.Slerp(Quaternion.Euler(0, spoutRotY, 0), transform.rotation, Time.deltaTime * spoutSpeed).eulerAngles.y;
            fillWater = false;
        }
        else
        {
            spoutTransform.LookAt(Tender.active.waterHatch);
            float targetY = spoutTransform.eulerAngles.y + 90f;

            spoutRotY = Quaternion.Slerp(Quaternion.Euler(0, spoutRotY, 0), Quaternion.Euler(0, targetY, 0), Time.deltaTime * spoutSpeed).eulerAngles.y;

            if(timeSinceLowered >= lowerTime && Quaternion.Angle(Quaternion.Euler(0, spoutRotY, 0), Quaternion.Euler(0, targetY, 0)) <= 1f)
            {
                fillWater = true;
            }
        }

        //Handle Water mesh and filling
        if (fillWater && Tender.active.waterLevel.health < Tender.active.waterLevel.maxHealth && waterLevel.health > 0f)
        {
            waterSize += waterSpeed * Time.deltaTime;
            waterSize = Mathf.Clamp(waterSize, 0f, 1f);

            waterTransform.localScale = new Vector3(originalWaterSize.x, waterSize, originalWaterSize.z);

            waterMesh.enabled = true;
            if (waterSize > 0.5f && waterParticle.gameObject.activeSelf == false)
                waterParticle.gameObject.SetActive(true);

            //Add water
            if(waterSize > 0.5f)
            {
                float change = fillSpeed * Time.deltaTime;
                change = Mathf.Clamp(change, 0f, Mathf.Min(Tender.active.waterLevel.maxHealth - Tender.active.waterLevel.health, waterLevel.health));

                Tender.active.waterLevel.TakeDamage(-change);
                waterLevel.TakeDamage(change);
            }
        }
        else
        {
            waterSize -= waterSpeed * Time.deltaTime;
            waterSize = Mathf.Clamp(waterSize, 0f, 1f);

            waterTransform.localScale = new Vector3(originalWaterSize.x, waterSize, originalWaterSize.z);

            if(waterSize == 0)
                waterMesh.enabled = false;
            if (waterParticle.gameObject.activeSelf)
                waterParticle.gameObject.SetActive(false);
        }
    }
    public void Interact()
    {
        lowered = !lowered;

        if (lowered)
            interactable.actionName = "Raise";
        else
            interactable.actionName = "Lower";

        animator.SetBool("lowered", lowered);
        timeSinceLowered = 0f;
    }
}
