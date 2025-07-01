using System;
using UnityEngine;

[Serializable]
public class PlayerInput
{
    public Vector3 InputMovement { get; private set; } = Vector3.zero;
    public float InputScroll => Input.mouseScrollDelta.y;
    public bool IsMousePressed = false;
    public bool IsInputtingMovement => InputMovement.sqrMagnitude > 0.01f;

    public void Init(Player player)
    {
        this.player = player;
    }

    public void ReceiveInput()
    {
        // Update mouse button inputs
        IsMousePressed = Input.GetMouseButtonDown(0);

        // Update movement input based on keyboard input on a flat plane
        Vector3 flatForward = Vector3.ProjectOnPlane(player.Camera.Camera.transform.forward, Vector3.up).normalized;
        InputMovement = Vector3.zero;
        InputMovement += Input.GetAxisRaw("Horizontal") * player.Camera.Camera.transform.right;
        InputMovement += Input.GetAxisRaw("Vertical") * flatForward;
        InputMovement = InputMovement.normalized;
    }

    private Player player;
}
