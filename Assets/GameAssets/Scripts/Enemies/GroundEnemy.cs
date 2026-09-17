using System;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

[RequireComponent(typeof(PathFindingGroundMovement))]
public class GroundEnemy : Enemy
{
    [NonSerialized]
    public PathFindingGroundMovement movement;
    
    //Animation
    [Header("Animation")]
    public float walkCycleSpeedMult;
    public float rotationSpeed;
    
    private float timeStandingStill;
    private float minMovementVelcoity = 0.1f;

    public float GetTimeStandingStill()
    {
        return timeStandingStill;
    }
    public void ResetTimeStandingStill()
    {
        timeStandingStill = 0f;
    }
    public override void Awake()
    {
        movement = GetComponent<PathFindingGroundMovement>();
    }

    public override void Start()
    {
        base.Start();
    }

    public override void FixedUpdateCall()
    {
        movement.FixedUpdateMovement();
    }
    public override void UpdateCall()
    {
        //movement.UpdateNavigationBehaviour();
        movement.UpdateNavMovement();
        Animate();

        if (movement.rb.linearVelocity.magnitude <= minMovementVelcoity)
            timeStandingStill += Time.deltaTime;
        else
            timeStandingStill = 0;
    }
    public override void UpdateBehaviour()
    {
        movement.UpdateNavigationBehaviour();
        base.UpdateBehaviour();
    }
    public override void Anger(Transform target, float intrestTime)
    {
        base.Anger(target, intrestTime);
        movement.SetTarget(target, 1f);
    }
    public override bool DoIWantToAttack()
    {
        return movement.distanceFromTarget <= 5f;
    }

    public override bool IsInPack()
    {
        return movement.IsInPack();
    }

    public override void Freeze()
    {
        movement.Freeze();
    }

    public override void UnFreeze()
    {
        movement.UnFreeze();
    }

    private void Animate()
    {
        animator.SetFloat("Velocity", movement.velocity);
        animator.SetFloat("SpeedMult", movement.velocity * walkCycleSpeedMult);
        if (movement.targetDirVector != Vector3.zero)
            movement.rb.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(new Vector3(movement.targetDirVector.x,0, movement.targetDirVector.z)), Time.deltaTime * rotationSpeed);
    }
}
