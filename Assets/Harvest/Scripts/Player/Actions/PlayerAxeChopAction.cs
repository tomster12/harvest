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

    public override bool OverridesMovementInput => true;

    public PlayerAxeChopAction(Player player, GameObject axeMesh, CustomTagRegistry taggedObjects)
    {
        this.player = player;
        this.axeMesh = axeMesh;
        this.taggedObjects = taggedObjects;
        this.stats = player.Stats;

        Configure(new()
        {
            new CancelOnBackwardInput(),
            new CancelOnMouseRelease()
        });

        // Get animation transforms
        atLeftHand = player.TaggedObjects.Get(CustomTagType.AnimationTransform, "Left Hand");
        atRightHand = player.TaggedObjects.Get(CustomTagType.AnimationTransform, "Right Hand");
        apLeftHandChopBase = player.TaggedObjects.Get(CustomTagType.AnimationPoint, "AP - Left Hand - Chop - Start");
        apLeftHandChopBack = player.TaggedObjects.Get(CustomTagType.AnimationPoint, "AP - Left Hand - Chop - Back");

        // Create mouse preview
        targetGhost = CreateGhost("Target Ghost");
        targetGhost.SetActive(false);
        targetLine = targetGhost.GetComponentInChildren<LineRenderer>(true);
        targetGhostRenderer = targetGhost.GetComponent<MeshRenderer>();
    }

    public void Dispose()
    {
        DestroyGhost(targetGhost);
    }

    public override void UpdateAvailable()
    {
        bool wasShowing = targetGhost.activeSelf;
        targetGhost.SetActive(false);

        // Find hovered tree and exit early if not hovering any
        chopTarget = FindTreeTarget();
        if (chopTarget == null) return;

        targetPoint = chopTarget.Hit.point;
        targetNormal = Vector3.ProjectOnPlane(targetPoint - chopTarget.Tree.transform.position, Vector3.up).normalized;

        // If there is nowhere to chop from just stop early
        if (!TryFindChopFromPos(targetPoint, targetNormal, out chopFromPos))
        {
            chopTarget = null;
            return;
        }

        // Ease the preview round the trunk, only snapping when it first appears
        var (ghostPos, ghostRot) = GetGhostPose(targetPoint, targetNormal);
        if (!wasShowing) targetGhost.transform.SetPositionAndRotation(ghostPos, ghostRot);
        else SmoothGhost(targetGhost.transform, ref targetGhostVelocity, ghostPos, ghostRot, GHOST_POSITION_SMOOTH_TIME, Time.deltaTime);

        // Hovering shows a ghost and a line that move together, easing round the trunk
        targetGhost.SetActive(true);
        targetGhostRenderer.enabled = true;
        targetGhostRenderer.sharedMaterial.color = HOVER_GHOST_COLOR;
        UpdateGhostShape(targetGhost);
        targetLine.enabled = true;
        SetLineColour(targetLine, LINE_GREY, LINE_ALPHA);
        Vector3 hoverNormal = targetGhost.transform.up;
        UpdateLine(targetLine, targetGhost.transform.position - hoverNormal * CHOP_GHOST_HOVER, hoverNormal);
    }

    public override void UpdateActive()
    {
        float dt = Time.time - previousUpdateTime;
        previousUpdateTime = Time.time;

        // The target preview follows the target as it is dragged round the trunk
        var (targetGhostPos, targetGhostRot) = GetGhostPose(targetPoint, targetNormal);
        SmoothGhost(targetGhost.transform, ref targetGhostVelocity, targetGhostPos, targetGhostRot, GHOST_POSITION_SMOOTH_TIME, Time.deltaTime);
        UpdateLine(targetLine, targetPoint, targetNormal);

        if (animationState == State.Travel)
        {
            // Just set to target when walking over
            SmoothGhost(aimGhost.transform, ref aimGhostVelocity, targetGhostPos, targetGhostRot, GHOST_POSITION_SMOOTH_TIME, Time.deltaTime);
            UpdateGhostShape(aimGhost);
            UpdateLine(aimLine, aimGhost.transform.position - aimGhost.transform.up * CHOP_GHOST_HOVER, aimGhost.transform.up);
            aimGhostRenderer.sharedMaterial.color = CHOP_YELLOW_COLOR;
            SetLineColour(aimLine, CHOP_YELLOW_COLOR, LINE_ALPHA);
        }
        else
        {
            // Aim axes come from the camera yaw and pitch without shake, so the mapping stays stable
            Quaternion aimRotation = player.Camera.AimRotation;
            Vector3 camRight = aimRotation * Vector3.right;
            Vector3 camUp = aimRotation * Vector3.up;
            Vector3 right = Vector3.ProjectOnPlane(camRight, targetNormal);
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

            // A and D move the target round the trunk, easing up to a slow top speed, and the player walks to follow
            float strafeInput = player.Input.InputMovementAxes.x;
            strafeVelocity = Mathf.MoveTowards(strafeVelocity, strafeInput * CHOP_STRAFE_SPEED, CHOP_STRAFE_ACCELERATION * dt);
            if (Mathf.Abs(strafeVelocity) > 0.001f) MoveTarget(right * strafeVelocity * dt);

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

            // Keep the aim on the trunk surface, which curves away as it moves sideways
            Vector3 aimPoint = targetPoint + worldOffsetDir;
            Vector3 aimNormal = targetNormal;
            if (chopTarget.Tree.TrySampleSurface(aimPoint, out Vector3 sampledPoint, out Vector3 sampledNormal))
            {
                aimPoint = sampledPoint;
                aimNormal = sampledNormal;
            }

            // The aim ghost eases to the real aim, and the aim line tracks it exactly
            float aimAccuracy = Mathf.Clamp01(1f - currentSwayPct);
            var (aimGhostPos, aimGhostRot) = GetGhostPose(aimPoint, aimNormal);
            SmoothGhost(aimGhost.transform, ref aimGhostVelocity, aimGhostPos, aimGhostRot, GHOST_POSITION_SMOOTH_TIME, Time.deltaTime);
            UpdateGhostShape(aimGhost);

            Vector3 aimGhostNormal = aimGhost.transform.up;
            UpdateLine(aimLine, aimGhost.transform.position - aimGhostNormal * CHOP_GHOST_HOVER, aimGhostNormal);

            // Red when wild, through orange and yellow to green near the middle
            Color ghostColour = GetAimColour(aimAccuracy);
            aimGhostRenderer.sharedMaterial.color = ghostColour;
            SetLineColour(aimLine, ghostColour, LINE_ALPHA);

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
    private GameObject aimGhost;
    private Renderer aimGhostRenderer;
    private GameObject targetGhost;
    private TreeTarget chopTarget;
    private Vector3 chopFromPos;
    private Vector3 targetPoint;
    private Vector3 targetNormal;
    private Vector3 targetGhostVelocity;
    private Vector3 aimGhostVelocity;
    private LineRenderer targetLine;
    private MeshRenderer targetGhostRenderer;
    private LineRenderer aimLine;
    private readonly Vector3[] linePoints = new Vector3[LINE_POINTS];
    private readonly Vector3[] lineNormals = new Vector3[LINE_POINTS];
    private static Vector3[] ghostVertices;
    private static Texture2D fadeTexture;
    private CameraFocus camFocus;

    private Vector2 currentSway;
    private float strafeVelocity;
    private float currentSwayPct;
    private float swayNoiseTimeX;
    private float swayNoiseTimeY;
    private float swayShakeNoiseTime;
    private float previousUpdateTime;

    private Vector3 HitRight => Vector3.Cross(targetNormal, Vector3.up).normalized;

    protected override async Task Start(CancellationToken ct)
    {
        acHandle = player.Animator.TakeControl(ANIMATION_PRIORITY);
        if (acHandle == null) throw new OperationCanceledException("Failed to take control of animator.");

        // Create current mouse preview
        aimGhost = CreateGhost("Aim Ghost");
        aimGhost.transform.SetPositionAndRotation(targetGhost.transform.position, targetGhost.transform.rotation);
        aimGhostVelocity = Vector3.zero;
        targetGhostVelocity = Vector3.zero;

        aimGhostRenderer = aimGhost.GetComponent<Renderer>();
        aimGhostRenderer.sharedMaterial.color = CHOP_YELLOW_COLOR;
        aimLine = aimGhost.GetComponentInChildren<LineRenderer>(true);
        SetLineColour(aimLine, CHOP_YELLOW_COLOR, LINE_ALPHA);

        // Only the target line stays behind as a faint grey marker while the aim ghost and line change colour
        targetGhostRenderer.enabled = false;
        targetLine.enabled = true;
        SetLineColour(targetLine, LINE_GREY, TARGET_LINE_ALPHA);

        // Initialise the player input state
        swayNoiseTimeX = Time.time;
        swayNoiseTimeY = Time.time + 100f;
        swayShakeNoiseTime = Time.time;
        previousUpdateTime = Time.time;
        currentSway = Vector2.zero;
        currentSwayPct = 0;
        strafeVelocity = 0f;

        animationState = State.Travel;
        player.Input.HideMouse();
        camFocus = new CameraFocus
        {
            Point = targetPoint,
            ViewDir = -targetNormal,
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
            + targetNormal * CHOP_LOOK_OFFSET.y;
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
        player.Movement.SetFollowTarget(null);
        player.Movement.SetFacingTarget(null);
        DestroyGhost(aimGhost);
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
            BitWidth = stats.GetStat(Stat.BitWidth) / CENTIMETRES_PER_METRE
        });
        if (result.Outcome == TreeHitOutcome.Miss) return;

        Debug.Log($"Tree hit: {result.Outcome} on {chopTarget.Tree.WoodType.DisplayName}, bite {result.BiteQuality:P0}, depth {result.Depth:F4}, removed {result.LitresRemoved:F2} L, wood damage {result.Damage:F2}, split {result.CausedSplit}");

        string popupText = result.LitresRemoved < MIN_POPUP_LITRES ? "Blunt" : result.LitresRemoved.ToString("F1");
        TextPopupManager.SpawnTextPopup(targetPos, popupText);
    }

    private (Vector3 pos, Vector3 normal) GetChopTargetPose()
    {
        // The ghost sits on the original trunk, so drop to the cut surface for the hit and the swing
        Vector3 normal = aimGhost.transform.up;
        Vector3 pos = aimGhost.transform.position - normal * CHOP_GHOST_HOVER;
        if (chopTarget.Tree.TrySampleSurface(pos, out Vector3 point, out Vector3 sampledNormal))
        {
            pos = point;
            normal = sampledNormal;
        }

        return (pos, normal);
    }

    private (Vector3 pos, Quaternion rot) GetGhostPose(Vector3 surfacePoint, Vector3 normal)
    {
        // The ghost follows the trunk's original radius, only the line follows the depth cut into it
        if (chopTarget.Tree.TrySampleSurface(surfacePoint, out Vector3 basePoint, out Vector3 baseNormal, true))
        {
            surfacePoint = basePoint;
            normal = baseNormal;
        }

        return (surfacePoint + normal * CHOP_GHOST_HOVER, Quaternion.LookRotation(-Vector3.up, normal));
    }

    private static Color GetAimColour(float accuracy)
    {
        // Red when wild, through orange and yellow, to green once the aim is close to the middle
        float closeness = Mathf.Clamp01(accuracy / AIM_GREEN_ACCURACY) * 3f;
        if (closeness < 1f) return Color.Lerp(CHOP_RED_COLOR, CHOP_ORANGE_COLOR, closeness);
        if (closeness < 2f) return Color.Lerp(CHOP_ORANGE_COLOR, CHOP_YELLOW_COLOR, closeness - 1f);
        return Color.Lerp(CHOP_YELLOW_COLOR, CHOP_GREEN_COLOR, closeness - 2f);
    }

    private void SmoothGhost(Transform ghost, ref Vector3 velocity, Vector3 targetPos, Quaternion targetRot, float smoothTime, float dt)
    {
        ghost.position = Vector3.SmoothDamp(ghost.position, targetPos, ref velocity, smoothTime, Mathf.Infinity, dt);
        ghost.rotation = Quaternion.Slerp(ghost.rotation, targetRot, 1f - Mathf.Exp(-GHOST_ROTATION_SHARPNESS * dt));
    }

    private bool TryFindChopFromPos(Vector3 point, Vector3 normal, out Vector3 pos)
    {
        // Raycast out, to the side, and down to find a valid chop from position
        Vector3 awayFromChopPos = point + normal * CHOP_BACK_OFFSET;
        awayFromChopPos += Vector3.Cross(normal, Vector3.up) * CHOP_SIDE_OFFSET;

        bool found = Physics.Raycast(awayFromChopPos, Vector3.down, out RaycastHit groundHit, MAX_CHOP_GROUND_DISTANCE, LayerMask.GetMask("Ground"))
            && groundHit.distance >= MIN_CHOP_GROUND_DISTANCE;

        pos = found ? groundHit.point : default;
        return found;
    }

    private void MoveTarget(Vector3 worldDelta)
    {
        if (!chopTarget.Tree.TrySampleSurface(targetPoint + worldDelta, out Vector3 point, out Vector3 normal)) return;

        targetPoint = point;
        targetNormal = normal;
        camFocus.Point = targetPoint;
        camFocus.ViewDir = -targetNormal;
        player.Camera.SetFocus(camFocus);

        // Walk round to the new spot if there is somewhere to stand
        if (!TryFindChopFromPos(targetPoint, targetNormal, out Vector3 newChopFromPos)) return;

        chopFromPos = newChopFromPos;
        player.Movement.SetFollowTarget(chopFromPos);

        // Prioritised so the player keeps facing the tree while sidestepping round it
        player.Movement.SetFacingTarget(player.transform.position + HitRight * CHOP_LOOK_OFFSET.x + targetNormal * CHOP_LOOK_OFFSET.y, 0.5f, true);
    }

    private static GameObject CreateGhost(string ghostName)
    {
        var ghost = new GameObject(ghostName);

        // Each ghost gets its own mesh and material, so the shape and fade never touch the asset
        var mesh = new Mesh { name = "Chop Ghost" };
        mesh.MarkDynamic();
        BuildGhostTopology(mesh);
        ghost.AddComponent<MeshFilter>().sharedMesh = mesh;

        var ghostMaterial = new Material(AssetDatabase.GetMaterial("Chop Preview"));
        ghostMaterial.SetTexture(BASE_MAP_ID, GetFadeTexture());
        ghostMaterial.color = CHOP_YELLOW_COLOR;
        ghost.AddComponent<MeshRenderer>().sharedMaterial = ghostMaterial;

        // Thin line that follows the trunk surface where the edge will land
        var traceObject = new GameObject("Line");
        traceObject.transform.SetParent(ghost.transform, false);
        var trace = traceObject.AddComponent<LineRenderer>();
        trace.useWorldSpace = true;
        trace.positionCount = LINE_POINTS;
        trace.widthMultiplier = LINE_WIDTH;
        trace.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        trace.receiveShadows = false;

        var traceMaterial = new Material(AssetDatabase.GetMaterial("Chop Preview"));
        traceMaterial.EnableKeyword("_EMISSION");
        trace.sharedMaterial = traceMaterial;
        SetLineColour(trace, LINE_GREY, LINE_ALPHA);

        return ghost;
    }

    private static void DestroyGhost(GameObject ghost)
    {
        if (ghost == null) return;

        GameObject.Destroy(ghost.GetComponent<MeshFilter>().sharedMesh);
        GameObject.Destroy(ghost.GetComponent<Renderer>().sharedMaterial);
        GameObject.Destroy(ghost.GetComponentInChildren<LineRenderer>(true).sharedMaterial);
        GameObject.Destroy(ghost);
    }

    private static Texture2D GetFadeTexture()
    {
        if (fadeTexture != null) return fadeTexture;

        // Opaque at the edge nearest the tree, fully transparent at the far end
        fadeTexture = new Texture2D(1, FADE_TEXTURE_SIZE, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
        for (int i = 0; i < FADE_TEXTURE_SIZE; i++)
        {
            float along = i / (FADE_TEXTURE_SIZE - 1f);
            float alpha = (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(FADE_PLATEAU_END, 1f, along))) * FADE_EDGE_ALPHA;
            fadeTexture.SetPixel(0, i, new Color(1f, 1f, 1f, alpha));
        }
        fadeTexture.Apply();
        return fadeTexture;
    }

    private static void BuildGhostTopology(Mesh mesh)
    {
        int cols = LINE_POINTS - 1;
        int rows = GHOST_GRID_ROWS;
        ghostVertices ??= new Vector3[(cols + 1) * (rows + 1)];

        var uvs = new Vector2[ghostVertices.Length];
        var normals = new Vector3[ghostVertices.Length];
        for (int row = 0; row <= rows; row++)
        {
            for (int col = 0; col <= cols; col++)
            {
                int index = row * (cols + 1) + col;
                uvs[index] = new Vector2(col / (float)cols, row / (float)rows);
                normals[index] = -Vector3.forward;
            }
        }

        // Both windings so the ghost shows from either side
        var tris = new int[cols * rows * 12];
        int t = 0;
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                int a = row * (cols + 1) + col;
                int b = a + 1;
                int c = a + cols + 1;
                int d = c + 1;

                tris[t++] = a; tris[t++] = c; tris[t++] = b;
                tris[t++] = b; tris[t++] = c; tris[t++] = d;
                tris[t++] = a; tris[t++] = b; tris[t++] = c;
                tris[t++] = b; tris[t++] = d; tris[t++] = c;
            }
        }

        mesh.vertices = ghostVertices;
        mesh.uv = uvs;
        mesh.normals = normals;
        mesh.triangles = tris;
    }

    private void SampleSurfaceLine(Vector3 surfacePoint, Vector3 normal, bool useBaseRadius)
    {
        // Points across the width of the bit, each pushed onto the trunk so the line follows its curve and dents
        Vector3 tangent = Vector3.Cross(Vector3.up, normal).normalized;
        float width = stats.GetStat(Stat.BitWidth) / CENTIMETRES_PER_METRE;

        for (int i = 0; i < LINE_POINTS; i++)
        {
            Vector3 probe = surfacePoint + tangent * ((i / (LINE_POINTS - 1f) - 0.5f) * width);
            linePoints[i] = probe;
            lineNormals[i] = normal;
            if (chopTarget.Tree.TrySampleSurface(probe, out Vector3 sampledPoint, out Vector3 sampledNormal, useBaseRadius))
            {
                linePoints[i] = sampledPoint;
                lineNormals[i] = sampledNormal;
            }
        }
    }

    private static void SetLineColour(LineRenderer trace, Color colour, float alpha)
    {
        Material traceMaterial = trace.sharedMaterial;
        traceMaterial.color = new Color(colour.r, colour.g, colour.b, alpha);
        traceMaterial.SetColor(EMISSION_COLOR_ID, colour * LINE_EMISSION_SCALE);
    }

    private void UpdateLine(LineRenderer trace, Vector3 surfacePoint, Vector3 normal)
    {
        SampleSurfaceLine(surfacePoint, normal, false);
        for (int i = 0; i < LINE_POINTS; i++) trace.SetPosition(i, linePoints[i] + lineNormals[i] * LINE_LIFT);
    }

    private void UpdateGhostShape(GameObject ghost)
    {
        // The ghost wraps the original trunk: its edge follows that outline and the body is that shape pushed outwards
        Transform ghostTransform = ghost.transform;
        Vector3 normal = ghostTransform.up;
        SampleSurfaceLine(ghostTransform.position - normal * CHOP_GHOST_HOVER, normal, true);

        // Blend the outline towards a smooth circle round the trunk, so the ghost keeps a generic curve and not every bump
        Vector3 axis = chopTarget.Tree.transform.position;
        float meanRadius = 0f;
        for (int i = 0; i < LINE_POINTS; i++) meanRadius += new Vector2(linePoints[i].x - axis.x, linePoints[i].z - axis.z).magnitude;
        meanRadius /= LINE_POINTS;

        for (int i = 0; i < LINE_POINTS; i++)
        {
            Vector3 outward = new(linePoints[i].x - axis.x, 0f, linePoints[i].z - axis.z);
            float strictRadius = outward.magnitude;
            if (strictRadius < 0.0001f) continue;

            float blendedRadius = Mathf.Lerp(meanRadius, strictRadius, GHOST_SHAPE_STRICTNESS);
            Vector3 blended = axis + outward / strictRadius * blendedRadius;
            linePoints[i] = new Vector3(blended.x, linePoints[i].y, blended.z);
        }

        int cols = LINE_POINTS - 1;
        int rows = GHOST_GRID_ROWS;
        Vector3 mid = linePoints[LINE_POINTS / 2];
        Vector3 farMid = mid + normal * (CHOP_GHOST_HOVER + CHOP_PREVIEW_HEIGHT);

        for (int row = 0; row <= rows; row++)
        {
            float along = row / (float)rows;
            for (int col = 0; col <= cols; col++)
            {
                Vector3 across = linePoints[col] - mid;
                Vector3 edge = mid + across + lineNormals[col] * CHOP_GHOST_HOVER;
                Vector3 far = farMid + across * GHOST_FAR_WIDTH;
                ghostVertices[row * (cols + 1) + col] = ghostTransform.InverseTransformPoint(Vector3.Lerp(edge, far, along));
            }
        }

        Mesh mesh = ghost.GetComponent<MeshFilter>().sharedMesh;
        mesh.vertices = ghostVertices;
        mesh.RecalculateBounds();
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
    private static readonly float MIN_POPUP_LITRES = 0.05f;
    private static readonly float CENTIMETRES_PER_METRE = 100f;
    private static readonly float CHOP_PREVIEW_HEIGHT = 0.12f;
    private static readonly float CHOP_GHOST_HOVER = 0.07f;
    private static readonly float GHOST_POSITION_SMOOTH_TIME = 0.06f;
    private static readonly float GHOST_ROTATION_SHARPNESS = 14f;
    private static readonly float CHOP_STRAFE_SPEED = 0.12f;
    private static readonly float CHOP_STRAFE_ACCELERATION = 2.5f;
    private static readonly int GHOST_GRID_ROWS = 6;
    private static readonly float GHOST_FAR_WIDTH = 1f;
    private static readonly float GHOST_SHAPE_STRICTNESS = 0.7f;
    private static readonly int FADE_TEXTURE_SIZE = 32;
    private static readonly float FADE_PLATEAU_END = 0.5f;
    private static readonly float FADE_EDGE_ALPHA = 0.5f;
    private static readonly int LINE_POINTS = 21;
    private static readonly float LINE_WIDTH = 0.012f;
    private static readonly float LINE_LIFT = 0.02f;
    private static readonly Color LINE_GREY = new(0.7f, 0.7f, 0.7f, 1f);
    private static readonly Color HOVER_GHOST_COLOR = new(0.7f, 0.7f, 0.7f, 0.28f);
    private static readonly float LINE_ALPHA = 0.53f;
    private static readonly float TARGET_LINE_ALPHA = 0.18f;
    private static readonly float LINE_EMISSION_SCALE = 0.6f;
    private static readonly int BASE_MAP_ID = Shader.PropertyToID("_BaseMap");
    private static readonly int EMISSION_COLOR_ID = Shader.PropertyToID("_EmissionColor");
    private static readonly float MAX_PLAYER_TARGET_DISTANCE = 10f;
    private static readonly float CHOP_BACK_OFFSET = 0.4f;
    private static readonly float CHOP_SIDE_OFFSET = 0.5f;
    private static readonly Vector2 CHOP_LOOK_OFFSET = new(-0.5f, -0.2f);
    private static readonly float MAX_CHOP_GROUND_DISTANCE = 1.5f;
    private static readonly float MIN_CHOP_GROUND_DISTANCE = 0.1f;
    private static readonly Color CHOP_RED_COLOR = new(1f, 0.2f, 0.15f, 0.64f);
    private static readonly Color CHOP_ORANGE_COLOR = new(1f, 0.55f, 0.1f, 0.64f);
    private static readonly Color CHOP_YELLOW_COLOR = new(1f, 0.9f, 0.15f, 0.64f);
    private static readonly Color CHOP_GREEN_COLOR = new(0.2f, 1f, 0.2f, 0.54f);
    private static readonly float AIM_GREEN_ACCURACY = 0.8f;

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
