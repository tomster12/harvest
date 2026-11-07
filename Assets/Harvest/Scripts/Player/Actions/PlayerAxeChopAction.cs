using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Mathematics;
using System;

[Serializable]
public class PlayerAxeChopAction : PlayerAction
{
    public override bool IsAvailable =>
        player.Animator.CanControl(ANIMATION_PRIORITY);

    public override bool IsRunnable =>
        chopTarget != null;

    public PlayerAxeChopAction(Player player, GameObject axeMesh, CustomTagRegistry taggedObjects)
    {
        this.player = player;
        this.axeMesh = axeMesh;
        this.taggedObjects = taggedObjects;
        this.stats = player.Stats;

        Configure(new()
        {
            new CancelOnMovementInput(),
            new CancelOnMouseRelease()
        });

        // Get animation transforms
        atLeftHand = player.TaggedObjects.Get(CustomTagType.AnimationTransform, "Left Hand");
        atRightHand = player.TaggedObjects.Get(CustomTagType.AnimationTransform, "Right Hand");
        apLeftHandChopBase = player.TaggedObjects.Get(CustomTagType.AnimationPoint, "AP - Left Hand - Chop - Start");
        apLeftHandChopBack = player.TaggedObjects.Get(CustomTagType.AnimationPoint, "AP - Left Hand - Chop - Back");

        // Create mouse preview
        targetChopPreview = GameObject.CreatePrimitive(PrimitiveType.Quad);
        targetChopPreview.transform.localScale = new Vector3(0.3f, 0.4f, 1f);
        targetChopPreview.SetActive(false);

        Renderer renderer = targetChopPreview.GetComponent<Renderer>();
        renderer.sharedMaterial = AssetDatabase.GetMaterial("Chop Preview");
    }

    public void Dispose()
    {
        GameObject.Destroy(targetChopPreview);
    }

    public override void UpdateAvailable()
    {
        targetChopPreview.SetActive(false);

        // Find hovered tree and exit early if not hovering any
        chopTarget = FindTreeTarget();
        if (chopTarget == null) return;

        // Raycast out, to the side, and down to find a valid chop from position
        Vector3 awayFromChopPos = chopTarget.Hit.point + chopTarget.Hit.normal * CHOP_BACK_OFFSET;
        awayFromChopPos += Vector3.Cross(chopTarget.Hit.normal, Vector3.up) * CHOP_SIDE_OFFSET;

        // If there is nowhere to chop from just stop early
        bool foundChopFromPos = Physics.Raycast(awayFromChopPos, Vector3.down, out RaycastHit playerChopFromHit, MAX_CHOP_GROUND_DISTANCE, LayerMask.GetMask("Ground"))
            && playerChopFromHit.distance >= MIN_CHOP_GROUND_DISTANCE;

        if (!foundChopFromPos)
        {
            chopTarget = null;
            return;
        }
        
        chopFromPos = playerChopFromHit.point;

        // Update preview to current hovered position
        targetChopPreview.transform.SetPositionAndRotation(chopTarget.Hit.point, Quaternion.LookRotation(-Vector3.up, chopTarget.Hit.normal));
        targetChopPreview.SetActive(true);
    }

