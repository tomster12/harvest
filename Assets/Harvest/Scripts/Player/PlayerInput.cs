using System;
using UnityEngine;

[Serializable]
public class PlayerInput
{
    public Vector3 InputMovement { get; private set; } = Vector3.zero;
    public float InputScroll => Input.mouseScrollDelta.y;
    public bool IsMousePressed = false;
    public bool IsMouseReleased => Input.GetMouseButtonUp(0);
    public bool IsInputtingMovement => InputMovement.sqrMagnitude > 0.01f;
    public RaycastHit[] RaycastHits { get; private set; } = new RaycastHit[0];
    public Vector3? HoveredWorldPoint { get; private set; } = Vector3.zero;

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

        // Raycast with the current mouse position
        float maxDistance = 10f;
        Ray ray = player.Camera.Camera.ScreenPointToRay(Input.mousePosition);
        RaycastHits = Physics.RaycastAll(ray, maxDistance);
        if (RaycastHits.Length > 0) HoveredWorldPoint = RaycastHits[0].point;
        else HoveredWorldPoint = null;
    }

    private Player player;
}
