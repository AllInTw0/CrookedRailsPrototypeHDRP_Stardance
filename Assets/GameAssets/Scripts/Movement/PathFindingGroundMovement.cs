using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class PathFindingGroundMovement : GroundMovement
{
    //Navigation defining variables
    public enum NavigationType
    {
        StraightLine,
        Path
    }
    [HideInInspector] public NavigationType currentNavigationType;
    public bool IsNavigatingByPath(){return currentNavigationType == NavigationType.Path;}
    public enum TargetType
    {
        Position,
        Transform,
        Pack
    }
    [HideInInspector] public TargetType currentTargetType;
    private Vector3 targetPosition;
    private Transform targetTransform;
    public Transform GetTargetTransform(){return targetTransform;}
    [HideInInspector] public EnemyPack targetPack;
    private float targetDistance = 5f;
    
    //Nav Path
    [NonSerialized]
    public NavMeshPath navPath;
    private int pathIndex = 0;

    [NonSerialized]
    public Vector3 targetDirVector;
    [NonSerialized]
    public float distanceFromTarget;
    
    //Other
    private Vector3 previousPosition;
    private float distanceFromPreviousPosition;
    private float timeSinceTargetChange;
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        navPath = new NavMeshPath();
    }

    private void FixedUpdate()
    {
        
    }
    private void Update()
    {
        
    }
    public bool UpdateNavigationBehaviour(bool preferPath = false, bool checkForPartialPaths = false)
    {
        //Make sure nav is off if we are near the target
        //if (IsNavigatingByPath() && distanceFromTarget < 1f)
        //{
        //    currentNavigationType = NavigationType.StraightLine;
        //}
        //Turn on nav if we have stopped
        if (IsNavigatingByPath() == false && (IsStuck() || preferPath))
        {
            currentNavigationType = NavigationType.Path;
        }

        //Calculate Nav Path
        if (IsNavigatingByPath() && NavMesh.SamplePosition(transform.position,out NavMeshHit hitStart, 10f, NavMesh.AllAreas) && NavMesh.SamplePosition(GetTargetPosition(), out NavMeshHit hitEnd, 10f, NavMesh.AllAreas))
        {
            NavMesh.CalculatePath(hitStart.position, hitEnd.position, 1 << NavMesh.GetAreaFromName("Walkable"), navPath);
            pathIndex = 1;
        }
        
        if (IsNavigatingByPath() && (checkForPartialPaths == false || navPath.status != NavMeshPathStatus.PathPartial)) 
        {
            if (navPath.corners.Length > 2) // (If the path is equal or less than 2 that means it's just a straight line)
            {
                //Update the target nav point
                if (navPath.corners.Length > pathIndex + 1 && Vector3.Distance(transform.position, navPath.corners[pathIndex]) < 0.2f)
                    pathIndex++;
            }
            else
            {
                //Stop navigating by path
                currentNavigationType = NavigationType.StraightLine;
            }
        }
        
        distanceFromPreviousPosition = Vector3.Distance(previousPosition, transform.position);
        previousPosition = transform.position;
        
        return navPath.status == NavMeshPathStatus.PathPartial;
    }
    
    //Movement
    public override Vector2 GetMovementVector()
    {
        return new Vector2(targetDirVector.x, targetDirVector.z).normalized;
    }

    public override float GetTargetSpeed()
    {
        //Check if at target
        CalculateDistanceFromTarget();
        if (HasReachedTarget())
            return 0f; 
        
        //Check if at last path point
        if (IsNavigatingByPath() && Vector3.Distance(transform.position, navPath.corners[^1]) < 0.1f)
            return  0f;
        
        return baseSpeed;
    }

    public void UpdateNavMovement()
    {
        timeSinceTargetChange += Time.deltaTime;
        
        //Get target Direction depending on navigation type
        if (IsNavigatingByPath() && navPath.corners.Length > pathIndex)
            targetDirVector = navPath.corners[pathIndex] - transform.position;
        else
            targetDirVector = GetTargetPosition() - transform.position;
        
        //2D and Normalize and Project
        targetDirVector = new Vector3(targetDirVector.x, 0, targetDirVector.z).normalized;
        
        UpdateMovement();
    }
    
    public void CalculateDistanceFromTarget()
    {
        distanceFromTarget = Vector3.Distance(transform.position, GetTargetPosition());
    }

    public bool HasReachedTarget()
    {
        CalculateDistanceFromTarget();
        if(distanceFromTarget <= targetDistance)
            return true;
        return false;
    }

    public bool IsStuck()
    {
        return distanceFromPreviousPosition < 0.5f && timeSinceTargetChange > 0.5f;
    }
    
    public Vector3 GetTargetPosition()
    {
        switch (currentTargetType){
            default:
            case TargetType.Position:
                return targetPosition;
            case TargetType.Transform:
                if (targetTransform == null)
                {
                    SetTarget(transform.position + transform.forward);
                    return GetTargetPosition();
                }
                return targetTransform.position;
            case TargetType.Pack:
                return targetPack.centerPos + targetPosition; // + offset
        }
    }
    public bool IsInPack()
    {
        return currentTargetType == TargetType.Pack;
    }
    public void SetTarget(Vector3 pos, float targetDistance = 1f)
    {
        if (IsInPack()) LeavePack();

        timeSinceTargetChange = 0f;
        currentTargetType = TargetType.Position;
        targetPosition = pos;
        //currentNavigationType = NavigationType.StraightLine;
        this.targetDistance = targetDistance;
    }
    public void SetTarget(Transform transform, float targetDistance = 1f)
    {
        if (IsInPack()) LeavePack();

        if (currentTargetType != TargetType.Transform || transform != targetTransform)
            timeSinceTargetChange = 0f;
        
        currentTargetType = TargetType.Transform;
        targetTransform = transform;
        //currentNavigationType = NavigationType.StraightLine;
        this.targetDistance = targetDistance;
    }
    public void SetTarget(EnemyPack pack, Vector3 offset, float targetDistance = 1f)
    {
        if (IsInPack()) LeavePack();
        pack.AddEnemy(GetComponent<Enemy>());

        timeSinceTargetChange = 0f;
        currentTargetType = TargetType.Pack;
        targetPack = pack;
        targetPosition = offset;
        //currentNavigationType = NavigationType.StraightLine;
        this.targetDistance = targetDistance;
    }
    public void LeavePack()
    {
        targetPack.RemoveEnemy(GetComponent<Enemy>());
        targetPack = null;
    }
    
    private void OnDrawGizmos()
    {
        Gizmos.color = IsNavigatingByPath() ? Color.green : Color.red;
        Gizmos.DrawLine(transform.position, GetTargetPosition());
        if (IsNavigatingByPath() && navPath.corners.Length > pathIndex)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, navPath.corners[pathIndex]);
            for (int i = pathIndex+1; i < navPath.corners.Length ; i++)
            {
                Gizmos.DrawLine(navPath.corners[i-1], navPath.corners[i]);
            }
            
        }
    }
}
