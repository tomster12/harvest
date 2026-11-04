using System;
using UnityEngine;
using UnityEngine.InputSystem;

[Serializable]
public class PlayerInput
{
    public static Vector2 PointerPosition => pointAction != null ? pointAction.ReadValue<Vector2>() : Vector2.zero;

    public Vector3 InputMovement { get; private set; } = Vector3.zero;
    public float InputScroll => Mathf.Abs(zoomAction.ReadValue<float>()) > 0.001f ? Mathf.Sign(zoomAction.ReadValue<float>()) : 0f;
    public Vector2 MouseDelta { get; private set; } = Vector2.zero;
    public bool IsMousePressed = false;
    public bool IsMouseReleased => primaryAction.WasReleasedThisFrame();
    public bool IsOrbiting => orbitAction.IsPressed();
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
        orbitAction = playerMap.FindAction("Orbit", true);
        playerMap.Enable();
    }

    public void ReceiveInput()
    {
        // Update mouse inputs
        IsMousePressed = primaryAction.WasPressedThisFrame();
        MouseDelta = lookAction.ReadValue<Vector2>() * MOUSE_DELTA_SCALE;

        // Update movement input relative to the camera yaw, ignoring sway
        Vector2 moveInput = moveAction.ReadValue<Vector2>();
        InputMovement = Vector3.zero;
        InputMovement += moveInput.x * player.Camera.FlatRight;
        InputMovement += moveInput.y * player.Camera.FlatForward;
        InputMovement = InputMovement.normalized;

        // Raycast with the current mouse position, reaching past the player at any zoom
        float maxDistance = player.Camera.Zoom + RAYCAST_RANGE_BEYOND_PLAYER;
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
    private InputAction orbitAction;

    private static readonly float MOUSE_DELTA_SCALE = 0.1f;
    private static readonly float RAYCAST_RANGE_BEYOND_PLAYER = 10f;
}
