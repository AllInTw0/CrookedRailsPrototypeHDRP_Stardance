using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : GroundMovement
{
    [NonSerialized] 
    public Player player;
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
    [Header("Variable Jump")] 
    [SerializeField]
    private Vector2 variableJumpTime;
    [SerializeField]
    private float variableJumpGravityForce;
    
    //Runtime References
    private float colliderStandingHeight;
    private float cameraStandingHeight;
    
    //Player States
    [NonSerialized]
    public bool crouched = false;
    private float jumpTimer = 0f;

    private float stamina;
    private float staminaGainDelayTime;

    private bool sentATooFarFromTrainWarning;
    private bool tooFarFromTrain;

    private Ladder currentLadder;
    //Input
    private Vector2 moveInput;
    private bool noclip;
    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        
        colliderStandingHeight = capsuleCollider.height;
        cameraStandingHeight = cameraTransform.localPosition.y;
    }

    public override float GetTargetSpeed()
    {
        if (player != Player.localPlayer)
            return 0f;
        
        if (tooFarFromTrain)
            return 0f;
        
        float speed = (InputManager.sprintAction.IsPressed() && stamina > 0f) ? sprintingSpeed : baseSpeed;
        if (crouched)
            speed = crouchingSpeed;
        return speed;
    }

    public override Vector2 GetMovementVector()
    {
        if (player != Player.localPlayer)
            return Vector3.zero;
        
        moveInput = InputManager.active.moveInput;
        Vector2 mag = FindVelRelativeToLook();
        float xMag = mag.x, zMag = mag.y;

        Debug.DrawLine(transform.position + Vector3.up, transform.position + Vector3.up + new Vector3(xMag, 0, zMag), Color.cyan);
        Debug.DrawLine(transform.position + Vector3.up, transform.position + Vector3.up + rb.linearVelocity, Color.green);

        if (targetSpeed > 0)//dont modify move input if target speed is zero. This will mess with movement prediction for gameplay space distance checking
        {
            if (moveInput.x > 0 && xMag > targetSpeed) moveInput.x = 0;
            if (moveInput.x < 0 && xMag < -targetSpeed) moveInput.x = 0;
            if (moveInput.y > 0 && zMag > targetSpeed) moveInput.y = 0;
            if (moveInput.y < 0 && zMag < -targetSpeed) moveInput.y = 0;
        }

        Vector3 targetVector = ((orientation.forward * moveInput.y) + (orientation.right * moveInput.x)).normalized;
        return new Vector2(targetVector.x, targetVector.z);
    }

    private Vector3 GetNoclipVector()
    {
        Vector3 movementVector = PlayerCamera.active.transform.forward * moveInput.y +
                                 PlayerCamera.active.transform.right * moveInput.x;
        movementVector = movementVector.normalized * noclipSpeed;

        if (InputManager.sprintAction.IsPressed())
            movementVector *= 6f;
        return movementVector;
    }
    private void Update()
    {
        if(noclip == false && currentLadder == null)
            UpdateMovement();

        if (player == Player.localPlayer)
        {
            if (InputManager.debugCamAction.triggered && canNoclip)
            {
                if (noclip)
                    DisableNoclip();
                else
                    EnableNoclip();
            }

            moveInput = InputManager.active.moveInput;
            if (noclip)
            {
                //Noclip
                rb.MovePosition(transform.position + GetNoclipVector() * Time.deltaTime);
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
                else if (moveInput != Vector2.zero)
                {
                    //Sprinting
                    staminaGainDelayTime = 0;
                    stamina -= staminaLoss * Time.deltaTime;
                    stamina = Mathf.Clamp(stamina, 0, maxStamina);
                }

                StatUI.active.UpdateStamina(Mathf.FloorToInt(stamina), maxStamina);


                //Jumping
                jumpTimer += Time.deltaTime;
                if (InputManager.jumpAction.WasPressedThisFrame() && grounded && jumpTimer >= jumpCoolDown)
                {
                    float force = jumpForce;
                    if (IsInWater())
                        force *= (1f - GetPercentUnderWater() * waterSlowDownMult);

                    rb.AddForce(0f, crouched ? force * 0.5f : force, 0f);

                    jumpTimer = 0f;

                    DisableGrounded();

                    if (IsInWater())
                        SoundManager.active.PlayAtPos(transform.position, "Water - Impact");
                    else
                        SoundManager.active.PlayAtPos(transform.position, "Jump");
                }

                //Distance walked statistic
                float distKM = (new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z).magnitude * Time.deltaTime) /
                               1000f;
                GameStateManager.AddToStatistic("Distance Walked", distKM,
                    GameStateManager.StatisticType.DistanceKilometers);

            }
        }
    }

    private void FixedUpdate()
    {
        if(noclip)
            return;
        
        if (currentLadder != null) 
        {
            if (player == Player.localPlayer)
            {
                //Ladder
                targetSpeed = climbingSpeed;

                Vector3 forceVector = Vector3.zero;
                Vector3 ladderDir = currentLadder.GetLadderDir();

                void CheckIfVectorIsClimbing(Vector3 vector)
                {
                    if (vector == Vector3.zero) return;

                    float angle = Vector2.Angle(new Vector2(vector.x, vector.z), new Vector2(ladderDir.x, ladderDir.z));
                    if (angle < climbingAngleThreshold)
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
        }
        else
        {
            if (player == Player.localPlayer)
            {
                if (jumpTimer >= variableJumpTime.x && jumpTimer <= variableJumpTime.y &&
                    InputManager.jumpAction.IsPressed() == false && grounded == false)
                {
                    rb.AddForce(Vector3.down * variableJumpGravityForce);
                }


                //Max Distance 
                if (maxDistanceLimitEnabled && Train.playerTrain != null && GameStateManager.gameStarted)
                {
                    DistanceWaypoint closestWaypoint = null;
                    float distanceToWaypoint = float.MaxValue;
                    foreach (DistanceWaypoint waypoint in GameStateManager.waypointList)
                    {
                        float d = Vector3.Distance(transform.position, waypoint.transform.position);
                        if (d < distanceToWaypoint)
                        {
                            closestWaypoint = waypoint;
                            distanceToWaypoint = d;
                        }
                    }

                    float trainDist = Train.playerTrain.GetClosestDistanceToPos(transform.position);

                    if (closestWaypoint != null && trainDist > distanceToWaypoint)
                    {

                        if (distanceToWaypoint > closestWaypoint.maxDistance &&
                            Vector3.Distance(transform.position + projectedVector, closestWaypoint.transform.position) >
                            distanceToWaypoint)
                        {
                            Debug.Log("Walking away from a waypoint!");
                            if (sentATooFarFromTrainWarning == false)
                            {
                                Override title = new Override("Title", OverrideType.Text, "WARNING");
                                Override message = new Override("Message", OverrideType.Text,
                                    "You're to far from the structure! You may NOT go FURTHER!");
                                Override subText = new Override("SubText", OverrideType.Text,
                                    Mathf.Round(distanceToWaypoint) + "m");
                                MiniPrinter.active.AddNotification(PaperRenderer.active.RenderPaper("Message",
                                    new List<Override>() { title, message, subText }));
                                sentATooFarFromTrainWarning = true;
                            }

                            tooFarFromTrain = true;
                        }
                        else
                        {
                            if (distanceToWaypoint < closestWaypoint.maxDistance - 2f)
                                sentATooFarFromTrainWarning = false;
                            tooFarFromTrain = false;
                        }
                    }
                    else
                    {
                        if (trainDist >= maxDistanceFromTrain &&
                            Train.playerTrain.GetClosestDistanceToPos(transform.position + projectedVector) > trainDist)
                        {
                            Debug.Log("Walking away from the train!");
                            if (sentATooFarFromTrainWarning == false)
                            {
                                Override title = new Override("Title", OverrideType.Text, "WARNING");
                                Override message = new Override("Message", OverrideType.Text,
                                    "You're to far from the train! You may NOT go FURTHER!");
                                Override subText = new Override("SubText", OverrideType.Text,
                                    Mathf.Round(trainDist) + "m");
                                MiniPrinter.active.AddNotification(PaperRenderer.active.RenderPaper("Message",
                                    new List<Override>() { title, message, subText }));
                                sentATooFarFromTrainWarning = true;
                            }

                            tooFarFromTrain = true;
                        }
                        else
                        {
                            if (trainDist < maxDistanceFromTrain - 2f)
                                sentATooFarFromTrainWarning = false;
                            tooFarFromTrain = false;
                        }
                    }
                }
            }

            FixedUpdateMovement();
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
        UnFreeze();
        MovingPlatformManager.active.RemoveEntry(transform);
        capsuleCollider.enabled = false;
        noclip = true;
        ExitLadder();
    }
    public void DisableNoclip()
    {
        capsuleCollider.enabled = true;
        noclip = false;
        rb.linearVelocity = GetNoclipVector();
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
    }
    private void ExitLadder()
    {
        currentLadder = null;
        MovingPlatformManager.active.RemoveEntry(transform);
    }

    public bool IsPositionInGameplaySpace(Vector3 worldPos)
    {
        DistanceWaypoint closestWaypoint = null;
        float distanceToWaypoint = float.MaxValue;
        foreach (DistanceWaypoint waypoint in GameStateManager.waypointList)
        {
            float d = Vector3.Distance(worldPos, waypoint.transform.position);
            if(d < distanceToWaypoint)
            {
                closestWaypoint = waypoint;
                distanceToWaypoint = d;
            }
        }

        float trainDist = 0f;
        if(Train.playerTrain != null)
            trainDist = Train.playerTrain.GetClosestDistanceToPos(worldPos);

        return trainDist < maxDistanceFromTrain || (closestWaypoint != null && distanceToWaypoint < closestWaypoint.maxDistance);
    }
}
