using UnityEngine;

public class AnimationPlayer : MonoBehaviour
{
    [SerializeField]
    public Animator animator;
    [SerializeField]
    public string defaultAnimName = "startPose1";
    [SerializeField]
    public string animName = "move1";
    [SerializeField]
    public float speed = 1f;
    private void Start()
    {
        PlayAniamtion(defaultAnimName, 1f);
    }
    public void PlayAniamtion(string name, float speed)
    {
        if (speed >= 0)
            animator.Play(name, 0, 0f);
        else
            animator.Play(name, 0, 1f);

        animator.SetFloat("speed", speed);
    }
}
