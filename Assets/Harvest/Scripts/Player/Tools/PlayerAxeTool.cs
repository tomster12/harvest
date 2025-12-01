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
    public static float CHOP_SWAY_NOISE_MAGNITUDE = 0.0025f;
    public static float CHOP_SWAY_NOISE_FREQUENCY_MIN = 0.4f;
    public static float CHOP_SWAY_NOISE_FREQUENCY = 0.4f;
    public static float CHOP_SWAY_SHAKE_MAGNITUDE = 0.02f;
    public static float CHOP_SWAY_SHAKE_FREQUENCY = 8f;

    public override void Equip(Player player, ItemInstance itemInstance)
    {
        base.Equip(player, itemInstance);

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

    public override void UpdateTool()
    {
        /*
        // Dont update preview during chop
        if (isChopping)
        {
            if (currentAction.IsRunning) return;
            isChopping = false;
            currentAction = null;
        }

        chopPreview.SetActive(false);

        if (player.IsRestricted(PlayerRestrictionFlag.DoMovement | PlayerRestrictionFlag.DoAction)) return;

        // Find a hovered valid target to chop
        (RaycastHit, ChoppableTree)? maybeTree = GetFirstRaycastChoppableTree();
        if (!maybeTree.HasValue) return;
        RaycastHit hit = maybeTree.Value.Item1;
        ChoppableTree tree = maybeTree.Value.Item2;

        // Get the exact chop position from the tree
        //ChopTarget target = tree.GetChopTarget(hit);

        // Check the hovered pos has a position in front it can be chopped from
        Vector3 aboveChopFromPos = target.pos + target.normal * CHOP_BACK_OFFSET;
        aboveChopFromPos += Vector3.Cross(target.normal, Vector3.up) * CHOP_SIDE_OFFSET;
        if (!Physics.Raycast(aboveChopFromPos, Vector3.down, out RaycastHit groundHit, MAX_CHOP_GROUND_DISTANCE, LayerMask.GetMask("Ground"))) return;
        if (groundHit.distance < MIN_CHOP_GROUND_DISTANCE) return;
        chopFromPos = groundHit.point;

        // Show chop preview on valid position
        chopPreview.transform.SetPositionAndRotation(target.pos, Quaternion.LookRotation(-Vector3.up, target.normal));
        chopPreview.SetActive(true);

        // Perform chop on click
        if (player.Input.IsMousePressed)
        {
            currentAction = new ChopTreeAction(player, tree, target, chopFromPos, toolMesh, chopPreview);
            player.Actions.StartAction(currentAction);
            isChopping = true;
            player.Input.IsMousePressed = false;
        }
        */
    }

    public override void DebugGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(chopFromPos, 0.1f);
    }

    private GameObject toolMesh;
    private GameObject chopPreview;
    private ChopTreeAction currentAction;
    private Vector3 chopFromPos;
    private bool isChopping = false;

    private (RaycastHit, ChoppableTree)? GetFirstRaycastChoppableTree()
    {
        foreach (var hit in player.Input.RaycastHits)
        {
            if (hit.transform.gameObject.TryGetComponent(out ChoppableTree tree))
            {
                float dist = Vector3.Distance(player.transform.position, hit.point);
                if (dist <= MAX_PLAYER_TARGET_DISTANCE) return (hit, tree);
            }
        }
        return null;
    }
}
