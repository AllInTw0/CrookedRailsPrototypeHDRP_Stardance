using System;
using UnityEngine;

public class GroundMovement : MonoBehaviour
{
    //Variables
    [Header("Speed")]
    [SerializeField]
    public float baseSpeed = 1f;
    [SerializeField]
    public float acceleration = 1f;
    [SerializeField]
    public float deceleration = 1f;
    [SerializeField]
    private float maxAngle = 45f;
    [SerializeField]
    public CapsuleCollider capsuleCollider;

    [Header("Ground")] 
    [SerializeField] 
    private bool playLandSound;
    [SerializeField] 
    private float gravityForce;
    [SerializeField]
    public LayerMask groundLayer;
    [Header("Step Detection")]
    [SerializeField]
    private float stepCheckLow;
    [SerializeField]
    private float stepCheckHigh;
    [SerializeField]
    private float stepCheckDistance;
    [Header("FootSteps")]
    [SerializeField]
    private float footStepDistance = 1f;
    [SerializeField] 
    private string footStepGroundString = "FootStep - Stone";
    [SerializeField] 
    private string footStepWaterString = "FootStep - Water";
    [Header("Water")]
    [SerializeField]
    private float waterHeight;
    [SerializeField]
    public float waterSlowDownMult;
    
    //Runtime References
    [NonSerialized]
    public Rigidbody rb;
    private RigidbodyInterpolation rbStartInterpolation;
    
    [NonSerialized]
    public bool grounded = true;
    private RailCar onRailCar;

    private float groundAngle;
    [NonSerialized]
    public Vector3 groundNormal = Vector3.up;
    
    private float distanceWalked; //for footsteps
    [HideInInspector] public float velocity;
    
    //Input from other scripts
    [NonSerialized]
    public float targetSpeed;
    private Vector2 movementVector; //2D space target direction of movement
    
