using UnityEngine;
using UnityEngine.UIElements.Experimental;

public class PlayerCamera : MonoBehaviour
{
    public Transform CameraTransform => cameraTransform;

    [Header("References")]
    [SerializeField] private PlayerMovement playerMovement = null;
    [SerializeField] private Transform cameraCentreTransform = null;
    [SerializeField] private Transform cameraTransform = null;

    [Header("Config")]
    [SerializeField] private Vector3 followOffset = new(0, 1, 0);
    [SerializeField] private float followLerpSpeed = 8;
    [SerializeField] private float zoomLerpSpeed = 16;
    [SerializeField] private float zoomStrength = 0.06f;
    [SerializeField] private float zoomMin = 0.5f;
    [SerializeField] private float zoomMax = 2;
    [SerializeField] private float swayEaseScale = 0.5f;
    [SerializeField] private float mouseSwayAmount = 5;
    [SerializeField] private float playerSwayAmount = 0.5f;
    [SerializeField] private float swayLerp = 10;
    [SerializeField] private float swayDeadzone = 0.05f;

    private Quaternion initialCameraRot;
    private Vector3 initialOffset;
    private float zoomAmount = 1.0f;

    private void Start()
    {
        // Set camera position to player position
        cameraCentreTransform.position = playerMovement.transform.position + followOffset;
        initialCameraRot = cameraTransform.rotation;
        initialOffset = cameraTransform.localPosition;
    }

    private void Update()
    {
        UpdateZoom();
        UpdateSway();
    }

    private void UpdateZoom()
    {
        // Handle mouse scroll for zooming
        float scroll = Input.mouseScrollDelta.y;
        zoomAmount = zoomAmount * (1.0f - scroll * zoomStrength);
        zoomAmount = Mathf.Clamp(zoomAmount, zoomMin, zoomMax);
        cameraTransform.localPosition = Vector3.Lerp(cameraTransform.localPosition, initialOffset * zoomAmount, zoomLerpSpeed * Time.deltaTime);
    }

    private void UpdateSway()
    {
        float xSway = 0;
        float ySway = 0;

        // Calculate current mouse deadzoned offset from centre
        float yOffset = Mathf.Clamp01(Input.mousePosition.y / Screen.height) - 0.5f;
        if (Mathf.Abs(yOffset) > swayDeadzone)
            xSway = -Easing.EaseOutQuad(swayEaseScale * (Mathf.Abs(yOffset) - swayDeadzone)) * Mathf.Sign(yOffset) * mouseSwayAmount;
        float xOffset = Mathf.Clamp01(Input.mousePosition.x / Screen.width) - 0.5f;
        if (Mathf.Abs(xOffset) > swayDeadzone)
            ySway = Easing.EaseOutQuad(swayEaseScale * (Mathf.Abs(xOffset) - swayDeadzone)) * Mathf.Sign(xOffset) * mouseSwayAmount;

        // Sway rotation in direction player is moving
        if (playerMovement.IsMoving)
        {
            Vector3 swayDir = Vector3.zero;
            swayDir += playerMovement.InputDir.z * cameraTransform.forward;
            swayDir += playerMovement.InputDir.x * cameraTransform.right;
            Vector3 swayDirLocal = cameraTransform.InverseTransformDirection(swayDir);
            xSway += -swayDirLocal.z * playerSwayAmount;
            ySway += swayDirLocal.x * playerSwayAmount;
        }

        // Lerp camera rotation towards sway rotation
        if (xSway != 0 || ySway != 0)
        {
            Quaternion swayRotation = Quaternion.Euler(xSway, ySway, 0);
            cameraTransform.rotation = Quaternion.Lerp(cameraTransform.rotation, initialCameraRot * swayRotation, swayLerp * Time.deltaTime);
        }

        // Lerp back to initial camera rotation
        else cameraTransform.rotation = Quaternion.Lerp(cameraTransform.rotation, initialCameraRot, swayLerp * Time.deltaTime);
    }

    private void FixedUpdate()
    {
        FixedUpdatePosition();
    }

    private void FixedUpdatePosition()
    {
        // Lerp camera position towards player position
        cameraCentreTransform.position = Vector3.Lerp(cameraCentreTransform.position, playerMovement.transform.position + followOffset, followLerpSpeed * Time.fixedDeltaTime);
    }
}