    public override void UpdateActive()
    {
        float dt = Time.time - previousUpdateTime;
        previousUpdateTime = Time.time;

        if (animationState == State.Travel)
        {
            // Just set to target when walking over
            chopPreview.transform.position = targetChopPreview.transform.position;
            chopPreviewRenderer.sharedMaterial.color = CHOP_GOOD_COLOR;
        }
        else
        {
            // Aim axes come from the camera yaw and pitch without shake, so the mapping stays stable
            Quaternion aimRotation = player.Camera.AimRotation;
            Vector3 camRight = aimRotation * Vector3.right;
            Vector3 camUp = aimRotation * Vector3.up;
            Vector3 right = Vector3.ProjectOnPlane(camRight, chopTarget.Hit.normal);
            right = right.sqrMagnitude > 0.001f ? right.normalized : HitRight;
            Vector3 up = Vector3.up;

            // Boost the more foreshortened axis so equal mouse movement moves the reticle equally on screen
            float screenRight = Mathf.Max(Mathf.Abs(Vector3.Dot(right, camRight)), 0.01f);
            float screenUp = Mathf.Max(Mathf.Abs(Vector3.Dot(up, camUp)), 0.01f);
            float screenMax = Mathf.Max(screenRight, screenUp);
            float gainRight = Mathf.Min(CHOP_AIM_GAIN_MAX, screenMax / screenRight);
            float gainUp = Mathf.Min(CHOP_AIM_GAIN_MAX, screenMax / screenUp);

            // Calculate mouse sway with mouse screen delta
            float mouseX = (player.Input.MouseDelta.x / Screen.width) / CHOP_SWAY_SCREEN_MAX * gainRight;
            float mouseY = (player.Input.MouseDelta.y / Screen.height) / CHOP_SWAY_SCREEN_MAX * gainUp;
            Vector2 mouseSway = new(mouseX, mouseY);

            // Calculate noise amounts based on existing distance from target
            currentSwayPct = Mathf.Clamp01(currentSway.magnitude);
            float swayPctEased = Easing.EaseOutQuad(currentSwayPct);
            float noiseSwayFreq = CHOP_SWAY_NOISE_FREQUENCY_MIN + CHOP_SWAY_NOISE_FREQUENCY * swayPctEased;

            // Calculate the current noise sway
            swayNoiseTimeX += dt * noiseSwayFreq;
            swayNoiseTimeY += dt * noiseSwayFreq;
            float2 noiseSwayInputX = new(swayNoiseTimeX, 0f);
            float2 noiseSwayInputY = new(swayNoiseTimeY, 0f);
            float noiseSwayX = noise.cnoise(noiseSwayInputX) / CHOP_SWAY_NOISE_CLAMP;
            float noiseSwayY = noise.cnoise(noiseSwayInputY) / CHOP_SWAY_NOISE_CLAMP;
            noiseSwayX = Mathf.Clamp(noiseSwayX, -1f, 1f) * CHOP_SWAY_NOISE_MAGNITUDE;
            noiseSwayY = Mathf.Clamp(noiseSwayY, -1f, 1f) * CHOP_SWAY_NOISE_MAGNITUDE;
            Vector2 noiseSway = new(noiseSwayX, noiseSwayY);

            // We want to magnetise towards the center very slightly
            Vector2 magnetSway = Vector2.zero;
            if (currentSwayPct < 0.1f && mouseSway.magnitude > 0.0f)
            {
                float magnetPct = Easing.EaseOutQuad(Mathf.Clamp01(currentSwayPct / 0.1f));
                magnetSway = currentSway * (-magnetPct * CHOP_SWAY_MAGNET_MAX);
            }

            // Update the current sway
            currentSway += mouseSway + noiseSway + magnetSway;
            currentSway.x = Mathf.Clamp(currentSway.x, -CHOP_SWAY_CLAMP, CHOP_SWAY_CLAMP);
            currentSway.y = Mathf.Clamp(currentSway.y, -CHOP_SWAY_CLAMP, CHOP_SWAY_CLAMP);
            currentSwayPct = Mathf.Clamp01(currentSway.magnitude);

            // Calculate an additive shake based on current sway
            // We do not update the sway with this, this is a layer on top
            float shakeOffsetMult = Easing.EaseInQuad(currentSwayPct);
            swayShakeNoiseTime += dt * CHOP_SWAY_SHAKE_FREQUENCY * shakeOffsetMult;
            float shakeAmount = CHOP_SWAY_SHAKE_MAGNITUDE * shakeOffsetMult;
            float shakeX = Mathf.Sin(swayShakeNoiseTime * Mathf.PI * 2f) * shakeAmount;
            float shakeY = Mathf.Cos(swayShakeNoiseTime * Mathf.PI * 2f) * shakeAmount;

            // And finally map the sway and shake to the world position
            Vector3 worldOffsetDir = ((CHOP_SWAY_WORLD_MAX * currentSway.x + shakeX) * right) +
                                     ((CHOP_SWAY_WORLD_MAX * currentSway.y + shakeY) * up);

            chopPreview.transform.position = targetChopPreview.transform.position + worldOffsetDir;
            chopPreviewRenderer.sharedMaterial.color = Color.Lerp(CHOP_GOOD_COLOR, CHOP_BAD_COLOR, shakeOffsetMult);

            // Shake the camera with how inaccurate the swing is
            float camShakeAmount = currentSwayPct * CHOP_CAM_SHAKE_AMOUNT;
            float camShakeNoiseTime = Time.time * CHOP_CAM_SHAKE_FREQUENCY;
            camFocus.RotationOffset = new(
                (Mathf.PerlinNoise(camShakeNoiseTime, 0.5f) - 0.5f) * 2f * camShakeAmount,
                (Mathf.PerlinNoise(camShakeNoiseTime, 10.5f) - 0.5f) * 2f * camShakeAmount
            );
        }
    }

    private Player player;
    private GameObject axeMesh;
    private CustomTagRegistry taggedObjects;
    private IStatProvider stats;
    private Transform atLeftHand;
    private Transform atRightHand;
    private Transform apLeftHandChopBase;
    private Transform apLeftHandChopBack;

    private State animationState = State.Travel;
    private PlayerAnimator.AcControlHandle acHandle;
    private GameObject chopPreview;
    private Renderer chopPreviewRenderer;
    private GameObject targetChopPreview;
    private TreeTarget chopTarget;
    private Vector3 chopFromPos;
    private CameraFocus camFocus;