    [NonSerialized]
    public Vector3 projectedVector; //World space target direction of movement also projected on ground
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rbStartInterpolation = rb.interpolation;
    }
    
    public virtual Vector2 GetMovementVector()
    {
        return Vector2.zero;
    }
    public virtual float GetTargetSpeed()
    {
        return baseSpeed;
    }

    public void DisableGrounded()
    {
        grounded = false;
        groundNormal = Vector3.up;
    }
    private void Update()
    {
        UpdateMovement();
    }

    public void UpdateMovement()
    {
        CheckGrounded();
        
        //FootStep Sound
        if (grounded == false)
            return;

        velocity = rb.linearVelocity.magnitude;
        distanceWalked += velocity * Time.deltaTime;
        if (distanceWalked > footStepDistance && footStepDistance > 0)
        {
            distanceWalked -= footStepDistance;
            if(IsInWater())
                SoundManager.active.PlayAtPos(transform.position, footStepWaterString);
            else
                SoundManager.active.PlayAtPos(transform.position, footStepGroundString);
        }

    }
    
    private void FixedUpdate()
    {
        FixedUpdateMovement();
    }

    public void FixedUpdateMovement()
    {
        targetSpeed = GetTargetSpeed();
        movementVector = GetMovementVector();
        
        //Gravity
        rb.AddForce(grounded ? -groundNormal * (gravityForce * 0.5f) : Vector3.down * gravityForce);

        //Movement
        Vector3 targetVector = new Vector3(movementVector.x,0,movementVector.y).normalized;
        projectedVector = Vector3.ProjectOnPlane(targetVector, groundNormal).normalized;

        Debug.DrawLine(transform.position + Vector3.up, transform.position + Vector3.up + projectedVector * acceleration, grounded ? Color.beige : Color.red);

        //Water slow down
        if (IsInWater())
            targetSpeed *= (1 - GetPercentUnderWater() * waterSlowDownMult);
        
        rb.AddForce(projectedVector * ((targetSpeed / baseSpeed) * acceleration));

        if (grounded)
        {
            Vector3 velocityPlane = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
            float velocity = velocityPlane.sqrMagnitude;

            if (movementVector == Vector2.zero || targetSpeed == 0f)
            {
                //Drag
                if (velocity > 0.1f)
                {
                    rb.AddForce(-velocityPlane.normalized * deceleration);
                }
                else
                {
                    rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
                }
            }
            else
            {
                //Limit speed
                float angleBetweenDirections = Vector2.Angle(new Vector2(rb.linearVelocity.x, rb.linearVelocity.z), new Vector2(targetVector.x, targetVector.z));
                if (velocity > targetSpeed && angleBetweenDirections <= 90f)
                {
                    float fallSpeed = rb.linearVelocity.y;
                    Vector3 n = rb.linearVelocity.normalized * targetSpeed;
                    rb.linearVelocity = new Vector3(n.x, fallSpeed, n.z);
                }

                //Counter movement
                if (angleBetweenDirections > 90f)
                {
                    rb.AddForce(projectedVector * ((targetSpeed / baseSpeed) * acceleration));
                }
            }
        }

        //Step detection
        if (movementVector != Vector2.zero && rb.linearVelocity.y < 0.1f)
        {
            if (Physics.Raycast(transform.position + new Vector3(0, stepCheckHigh, 0), projectedVector, stepCheckDistance, groundLayer) == false)
            {
                if (Physics.Raycast(transform.position + new Vector3(0, stepCheckLow, 0), projectedVector, out RaycastHit hit, stepCheckDistance, groundLayer))
                {
                    transform.position += projectedVector * hit.distance + new Vector3(0, stepCheckHigh, 0);
                    Debug.Log("Preformed step!");
                }
            }
            Debug.DrawRay(transform.position + new Vector3(0, stepCheckLow, 0), projectedVector.normalized * stepCheckDistance, Color.red);
            Debug.DrawRay(transform.position + new Vector3(0, stepCheckHigh, 0), projectedVector.normalized * stepCheckDistance, Color.red);
        }

    }
    public bool IsInWater()
    {
        return transform.position.y < waterHeight;
    }
    public bool IsSubmergedInWater()
    {
        if (IsInWater())
            return GetPercentUnderWater() >= 1f;
        return false;
    }
    public float GetPercentUnderWater()
    {
        float precentUnderWater = Mathf.Abs(transform.position.y - waterHeight) / capsuleCollider.height;
        return Mathf.Clamp01(precentUnderWater);     
    }

    private void CheckGrounded()
    {
        if (rb.constraints == RigidbodyConstraints.FreezeAll) return;
        
        Debug.DrawRay(transform.position + new Vector3(0, capsuleCollider.radius, 0),Vector3.down * (capsuleCollider.radius * 2f));
        if (Physics.SphereCast(transform.position + new Vector3(0, capsuleCollider.radius, 0), capsuleCollider.radius - 0.01f, Vector3.down, out RaycastHit  hit , capsuleCollider.radius * 1.5f, groundLayer))
        {
            groundNormal = hit.normal;
            groundAngle = Vector3.Angle(Vector3.up, groundNormal);

            if (groundAngle <= maxAngle)
            {
                if (playLandSound && grounded == false && rb.linearVelocity.y < -0.25f)
                {
                    if (IsInWater())
                        SoundManager.active.PlayAtPos(transform.position, "Water - Impact");
                    else
                        SoundManager.active.PlayAtPos(transform.position, "Land");
                }
                grounded = true;
                
                if(hit.transform.CompareTag("Moving"))
                {
                    MovingPlatformManager.active.AddEntry(rb,transform,hit.transform,true,rbStartInterpolation);
                    onRailCar = hit.transform.GetComponentInParent<RailCar>();
                }
                else
                {
                    MovingPlatformManager.active.RemoveEntry(transform);
                    onRailCar = null;
                }
            }
            else
            {
                grounded = false;
                groundNormal = Vector3.up;
                MovingPlatformManager.active.RemoveEntry(transform);
                onRailCar = null;
            }
        }
        else
        {
            grounded = false;
            groundNormal = Vector3.up;
            MovingPlatformManager.active.RemoveEntry(transform);
            onRailCar = null;
        }
    }

    public bool IsOnRailCar(out RailCar railCar)
    {
        railCar = onRailCar;
        return railCar != null;
    }
    public void Freeze(RailCar lockToRailCar = null)
    {
        rb.constraints = RigidbodyConstraints.FreezeAll;

        if (lockToRailCar != null)
        {
            MovingPlatformManager.active.AddEntry(rb, transform, lockToRailCar.transform, true, rbStartInterpolation);
        }
    }
    public void UnFreeze()
    {
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        MovingPlatformManager.active.RemoveEntry(transform);
    }
    
}
