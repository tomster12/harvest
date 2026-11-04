using System;
using UnityEngine;

[Serializable]
public class PlayerCamera
{
    public Camera Camera => Camera.main;
    public float Zoom => camZoom;
    public Quaternion AimRotation => Quaternion.Euler(0, camYaw, 0) * camBaseRot;
    public Vector3 FlatForward => Quaternion.Euler(0, camYaw, 0) * Vector3.forward;
    public Vector3 FlatRight => Quaternion.Euler(0, camYaw, 0) * Vector3.right;

    public void Init(Player player)
    {
        this.player = player;

        // Initialize camera state
        camBaseRot = Quaternion.Euler(camBaseRotEuler);
        camYaw = camStartYaw;
        camYawVelocity = 0f;
        camZoom = camZoomBase;
        camZoomTarget = camZoomBase;
        camCentre = GetTargetCentre();
        UpdateCamera(true);
    }

    public void SetFocus(Vector3 point, Vector3 normal)
    {
        SetFocusFraming(point, -normal, camFocusYawOffset, camFocusBlend, false);
    }

    public void SetDragFocus(Vector3 point, Vector3 viewDir)
    {
        SetFocusFraming(point, viewDir, camDragYawOffset, camDragFocusBlend, true);
    }

    public void UpdateFocusPoint(Vector3 point)
    {
        if (focusPoint.HasValue) focusPoint = point;
    }

    public void SetFocusSway(float swayPct)
    {
        focusSway = swayPct;
    }

    public void ClearFocus()
    {
        focusPoint = null;
        focusDrivesYaw = false;
        focusSway = 0f;
    }

    public void UpdateCamera(bool force = false)
    {
        UpdateYaw(force);

        // Update camera zoom with mouse wheel
        camZoomTarget *= 1.0f - player.Input.InputScroll * camZoomStep;
        camZoomTarget = Mathf.Clamp(camZoomTarget, camZoomMin, camZoomMax);
        camZoom = force ? camZoomTarget : Mathf.Lerp(camZoom, camZoomTarget, camZoomLerp * Time.deltaTime);

        // Only the chop accuracy moves the view, so the world never slides under the cursor
        float focusSwayAmount = focusSway * camFocusSwayAmount;
        float focusNoiseTime = Time.time * camFocusSwayFrequency;
        Vector2 targetSway = new(
            (Mathf.PerlinNoise(focusNoiseTime, 0.5f) - 0.5f) * 2f * focusSwayAmount,
            (Mathf.PerlinNoise(focusNoiseTime, 10.5f) - 0.5f) * 2f * focusSwayAmount
        );
        camSway = force ? targetSway : Vector2.Lerp(camSway, targetSway, camSwayLerp * Time.deltaTime);

        // Follow the player or focus point and orbit around it
        Vector3 targetCentre = GetTargetCentre();
        if (force) camCentre = targetCentre;
        else
        {
            float followBlend = 1f - Mathf.Exp(-camFollowLerp * Time.deltaTime);
            float followBlendVertical = 1f - Mathf.Exp(-camFollowLerpVertical * Time.deltaTime);
            camCentre.x = Mathf.Lerp(camCentre.x, targetCentre.x, followBlend);
            camCentre.z = Mathf.Lerp(camCentre.z, targetCentre.z, followBlend);
            camCentre.y = Mathf.Lerp(camCentre.y, targetCentre.y, followBlendVertical);
        }

        Quaternion yawRotation = Quaternion.Euler(0, camYaw, 0);
        Camera.transform.position = camCentre + yawRotation * camOrbitDir.normalized * camZoom;
        Camera.transform.rotation = yawRotation * camBaseRot * Quaternion.Euler(camSway.x, camSway.y, 0);
    }

    [Header("Camera Config")]
    [SerializeField] private Vector3 camCentreOffset = new(0, 1, 0);
    [SerializeField] private Vector3 camOrbitDir = new(0, 4.0f, -5.4f);
    [SerializeField] private Vector3 camBaseRotEuler = new(30, 0, 0);
    [SerializeField] private float camStartYaw = 45f;
    [SerializeField] private float camFollowLerp = 8;
    [SerializeField] private float camFollowLerpVertical = 3;

