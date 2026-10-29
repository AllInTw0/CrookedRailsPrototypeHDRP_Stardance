using System;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

public class Companion : NetworkBehaviour
{
    public struct CompanionNetworkData : INetworkSerializable
    {
        public Vector3 position;
        public Vector3 velocity;

        public int currentState;
        public NetworkObjectReference targetObject;

        public float time;
        
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref position);
            serializer.SerializeValue(ref velocity);
            serializer.SerializeValue(ref currentState);
            serializer.SerializeValue(ref time);
            serializer.SerializeValue(ref targetObject);
        }
    }
    private NetworkVariable<CompanionNetworkData> networkData = new(writePerm: NetworkVariableWritePermission.Owner);
    
    private void NetworkStateChanged(CompanionNetworkData previous, CompanionNetworkData current)
    {
        if (IsOwner == false)
        {
            float timeDiff = NetworkManager.Singleton.LocalTime.TimeAsFloat - current.time;
            
            Rigidbody rb = GetComponent<Rigidbody>();
            rb.MovePosition(current.position + current.velocity * timeDiff);
            rb.linearVelocity = current.velocity;
            
            state = (CompanionState)current.currentState;

            if (current.targetObject.TryGet(out NetworkObject networkObject))
                movement.SetTarget(networkObject.transform,2f);
        }
    }
    
    public enum CompanionState
    {
        turnedOff,
        normal,
        goingToJumpStart,
        ragdolled,
        TV,
        flee,
        shooting
    }
    private PathFindingGroundMovement movement;
    private CompanionState _state;

    private CompanionState state
    {
        get
        {
            return _state;
        }
        set
        {
            if (_state == CompanionState.turnedOff && value != CompanionState.turnedOff)
            {
                health.SetGlobalVisibility(true);
                faceText.enabled = true;
                turnOnInteractable.gameObject.SetActive(false);
                movement.rb.isKinematic = false;
                animator.SetBool("isOn", true);
            }
            if (value == CompanionState.turnedOff)
            {
                health.SetGlobalVisibility(false);
                faceText.enabled = false;
                turnOnInteractable.gameObject.SetActive(true);
                movement.rb.isKinematic = true;
                animator.SetBool("isOn", false);
            }
            
            if (_state != CompanionState.TV && value == CompanionState.TV)
            {
                //Enable TV
                animator.SetTrigger("twitch");
                Invoke(nameof(EnableTV),0.1f);
                
            }
            else if (_state == CompanionState.TV && value != CompanionState.TV)
            {
                //Disable TV
                animator.SetTrigger("twitch");
                Invoke(nameof(DisableTV),0.1f);
            }

            if (value != CompanionState.ragdolled)
            {
                if (value == CompanionState.flee)
                {
                    ChangeFaceState(1, 50000f);
                }
                else if (_state == CompanionState.flee)
                {
                    ChangeFaceState(0, 0f);
                }
            }

            if (value == CompanionState.shooting)
            {
                animator.SetBool("shooting", true);
                ChangeFaceState(2, 50000f);
            }
            else if (_state == CompanionState.shooting)
            {
                animator.SetBool("shooting", false);
                ChangeFaceState(0, 0f);
            }
            
            _state = value;
        }
    }

    private void EnableTV() {tvObject.SetActive(true);}
    private void DisableTV() {tvObject.SetActive(false);}
    
    [Header("Animation")] 
    public Animator animator;
    public float rotationSpeed;
    public Transform headRotX;
    public Transform headRotY;
    public Transform screen;
    [Header("Faces")] 
    public TMP_Text faceText;
    public Color[] faceColorArray;
    public float faceTimerDecay;
    private string[][] faceArray = new[]
    {
        new[]{
            ":)",
            ":("
        },
        new[]{
            ":o",
            "X("
        },
        new[]{
            ":)",
            ":("
        }
    };

    private int faceStateIndex;
    private int faceIndex;
    private float faceStateTimer;
    private float faceTimer;
    [Header("Behaviour")] 
    public float behaviourUpdateTime = 0.5f;
    public float maxDistanceFromPlayer;
    [Header("TV")] 
    public GameObject tvObject;
    public Vector2 randomTimeToActivateTV;
    private float tvTime;
    [Header("Jumping")] 
    public float minJumpVelocityAddition;
    public float jumpDelay;
    public bool angleMode;
    [Header("Shooting")] 
    public Transform gunRot;
    public Transform gunEnd;
    public ItemGun gun;
    public List<HealthType> healthTypeFilterList;
    public float maxDistance;
    public float fleeStartDistance;
    public float fleeEndDistance;
    [Header("Health")] 
    public Health health;
    public ItemSO itemDropOnDeath;
    [Header("OffState")] 
    public bool isOnOnStart = false;
    public EventInteractable turnOnInteractable;
    
    private Vector3 lookVectorOverride;
    private float jumpTimmer;
    private bool touchingGround = true;
    private Transform helperTransform;
    private Transform[] navigationTransformArray;

    private float lastScreenRotX;
    private float lastScreenRotY;

    public override void OnNetworkSpawn()
    {
        networkData.OnValueChanged += NetworkStateChanged;
    }

    private void Start()
    {
        movement = GetComponent<PathFindingGroundMovement>();
        helperTransform = new GameObject("CompanionHelperTransform").transform;
        navigationTransformArray = new Transform[2];
        for (int i = 0; i < navigationTransformArray.Length; i++)
        {
            navigationTransformArray[i] = new GameObject("CompanionNavigationTransform: " + i).transform;
        }
        
        tvObject.SetActive(false);
        health.onTakeDamage.AddListener((dmg, force) =>
        {
            AddFaceChangeImpact(1.2f + dmg * 0.1f);
            ChangeFaceState(1, 10f);
            Ragdoll(force);
            movement.rb.angularVelocity = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f));
            
        });
        health.onHealthReachedZero.AddListener((force)=>
        {
            if (IsServer)
            {
                Item.SpawnItem(itemDropOnDeath, transform.position + Vector3.up * 0.2f, transform.rotation);
                NetworkObject.Despawn(true);
            }
        });
        InvokeRepeating(nameof(UpdateBehaviour), behaviourUpdateTime, behaviourUpdateTime);

        turnOnInteractable.interactEventEveryone.AddListener(() =>
        {
            state = CompanionState.normal; //TurnOn
        });
        if (isOnOnStart)
            state = CompanionState.normal; //TurnOn
        else
        {
            state = CompanionState.normal;
            state = CompanionState.turnedOff; //TurnOff
        }
    }
    private void UpdateBehaviour()
    { 
        //Debug.Log("Current state: " + state);
        switch (state)
        {
            case CompanionState.normal:
                if (CheckForTargets(out Health closestPlayer, new List<HealthType>() { HealthType.Player }, float.MaxValue))
                {
                    if (Vector3.Distance(closestPlayer.transform.position, transform.position) > maxDistanceFromPlayer &&
                        NavMesh.SamplePosition(closestPlayer.transform.position + Random.insideUnitSphere * Random.Range(3,8f),out NavMeshHit hit, 20f,NavMesh.AllAreas))
                    {
                        transform.position = hit.position;
                    }
                    movement.SetTarget(closestPlayer.transform, 2f);
                }

                //Check if should jump
                bool shouldJump = false;
                if (IsPointOnMovingPlatform(movement.GetTargetPosition(), out Transform playerPlatform))
                {
                    if (IsPointOnMovingPlatform(transform.position, out Transform companionPlatform))
                    {
                        if (!(playerPlatform == companionPlatform || playerPlatform.parent == companionPlatform || playerPlatform == companionPlatform.parent))
                        {
                            shouldJump = true;
                        }
                    }
                    else
                    {
                        shouldJump = true;
                    }
                }
                
                if (CheckForTargets(out Health h, healthTypeFilterList, maxDistance))
                {
                    state = CompanionState.shooting;
                }
                else if (shouldJump)
                {
                    //Partial path - jump
                    
                    //Find jump start / end
                    
                    //Path from target to companion. Last path point will be the jump end
                    NavMeshPath reversedPath = new NavMeshPath();
                    NavMesh.CalculatePath(movement.GetTargetPosition(), transform.position, NavMesh.AllAreas, reversedPath);
                    
                    Debug.Log("Path Length: " + reversedPath.corners.Length);
                    if(reversedPath.corners.Length < 2)
                        break;
                    
                    //Assign jump end pos to a transform that will move with a moving platform if needed
                    MovingPlatformManager.active.RemoveEntry(navigationTransformArray[1]);
                    navigationTransformArray[1].position = reversedPath.corners[^1];
                    if (IsPointOnMovingPlatform(reversedPath.corners[^1], out Transform platform))
                    {
                        MovingPlatformManager.active.AddEntry(navigationTransformArray[1],platform);
                    }
                    
                    //Find jump start
                    int safety = 10;
                    while (true)
                    {
                        Vector3 pathDir = (reversedPath.corners[^1] - reversedPath.corners[^2]).normalized;
                        pathDir = Quaternion.AngleAxis(Random.Range(-20f, 20f), Vector3.up) * pathDir;
                        Vector3 samplePos = reversedPath.corners[^1] + pathDir * 6f + Vector3.up * 2f;


                        if (SampleNavPos(samplePos, 6f, out Vector3 hitPos, out Transform movingPlatform))
                        {
                            NavMeshPath safetyPath = new NavMeshPath();
                            NavMesh.CalculatePath(transform.position, hitPos, NavMesh.AllAreas, safetyPath);
                            
                            if (!Physics.Raycast(safetyPath.corners[^1] + Vector3.up, reversedPath.corners[^1] - safetyPath.corners[^1], Vector3.Distance(reversedPath.corners[^1], safetyPath.corners[^1])))
                            {
                                //Assign jump start to a transform that will move with a platform if needed
                                MovingPlatformManager.active.RemoveEntry(navigationTransformArray[0]);
                                navigationTransformArray[0].position = safetyPath.corners[^1];
                                if (movingPlatform != null)
                                    MovingPlatformManager.active.AddEntry(navigationTransformArray[0], platform);
                                break;
                            }
                        }
    
                        safety--;
                        if (safety < 0)
                        {
                            Debug.LogWarning("[Companion] could not find jump");
                            return;
                        }
                    }

                    
                    
                    movement.SetTarget(navigationTransformArray[0],0.5f);
                    jumpTimmer = 0f;
                    state = CompanionState.goingToJumpStart;
                }
                else
                {
                    if (movement.HasReachedTarget())
                    {
                        if(tvTime <= 0)
                            tvTime = Random.Range(randomTimeToActivateTV.x, randomTimeToActivateTV.y);
                        else
                        {
                            tvTime -= behaviourUpdateTime;
                            if (tvTime <= 0)
                                state = CompanionState.TV;

                        }
                    }
                    else
                    {
                        tvTime = 0f;
                        
                    }
                }
                break;
            case  CompanionState.goingToJumpStart:
                movement.SetTarget(navigationTransformArray[0],0.5f);
                if (movement.HasReachedTarget())
                {
                    //At jump start
                    if (jumpTimmer < jumpDelay)
                    {
                        if (jumpTimmer == 0f)
                        {
                            Debug.Log("Jump Animation");
                            animator.SetTrigger("jump");
                        }

                        jumpTimmer += behaviourUpdateTime;
                        lookVectorOverride = navigationTransformArray[1].position - transform.position;
                        break;
                    }
                    lookVectorOverride = Vector3.zero;
                    
                    float angle = Util.SolveTrajectoryMinAngle(transform.position, navigationTransformArray[1].position);
                    float velocity = Util.SolveVelocityForAngle(angle, transform.position, navigationTransformArray[1].position);
                    velocity += minJumpVelocityAddition;
                    
                    Util.SolveTrajectory(velocity, transform.position, navigationTransformArray[1].position, angleMode, out angle);

                    helperTransform.position = transform.position;
                    helperTransform.LookAt(navigationTransformArray[1].position);
                    if (angle == float.NaN)
                        angle = 0f;
                    helperTransform.rotation = Quaternion.Euler(angle, helperTransform.eulerAngles.y, 0f);
                    
                    Debug.DrawRay(transform.position, helperTransform.forward, Color.white, 60f);
                    
                    Ragdoll();
                    movement.rb.linearVelocity = helperTransform.forward * (velocity + 3f);
                    
                    //AddFaceChangeImpact(1f);
                    ChangeFaceState(1, 500f);
                    SoundManager.active.PlayAtPos(transform.position, "Robot1", parent: transform);
                }
                else if (movement.IsStuck())
                {
                    Debug.LogWarning("Got stuck going to jump start");
                    state = CompanionState.normal;
                    movement.SetTarget(transform.position + new Vector3(Random.Range(-2f,2f),0f,Random.Range(-2f,2f)));
                }

                break;
            case CompanionState.ragdolled:
                if (movement.rb.linearVelocity.magnitude < 0.1f)
                {
                    movement.rb.constraints = RigidbodyConstraints.FreezeRotation;
                    state = CompanionState.normal;
                    
                    ChangeFaceState(0, 0f);
                }
                break;
            case CompanionState.TV:
                movement.SetTarget(Player.localPlayer.transform,1f);
                if (movement.HasReachedTarget() == false)
                {
                    state = CompanionState.normal;
                }
                break;
            case CompanionState.shooting:
                if (CheckForTargets(out Health closestHealth, healthTypeFilterList, maxDistance))
                {
                    //Stop movement
                    movement.SetTarget(closestHealth.origin,50000000f);
                    
                    helperTransform.position = gunRot.position;
                    helperTransform.LookAt(closestHealth.origin);
                    
                    gunRot.rotation = Quaternion.Lerp(gunRot.rotation, Quaternion.LookRotation(helperTransform.forward), Time.deltaTime * rotationSpeed);

                    float dist = Vector3.Distance(closestHealth.origin.position, transform.position);
                    if (dist <= fleeStartDistance)
                        state = CompanionState.flee;
                    else
                    {
                        //Debug.Log($"[Companion] cooldown {gun.GetCooldown()}, { IsOwner}");
                        if (gun.IsOnCooldown() == false && IsOwner)
                        {
                            //Debug.Log("[Companion] 2");
                            if (gun.HasAmmoInClip())
                            {
                                //Debug.Log("[Companion] 3");
                                if(Quaternion.Angle(gunRot.rotation,Quaternion.LookRotation(helperTransform.forward)) < 5f)
                                    ShootRpc();
                            }
                            else
                            {
                                //Debug.Log("[Companion] 4");
                                ReloadRpc(gun.clipSize);
                            }
                        }
                    }
                }
                else
                {
                    state = CompanionState.normal;
                }
                break;
            case CompanionState.flee:
                if (CheckForTargets(out Health closestHealth2, healthTypeFilterList, maxDistance))
                {
                    float dist = Vector3.Distance(closestHealth2.origin.position, transform.position);
                    if (dist <= fleeEndDistance)
                    {
                        NavMesh.SamplePosition(transform.position + (transform.position - closestHealth2.origin.position).normalized * fleeEndDistance,out NavMeshHit hit,20f,NavMesh.AllAreas);
                        movement.SetTarget(hit.position);
                    }
                    else
                    {
                        state = CompanionState.normal;
                    }
                }
                else
                {
                    state = CompanionState.normal;
                }
                break;
        }
        
        movement.UpdateNavigationBehaviour(preferPath: true);
        
        //Multiplayer
        
        if (IsServer)
        {
            Transform target = movement.GetTargetTransform();
            if (target && target.TryGetComponent(out Player player) && NetworkObject.OwnerClientId != player.OwnerClientId)
            {
                NetworkObject.ChangeOwnership(player.OwnerClientId);
                Debug.Log("Changed Companion Ownership");
            }
        }
        if (IsOwner)
        {
            NetworkObject target = null;
            if (movement.GetTargetTransform() != null && movement.GetTargetTransform().TryGetComponent(out NetworkBehaviour networkBehaviour))
            {
                target = networkBehaviour.NetworkObject;
            }
            networkData.Value = new CompanionNetworkData()
            {
                position = transform.position,
                velocity = movement.rb.linearVelocity,
                currentState = (int)state,
                time = NetworkManager.Singleton.LocalTime.TimeAsFloat,
                targetObject = target
            };
        }
    }

    [Rpc(SendTo.Everyone)]
    private void ShootRpc()
    {
        bool doDamage = IsOwner;
        gun.Shoot(gunEnd.position, gunEnd.forward, doDamage, ShooterType.Sentry);
    }
    [Rpc(SendTo.Everyone)]
    private void ReloadRpc(int count)
    {
        gun.ReloadClient(count, gunEnd.position);
    }
    private bool SampleNavPos(Vector3 worldPos, float range, out Vector3 hitPos, out Transform movingPlatform)
    {
        if (NavMesh.SamplePosition(worldPos, out NavMeshHit navHit, range, NavMesh.AllAreas))
        {
            if (IsPointOnMovingPlatform(navHit.position, out Transform platform))
            {
                hitPos = navHit.position;
                movingPlatform = platform;
                return true;
            }
            
            hitPos = navHit.position;
            movingPlatform = null;
            return true;
        }

        hitPos = Vector3.zero;
        movingPlatform = null;
        return false;
    }

    private bool IsPointOnMovingPlatform(Vector3 worldPos, out Transform movingPlatform)
    {
        if (Physics.Raycast(worldPos + new Vector3(0f, 0.1f, 0f), Vector3.down, out RaycastHit rayHit, 1f,
                movement.groundLayer))
        {
            if (rayHit.collider.tag == "Moving")
            {
                //Is on a moving platform
                movingPlatform = rayHit.collider.transform;
                return true;
            }
        }
        movingPlatform = null;
        return false;
    }
    private void Update()
    {
        if(movement.rb.isKinematic)
            return;
        
        if (state != CompanionState.ragdolled)
        {
            movement.UpdateNavMovement();
        }
    }

    private void LateUpdate()
    {
        Animate();
    }

    private void FixedUpdate()
    {
        if(movement.rb.isKinematic)
            return;
        
        if (state != CompanionState.ragdolled)
        {
            movement.FixedUpdateMovement();
        }
        else
        {
            if (touchingGround)
                movement.rb.linearVelocity *= 0.99f;
        }
    }

    private void Ragdoll(Vector3 additionalVelocity = default)
    {
        if(state == CompanionState.turnedOff)
            return;
        
        movement.rb.constraints = RigidbodyConstraints.None;
        movement.rb.linearVelocity += additionalVelocity;
        state = CompanionState.ragdolled;
    }

    private void ChangeFaceState(int faceState, float time = 5f)
    {
        faceStateIndex = faceState;
        faceStateTimer = time;
    }

    private void AddFaceChangeImpact(float impact)
    {
        faceTimer += impact;
    }
    private void Animate()
    {
        
        if(state == CompanionState.turnedOff)
            return;
        
        //Animation
        if(state == CompanionState.ragdolled)
            animator.SetBool("move",false);
        else
        {
            if(movement.HasReachedTarget())
                animator.SetBool("move",false);
            else
                animator.SetBool("move",true);
        }
        //Faces
        faceTimer -= faceTimerDecay * Time.deltaTime;
        if (faceTimer < 0f) faceTimer = 0f;
        if (faceStateIndex != 0)
        {
            faceStateTimer -= Time.deltaTime;
            if (faceStateTimer < 0f)
                faceStateIndex = 0;
        }
        
        float clamped = Mathf.Clamp(faceTimer, 0f, faceArray[faceStateIndex].Length-1);
        faceText.text = faceArray[faceStateIndex][Mathf.FloorToInt(clamped+0.99f)];
        faceText.color = faceColorArray[faceStateIndex];
        
        //Rotation
        //Logic
        Vector3 bodyLook = Vector3.zero;
        Vector3 screenLook = movement.GetTargetTransform() == Player.localPlayer.transform ? PlayerCamera.active.transform.position - screen.position : movement.GetTargetPosition() - screen.position;
        if (movement.HasReachedTarget() == false)
        {
            bodyLook = movement.targetDirVector;
        }

        if (lookVectorOverride != Vector3.zero)
        {
            bodyLook = lookVectorOverride;
            screenLook = lookVectorOverride;
        }

        if (state == CompanionState.ragdolled)
            screenLook = transform.forward;
        
        //Body Rotation
        //Debug.Log($"{bodyLook}, {screenLook}");
        if (state != CompanionState.ragdolled)
        {
            if (bodyLook != Vector3.zero)
                transform.rotation = Quaternion.Lerp(transform.rotation,Quaternion.LookRotation(new Vector3(bodyLook.x, 0, bodyLook.z)), Time.deltaTime * rotationSpeed);
            else
                transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(0, transform.eulerAngles.y, 0), Time.deltaTime * rotationSpeed);
        }

        //Screen Rotation
        if (screenLook != Vector3.zero)
        {
            Vector3 inverseScreenLook = transform.InverseTransformDirection(screenLook);
            headRotY.localRotation = Quaternion.Lerp(Quaternion.Euler(headRotY.localEulerAngles.x,lastScreenRotY,screen.localEulerAngles.z), Quaternion.LookRotation(new Vector3(inverseScreenLook.x, 0, inverseScreenLook.z).normalized), Time.deltaTime * rotationSpeed);
            lastScreenRotY = headRotY.localEulerAngles.y;
            float angle = Vector3.Angle(new Vector3(inverseScreenLook.x, 0, inverseScreenLook.z), inverseScreenLook);
            screen.localRotation = Quaternion.Lerp(Quaternion.Euler(lastScreenRotX,screen.localEulerAngles.y,screen.localEulerAngles.z), Quaternion.Euler(-angle,screen.localEulerAngles.y,screen.localEulerAngles.z), Time.deltaTime * rotationSpeed);
            lastScreenRotX = screen.localEulerAngles.x;
        }
    }
    public bool CheckForTargets(out Health closestHealth, List<HealthType> filterList, float maxDistance)
    {
        closestHealth = null;
        float closestDistance = float.PositiveInfinity;
        for (int i = 0; i < filterList.Count; i++)
        {
            foreach (Health health in EnemyManager.active.healthTypeArray[(int)filterList[i]])
            {
                if (health.GetHealth() <= 0f)
                    continue;

                float distance = Vector3.Distance(transform.position, health.transform.position);

                if (distance <= maxDistance && distance < closestDistance)
                {
                    closestHealth = health;
                    closestDistance = distance;
                }
            }
        }

        if (closestHealth)
            return true;
        return false;
    }
    private void OnCollisionEnter(Collision collision)
    {
        if(state == CompanionState.turnedOff)
            return;
        
        if ((movement.groundLayer.value & (1 << collision.gameObject.layer)) != 0)
        {
            //Debug.Log("OWawahwa!");
            touchingGround = true;
        }

        if (collision.rigidbody != null && collision.rigidbody.linearVelocity.magnitude > 1f)
        {
            AddFaceChangeImpact(1.2f + collision.relativeVelocity.magnitude * 0.1f);
            ChangeFaceState(1, 10f);
            SoundManager.active.PlayAtPos(transform.position, "Robot0", parent: transform);
            Ragdoll(collision.rigidbody.linearVelocity * 0.9f);
        }
        else if (state == CompanionState.ragdolled && movement.rb.linearVelocity.magnitude > 1f)
            SoundManager.active.PlayAtPos(transform.position, "Robot0", parent: transform);
    }

    private void OnCollisionExit(Collision collision)
    {
        if(state == CompanionState.turnedOff)
            return;
        
        if ((movement.groundLayer.value & (1 << collision.gameObject.layer)) != 0)
        {
            //Debug.Log("Im Fine!");
            touchingGround = false;
        }
    }

    private void OnDrawGizmos()
    {
        if(Application.isPlaying == false) return;
        
        Gizmos.color = Color.green;
        Gizmos.DrawCube(navigationTransformArray[0].position, Vector3.one);
        Gizmos.color = Color.red;
        Gizmos.DrawCube(navigationTransformArray[1].position, Vector3.one);
    }
}
