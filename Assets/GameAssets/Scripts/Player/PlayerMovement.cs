using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : GroundMovement
{
    public static PlayerMovement active;
    //Variables
    [Header("Player Movement")]
    [SerializeField]
    private bool canNoclip;
    [SerializeField]
    public Transform orientation;
    [Header("Speeds")]
    [SerializeField]
    private float sprintingSpeed = 1f;
    [SerializeField]
    private float crouchingSpeed = 1f;
    [SerializeField]
    private float noclipSpeed = 1f;

    [Header("Ladder")]
    [SerializeField]
    private float climbingSpeed = 1f;
    [SerializeField]
    private float climbingAngleThreshold = 45f;
    [Header("Crouching")]
    [SerializeField]
    private Transform cameraTransform;
    [SerializeField]
    private float colliderCrouchHeight;
    [SerializeField]
    private float cameraCrouchHeight;
    [Header("Max Distance From Objects")]
    [SerializeField]
    private float maxDistanceFromTrain;
    [SerializeField]
    private bool maxDistanceLimitEnabled;
    [Header("Stamina")] 
    [SerializeField] 
    private float maxStamina;
    [SerializeField] 
    private float staminaGainDelay;
    [SerializeField] 
    private float staminaGain;
    [SerializeField] 
    private float staminaLoss;
    [Header("Jumping")]
    [SerializeField]
    private float jumpForce = 1f;
    [SerializeField]
    private float jumpCoolDown = 0.25f;
    
    //Runtime References
    private float colliderStandingHeight;
    private float cameraStandingHeight;
    
    //Player States
    [NonSerialized]
    public bool crouched = false;
    private float jumpTimer = 0f;

    private float stamina;
    private float staminaGainDelayTime;

    private bool tooFarFromTrain;

    private Ladder currentLadder;
    //Input
    private Vector2 moveInput;
    private bool noclip;
    private void Awake()
    {
        active = this;
    }
    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        
        colliderStandingHeight = capsuleCollider.height;
        cameraStandingHeight = cameraTransform.localPosition.y;
        Debug.Log("Compile!!");
    }

    public override float GetTargetSpeed()
    {
        if (tooFarFromTrain)
            return 0f;
        
        float speed = (InputManager.sprintAction.IsPressed() && stamina > 0f) ? sprintingSpeed : baseSpeed;
        if (crouched)
            speed = crouchingSpeed;
        return speed;
    }

    public override Vector2 GetMovementVector()
    {
        moveInput = InputManager.active.moveInput;
        Vector2 mag = FindVelRelativeToLook();
        float xMag = mag.x, zMag = mag.y;

        Debug.DrawLine(transform.position + Vector3.up, transform.position + Vector3.up + new Vector3(xMag, 0, zMag), Color.cyan);
        Debug.DrawLine(transform.position + Vector3.up, transform.position + Vector3.up + rb.linearVelocity, Color.green);

        if (moveInput.x > 0 && xMag > targetSpeed) moveInput.x = 0;
        if (moveInput.x < 0 && xMag < -targetSpeed) moveInput.x = 0;
        if (moveInput.y > 0 && zMag > targetSpeed) moveInput.y = 0;
        if (moveInput.y < 0 && zMag < -targetSpeed) moveInput.y = 0;

        Vector3 targetVector = ((orientation.forward * moveInput.y) + (orientation.right * moveInput.x)).normalized;
        return new Vector2(targetVector.x, targetVector.z);
    }

    private void Update()
    {
        UpdateMovement();
        
        if (InputManager.debugCamAction.triggered && canNoclip)
        {
            if(noclip)
                DisableNoclip();
            else
                EnableNoclip();
        }
        
        moveInput = InputManager.active.moveInput;
        if (noclip)
        {
            //Noclip
            Vector3 movementVector = PlayerCamera.active.transform.forward * moveInput.y +
                                     PlayerCamera.active.transform.right * moveInput.x;
            movementVector = movementVector.normalized * (noclipSpeed * Time.deltaTime);

            if (InputManager.sprintAction.IsPressed())
                movementVector *= 6f;
            rb.MovePosition(transform.position + movementVector);
        }
        else
        {
            //Player specific movement
            
            //Crouching 
            if (crouched == false && InputManager.crouchAction.IsPressed())
            {
                crouched = true;
                capsuleCollider.height = colliderCrouchHeight;
                capsuleCollider.center = new Vector3(0, capsuleCollider.height * 0.5f, 0);
                cameraTransform.localPosition = new Vector3(0, cameraCrouchHeight, 0);

                //if (grounded == false)
                //rb.position += new Vector3(0, cameraStandingHeight - cameraCrouchHeight, 0);
            }
            else if (crouched && InputManager.crouchAction.IsPressed() == false)
            {
                crouched = false;
                capsuleCollider.height = colliderStandingHeight;
                capsuleCollider.center = new Vector3(0, capsuleCollider.height * 0.5f, 0);
                cameraTransform.localPosition = new Vector3(0, cameraStandingHeight, 0);

                //if (grounded == false)
                //rb.position -= new Vector3(0, cameraStandingHeight - cameraCrouchHeight, 0);
            }

            //Calculating Stamina
            if (InputManager.sprintAction.IsPressed() == false)
            {
                //Not Sprinting
                staminaGainDelayTime += Time.deltaTime;
                if (staminaGainDelayTime >= staminaGainDelay)
                {
                    stamina += staminaGain * Time.deltaTime;
                    stamina = Mathf.Clamp(stamina, 0, maxStamina);
                }
            }
            else if(moveInput != Vector2.zero)
            {
                //Sprinting
                staminaGainDelayTime = 0;
                stamina -= staminaLoss * Time.deltaTime;
                stamina = Mathf.Clamp(stamina, 0, maxStamina);
            }
            StatUI.active.UpdateStamina(Mathf.FloorToInt(stamina),maxStamina);
            

            //Jumping
            jumpTimer -= Time.deltaTime;
            if (InputManager.jumpAction.IsPressed() && grounded && jumpTimer <= 0f)
            {
                float force = jumpForce;
                if (IsInWater())
                    force *= (1f - GetPercentUnderWater() * waterSlowDownMult);

                rb.AddForce(0f, crouched ? force * 0.5f : force, 0f);
                
                jumpTimer = jumpCoolDown;
                
                DisableGrounded();

                if(IsInWater())
                    SoundManager.active.PlayAtPos(transform.position, "Water - Impact");
                else
                    SoundManager.active.PlayAtPos(transform.position, "Jump");
            }

            //Distance walked statistic
            float distKM = (new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z).magnitude * Time.deltaTime) / 1000f;
            GameStateManager.AddToStatistic("Distance Walked", distKM, GameStateManager.StatisticType.DistanceKilometers);
            
        }
    }

    private void FixedUpdate()
    {
        FixedUpdateMovement();
        
        if(noclip)
            return;
       

        if (currentLadder != null) 
        {
            //Ladder
            targetSpeed = climbingSpeed;
            
            Vector3 forceVector = Vector3.zero;
            Vector3 ladderDir = currentLadder.GetLadderDir();
            void CheckIfVectorIsClimbing(Vector3 vector)
            {
                if (vector == Vector3.zero) return;

                float angle = Vector2.Angle(new Vector2(vector.x, vector.z), new Vector2(ladderDir.x, ladderDir.z));
                if(angle < climbingAngleThreshold)
                {
                    forceVector += new Vector3(0f, vector.y + 0.3f, 0f); // +0.3f bias to climbing upwards
                }
                else
                {
                    forceVector += vector;
                }
            }
            CheckIfVectorIsClimbing(PlayerCamera.active.GetDir() * moveInput.y);
            CheckIfVectorIsClimbing(orientation.right * moveInput.x);

            forceVector = forceVector.normalized;

            rb.AddForce(forceVector * ((targetSpeed / baseSpeed) * acceleration));

            //Limit speed
            float velocity = rb.linearVelocity.magnitude;
            if (velocity > targetSpeed && moveInput != Vector2.zero)
            {
                Vector3 n = rb.linearVelocity.normalized * targetSpeed;
                rb.linearVelocity = new Vector3(n.x, n.y, n.z);
            }

            //Drag
            if (moveInput == Vector2.zero && velocity > 0.1f)
            {
                rb.AddForce(-rb.linearVelocity.normalized * ((targetSpeed / baseSpeed) * deceleration));
            }
            else if (moveInput == Vector2.zero)
            {
                rb.linearVelocity = new Vector3(0, 0, 0);
            }
        }
        else
        {

            //Max Distance 
            if (maxDistanceLimitEnabled && Train.playerTrain != null && GameStateManager.gameStarted)
            {
                DistanceWaypoint closestWaypoint = null;
                float distanceToWaypoint = float.MaxValue;
                foreach (DistanceWaypoint waypoint in GameStateManager.waypointList)
                {
                    float d = Vector3.Distance(transform.position, waypoint.transform.position);
                    if(d < distanceToWaypoint)
                    {
                        closestWaypoint = waypoint;
                        distanceToWaypoint = d;
                    }
                }

                float trainDist = Train.playerTrain.GetClosestDistanceToPos(transform.position);

                if (closestWaypoint != null && trainDist > distanceToWaypoint)
                {

                    if (distanceToWaypoint > closestWaypoint.maxDistance && Vector3.Distance(transform.position + projectedVector, closestWaypoint.transform.position) > distanceToWaypoint)
                    {
                        Debug.Log("Walking away from a waypoint!");
                        targetSpeed = 0f;
                        if (tooFarFromTrain == false)
                        {
                            Override title = new Override("Title", OverrideType.Text, "WARNING");
                            Override message = new Override("Message", OverrideType.Text, "You're to far from the structure! You may NOT go FURTHER!");
                            Override subText = new Override("SubText", OverrideType.Text, Mathf.Round(distanceToWaypoint) + "m");
                            MiniPrinter.active.AddNotification(PaperRenderer.active.RenderPaper("Message", new List<Override>() { title, message, subText }));
                        }
                        tooFarFromTrain = true;
                    }
                    else if (distanceToWaypoint < closestWaypoint.maxDistance - 2f)
                        tooFarFromTrain = false;
                }
                else
                {
                    if (trainDist >= maxDistanceFromTrain && Train.playerTrain.GetClosestDistanceToPos(transform.position + projectedVector) > trainDist)
                    {
                        Debug.Log("Walking away from the train!");
                        targetSpeed = 0f;
                        if (tooFarFromTrain == false)
                        {
                            Override title = new Override("Title", OverrideType.Text, "WARNING");
                            Override message = new Override("Message", OverrideType.Text, "You're to far from the train! You may NOT go FURTHER!");
                            Override subText = new Override("SubText", OverrideType.Text, Mathf.Round(trainDist) + "m");
                            MiniPrinter.active.AddNotification(PaperRenderer.active.RenderPaper("Message", new List<Override>() { title, message, subText }));
                        }
                        tooFarFromTrain = true;
                    }
                    else if (trainDist < maxDistanceFromTrain - 2f)
                        tooFarFromTrain = false;
                }
            }
        }
    }
    
    private Vector2 FindVelRelativeToLook() {
        //Not my code. From Dani
        float lookAngle = orientation.eulerAngles.y;
        float moveAngle = Mathf.Atan2(rb.linearVelocity.x, rb.linearVelocity.z) * Mathf.Rad2Deg;

        float u = Mathf.DeltaAngle(lookAngle, moveAngle);
        float v = 90 - u;

        float magnitude = rb.linearVelocity.magnitude;
        float yMag = magnitude * Mathf.Cos(u * Mathf.Deg2Rad);
        float xMag = magnitude * Mathf.Cos(v * Mathf.Deg2Rad);
        
        return new Vector2(xMag, yMag);
    }
    
    public void EnableNoclip()
    {
        Freeze();
        MovingPlatformManager.active.RemoveEntry(transform);
        capsuleCollider.enabled = false;
        noclip = true;
        ExitLadder();
        
        SetMovementScriptState(MovementScriptState.Disabled);
    }
    public void DisableNoclip()
    {
        UnFreeze();
        capsuleCollider.enabled = true;
        noclip = false;
        
        SetMovementScriptState(MovementScriptState.Default);
    }

    //Ladders
    private void OnTriggerEnter(Collider other)
    {
        if(currentLadder == null && other.transform.TryGetComponent(out Ladder ladder))
        {
            EnterLadder(ladder);
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (currentLadder != null && other.transform == currentLadder.ladderCollider.transform)
        {
            ExitLadder();
        }
    }
    private void EnterLadder(Ladder ladder)
    {
        currentLadder = ladder;
        MovingPlatformManager.active.RemoveEntry(transform);
        MovingPlatformManager.active.AddEntry(rb, orientation, currentLadder.ladderCollider.transform, true);
        grounded = false;
        
        SetMovementScriptState(MovementScriptState.Disabled);
    }
    private void ExitLadder()
    {
        currentLadder = null;
        MovingPlatformManager.active.RemoveEntry(transform);
        
        SetMovementScriptState(MovementScriptState.Default);
    }
}
