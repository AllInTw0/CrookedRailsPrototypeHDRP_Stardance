using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class ShopStand : AnimationPlayer
{
    [Header("Glow")]
    [SerializeField]
    private Flickerer glowFlickerer;
    [SerializeField]
    private Transform glow;
    [SerializeField]
    private float glowRadius;
    [SerializeField]
    private float spinSpeed;
    [Header("Button")]
    [SerializeField]
    private AnimationPlayer buttonAnimationPlayer;
    [SerializeField]
    private EventInteractable buttonInteractable;
    [Header("Text")]
    [SerializeField]
    private TMP_Text nameText;
    [SerializeField]
    private TMP_Text priceText;
    [Header("Missing")]
    [SerializeField]
    private GameObject missingPrefab;

    public UnityEvent onInteract;

    //Run time
    private bool shopStandEnabled;
    private bool outOfStock;
    private ShopItem shopItem;
    void Start()
    {
        nameText.text = "";
        priceText.text = "";

        if (buttonInteractable != null) buttonInteractable.interactEvent.AddListener(() => { onInteract.Invoke(); });
    }

    void Update()
    {
        if(shopItem != null)
        {
            if(shopItem.stock <= 0 && outOfStock == false)
            {
                //DisableButton();
                //glowFlickerer.TurnOff();
                DisableStand();
                outOfStock = true;
            }
            if(shopItem.linkedShop.selectedShopItem == shopItem)
            {
                glow.localRotation *= Quaternion.Euler(0f, Time.deltaTime * spinSpeed, 0f);
            }
        }
    }

    public void EnableStand()
    {
        if (shopStandEnabled == true) return;

        glowFlickerer.TurnOn();
        PlayAniamtion(animName, 1f);
        EnableButton();
        shopStandEnabled = true;
    }
    public void DisableStand()
    {
        if (shopStandEnabled == false) return;

        glowFlickerer.TurnOff();
        PlayAniamtion(animName, -1f);
        DisableButton();
        shopStandEnabled = false;
    }
    public void EnableButton()
    {
        if (buttonAnimationPlayer != null) buttonAnimationPlayer.PlayAniamtion(buttonAnimationPlayer.animName, 1f);
        if (buttonInteractable != null) buttonInteractable.gameObject.SetActive(true);
    }
    public void DisableButton()
    {
        if (buttonAnimationPlayer != null) buttonAnimationPlayer.PlayAniamtion(buttonAnimationPlayer.animName, -1f);
        if (buttonInteractable != null) buttonInteractable.gameObject.SetActive(false);
    }
    public void Intialize(ShopItem shopItem)
    {
        this.shopItem = shopItem;

        if (shopItem.itemInfo.prefab != null || missingPrefab != null)
        {
            Transform copy = Instantiate(shopItem.itemInfo.prefab != null ? shopItem.itemInfo.prefab : missingPrefab, glow).transform;

            if (copy.TryGetComponent(out Item item)) item.enabled = false;
            copy.gameObject.layer = 0;

            //Set pos and scale based on box collider
            BoxCollider boxCollider = copy.GetComponent<BoxCollider>();

            float scale = Mathf.Max(Mathf.Sqrt(boxCollider.size.x * boxCollider.size.x + boxCollider.size.y * boxCollider.size.y), Mathf.Sqrt(boxCollider.size.x * boxCollider.size.x + boxCollider.size.z * boxCollider.size.z), Mathf.Sqrt(boxCollider.size.y * boxCollider.size.y + boxCollider.size.z * boxCollider.size.z));
            scale = Mathf.Clamp((glowRadius / scale) * 2f, 0f, 2.5f);
            copy.localScale = Vector3.one * scale;

            copy.localPosition = -boxCollider.center * scale;

            copy.localRotation = Quaternion.Euler(shopItem.itemInfo.rotation);
        }
        nameText.text = shopItem.itemInfo.GetName();
        priceText.text = shopItem.price + "$";

        EnableStand();

        onInteract.AddListener(() => { shopItem.linkedShop.SelectShopItem(shopItem); });
    }

}
