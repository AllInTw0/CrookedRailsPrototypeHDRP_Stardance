using System;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

public class Companion : MonoBehaviour
{
    public enum CompanionState
    {
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

                if (value == CompanionState.flee)
                {
                    ChangeFaceState(1, 50000f);
                }
                else if (_state == CompanionState.flee)
                {
                    ChangeFaceState(0, 0f);
                }
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
    [Header("TV")] 
    public GameObject tvObject;
    public Vector2 randomTimeToActivateTV;
    private float tvTime;
    [Header("Jumping")] 
    public float minJumpVelocityAddition;
    public float jumpDelay;
    public bool angleMode;
    private Vector3 jumpStart;
    private Vector3 jumpEnd;
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
    
    private Vector3 lookVectorOverride;
    private float jumpTimmer;
    private bool touchingGround = true;
    private Transform helperTransform;

    private float lastScreenRotX;
    private void Start()
    {
        movement = GetComponent<PathFindingGroundMovement>();
        helperTransform = new GameObject("CompanionHelperTransform").transform;
        tvObject.SetActive(false);
        health.onTakeDamage.AddListener((dmg, force) =>
        {
            AddFaceChangeImpact(1.2f + dmg * 0.1f);
            ChangeFaceState(1, 10f);
            Ragdoll(force);
            movement.rb.angularVelocity = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f));
        });
    }

    private void UpdateBehaviour()
    { 
        switch (state)
        {
            case CompanionState.normal:
                movement.SetTarget(PlayerCamera.active.transform,2f);
                if (CheckForTargets(out Health h))
                {
                    state = CompanionState.shooting;
                }
                else if (movement.UpdateNavigationBehaviour(true))
                {
                    //Partial path - jump
                    
                    //Find jump start / end
                    NavMeshPath reversedPath = new NavMeshPath();
                    NavMesh.CalculatePath(movement.GetTargetPosition(), transform.position, NavMesh.AllAreas, reversedPath);
                    
                    Debug.Log("Path Length: " + reversedPath.corners.Length);
                    if(reversedPath.corners.Length < 2)
                        break;
                    
                    jumpEnd = reversedPath.corners[^1];
                    Vector3 pathDir = (reversedPath.corners[^1] - reversedPath.corners[^2]).normalized;
                    
                    Vector3 bias = Vector3.down;
                    if (jumpEnd.y < transform.position.y) bias = -bias;
                    NavMesh.SamplePosition(jumpEnd + pathDir * 6f + Vector3.up * 2f, out NavMeshHit hit, 6f, NavMesh.AllAreas);
                    
                    NavMeshPath safetyPath = new NavMeshPath();
                    NavMesh.CalculatePath(transform.position, hit.position, NavMesh.AllAreas, safetyPath);
                    jumpStart = safetyPath.corners[^1];
            
                    Debug.DrawRay(jumpStart,Vector3.up,Color.red,60f);
                    Debug.DrawRay(jumpEnd,Vector3.up,Color.green,60f);
                    
                    movement.SetTarget(jumpStart,0.5f);
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
                            tvTime -= Time.deltaTime;
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
                movement.SetTarget(jumpStart,0.5f);
                movement.UpdateNavigationBehaviour();
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

                        jumpTimmer += Time.deltaTime;
                        lookVectorOverride = jumpEnd - transform.position;
                        break;
                    }
                    lookVectorOverride = Vector3.zero;
                    
                    float angle = Util.SolveTrajectoryMinAngle(transform.position, jumpEnd);
                    float velocity = Util.SolveVelocityForAngle(angle, transform.position, jumpEnd);
                    velocity += minJumpVelocityAddition;
                    //Debug.Log("Angle: " + angle + ", Velocity: " + velocity);
                    Debug.Log(Util.SolveTrajectory(velocity, transform.position, jumpEnd, angleMode, out angle));

                    helperTransform.position = transform.position;
                    helperTransform.LookAt(jumpEnd);
                    if (angle == float.NaN)
                        angle = 0f;
                    helperTransform.rotation = Quaternion.Euler(angle, helperTransform.eulerAngles.y, 0f);
                    
                    Debug.DrawRay(transform.position, helperTransform.forward, Color.white, 60f);
                    
                    Ragdoll();
                    movement.rb.linearVelocity = helperTransform.forward * (velocity + 1.5f);
                    
                    //AddFaceChangeImpact(1f);
                    ChangeFaceState(1, 500f);
                    SoundManager.active.PlayAtPos(transform.position, "Robot1", parent: transform);
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
                movement.SetTarget(PlayerCamera.active.transform,2f);
                if (movement.HasReachedTarget() == false)
                {
                    state = CompanionState.normal;
                }
                break;
            case CompanionState.shooting:
                if (CheckForTargets(out Health closestHealth))
                {
                    //Stop movement
                    movement.SetTarget(closestHealth.origin,50000000f);
                    
                    helperTransform.position = gunRot.position;
                    helperTransform.LookAt(closestHealth.origin);
                    
                    gunRot.rotation = Quaternion.Lerp(gunRot.rotation, Quaternion.LookRotation(helperTransform.forward), Time.deltaTime * rotationSpeed);

                    float dist = Vector3.Distance(closestHealth.origin.position, transform.position);
                    if (dist <= fleeStartDistance)
                        state = CompanionState.flee;
                    else if(Quaternion.Angle(gunRot.rotation,Quaternion.LookRotation(helperTransform.forward)) < 5f)
                    {
                        if (gun.HasAmmoInClip())
                        {
                            gun.Shoot(false, gunEnd, ShooterType.Sentry);
                        }
                        else
                        {
                            gun.Reload(gun.clipSize, gunEnd);
                        }
                    }
                }
                else
                {
                    state = CompanionState.normal;
                }
                break;
            case CompanionState.flee:
                if (CheckForTargets(out Health closestHealth2))
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

    }
    private void Update()
    {
        UpdateBehaviour();
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
        //Stand back up

        
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
        Vector3 screenLook = movement.GetTargetPosition() - screen.position;

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
        if (bodyLook != Vector3.zero && state != CompanionState.ragdolled)
            transform.rotation = Quaternion.Lerp(transform.rotation,
                Quaternion.LookRotation(new Vector3(bodyLook.x, 0, bodyLook.z)),
                Time.deltaTime * rotationSpeed);
        else if(state != CompanionState.ragdolled)
            transform.rotation = Quaternion.Lerp(transform.rotation,
                Quaternion.Euler(0, transform.eulerAngles.y, 0),
                Time.deltaTime * rotationSpeed);
        //Screen Rotation
        if (screenLook != Vector3.zero)
        {
            screenLook = transform.InverseTransformDirection(screenLook);
            headRotY.localRotation = Quaternion.Lerp(headRotY.localRotation,
                Quaternion.LookRotation(new Vector3(screenLook.x, 0, screenLook.z)),
                Time.deltaTime * rotationSpeed);
            float angle = Vector3.Angle(new Vector3(screenLook.x, 0, screenLook.z), screenLook);
            screen.localRotation = Quaternion.Lerp(Quaternion.Euler(lastScreenRotX,screen.localEulerAngles.y,screen.localEulerAngles.z), Quaternion.Euler(-angle,screen.localEulerAngles.y,screen.localEulerAngles.z), Time.deltaTime * rotationSpeed);
            lastScreenRotX = screen.localEulerAngles.x;
        }
    }
    public bool CheckForTargets(out Health closestHealth)
    {
        closestHealth = null;
        float closestDistance = float.PositiveInfinity;
        for (int i = 0; i < healthTypeFilterList.Count; i++)
        {
            foreach (Health health in EnemyManager.active.healthTypeArray[(int)healthTypeFilterList[i]])
            {
                if (health.health <= 0f)
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
        else if (movement.rb.linearVelocity.magnitude > 1f)
            SoundManager.active.PlayAtPos(transform.position, "Robot0", parent: transform);
    }

    private void OnCollisionExit(Collision collision)
    {
        if ((movement.groundLayer.value & (1 << collision.gameObject.layer)) != 0)
        {
            //Debug.Log("Im Fine!");
            touchingGround = false;
        }
    }
}
