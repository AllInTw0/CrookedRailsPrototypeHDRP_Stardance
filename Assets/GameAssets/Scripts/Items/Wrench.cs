using UnityEngine;

public class Wrench : ItemMelee
{
    //Variables
    [Header("Wrench")] 
    [SerializeField] 
    private GameObject turretPrefab;
    
    //RunTime
    private Turret hologram;
    private Turret linkedTurret;
    private bool canBuild;
    private bool attack2Released;
    private void Update()
    {
        if (InputManager.attack2Action.IsPressed() && PlayerInventory.active.tool == this)
        {
            if (attack2Released)
            {
                if (hologram == null)
                {
                    hologram = Instantiate(turretPrefab).GetComponent<Turret>();
                }

                //Set Hologram pos 
                Vector3 startPos = PlayerCamera.active.player.position + PlayerCamera.active.player.forward * 1.5f +
                                   Vector3.up;
                if (Physics.Raycast(startPos, Vector3.down, out RaycastHit hit, 1.5f))
                {
                    hologram.transform.position = hit.point;
                    canBuild = true;
                }
                else
                {
                    hologram.transform.position = startPos - Vector3.up;
                    canBuild = false;
                }

                hologram.transform.rotation = Quaternion.Euler(0, PlayerCamera.active.player.eulerAngles.y, 0);


                if (canBuild && InputManager.attackAction.triggered)
                {
                    hologram.SetMaterialTo(hologram.defaultMat);
                    hologram.Activate();
                    linkedTurret = hologram;
                    hologram = null;
                    attack2Released = false;
                }
                else
                {
                    hologram.SetMaterialTo(canBuild ? hologram.hologramMatGreen : hologram.hologramMatRed);
                }
            }
        }
        else
        {
            if (hologram != null)
            {
                Destroy(hologram.gameObject);
            }

            attack2Released = true;
        }
        
        UpdateMelee();
    }
    
}
