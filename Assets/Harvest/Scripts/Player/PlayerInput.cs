using System;
using UnityEngine;
using UnityEngine.InputSystem;

[Serializable]
public class PlayerInput
{
    public static Vector2 PointerPosition => pointAction != null ? pointAction.ReadValue<Vector2>() : Vector2.zero;

    public Vector3 InputMovement { get; private set; } = Vector3.zero;
    public float InputScroll => zoomAction.ReadValue<float>() / SCROLL_PER_NOTCH;
    public Vector2 MouseDelta { get; private set; } = Vector2.zero;
    public bool IsMousePressed = false;
    public bool IsMouseReleased => primaryAction.WasReleasedThisFrame();
    public bool IsInputtingMovement => InputMovement.sqrMagnitude > 0.01f;
    public RaycastHit[] RaycastHits { get; private set; } = new RaycastHit[0];
    public Vector3? HoveredWorldPos { get; private set; } = Vector3.zero;

    public void Init(Player player)
    {
        this.player = player;

        var playerMap = inputActions.FindActionMap("Player", true);
        moveAction = playerMap.FindAction("Move", true);
        lookAction = playerMap.FindAction("Look", true);
        pointAction = playerMap.FindAction("Point", true);
        primaryAction = playerMap.FindAction("Primary", true);
        zoomAction = playerMap.FindAction("Zoom", true);
        playerMap.Enable();
    }

    public void ReceiveInput()
    {
        // Update mouse inputs
        IsMousePressed = primaryAction.WasPressedThisFrame();
        MouseDelta = lookAction.ReadValue<Vector2>() * MOUSE_DELTA_SCALE;

        // Update movement input based on keyboard input on a flat plane
        Vector2 moveInput = moveAction.ReadValue<Vector2>();
        Vector3 flatForward = Vector3.ProjectOnPlane(player.Camera.Camera.transform.forward, Vector3.up).normalized;
        InputMovement = Vector3.zero;
        InputMovement += moveInput.x * player.Camera.Camera.transform.right;
        InputMovement += moveInput.y * flatForward;
        InputMovement = InputMovement.normalized;

        // Raycast with the current mouse position
        float maxDistance = 10f;
        Ray ray = player.Camera.Camera.ScreenPointToRay(PointerPosition);
        RaycastHits = Physics.RaycastAll(ray, maxDistance);
        if (RaycastHits.Length > 0) HoveredWorldPos = RaycastHits[0].point;
        else HoveredWorldPos = null;
    }

    public void HideMouse()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void ShowMouse()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    [Header("Config")]
    [SerializeField] private InputActionAsset inputActions;

    private static InputAction pointAction;
    private Player player;
    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction primaryAction;
    private InputAction zoomAction;

    private static readonly float MOUSE_DELTA_SCALE = 0.1f;
    private static readonly float SCROLL_PER_NOTCH = 120f;
}
