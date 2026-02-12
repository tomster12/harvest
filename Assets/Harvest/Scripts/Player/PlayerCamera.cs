using System;
using UnityEngine;

[Serializable]
public class PlayerCamera
{
    public Camera Camera => Camera.main;

    public void Init(Player player)
    {
        this.player = player;

        // Initialize camera state
        camBaseRot = Quaternion.Euler(camBaseRotEuler);
        camZoom = camZoomBase;
        UpdateCamera(true);
        FixedFollowPlayer(true);
    }

    public void UpdateCamera(bool force = false)
    {
        float xSway = 0;
        float ySway = 0;

        // Update camera zoom with mouse wheel
        camZoom *= 1.0f - player.Input.InputScroll * camZoomStrength;
        camZoom = Mathf.Clamp(camZoom, camZoomMin, camZoomMax);

        // Sway with the mouse
        float yOffset = Mathf.Clamp01(Input.mousePosition.y / Screen.height) - 0.5f;
        if (Mathf.Abs(yOffset) > camSwayDeadzone)
            xSway = -Easing.EaseOutQuad(camSwayEaseScale * (Mathf.Abs(yOffset) - camSwayDeadzone)) * Mathf.Sign(yOffset) * camSwayMouseAmount;
        float xOffset = Mathf.Clamp01(Input.mousePosition.x / Screen.width) - 0.5f;
        if (Mathf.Abs(xOffset) > camSwayDeadzone)
            ySway = Easing.EaseOutQuad(camSwayEaseScale * (Mathf.Abs(xOffset) - camSwayDeadzone)) * Mathf.Sign(xOffset) * camSwayMouseAmount;

        // Sway with player movement
        if (player.Input.IsInputtingMovement)
        {
            Vector3 swayDir = Vector3.zero;
            swayDir += player.Input.InputMovement.z * Camera.transform.forward;
            swayDir += player.Input.InputMovement.x * Camera.transform.right;
            Vector3 swayDirLocal = Camera.transform.InverseTransformDirection(swayDir);
            xSway += -swayDirLocal.z * camSwayPlayerAmount;
            ySway += swayDirLocal.x * camSwayPlayerAmount;
        }

        // Rotate the camera based on the sway
        Quaternion targetRotation = camBaseRot * Quaternion.Euler(xSway, ySway, 0);
        if (!force) Camera.transform.rotation = Quaternion.Lerp(Camera.transform.rotation, targetRotation, camSwayLerp * Time.deltaTime);
        else Camera.transform.rotation = targetRotation;
    }

    public void FixedFollowPlayer(bool force = false)
    {
        // Move camera based on player position and zoom
        Vector3 centrePosition = player.transform.position + camCentreOffset;
        Vector3 targetPosition = centrePosition + camOrbitDir.normalized * camZoom;
        if (!force) Camera.transform.position = Vector3.Lerp(Camera.transform.position, targetPosition, camFollowLerp * Time.deltaTime);
        else Camera.transform.position = targetPosition;
    }

    [Header("Camera Config")]
    [SerializeField] private Vector3 camCentreOffset = new(0, 1, 0);
    [SerializeField] private Vector3 camOrbitDir = new(0, 4.0f, -5.4f);
    [SerializeField] private Vector3 camBaseRotEuler = new(30, 0, 0);
    [SerializeField] private float camFollowLerp = 8;

    [Header("Camera Zoom Config")]
    [SerializeField] private float camZoomStrength = 0.1f;
    [SerializeField] private float camZoomMin = 2f;
    [SerializeField] private float camZoomMax = 15f;
    [SerializeField] private float camZoomBase = 6;

    [Header("Camera Sway Config")]
    [SerializeField] private float camSwayEaseScale = 0.5f;
    [SerializeField] private float camSwayMouseAmount = 5;
    [SerializeField] private float camSwayPlayerAmount = 0.5f;
    [SerializeField] private float camSwayLerp = 10;
    [SerializeField] private float camSwayDeadzone = 0.05f;

    private Player player;
    private Quaternion camBaseRot = Quaternion.identity;
    private float camZoom = 1.0f;
}
