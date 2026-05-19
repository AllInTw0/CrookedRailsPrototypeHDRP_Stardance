using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCamera : MonoBehaviour
{
    public static PlayerCamera active;
    //Variables
    [SerializeField]
    public Transform player;
    [SerializeField]
    private float minRotX,maxRotX;
    [SerializeField]
    private float bobSpeed, bobSmoothSpeed;
    [SerializeField]
    private float bobAmountY,bobAmountX;
    [SerializeField]
    private Vector2 renderTextureResolution;
    [Header("Ragdoll")]
    [SerializeField]
    private float targetRagdollDistance = 7f;
    [SerializeField]
    private float ragdollCameraZoomSpeed = 7f;
    [SerializeField]
    private float ragdollCameraRotSpeed = 120f;
    //Run time
    [NonSerialized]
    public float rotationX;
    private float bobX, bobY;
    private Camera camera;
    private Vector3 ragdollCamPos;

    private bool inThirdPerson;

    //New Input Package
    private InputAction lookAction;
    private void Start()
    {
        active = this;
        
        lookAction = InputSystem.actions.FindAction("Look");
        Cursor.lockState = CursorLockMode.Locked;

        camera = GetComponent<Camera>();
    }
    private void Update()
    {
        camera.fieldOfView = Settings.fov;

        if (GameOverScreen.IsGameOver()) return;

        if (InputManager.altAction.WasPerformedThisFrame())
            Cursor.lockState = Cursor.lockState == CursorLockMode.Locked ? CursorLockMode.None : CursorLockMode.Locked;

        //First Person
        if (Cursor.lockState == CursorLockMode.Locked)
        {
            var lookInput = lookAction.ReadValue<Vector2>() * Time.timeScale;

            rotationX -= Settings.invertedY ? -lookInput.y * Settings.sensitivity : lookInput.y * Settings.sensitivity;
            rotationX = Mathf.Clamp(rotationX, minRotX, maxRotX);
            transform.localRotation = Quaternion.Euler(rotationX, 0, 0);

            player.rotation *= Quaternion.Euler(0, Settings.invertedX ? -lookInput.x * Settings.sensitivity : lookInput.x * Settings.sensitivity, 0);
        }

        CameraBob();
    }
    private void LateUpdate()
    {
        if (GameOverScreen.IsGameOver())
        {
            if (Cursor.lockState != CursorLockMode.None)
                Cursor.lockState = CursorLockMode.None;

            GameObject ragdoll = PlayerHealth.active.ragdoll;
            Vector3 ragdollPosition = ragdoll.GetComponentInChildren<Rigidbody>().transform.position;

            if (ragdollCamPos == Vector3.zero)
            {
                Vector3 firstDir = ragdollPosition - camera.transform.position;
                firstDir = firstDir.normalized;
                firstDir = new Vector3(firstDir.x, Mathf.Clamp(firstDir.y,0f,0.15f), firstDir.z);
                firstDir = firstDir.normalized;
                ragdollCamPos = ragdollPosition + firstDir * 0.5f;
            }

            Vector3 dir = ragdollPosition - ragdollCamPos;
            dir = dir.normalized;
            float distance = Vector3.Distance(ragdollPosition, ragdollCamPos);

            float delta = targetRagdollDistance - distance;
            if (delta > 0)
                ragdollCamPos += -dir * Mathf.Clamp(ragdollCameraZoomSpeed * Time.deltaTime, 0f, delta);
            else if (delta < 0)
                ragdollCamPos += -dir * Mathf.Clamp(-ragdollCameraZoomSpeed * Time.deltaTime, delta, 0f);

            camera.transform.position = ragdollCamPos;

            Quaternion start = camera.transform.rotation;
            camera.transform.LookAt(ragdollPosition);
            Quaternion end = camera.transform.rotation;
            camera.transform.rotation = Quaternion.RotateTowards(start, end, ragdollCameraRotSpeed * Time.deltaTime);
        }
    }
    private void CameraBob()
    {
        float mult = Mathf.Clamp01(PlayerMovement.active.rb.linearVelocity.magnitude  * 0.3f);
        
        bobX = Mathf.Lerp(bobX, Mathf.Cos(Time.time * bobSpeed * 0.5f) * bobAmountX * mult, Time.deltaTime * bobSmoothSpeed);
        bobY = Mathf.Lerp(bobY, Mathf.Sin(Time.time * bobSpeed ) * bobAmountY * mult, Time.deltaTime * bobSmoothSpeed);

        transform.localPosition = new Vector3(bobX, bobY, 0);
    }

    public Vector3 GetRaycastPos()
    {
        if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, 100f))
        {
            return hit.point;
        }
        else
        {
            return transform.position + transform.forward * 100f;
        }
    }

    public Ray GetRay()
    {
        return new Ray(transform.position, transform.forward);
    }
    public Vector3 GetDir()
    {
        return transform.forward;
    }

    public void WorldPosToUI(Vector3 worldPos, out Vector3 screenPos, out bool onScreen)
    {
        Vector3 renderTexturePos = camera.WorldToScreenPoint(worldPos);
        if (renderTexturePos.z < 0f)
        {
            onScreen = false;
        }
        else
        {
            onScreen = true;
        }
        screenPos = new Vector3(renderTexturePos.x * (Screen.width / renderTextureResolution.x),renderTexturePos.y * (Screen.height / renderTextureResolution.y),renderTexturePos.z);
    }
    
}
