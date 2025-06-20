using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
    public Transform Camera => UnityEngine.Camera.main.transform;

    [Header("References")]
    [SerializeField] private PlayerController playerMovement = null;

    [Header("Config")]
    [SerializeField] private Vector3 playerOffset = new(0, 1, 0);
    [SerializeField] private Vector3 cameraOffset = new(0, 4.0f, -5.4f);
    [SerializeField] private Vector3 baseRotationEuler = new(30, 0, 0);
    [SerializeField] private float followLerpSpeed = 8;

    [Header("Zoom Config")]
    [SerializeField] private float zoomLerpSpeed = 16;
    [SerializeField] private float zoomStrength = 0.06f;
    [SerializeField] private float zoomMin = 0.5f;
    [SerializeField] private float zoomMax = 2;
    [SerializeField] private float baseZoom = 2;

    [Header("Sway Config")]
    [SerializeField] private float swayEaseScale = 0.5f;
    [SerializeField] private float mouseSwayAmount = 5;
    [SerializeField] private float playerSwayAmount = 0.5f;
    [SerializeField] private float swayLerp = 10;
    [SerializeField] private float swayDeadzone = 0.05f;

    private Quaternion baseRotation;
    private float zoomAmount;

    private void Awake()
    {
        // Initialize camer state
        baseRotation = Quaternion.Euler(baseRotationEuler);
        zoomAmount = baseZoom;
        UpdateCameraPosition(true);
        UpdateRotation(true);
    }

    private void Update()
    {
        HandleInput();
        UpdateRotation();
    }

    private void HandleInput()
    {
        // Zoom with mouse wheel
        float scroll = Input.mouseScrollDelta.y;
        zoomAmount = zoomAmount * (1.0f - scroll * zoomStrength);
        zoomAmount = Mathf.Clamp(zoomAmount, zoomMin, zoomMax);
    }

    private void UpdateRotation(bool force = false)
    {
        float xSway = 0;
        float ySway = 0;

        // Sway with the mouse
        float yOffset = Mathf.Clamp01(Input.mousePosition.y / Screen.height) - 0.5f;
        if (Mathf.Abs(yOffset) > swayDeadzone)
            xSway = -Easing.EaseOutQuad(swayEaseScale * (Mathf.Abs(yOffset) - swayDeadzone)) * Mathf.Sign(yOffset) * mouseSwayAmount;
        float xOffset = Mathf.Clamp01(Input.mousePosition.x / Screen.width) - 0.5f;
        if (Mathf.Abs(xOffset) > swayDeadzone)
            ySway = Easing.EaseOutQuad(swayEaseScale * (Mathf.Abs(xOffset) - swayDeadzone)) * Mathf.Sign(xOffset) * mouseSwayAmount;

        // Sway with player movement
        if (playerMovement.IsMoving)
        {
            Vector3 swayDir = Vector3.zero;
            swayDir += playerMovement.InputDir.z * Camera.forward;
            swayDir += playerMovement.InputDir.x * Camera.right;
            Vector3 swayDirLocal = Camera.InverseTransformDirection(swayDir);
            xSway += -swayDirLocal.z * playerSwayAmount;
            ySway += swayDirLocal.x * playerSwayAmount;
        }

        // Rotate the camera based on the sway
        Quaternion targetRotation = baseRotation * Quaternion.Euler(xSway, ySway, 0);
        if (!force) Camera.rotation = Quaternion.Lerp(Camera.rotation, targetRotation, swayLerp * Time.deltaTime);
        else Camera.rotation = targetRotation;
    }

    private void FixedUpdate()
    {
        UpdateCameraPosition();
    }

    private void UpdateCameraPosition(bool force = false)
    {
        // Move camera based on player position and zoom
        Vector3 centrePosition = playerMovement.transform.position + playerOffset;
        Vector3 targetPosition = centrePosition + cameraOffset.normalized * zoomAmount;
        if (!force) Camera.position = Vector3.Lerp(Camera.position, targetPosition, followLerpSpeed * Time.deltaTime);
        else Camera.position = targetPosition;
    }
}
