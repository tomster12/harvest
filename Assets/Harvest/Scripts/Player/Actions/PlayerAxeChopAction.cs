using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Mathematics;
using System;
using static ChoppableTree;
using static PlayerAxeTool;

public class PlayerAxeChopAction : PlayerAction
{
    public PlayerAxeChopAction(Player player, ChoppableTree tree, TreeTarget target, Vector3 chopStandPos, GameObject axeMesh, GameObject chopPreview) : base(player)
    {
        this.tree = tree;
        this.target = target;
        this.chopStandPos = chopStandPos;
        this.axeMesh = axeMesh;
        this.chopPreview = chopPreview;

        AddPlayerRestriction(Player.ActionRestriction.Movement);
        AddCancelCondition(new CancelOnMovementInput());
        AddCancelCondition(new CancelOnMouseRelease());
        SetCancellable(true);

        leftHandTransform = player.Animator.GetAnimationTransform("Left Hand");
        rightHandTransform = player.Animator.GetAnimationTransform("Right Hand");
        leftHandChopBaseTransform = player.Animator.GetAnimationPoint("Left Hand - Chop - Base Point");
        leftHandChopFromTransform = player.Animator.GetAnimationPoint("Left Hand - Chop - Back Point");

        // Create current mouse preview
        currentChopPreview = GameObject.CreatePrimitive(PrimitiveType.Quad);
        currentChopPreview.transform.position = chopPreview.transform.position;
        currentChopPreview.transform.localScale = new Vector3(0.25f, 0.4f, 1f);
        currentChopPreview.transform.rotation = Quaternion.LookRotation(-Vector3.up, this.target.Hit.normal);
        currentChopPreviewRenderer = currentChopPreview.GetComponent<Renderer>();
        currentChopPreviewRenderer.sharedMaterial = new(AssetDatabase.GetMaterial("Chop Preview"));
        currentChopPreviewRenderer.sharedMaterial.color = CHOP_GOOD_COLOR;
    }

    protected override async Task RunAsync(CancellationToken ct)
    {
        handle = player.Animator.TakeControl();
        if (handle == null) throw new OperationCanceledException("Failed to take control of animator.");

        // Start the player input reaction task
        handleMouseTask = HandleMouseInput(ct);
        player.Input.HideMouse();

        // Move hand in front and move towards target
        rightHandTransform.SetParent(leftHandTransform, true);
        player.Movement.MoveTowardsPosition(chopStandPos, 0.02f);

        await Task.WhenAll(
            AsyncUtil.WaitUntil(() => player.Movement.HasReachedTarget, ct),

            AnimationUtil.MoveAndRotateTo(ct,
                leftHandTransform, leftHandChopBaseTransform,
                0.3f, Axis.Local, Easing.EaseInQuad),

            AnimationUtil.MoveTo(ct,
                rightHandTransform, Vector3.forward * 0.15f,
                0.3f, Axis.Local)
        );

        // Start facing towards the hit point
        Vector3 facePosition = player.transform.position + HitRight * CHOP_LOOK_OFFSET.x + target.Hit.normal * CHOP_LOOK_OFFSET.y;
        player.Movement.FaceTowardsPosition(facePosition, 0.5f);

        // Start the chopping animation loop
        while (true)
        {
            ct.ThrowIfCancellationRequested();

            if (state == State.Setup)
            {
                await AnimationUtil.MoveAndRotateTo(ct,
                    leftHandTransform, leftHandChopFromTransform,
                    1.2f, Axis.Local, Easing.EaseInQuad);

                state = State.Swing;
            }
            else if (state == State.Swing)
            {
                var (previewPos, previewNormal) = GetPreviewHitPose();

                Transform axeHitPoint = axeMesh.transform.Find("Hit Point");
                var hitPoseMatrix = Matrix4x4.TRS(previewPos, Quaternion.LookRotation(previewNormal, Vector3.up), Vector3.one);
                var chopToMatrix = hitPoseMatrix * (leftHandTransform.worldToLocalMatrix * axeHitPoint.localToWorldMatrix).inverse;
                Vector3 dynamicChopPos = chopToMatrix.GetColumn(3);
                Quaternion dynamicChopRot = chopToMatrix.rotation;

                await AnimationUtil.MoveAndRotateTo(ct,
                    leftHandTransform, dynamicChopPos, dynamicChopRot,
                    0.1f, Axis.Global);

                OnHitTree();
                await Task.Delay(300, ct);
                state = State.Pull;
            }
            else if (state == State.Pull)
            {
                await AnimationUtil.MoveAndRotateTo(ct,
                    leftHandTransform, leftHandChopFromTransform,
                    0.65f, Axis.Local, Easing.EaseOutQuad);

                state = State.Swing;
            }
        }
    }

    protected override async Task FinishAsync(CancellationToken ct)
    {
        player.Input.ShowMouse();
        GameObject.Destroy(currentChopPreview);
        rightHandTransform.SetParent(player.transform, true);

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
    private readonly GameObject chopPreview;
    private readonly TreeTarget target;
    private readonly ChoppableTree tree;
    private readonly Vector3 chopStandPos;

    private readonly Transform leftHandTransform;
    private readonly Transform rightHandTransform;
    private readonly Transform leftHandChopBaseTransform;
    private readonly Transform leftHandChopFromTransform;
    private readonly GameObject currentChopPreview;
    private readonly Renderer currentChopPreviewRenderer;

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

            currentChopPreview.transform.position = chopPreview.transform.position + worldOffset;
            currentChopPreviewRenderer.sharedMaterial.color = Color.Lerp(CHOP_GOOD_COLOR, CHOP_BAD_COLOR, shakeOffsetMult);

            await Task.Yield();
        }
    }

    private (Vector3 pos, Vector3 normal) GetPreviewHitPose()
    {
        Vector3 pos = currentChopPreview.transform.position;
        return (pos, target.Hit.normal);
    }

    private void OnHitTree()
    {
        float accuracy = Mathf.Clamp01(1f - Mathf.Abs(offset));
        float depth = 0.01f + accuracy * 0.03f;
        float width = 1.0f + accuracy * 2.0f;
        float height = 0.04f + accuracy * 0.04f;

        var (previewPos, _) = GetPreviewHitPose();
        tree.Hit(previewPos, depth, width, height);
    }
}
