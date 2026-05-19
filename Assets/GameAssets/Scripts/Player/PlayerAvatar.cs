using System;
using UnityEngine;
using System.Collections.Generic;

public class PlayerAvatar : MonoBehaviour
{
    public static PlayerAvatar active;
    
    [SerializeField] 
    private PlayerMovement player;
    [SerializeField] 
    public Animator animator;
    [SerializeField] 
    private float speedMult;
    [SerializeField] 
    private Transform armIKParent;
    [SerializeField] 
    private SkinnedMeshRenderer skinRenderer;
    
    [Header("Tool Animation")] 
    [SerializeField]
    private List<ToolAnimationInfo> toolAnimationInfo = new List<ToolAnimationInfo>();
    
    //Run Time
    private Item equippedTool;
    private ToolAnimationInfo equippedToolAnimInfo;
    private void Start()
    {
        active = this;
        foreach (var animInfo in toolAnimationInfo)
        {
            animInfo.animatedObject.SetActive(false);
        }
    }

    private void LateUpdate()
    {
        if (player.rb.linearVelocity.magnitude > 0.1f)
        {
            animator.SetBool("Walking",true);
            animator.SetFloat("SpeedMult", player.rb.linearVelocity.magnitude * speedMult);
        }
        else
        {
            animator.SetBool("Walking",false);
        }
        
        animator.SetBool("Jumping", player.grounded);
        animator.SetBool("Crouching", player.crouched);
        
        if (equippedTool != null)
        {
            armIKParent.localRotation = Quaternion.Euler(PlayerCamera.active.rotationX,0,0);
        }
        else
        {
            armIKParent.localRotation = Quaternion.identity;
        }
        
    }

    public void EquipTool(Item item)
    {
        equippedTool = item;
        foreach (var animInfo in toolAnimationInfo)
        {
            if (animInfo.itemName == equippedTool.itemInfo.name)
            {
                animator.SetLayerWeight(animInfo.layerIndex,1);
                animInfo.animatedObject.SetActive(true);
                equippedToolAnimInfo = animInfo;
                break;
            }
        }

        if (equippedToolAnimInfo == null)
        {
            Debug.LogWarning("Didnt find animation info for item: " + item);
        }
    }
    public void UnEquipTool()
    {
        if (equippedToolAnimInfo != null)
        {
            animator.SetLayerWeight(equippedToolAnimInfo.layerIndex, 0);
            equippedToolAnimInfo.animatedObject.SetActive(false);
        }

        equippedToolAnimInfo = null;
        equippedTool = null;
    }

    public ToolAnimationInfo GetAnimationInfo()
    {
        return equippedToolAnimInfo;
    }

    public void Hide()
    {
        skinRenderer.enabled = false;
    }
    public void UnHide()
    {
        skinRenderer.enabled = true;
    }
}

[System.Serializable]
public class ToolAnimationInfo
{
    public string itemName;
    public GameObject animatedObject;
    public int layerIndex;
    public Transform bulletSpawn;
}
