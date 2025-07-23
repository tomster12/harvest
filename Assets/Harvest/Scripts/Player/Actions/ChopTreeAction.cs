using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Mathematics;
using System;

public class ChopTreeAction : PlayerAction
{
    public ChopTreeAction(Player player, ChoppableTree tree, ChopTarget chopPoint, Vector3 chopStandPos, GameObject axeMesh, GameObject chopPreview) : base(player)
    {
        this.tree = tree;
        this.chopPoint = chopPoint;
        this.chopStandPos = chopStandPos;
        this.axeMesh = axeMesh;
        this.chopPreview = chopPreview;

        AddPlayerBlock(PlayerBlockFlags.Movement);
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
        currentChopPreview.transform.rotation = Quaternion.LookRotation(-Vector3.up, this.chopPoint.normal);
        currentChopPreviewRenderer = currentChopPreview.GetComponent<Renderer>();
        currentChopPreviewRenderer.sharedMaterial = new(AssetDatabase.GetMaterial("Chop Preview"));
        currentChopPreviewRenderer.sharedMaterial.color = PlayerAxeTool.CHOP_GOOD_COLOR;

        // We want to place the axe so that the hit point is on the target
        Transform axeHitPoint = axeMesh.transform.Find("Hit Point");
        Matrix4x4 hitPoseMatrix = Matrix4x4.TRS(this.chopPoint.pos, Quaternion.LookRotation(this.chopPoint.normal, Vector3.up), Vector3.one);
        Matrix4x4 handToHitMatrix = leftHandTransform.worldToLocalMatrix * axeHitPoint.localToWorldMatrix;
        Matrix4x4 chopToMatrix = hitPoseMatrix * handToHitMatrix.inverse;
        chopToPos = chopToMatrix.GetColumn(3);
        chopToRot = chopToMatrix.rotation;
    }

    protected override async Task RunAsync(CancellationToken ct)
    {
        handle = player.Animator.Lock();

        // Start the player input reaction task
        handleMouseTask = HandleMouseInputAsync(ct);
        player.Input.HideMouse();

        // Move hand in front and move towards target
        rightHandTransform.SetParent(leftHandTransform, true);
        player.Movement.MoveTowardsPosition(chopStandPos, 0.02f);
        await Task.WhenAll(
            AsyncUtil.WaitUntil(() => player.Movement.HasReachedTarget, ct),
            AnimationUtil.MoveAndRotateTo(ct, leftHandTransform, leftHandChopBaseTransform.localPosition, leftHandChopBaseTransform.localRotation, 0.3f, Axis.Local, Easing.EaseInQuad),
            AnimationUtil.MoveTo(ct, rightHandTransform, Vector3.forward * 0.15f, 0.3f, Axis.Local)
        );

        // Start facing towards the hit point
        player.Movement.FaceTowardsPosition(player.transform.position + HitRight * PlayerAxeTool.CHOP_LOOK_OFFSET.x + chopPoint.normal * PlayerAxeTool.CHOP_LOOK_OFFSET.y, 0.5f);

        // Start the chopping animation loop
        while (true)
        {
            ct.ThrowIfCancellationRequested();

            if (state == State.Setup)
            {
                await AnimationUtil.MoveAndRotateTo(ct, leftHandTransform, leftHandChopFromTransform.localPosition, leftHandChopFromTransform.localRotation, 1f, Axis.Local, Easing.EaseInQuad);
                state = State.Swing;
            }
            else if (state == State.Swing)
            {
                Vector3 finalChopToPos = chopToPos + worldOffset;
                await AnimationUtil.MoveAndRotateTo(ct, leftHandTransform, finalChopToPos, chopToRot, 0.1f, Axis.Global);
                OnHitTree();
                await Task.Delay(200, ct);
                state = State.Pull;
            }
            else if (state == State.Pull)
            {
                await AnimationUtil.MoveAndRotateTo(ct, leftHandTransform, leftHandChopFromTransform.localPosition, leftHandChopFromTransform.localRotation, 0.65f, Axis.Local, Easing.EaseOutQuad);
                state = State.Swing;
            }
        }
    }

