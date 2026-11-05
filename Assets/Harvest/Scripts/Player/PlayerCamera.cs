using System;
using UnityEngine;

[Serializable]
public class PlayerCamera
{
    public Camera Camera => Camera.main;
    public float Zoom => camZoom;
    public Quaternion AimRotation => Quaternion.Euler(0, camYaw, 0) * camRotBase;
    public Vector3 FlatForward => Quaternion.Euler(0, camYaw, 0) * Vector3.forward;
    public Vector3 FlatRight => Quaternion.Euler(0, camYaw, 0) * Vector3.right;

    public void Init(Player player)
    {
        this.player = player;

        // Initialize camera state
        camRotBase = Quaternion.Euler(camRotEulerBase);
        camYaw = camYawBase;
        camYawVelocity = 0f;
        camZoom = camZoomBase;
        camZoomTarget = camZoomBase;
        camCentre = GetTargetCentre();
        UpdateCamera(true);
    }

    public void SetFocus(CameraFocus newFocus)
    {
        focus = newFocus;
        focusDrivesYaw = false;
        if (!focus.ViewDir.HasValue) return;

        Vector3 viewDir = focus.ViewDir.Value;
        viewDir.y = 0f;
        if (viewDir.sqrMagnitude < 0.001f) return;
        focusDrivesYaw = true;

        // Some focuses pick the side nearest the current yaw so the swing stays small
        float baseYaw = Mathf.Atan2(viewDir.x, viewDir.z) * Mathf.Rad2Deg;
        float yawPlus = baseYaw + focus.YawOffset;
        float yawMinus = baseYaw - focus.YawOffset;
        bool plusIsNearer = Mathf.Abs(Mathf.DeltaAngle(camYaw, yawPlus)) <= Mathf.Abs(Mathf.DeltaAngle(camYaw, yawMinus));
        focusYaw = !focus.PicksNearestSide || plusIsNearer ? yawPlus : yawMinus;
    }

    public void ClearFocus()
    {
        focus = null;
        focusDrivesYaw = false;
    }

    public void UpdateCamera(bool force = false)
    {
        UpdateYaw(force);

        // Update camera zoom with mouse wheel
        camZoomTarget *= 1.0f - player.Input.InputScroll * camZoomStep;
        camZoomTarget = Mathf.Clamp(camZoomTarget, camZoomMin, camZoomMax);
        camZoom = force ? camZoomTarget : Mathf.Lerp(camZoom, camZoomTarget, camZoomLerp * Time.deltaTime);

        // Only a focus moves the view, so the world never slides under the cursor
        Vector2 targetRotOffset = focus?.RotationOffset ?? Vector2.zero;
        camRotOffset = force ? targetRotOffset : Vector2.Lerp(camRotOffset, targetRotOffset, camRotOffsetLerp * Time.deltaTime);

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
        Camera.transform.rotation = yawRotation * camRotBase * Quaternion.Euler(camRotOffset.x, camRotOffset.y, 0);
    }

    [Header("Camera Config")]
    [SerializeField] private Vector3 camCentreOffset = new(0, 1, 0);
    [SerializeField] private Vector3 camOrbitDir = new(0, 4.0f, -5.4f);
    [SerializeField] private Vector3 camRotEulerBase = new(30, 0, 0);
    [SerializeField] private float camYawBase = 45f;
    [SerializeField] private float camFollowLerp = 8;
    [SerializeField] private float camFollowLerpVertical = 3;
    [SerializeField] private float camRotOffsetLerp = 10;

    [Header("Camera Zoom Config")]
    [SerializeField] private float camZoomStep = 0.12f;
    [SerializeField] private float camZoomLerp = 18f;
    [SerializeField] private float camZoomMin = 2f;
    [SerializeField] private float camZoomMax = 15f;
    [SerializeField] private float camZoomBase = 6;

    [Header("Camera Orbit Config")]
    [SerializeField] private float camOrbitSensitivity = 3f;

    [Header("Camera Focus Config")]
    [SerializeField] private float camFocusYawSmoothTime = 0.45f;
    [SerializeField] private float camFocusYawMaxSpeed = 180f;

    private Player player;
    private CameraFocus focus;
    private Quaternion camRotBase = Quaternion.identity;
    private float camZoom = 1.0f;
    private float camZoomTarget = 1.0f;
    private float camYaw = 0.0f;
    private float camYawVelocity = 0.0f;
    private Vector3 camCentre = Vector3.zero;
    private Vector2 camRotOffset = Vector2.zero;
    private float focusYaw = 0.0f;
    private bool focusDrivesYaw = false;

    private Vector3 GetTargetCentre()
    {
        Vector3 playerCentre = player.transform.position + camCentreOffset;
        if (focus == null) return playerCentre;
        return Vector3.Lerp(playerCentre, focus.Point, focus.CentreBlend);
    }

    private void UpdateYaw(bool force)
    {
        // Manual orbit by dragging the middle mouse button
        float orbitDelta = player.Input.IsOrbiting ? player.Input.MouseDelta.x * camOrbitSensitivity : 0f;

        if (focusDrivesYaw)
        {
            // The yaw is left where it ends up once the focus is cleared
            if (focus.AllowsOrbit)
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
