using System;
using UnityEngine;
using UnityEngine.InputSystem;
public class InputManager : MonoBehaviour
{
    public static InputManager active;
    //New Input Package
    private InputAction moveAction;
    [NonSerialized]
    public static InputAction jumpAction,sprintAction,crouchAction,interactAction,scrollAction,dropAction,attackAction,attack2Action,reloadAction,debugCamAction,confirmAction, altAction, moneyAction, swapToolAction, escapeAction;
    
    private Vector2 _moveInput;
    public Vector2 moveInput {
        get
        {
            //This is here so .ReadValue doesn't get called more than once each frame
            if (nextFrameMoveInput)
            {
                _moveInput = moveAction.ReadValue<Vector2>();
                nextFrameMoveInput = false;
            }

            return _moveInput;
        }
    }
    private bool nextFrameMoveInput = false;
    private void Start()
    {
        active = this;

        moveAction = InputSystem.actions.FindAction("Move");
        jumpAction = InputSystem.actions.FindAction("Jump");
        sprintAction = InputSystem.actions.FindAction("Sprint");
        crouchAction = InputSystem.actions.FindAction("Crouch");
        interactAction = InputSystem.actions.FindAction("Interact");
        scrollAction = InputSystem.actions.FindAction("Scroll");
        dropAction = InputSystem.actions.FindAction("Drop");
        attackAction = InputSystem.actions.FindAction("Attack");
        attack2Action = InputSystem.actions.FindAction("Attack2");
        reloadAction = InputSystem.actions.FindAction("Reload");
        debugCamAction = InputSystem.actions.FindAction("DebugCam");
        confirmAction = InputSystem.actions.FindAction("Confirm");
        altAction = InputSystem.actions.FindAction("Alt");
        moneyAction = InputSystem.actions.FindAction("Money");
        swapToolAction = InputSystem.actions.FindAction("SwapTool");
        escapeAction = InputSystem.actions.FindAction("Escape");
    }
    private void LateUpdate()
    {
        nextFrameMoveInput = true;
    }
}