    [Header("Camera Zoom Config")]
    [SerializeField] private float camZoomStep = 0.12f;
    [SerializeField] private float camZoomLerp = 18f;
    [SerializeField] private float camZoomMin = 2f;
    [SerializeField] private float camZoomMax = 15f;
    [SerializeField] private float camZoomBase = 6;

    [Header("Camera Orbit Config")]
    [SerializeField] private float camOrbitSensitivity = 3f;

    [Header("Camera Focus Config")]
    [SerializeField] private float camFocusYawOffset = 50f;
    [SerializeField] private float camFocusYawSmoothTime = 0.45f;
    [SerializeField] private float camFocusYawMaxSpeed = 180f;
    [SerializeField] private float camFocusBlend = 0.6f;
    [SerializeField] private float camFocusSwayAmount = 0.8f;
    [SerializeField] private float camFocusSwayFrequency = 1.5f;

    [Header("Camera Drag Focus Config")]
    [SerializeField] private float camDragYawOffset = 40f;
    [SerializeField] private float camDragFocusBlend = 0.3f;

    [Header("Camera Sway Config")]
    [SerializeField] private float camSwayLerp = 10;

    private Player player;
    private Quaternion camBaseRot = Quaternion.identity;
    private float camZoom = 1.0f;
    private float camZoomTarget = 1.0f;
    private float camYaw = 0.0f;
    private float camYawVelocity = 0.0f;
    private Vector3 camCentre = Vector3.zero;
    private Vector2 camSway = Vector2.zero;
    private Vector3? focusPoint;
    private float focusSway = 0.0f;
    private float focusBlend = 0.0f;
    private float focusYaw = 0.0f;
    private bool focusDrivesYaw = false;
    private bool focusFlexible = false;

    private Vector3 GetTargetCentre()
    {
        Vector3 playerCentre = player.transform.position + camCentreOffset;
        if (!focusPoint.HasValue) return playerCentre;
        return Vector3.Lerp(playerCentre, focusPoint.Value, focusBlend);
    }

    private void SetFocusFraming(Vector3 point, Vector3 viewDir, float yawOffset, float blend, bool flexible)
    {
        focusPoint = point;
        focusBlend = blend;
        focusFlexible = flexible;

        viewDir.y = 0f;
        focusDrivesYaw = viewDir.sqrMagnitude >= 0.001f;
        if (!focusDrivesYaw) return;

        // Flexible framings pick the side nearest the current yaw so the swing stays small
        float baseYaw = Mathf.Atan2(viewDir.x, viewDir.z) * Mathf.Rad2Deg;
        float yawPlus = baseYaw + yawOffset;
        float yawMinus = baseYaw - yawOffset;
        bool plusIsNearer = Mathf.Abs(Mathf.DeltaAngle(camYaw, yawPlus)) <= Mathf.Abs(Mathf.DeltaAngle(camYaw, yawMinus));
        focusYaw = !flexible || plusIsNearer ? yawPlus : yawMinus;
    }

    private void UpdateYaw(bool force)
    {
        // Manual orbit by dragging the middle mouse button
        float orbitDelta = player.Input.IsOrbiting ? player.Input.MouseDelta.x * camOrbitSensitivity : 0f;

        if (focusDrivesYaw)
        {
            // The yaw is left where it ends up once the focus is cleared
            if (focusFlexible)
            {
                focusYaw += orbitDelta;
                camYaw += orbitDelta;
            }

            if (force) camYaw = focusYaw;
            else camYaw = Mathf.SmoothDampAngle(camYaw, focusYaw, ref camYawVelocity, camFocusYawSmoothTime, camFocusYawMaxSpeed);
            return;
        }

        camYawVelocity = 0f;
        camYaw += orbitDelta;
    }
}