    private Vector2 currentSway;
    private float currentSwayPct;
    private float swayNoiseTimeX;
    private float swayNoiseTimeY;
    private float swayShakeNoiseTime;
    private float previousUpdateTime;

    private Vector3 HitRight => Vector3.Cross(chopTarget.Hit.normal, Vector3.up).normalized;

    protected override async Task Start(CancellationToken ct)
    {
        acHandle = player.Animator.TakeControl(ANIMATION_PRIORITY);
        if (acHandle == null) throw new OperationCanceledException("Failed to take control of animator.");

        // Create current mouse preview
        chopPreview = GameObject.CreatePrimitive(PrimitiveType.Quad);
        chopPreview.transform.position = targetChopPreview.transform.position;
        chopPreview.transform.localScale = new Vector3(0.25f, 0.4f, 1f);
        chopPreview.transform.rotation = Quaternion.LookRotation(-Vector3.up, chopTarget.Hit.normal);

        chopPreviewRenderer = chopPreview.GetComponent<Renderer>();
        chopPreviewRenderer.sharedMaterial = new(AssetDatabase.GetMaterial("Chop Preview"));
        chopPreviewRenderer.sharedMaterial.color = CHOP_GOOD_COLOR;

        // Initialise the player input state
        swayNoiseTimeX = Time.time;
        swayNoiseTimeY = Time.time + 100f;
        swayShakeNoiseTime = Time.time;
        previousUpdateTime = Time.time;
        currentSway = Vector2.zero;
        currentSwayPct = 0;

        animationState = State.Travel;
        player.Input.HideMouse();
        camFocus = new CameraFocus
        {
            Point = chopTarget.Hit.point,
            ViewDir = -chopTarget.Hit.normal,
            YawOffset = CHOP_CAM_YAW_OFFSET,
            CentreBlend = CHOP_CAM_CENTRE_BLEND
        };
        player.Camera.SetFocus(camFocus);

        // Start moving towards target, with right hand locally forward of the left, and left in base position
        player.Movement.SetMovementTarget(chopFromPos, 0.02f);

        atRightHand.SetParent(atLeftHand, true);

        await Task.WhenAll(
            AsyncUtil.While(() => player.Movement.HasReachedTarget, ct),

            AnimationUtil.MoveAndRotateTo(ct,
                atLeftHand, apLeftHandChopBase, Axis.Local,
                0.3f, Easing.EaseInQuad
            ),
            AnimationUtil.MoveTo(ct,
                atRightHand, -Vector3.up * 0.15f, Axis.Local,
                0.3f, Easing.Linear
            )
        );

        // Start facing towards the hit point
        Vector3 facePosition = player.transform.position
            + HitRight * CHOP_LOOK_OFFSET.x
            + chopTarget.Hit.normal * CHOP_LOOK_OFFSET.y;
        player.Movement.SetFacingTarget(facePosition, 0.5f);

        animationState = State.Setup;

        // Start the chopping animation loop
        while (true)
        {
            ct.ThrowIfCancellationRequested();

            float swingSpeed = stats.GetStat(Stat.SwingSpeed);
            float durationMult = Mathf.Max(0.001f, 1 / Mathf.Max(0.001f, swingSpeed));

            if (animationState == State.Setup)
            {
                await AnimationUtil.MoveAndRotateTo(ct,
                    atLeftHand, apLeftHandChopBack, Axis.Local,
                    1.0f * durationMult, Easing.EaseInQuad
                );

                await Task.Delay((int)(200 * durationMult), ct);
                animationState = State.Swing;
            }

            else if (animationState == State.Swing)
            {
                // Sample the preview pose and calculate the target hand pose
                var (previewPos, previewNormal) = GetChopTargetPose();
                var mpHit = taggedObjects.Get(CustomTagType.MeshPoint, "MP - Hit");
                var previewPoseMatrix = Matrix4x4.TRS(previewPos, Quaternion.LookRotation(previewNormal, Vector3.up), Vector3.one);
                var chopToMatrix = previewPoseMatrix * (atLeftHand.worldToLocalMatrix * mpHit.localToWorldMatrix).inverse;
                Vector3 leftHandChopToPos = chopToMatrix.GetColumn(3);
                Quaternion leftHandChopToRot = chopToMatrix.rotation;

                await AnimationUtil.MoveAndRotateTo(ct,
                    atLeftHand, leftHandChopToPos, leftHandChopToRot,
                    Axis.Global, 0.1f * durationMult, Easing.Linear
                );

                HitTree();

                await Task.Delay((int)(700 * durationMult), ct);
                animationState = State.Pull;
            }

            else if (animationState == State.Pull)
            {
                await AnimationUtil.MoveAndRotateTo(ct,
                    atLeftHand, apLeftHandChopBack,
                    Axis.Local, 0.7f * durationMult, Easing.EaseOutQuad
                );

                animationState = State.Swing;
            }
        }
    }

