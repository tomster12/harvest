using UnityEngine;

public class PlayerAxeTool : PlayerTool
{
    public static float MAX_PLAYER_TARGET_DISTANCE = 10f;
    public static float CHOP_BACK_OFFSET = 0.4f;
    public static float CHOP_SIDE_OFFSET = 0.5f;
    public static Vector2 CHOP_LOOK_OFFSET = new(-0.5f, -0.2f);
    public static float MAX_CHOP_GROUND_DISTANCE = 0.8f;
    public static float MIN_CHOP_GROUND_DISTANCE = 0.15f;
    public static Color CHOP_GOOD_COLOR = new(0.2f, 1f, 0.2f, 0.5f);
    public static Color CHOP_BAD_COLOR = new(1f, 0.2f, 0.2f, 0.5f);
    public static float CHOP_SWAY_SCREEN_MAX = 0.03f;
    public static float CHOP_SWAY_WORLD_MAX = 0.15f;
    public static float CHOP_SWAY_NOISE_CLAMP = 0.1f;
    public static float CHOP_SWAY_NOISE_MAGNITUDE = 0.004f;
    public static float CHOP_SWAY_NOISE_FREQUENCY_MIN = 0.4f;
    public static float CHOP_SWAY_NOISE_FREQUENCY = 0.4f;
    public static float CHOP_SWAY_SHAKE_MAGNITUDE = 0.02f;
    public static float CHOP_SWAY_SHAKE_FREQUENCY = 8f;

    public PlayerAxeTool(Player player, ItemInstance itemInstance) : base(player, itemInstance)
    { }

    public override void Equip()
    {
        // Create tool mesh attached to the left hand
        var leftHandSlot = player.Animator.GetAttachmentSlot("Left Hand");
        toolMesh = GameObject.Instantiate(itemInstance.Data.MeshPrefab, leftHandSlot);

        // Set the local offset to match the handle point
        Transform toolHandlePoint = toolMesh.transform.Find("Handle Point");
        toolMesh.transform.localPosition = -toolHandlePoint.localPosition;
        toolMesh.transform.localRotation = toolHandlePoint.localRotation;

        // Make colliders trigger
        var colliders = toolMesh.GetComponentsInChildren<Collider>();
        foreach (var collider in colliders) collider.isTrigger = true;

        // Create mouse preview
        chopPreview = GameObject.CreatePrimitive(PrimitiveType.Quad);
        chopPreview.transform.localScale = new Vector3(0.3f, 0.4f, 1f);
        Renderer renderer = chopPreview.GetComponent<Renderer>();
        renderer.sharedMaterial = AssetDatabase.GetMaterial("Chop Preview");
        chopPreview.SetActive(false);
    }

    public override void Unequip()
    {
        if (toolMesh != null) GameObject.Destroy(toolMesh);
        if (chopPreview != null) GameObject.Destroy(chopPreview);
    }

    public override void Update()
    {
        // Dont update preview during chop
        if (isChopping)
        {
            if (chopAction.IsRunning) return;
            isChopping = false;
            chopAction = null;
        }

        chopPreview.SetActive(false);

        if (player.IsRestricted(Player.ActionRestriction.Movement | Player.ActionRestriction.Action)) return;

        // Find a hovered valid target to chop
        TreeTarget target = FindTreeTarget();
        if (target == null) return;

        // Raycast out, to the side, and down to find a valid chop from position
        Vector3 awayFromChopPos = target.Hit.point + target.Hit.normal * CHOP_BACK_OFFSET;
        awayFromChopPos += Vector3.Cross(target.Hit.normal, Vector3.up) * CHOP_SIDE_OFFSET;
        
        if (!Physics.Raycast(awayFromChopPos, Vector3.down, out RaycastHit playerChopFromHit, MAX_CHOP_GROUND_DISTANCE, LayerMask.GetMask("Ground"))) return;
        if (playerChopFromHit.distance < MIN_CHOP_GROUND_DISTANCE) return;
        playerChopFromPos = playerChopFromHit.point;

        // Show chop preview on valid position
        chopPreview.transform.SetPositionAndRotation(target.Hit.point, Quaternion.LookRotation(-Vector3.up, target.Hit.normal));
        chopPreview.SetActive(true);

        // Perform chop on click
        if (player.Input.IsMousePressed)
        {
            chopAction = new PlayerAxeChopAction(player, target.Tree, target, playerChopFromPos, toolMesh, chopPreview);
            player.Actions.StartAction(chopAction);
            isChopping = true;
            player.Input.IsMousePressed = false;
        }
    }

    public override void DebugGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(playerChopFromPos, 0.1f);
    }

    private GameObject toolMesh;
    private GameObject chopPreview;
    private PlayerAxeChopAction chopAction;
    private Vector3 playerChopFromPos;
    private bool isChopping = false;

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

    public class TreeTarget
    {
        public RaycastHit Hit;
        public Vector3 HitPointLocal;
        public ChoppableTree Tree;
    }
}
