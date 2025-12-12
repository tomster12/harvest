using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Mathematics;
using System;
using static PlayerAxeTool;

public class PlayerAxeChopAction : PlayerAction
{
    public PlayerAxeChopAction(Player player, ChoppableTree tree, TreeTarget target, Vector3 playerChopFromPos, GameObject axeMesh, GameObject targetChopPreview) : base(player)
    {
        this.tree = tree;
        this.target = target;
        this.playerChopFromPos = playerChopFromPos;
        this.axeMesh = axeMesh;
        this.targetChopPreview = targetChopPreview;

        AddPlayerRestriction(Player.ActionRestriction.Movement);
        AddCancelCondition(new CancelOnMovementInput());
        AddCancelCondition(new CancelOnMouseRelease());
        SetCancellable(true);

        leftHand = player.Animator.GetAnimationTransform("Left Hand");
        rightHand = player.Animator.GetAnimationTransform("Right Hand");
        leftHandChopBase = player.Animator.GetAnimationPoint("Left Hand - Chop - Base Point");
        leftHandChopFrom = player.Animator.GetAnimationPoint("Left Hand - Chop - Back Point");

        // Create current mouse preview
        chopPreview = GameObject.CreatePrimitive(PrimitiveType.Quad);
        chopPreview.transform.position = targetChopPreview.transform.position;
        chopPreview.transform.localScale = new Vector3(0.25f, 0.4f, 1f);
        chopPreview.transform.rotation = Quaternion.LookRotation(-Vector3.up, this.target.Hit.normal);

        chopPreviewRenderer = chopPreview.GetComponent<Renderer>();
        chopPreviewRenderer.sharedMaterial = new(AssetDatabase.GetMaterial("Chop Preview"));
        chopPreviewRenderer.sharedMaterial.color = CHOP_GOOD_COLOR;
    }

    protected override async Task RunAsync(CancellationToken ct)
    {
        handle = player.Animator.TakeControl();
        if (handle == null) throw new OperationCanceledException("Failed to take control of animator.");

        // Start the player input reaction task
        handleMouseTask = HandleMouseInput(ct);
        player.Input.HideMouse();

        // Put right hand locally forward of the left, and left in base position
        rightHand.SetParent(leftHand, true);
        player.Movement.MoveTowardsPosition(playerChopFromPos, 0.02f);

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
            + target.Hit.normal * CHOP_LOOK_OFFSET.y;
        player.Movement.FaceTowardsPosition(facePosition, 0.5f);

        // Start the chopping animation loop
        while (true)
        {
            ct.ThrowIfCancellationRequested();

            if (state == State.Setup)
            {
                await AnimationUtil.MoveAndRotateTo(ct,
                    leftHand, leftHandChopFrom, Axis.Local,
                    1.0f, Easing.EaseInQuad
                );

                await Task.Delay(200, ct);
                state = State.Swing;
            }

            else if (state == State.Swing)
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
                state = State.Pull;
            }

            else if (state == State.Pull)
            {
                await AnimationUtil.MoveAndRotateTo(ct,
                    leftHand, leftHandChopFrom,
                    Axis.Local, 0.7f, Easing.EaseOutQuad
                );

                state = State.Swing;
            }
        }
    }

    protected override async Task FinishAsync(CancellationToken ct)
    {
        player.Input.ShowMouse();
        GameObject.Destroy(chopPreview);
        rightHand.SetParent(player.transform, true);

        // Wait for the mouse task to finish and ignore cancels
        if (handleMouseTask != null)
        {
            try { await handleMouseTask; }
            catch (OperationCanceledException) { }
            catch (Exception e) { Debug.LogError($"Error in HandleMouseInputAsync: {e}"); }
            handleMouseTask.Dispose();
        }

        handle?.Release();
    }

    private enum State
    { Setup, Swing, Pull };

    private readonly GameObject axeMesh;
    private readonly GameObject targetChopPreview;
    private readonly TreeTarget target;
    private readonly ChoppableTree tree;
    private readonly Vector3 playerChopFromPos;

    private readonly Transform leftHand;
    private readonly Transform rightHand;
    private readonly Transform leftHandChopBase;
    private readonly Transform leftHandChopFrom;
    private readonly GameObject chopPreview;
    private readonly Renderer chopPreviewRenderer;

    private State state = State.Setup;
    private PlayerAnimator.ControlHandle handle;
    private Task handleMouseTask;
    private float offset = 0f;

    private Vector3 HitRight => Vector3.Cross(target.Hit.normal, Vector3.up).normalized;

    private async Task HandleMouseInput(CancellationToken ct)
    {
        float swayNoiseTimeX = Time.time;
        float swayNoiseTimeY = Time.time + 100f;
        float shakeNoiseTime = Time.time;
        float lastTime = Time.time;

        Vector2 offset = Vector2.zero;
        Vector3 worldOffset = Vector3.zero;
        const float clamp = 1f;

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
            float offsetAmount = Mathf.Clamp01(offset.magnitude);
            float swayOffsetMult = Easing.EaseOutQuad(offsetAmount);
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
            offset += swayMouse + swayNoise;
            offset.x = Mathf.Clamp(offset.x, -clamp, clamp);
            offset.y = Mathf.Clamp(offset.y, -clamp, clamp);

            // Calculate small shake based on existing offset
            float shakeOffsetMult = Easing.EaseInQuad(offsetAmount);
            shakeNoiseTime += dt * CHOP_SWAY_SHAKE_FREQUENCY * shakeOffsetMult;
            float shakeAmount = CHOP_SWAY_SHAKE_MAGNITUDE * shakeOffsetMult;

            float shakeX = Mathf.Sin(shakeNoiseTime * Mathf.PI * 2f) * shakeAmount;
            float shakeY = Mathf.Cos(shakeNoiseTime * Mathf.PI * 2f) * shakeAmount;

            // Map sway and shake to world offset and apply to preview
            worldOffset = ((CHOP_SWAY_WORLD_MAX * offset.x + shakeX) * right) +
                          ((CHOP_SWAY_WORLD_MAX * offset.y + shakeY) * up);


            chopPreview.transform.position = targetChopPreview.transform.position + worldOffset;
            chopPreviewRenderer.sharedMaterial.color = Color.Lerp(CHOP_GOOD_COLOR, CHOP_BAD_COLOR, shakeOffsetMult);

            await Task.Yield();
        }
    }

    private (Vector3 pos, Vector3 normal) GetPreviewHitPose()
    {
        Vector3 pos = chopPreview.transform.position;
        return (pos, target.Hit.normal);
    }

    private void OnHitTree()
    {
        float accuracy = Mathf.Clamp01(1f - Mathf.Abs(offset));
        float depth = 0.01f + accuracy * 0.05f;
        float width = 0.1f + accuracy * 0.25f;
        float height = 0.02f + accuracy * 0.05f;

        var (previewPos, _) = GetPreviewHitPose();
        tree.Hit(previewPos, depth, width, height);
    }
}