    protected override Task Stop(CancellationToken ct)
    {
        player.Input.ShowMouse();
        player.Camera.ClearFocus();
        GameObject.Destroy(chopPreview);
        atRightHand.SetParent(atLeftHand.parent, true);
        acHandle?.Release();
        return Task.CompletedTask;
    }

    private void HitTree()
    {
        var (targetPos, _) = GetChopTargetPose();

        var result = chopTarget.Tree.Hit(new TreeHitInput
        {
            Position = targetPos,
            Strength = stats.GetStat(Stat.SwingStrength),
            Sharpness = stats.GetStat(Stat.Sharpness),
            Accuracy = Mathf.Clamp01(1f - Mathf.Abs(currentSwayPct)),
            BitWidth = stats.GetStat(Stat.BitWidth)
        });
        if (result.Outcome == TreeHitOutcome.Miss) return;

        Debug.Log($"Tree hit: {result.Outcome} on {chopTarget.Tree.WoodType.DisplayName}, land chance {result.LandChance:P0}, depth {result.Depth:F4}, removed {result.LitresRemoved:F2} L, wood damage {result.Damage:F2}, split {result.CausedSplit}");

        string popupText = result.Outcome == TreeHitOutcome.Glance ? "Miss!" : result.LitresRemoved.ToString("F1");
        TextPopupManager.SpawnTextPopup(targetPos, popupText);
    }

    private (Vector3 pos, Vector3 normal) GetChopTargetPose()
    {
        Vector3 pos = chopPreview.transform.position;
        return (pos, chopTarget.Hit.normal);
    }

    private TreeTarget FindTreeTarget()
    {
        foreach (var hit in player.Input.RaycastHits)
        {
            if (hit.transform.gameObject.TryGetComponent(out ChoppableTree tree))
            {
                float dist = Vector3.Distance(player.transform.position, hit.point);
                if (dist <= MAX_PLAYER_TARGET_DISTANCE && tree.CanChop)
                {
                    Vector3 hitLocal = tree.transform.InverseTransformPoint(hit.point);
                    return new TreeTarget()
                    {
                        Hit = hit,
                        HitPointLocal = hitLocal,
                        Tree = tree
                    };
                }
            }
        }
        return null;
    }

    private enum State
    { Travel, Setup, Swing, Pull };

    private class TreeTarget
    {
        public RaycastHit Hit;
        public Vector3 HitPointLocal;
        public ChoppableTree Tree;
    }

    private static readonly int ANIMATION_PRIORITY = 0;
    private static readonly float MAX_PLAYER_TARGET_DISTANCE = 10f;
    private static readonly float CHOP_BACK_OFFSET = 0.4f;
    private static readonly float CHOP_SIDE_OFFSET = 0.5f;
    private static readonly Vector2 CHOP_LOOK_OFFSET = new(-0.5f, -0.2f);
    private static readonly float MAX_CHOP_GROUND_DISTANCE = 1.5f;
    private static readonly float MIN_CHOP_GROUND_DISTANCE = 0.1f;
    private static readonly Color CHOP_GOOD_COLOR = new(0.2f, 1f, 0.2f, 0.5f);
    private static readonly Color CHOP_BAD_COLOR = new(1f, 0.2f, 0.2f, 0.5f);

    private static readonly float CHOP_CAM_YAW_OFFSET = 10f;
    private static readonly float CHOP_CAM_CENTRE_BLEND = 0.6f;
    private static readonly float CHOP_CAM_SHAKE_AMOUNT = 1f;
    private static readonly float CHOP_CAM_SHAKE_FREQUENCY = 3f;
    private static readonly float CHOP_AIM_GAIN_MAX = 2f;
    private static readonly float CHOP_SWAY_SCREEN_MAX = 0.03f;
    private static readonly float CHOP_SWAY_WORLD_MAX = 0.15f;
    private static readonly float CHOP_SWAY_NOISE_CLAMP = 0.1f;
    private static readonly float CHOP_SWAY_NOISE_MAGNITUDE = 0.004f;
    private static readonly float CHOP_SWAY_NOISE_FREQUENCY_MIN = 0.4f;
    private static readonly float CHOP_SWAY_NOISE_FREQUENCY = 0.4f;
    private static readonly float CHOP_SWAY_SHAKE_MAGNITUDE = 0.02f;
    private static readonly float CHOP_SWAY_SHAKE_FREQUENCY = 8f;
    private static readonly float CHOP_SWAY_CLAMP = 1.0f;
    private static readonly float CHOP_SWAY_MAGNET_MAX = 0.01f;
}
