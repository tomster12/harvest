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
        chopTarget != null &&
        chopFromPos != null;

    public PlayerAxeChopAction(Player player, GameObject axeMesh, CustomTagRegistry axeTags)
    {
        this.player = player;
        this.axeMesh = axeMesh;
        this.axeTags = axeTags;
        this.stats = player.Stats;

        Configure(new()
        {
            new CancelOnMovementInput(),
            new CancelOnMouseRelease()
        });

        // Get animation transforms
        atLeftHand = player.CustomTags.Get(CustomTagType.AnimationTransform, "Left Hand");
        atRightHand = player.CustomTags.Get(CustomTagType.AnimationTransform, "Right Hand");
        apLeftHandChopBase = player.CustomTags.Get(CustomTagType.AnimationPoint, "AP - Left Hand - Chop - Start");
        apLeftHandChopBack = player.CustomTags.Get(CustomTagType.AnimationPoint, "AP - Left Hand - Chop - Back");

        // Create mouse preview
        targetChopPreview = GameObject.CreatePrimitive(PrimitiveType.Quad);
        targetChopPreview.transform.localScale = new Vector3(0.3f, 0.4f, 1f);
        Renderer renderer = targetChopPreview.GetComponent<Renderer>();
        renderer.sharedMaterial = AssetDatabase.GetMaterial("Chop Preview");
        targetChopPreview.SetActive(false);
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
        if (!Physics.Raycast(awayFromChopPos, Vector3.down, out RaycastHit playerChopFromHit, MAX_CHOP_GROUND_DISTANCE, LayerMask.GetMask("Ground"))) return;
        if (playerChopFromHit.distance < MIN_CHOP_GROUND_DISTANCE) return;
        chopFromPos = playerChopFromHit.point;

        // Update preview to current hovered position
        targetChopPreview.transform.SetPositionAndRotation(chopTarget.Hit.point, Quaternion.LookRotation(-Vector3.up, chopTarget.Hit.normal));
        targetChopPreview.SetActive(true);
    }

    public override void UpdateActive()
    {
        float dt = Time.time - swayLastTime;
        swayLastTime = Time.time;

        Vector3 right = HitRight;
        Vector3 up = Vector3.up;

        // Calculate mouse sway with mouse screen delta
        float mouseX = (player.Input.MouseDelta.x / Screen.width) / CHOP_SWAY_SCREEN_MAX;
        float mouseY = (player.Input.MouseDelta.y / Screen.height) / CHOP_SWAY_SCREEN_MAX;
        Vector2 swayMouse = new(mouseX, mouseY);

        // Calculate noise sway based on existing offset over time
        swayCurrentOffsetAmount = Mathf.Clamp01(swayOffsetDir.magnitude);
        float swayOffsetMult = Easing.EaseOutQuad(swayCurrentOffsetAmount);
        float swayNoiseFreq = CHOP_SWAY_NOISE_FREQUENCY_MIN + CHOP_SWAY_NOISE_FREQUENCY * swayOffsetMult;

        swayNoiseTimeX += dt * swayNoiseFreq;
        swayNoiseTimeY += dt * swayNoiseFreq;
        float2 swayNoiseInputX = new(swayNoiseTimeX, 0f);
        float2 swayNoiseInputY = new(swayNoiseTimeY, 0f);
        float swayNoiseX = noise.cnoise(swayNoiseInputX) / CHOP_SWAY_NOISE_CLAMP;
        float swayNoiseY = noise.cnoise(swayNoiseInputY) / CHOP_SWAY_NOISE_CLAMP;
        swayNoiseX = Mathf.Clamp(swayNoiseX, -1f, 1f) * CHOP_SWAY_NOISE_MAGNITUDE;
        swayNoiseY = Mathf.Clamp(swayNoiseY, -1f, 1f) * CHOP_SWAY_NOISE_MAGNITUDE;
        Vector2 swayNoise = new(swayNoiseX, swayNoiseY);

        // Update 2D offset with mouse and noise sway
        swayOffsetDir += swayMouse + swayNoise;
        swayOffsetDir.x = Mathf.Clamp(swayOffsetDir.x, -swayOffsetDirClamp, swayOffsetDirClamp);
        swayOffsetDir.y = Mathf.Clamp(swayOffsetDir.y, -swayOffsetDirClamp, swayOffsetDirClamp);

        // Calculate small shake based on existing offset
        float shakeOffsetMult = Easing.EaseInQuad(swayCurrentOffsetAmount);
        swayShakeNoiseTime += dt * CHOP_SWAY_SHAKE_FREQUENCY * shakeOffsetMult;
        float shakeAmount = CHOP_SWAY_SHAKE_MAGNITUDE * shakeOffsetMult;

        float shakeX = Mathf.Sin(swayShakeNoiseTime * Mathf.PI * 2f) * shakeAmount;
        float shakeY = Mathf.Cos(swayShakeNoiseTime * Mathf.PI * 2f) * shakeAmount;

        // Map sway and shake to world offset and apply to preview
        Vector3 worldOffsetDir = ((CHOP_SWAY_WORLD_MAX * swayOffsetDir.x + shakeX) * right) +
                                 ((CHOP_SWAY_WORLD_MAX * swayOffsetDir.y + shakeY) * up);

        chopPreview.transform.position = targetChopPreview.transform.position + worldOffsetDir;
        chopPreviewRenderer.sharedMaterial.color = Color.Lerp(CHOP_GOOD_COLOR, CHOP_BAD_COLOR, shakeOffsetMult);
    }

    private Player player;
    private GameObject axeMesh;
    private CustomTagRegistry axeTags;
    private IStatProvider stats;
    private Transform atLeftHand;
    private Transform atRightHand;
    private Transform apLeftHandChopBase;
    private Transform apLeftHandChopBack;

    private State animationState = State.Setup;
    private PlayerAnimator.AcControlHandle acHandle;
    private GameObject chopPreview;
    private Renderer chopPreviewRenderer;
    private GameObject targetChopPreview;
    private TreeTarget chopTarget;
    private Vector3 chopFromPos;
    private float swayCurrentOffsetAmount;
    private float swayNoiseTimeX;
    private float swayNoiseTimeY;
    private float swayShakeNoiseTime;
    private float swayLastTime;
    private Vector2 swayOffsetDir;
    private const float swayOffsetDirClamp = 1.0f;

    private Vector3 HitRight => Vector3.Cross(chopTarget.Hit.normal, Vector3.up).normalized;

    protected override async Task Start(CancellationToken ct)
    {
        animationState = State.Setup;
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
        swayLastTime = Time.time;
        swayOffsetDir = Vector2.zero;
        swayCurrentOffsetAmount = 0;

        player.Input.HideMouse();

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
                var mpHit = axeTags.Get(CustomTagType.MeshPoint, "MP - Hit");
                var previewPoseMatrix = Matrix4x4.TRS(previewPos, Quaternion.LookRotation(previewNormal, Vector3.up), Vector3.one);
                var chopToMatrix = previewPoseMatrix * (atLeftHand.worldToLocalMatrix * mpHit.localToWorldMatrix).inverse;
                Vector3 leftHandChopToPos = chopToMatrix.GetColumn(3);
                Quaternion leftHandChopToRot = chopToMatrix.rotation;

                await AnimationUtil.MoveAndRotateTo(ct,
                    atLeftHand, leftHandChopToPos, leftHandChopToRot,
                    Axis.Global, 0.1f * durationMult, Easing.Linear
                );

                OnHitTree();
                await Task.Delay((int)(400 * durationMult), ct);
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
        GameObject.Destroy(chopPreview);
        atRightHand.SetParent(atLeftHand.parent, true);
        acHandle?.Release();
        return Task.CompletedTask;
    }

    private (Vector3 pos, Vector3 normal) GetChopTargetPose()
    {
        Vector3 pos = chopPreview.transform.position;
        return (pos, chopTarget.Hit.normal);
    }

    private void OnHitTree()
    {
        float swingDamage = stats.GetStat(Stat.SwingDamage);
        float accuracy = Mathf.Clamp01(1f - Mathf.Abs(swayCurrentOffsetAmount));

        float depth = (0.02f + accuracy * 0.05f) * swingDamage;
        float width = (0.05f + accuracy * 0.3f) * swingDamage;
        float height = (0.02f + accuracy * 0.05f) * swingDamage;

        var (targetPos, _) = GetChopTargetPose();

        var hit = chopTarget.Tree.Hit(targetPos, depth, width, height);

        if (hit) TextPopupManager.SpawnTextPopup(targetPos, (depth * CHOP_POPUP_MULT).ToString("F1"));
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
    { Setup, Swing, Pull };

    private class TreeTarget
    {
        public RaycastHit Hit;
        public Vector3 HitPointLocal;
        public ChoppableTree Tree;
    }

    private static readonly float MAX_PLAYER_TARGET_DISTANCE = 10f;
    private static readonly float CHOP_BACK_OFFSET = 0.4f;
    private static readonly float CHOP_SIDE_OFFSET = 0.5f;
    private static readonly Vector2 CHOP_LOOK_OFFSET = new(-0.5f, -0.2f);
    private static readonly float MAX_CHOP_GROUND_DISTANCE = 0.8f;
    private static readonly float MIN_CHOP_GROUND_DISTANCE = 0.15f;
    private static readonly Color CHOP_GOOD_COLOR = new(0.2f, 1f, 0.2f, 0.5f);
    private static readonly Color CHOP_BAD_COLOR = new(1f, 0.2f, 0.2f, 0.5f);
    private static readonly float CHOP_SWAY_SCREEN_MAX = 0.03f;
    private static readonly float CHOP_SWAY_WORLD_MAX = 0.15f;
    private static readonly float CHOP_SWAY_NOISE_CLAMP = 0.1f;
    private static readonly float CHOP_SWAY_NOISE_MAGNITUDE = 0.004f;
    private static readonly float CHOP_SWAY_NOISE_FREQUENCY_MIN = 0.4f;
    private static readonly float CHOP_SWAY_NOISE_FREQUENCY = 0.4f;
    private static readonly float CHOP_SWAY_SHAKE_MAGNITUDE = 0.02f;
    private static readonly float CHOP_SWAY_SHAKE_FREQUENCY = 8f;
    private static readonly int ANIMATION_PRIORITY = 0;
    private static float CHOP_POPUP_MULT = 100.0f;
}