    protected override async Task FinishAsync(CancellationToken ct)
    {
        player.Input.ShowMouse();
        GameObject.Destroy(currentChopPreview);
        rightHandTransform.parent = player.transform;

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
    private readonly ChopTarget chopPoint;
    private readonly ChoppableTree tree;
    private readonly Vector3 chopStandPos;

    private readonly Transform leftHandTransform;
    private readonly Transform rightHandTransform;
    private readonly Transform leftHandChopBaseTransform;
    private readonly Transform leftHandChopFromTransform;
    private readonly GameObject currentChopPreview;
    private readonly Renderer currentChopPreviewRenderer;
    private readonly Vector3 chopToPos;
    private readonly Quaternion chopToRot;

    private State state = State.Setup;
    private PlayerAnimator.Handle handle;
    private Task handleMouseTask;
    private float offset = 0f;
    private Vector3 worldOffset = Vector3.zero;

    private Vector3 HitRight => Vector3.Cross(chopPoint.normal, Vector3.up).normalized;

    private async Task HandleMouseInputAsync(CancellationToken ct)
    {
        float swayNoiseTime = Time.time;
        float shakeNoiseTime = Time.time;
        float lastTime = Time.time;

        while (true)
        {
            ct.ThrowIfCancellationRequested();

            float deltaTime = Time.time - lastTime;
            lastTime = Time.time;

            // Sway along with the mouse
            float swayMouse = (player.Input.MouseDelta.y / Screen.height) / PlayerAxeTool.CHOP_SWAY_SCREEN_MAX;

            // Sway more the bigger the current offset
            float prevEasedOutAbsOffset = Easing.EaseOutQuad(Mathf.Abs(offset));
            swayNoiseTime += deltaTime * (PlayerAxeTool.CHOP_SWAY_NOISE_FREQUENCY_MIN + PlayerAxeTool.CHOP_SWAY_NOISE_FREQUENCY * prevEasedOutAbsOffset);
            float swayNoise = noise.cnoise(new float2(swayNoiseTime, 0f));
            swayNoise = Mathf.Clamp(swayNoise / PlayerAxeTool.CHOP_SWAY_NOISE_CLAMP, -1f, 1f) * PlayerAxeTool.CHOP_SWAY_NOISE_MAGNITUDE;

            // Update offset and apply easing
            offset = Mathf.Clamp(offset + swayMouse + swayNoise, -1f, 1f);
            float easedInAbsOffset = Easing.EaseInQuad(Mathf.Abs(offset));

            // Shake more the bigger the current offset
            shakeNoiseTime += deltaTime * PlayerAxeTool.CHOP_SWAY_SHAKE_FREQUENCY * easedInAbsOffset;
            float shakeNoiseMagnitude = PlayerAxeTool.CHOP_SWAY_SHAKE_MAGNITUDE * easedInAbsOffset;
            Vector3 shakeNoise = Mathf.Sin(shakeNoiseTime * Mathf.PI * 2f) * shakeNoiseMagnitude * HitRight;

            // Update the preview based on the offset
            worldOffset = (Mathf.Sign(offset) * easedInAbsOffset) * PlayerAxeTool.CHOP_SWAY_WORLD_MAX * Vector3.up + shakeNoise;
            currentChopPreview.transform.position = chopPreview.transform.position + worldOffset;
            currentChopPreviewRenderer.sharedMaterial.color = Color.Lerp(PlayerAxeTool.CHOP_GOOD_COLOR, PlayerAxeTool.CHOP_BAD_COLOR, easedInAbsOffset);

            await Task.Yield();
        }
    }

    private void OnHitTree()
    {
        float inaccuracy = Mathf.Clamp01(1f - Mathf.Abs(offset));
        float strength = 0.01f + inaccuracy * 0.03f;
        float height = 0.04f + inaccuracy * 0.04f;
        tree.Hit(chopPoint, strength, 0.5f, height);
    }
}
