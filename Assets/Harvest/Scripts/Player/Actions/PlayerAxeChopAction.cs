using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Mathematics;
using System;

public class PlayerAxeChopAction : PlayerAction
{
    public override bool IsAvailable =>
        !player.IsRestricted(Player.Restriction.Movement) &&
        player.Animator.CanTakeControl(ANIMATION_PRIORITY);

    public override bool IsRunnable =>
        IsAvailable
        && chopTarget != null
        && chopFromPos != null;

    public PlayerAxeChopAction(Player player, GameObject axeMesh)
    {
        this.player = player;
        this.axeMesh = axeMesh;

        ConfigActionPlayerRestriction(Player.Restriction.Movement);
        ConfigActionCancelCondition(new CancelOnMovementInput());
        ConfigActionCancelCondition(new CancelOnMouseRelease());
        ConfigActionCancellable(true);

        // Grab animation transforms
        leftHand = player.Animator.GetAnimationTransform("Left Hand");
        rightHand = player.Animator.GetAnimationTransform("Right Hand");
        leftHandChopBase = player.Animator.GetAnimationPoint("Left Hand - Chop - Base Point");
        leftHandChopFrom = player.Animator.GetAnimationPoint("Left Hand - Chop - Back Point");

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

    public override void Preview()
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

    protected override async Task Start(CancellationToken ct)
    {
        animationState = State.Setup;
        animationHandle = player.Animator.TakeControl(ANIMATION_PRIORITY);
        if (animationHandle == null) throw new OperationCanceledException("Failed to take control of animator.");

        // Create current mouse preview
        chopPreview = GameObject.CreatePrimitive(PrimitiveType.Quad);
        chopPreview.transform.position = targetChopPreview.transform.position;
        chopPreview.transform.localScale = new Vector3(0.25f, 0.4f, 1f);
        chopPreview.transform.rotation = Quaternion.LookRotation(-Vector3.up, chopTarget.Hit.normal);

        chopPreviewRenderer = chopPreview.GetComponent<Renderer>();
        chopPreviewRenderer.sharedMaterial = new(AssetDatabase.GetMaterial("Chop Preview"));
        chopPreviewRenderer.sharedMaterial.color = CHOP_GOOD_COLOR;

        // Start the player input reaction task
        handleMouseInputTask = HandleMouseInput(ct);
        player.Input.HideMouse();

        // Put right hand locally forward of the left, and left in base position
        rightHand.SetParent(leftHand, true);
        player.Movement.MoveTowardsPosition(chopFromPos, 0.02f);

        await Task.WhenAll(
            AsyncUtil.WaitUntil(() => player.Movement.HasReachedTarget, ct),

            AnimationUtil.MoveAndRotateTo(ct,
                leftHand, leftHandChopBase, Axis.Local,
                0.3f, Easing.EaseInQuad
            ),
            AnimationUtil.MoveTo(ct,
                rightHand, Vector3.forward * 0.15f, Axis.Local,
                0.3f, Easing.Linear
            )
        );

        // Start facing towards the hit point
        Vector3 facePosition = player.transform.position
            + HitRight * CHOP_LOOK_OFFSET.x
            + chopTarget.Hit.normal * CHOP_LOOK_OFFSET.y;
        player.Movement.FaceTowardsPosition(facePosition, 0.5f);

        // Start the chopping animation loop
        while (true)
        {
            ct.ThrowIfCancellationRequested();

            if (animationState == State.Setup)
            {
                await AnimationUtil.MoveAndRotateTo(ct,
                    leftHand, leftHandChopFrom, Axis.Local,
                    1.0f, Easing.EaseInQuad
                );

                await Task.Delay(200, ct);
                animationState = State.Swing;
            }

            else if (animationState == State.Swing)
            {
                // Sample the preview pose and calculate the target hand pose
                var (previewPos, previewNormal) = GetPreviewHitPose();
                Transform axeHitPoint = axeMesh.transform.Find("Hit Point");
                var previewPoseMatrix = Matrix4x4.TRS(previewPos, Quaternion.LookRotation(previewNormal, Vector3.up), Vector3.one);
                var chopToMatrix = previewPoseMatrix * (leftHand.worldToLocalMatrix * axeHitPoint.localToWorldMatrix).inverse;
                Vector3 leftHandChopToPos = chopToMatrix.GetColumn(3);
                Quaternion leftHandChopToRot = chopToMatrix.rotation;

                await AnimationUtil.MoveAndRotateTo(ct,
                    leftHand, leftHandChopToPos, leftHandChopToRot,
                    Axis.Global, 0.1f, Easing.Linear
                );

                OnHitTree();
                await Task.Delay(400, ct);
                animationState = State.Pull;
            }

            else if (animationState == State.Pull)
            {
                await AnimationUtil.MoveAndRotateTo(ct,
                    leftHand, leftHandChopFrom,
                    Axis.Local, 0.7f, Easing.EaseOutQuad
                );

                animationState = State.Swing;
            }
        }
    }

    protected override async Task Stop(CancellationToken ct)
    {
        player.Input.ShowMouse();
        GameObject.Destroy(chopPreview);
        rightHand.SetParent(player.transform, true);

        // Wait for the mouse task to finish and ignore cancels
        if (handleMouseInputTask != null)
        {
            try { await handleMouseInputTask; }
            catch (OperationCanceledException) { }
            catch (Exception e) { Debug.LogError($"Error in HandleMouseInputAsync: {e}"); }
            handleMouseInputTask.Dispose();
        }

        animationHandle?.Release();
    }

    private Player player;
    private GameObject axeMesh;
    private GameObject targetChopPreview;
    private GameObject chopPreview;
    private Renderer chopPreviewRenderer;
    private Transform leftHand;
    private Transform rightHand;
    private Transform leftHandChopBase;
    private Transform leftHandChopFrom;
    private TreeTarget chopTarget;
    private Vector3 chopFromPos;
    private State animationState = State.Setup;
    private PlayerAnimator.ControlHandle animationHandle;
    private Task handleMouseInputTask;
    private float currentOffsetAmount = 0f;
    private Vector3 HitRight => Vector3.Cross(chopTarget.Hit.normal, Vector3.up).normalized;

    private async Task HandleMouseInput(CancellationToken ct)
    {
        float swayNoiseTimeX = Time.time;
        float swayNoiseTimeY = Time.time + 100f;
        float shakeNoiseTime = Time.time;
        float lastTime = Time.time;

        currentOffsetAmount = 0;
        Vector2 offsetDir = Vector2.zero;
        const float offsetDirClamp = 1f;

        while (true)
        {
            ct.ThrowIfCancellationRequested();

            float dt = Time.time - lastTime;
            lastTime = Time.time;

            Vector3 right = HitRight;
            Vector3 up = Vector3.up;

            // Calculate mouse sway with mouse screen delta
            float mouseX = (player.Input.MouseDelta.x / Screen.width) / CHOP_SWAY_SCREEN_MAX;
            float mouseY = (player.Input.MouseDelta.y / Screen.height) / CHOP_SWAY_SCREEN_MAX;
            Vector2 swayMouse = new(mouseX, mouseY);

            // Calculate noise sway based on existing offset over time
            currentOffsetAmount = Mathf.Clamp01(offsetDir.magnitude);
            float swayOffsetMult = Easing.EaseOutQuad(currentOffsetAmount);
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
            offsetDir += swayMouse + swayNoise;
            offsetDir.x = Mathf.Clamp(offsetDir.x, -offsetDirClamp, offsetDirClamp);
            offsetDir.y = Mathf.Clamp(offsetDir.y, -offsetDirClamp, offsetDirClamp);

            // Calculate small shake based on existing offset
            float shakeOffsetMult = Easing.EaseInQuad(currentOffsetAmount);
            shakeNoiseTime += dt * CHOP_SWAY_SHAKE_FREQUENCY * shakeOffsetMult;
            float shakeAmount = CHOP_SWAY_SHAKE_MAGNITUDE * shakeOffsetMult;

            float shakeX = Mathf.Sin(shakeNoiseTime * Mathf.PI * 2f) * shakeAmount;
            float shakeY = Mathf.Cos(shakeNoiseTime * Mathf.PI * 2f) * shakeAmount;

            // Map sway and shake to world offset and apply to preview
            Vector3 worldOffsetDir = ((CHOP_SWAY_WORLD_MAX * offsetDir.x + shakeX) * right) +
                                     ((CHOP_SWAY_WORLD_MAX * offsetDir.y + shakeY) * up);

            chopPreview.transform.position = targetChopPreview.transform.position + worldOffsetDir;
            chopPreviewRenderer.sharedMaterial.color = Color.Lerp(CHOP_GOOD_COLOR, CHOP_BAD_COLOR, shakeOffsetMult);

            await Task.Yield();
        }
    }

    private (Vector3 pos, Vector3 normal) GetPreviewHitPose()
    {
        Vector3 pos = chopPreview.transform.position;
        return (pos, chopTarget.Hit.normal);
    }

    private void OnHitTree()
    {
        float accuracy = Mathf.Clamp01(1f - Mathf.Abs(currentOffsetAmount));
        float depth = 0.01f + accuracy * 0.05f;
        float width = 0.1f + accuracy * 0.25f;
        float height = 0.02f + accuracy * 0.05f;

        var (previewPos, _) = GetPreviewHitPose();
        chopTarget.Tree.Hit(previewPos, depth, width, height);
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
}
