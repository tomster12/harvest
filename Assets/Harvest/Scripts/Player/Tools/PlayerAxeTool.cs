using Unity.VisualScripting.ReorderableList;
using UnityEngine;
using static UnityEditor.Progress;

public class PlayerAxeTool : PlayerTool
{
    public const float MAX_PLAYER_TARGET_DISTANCE = 10f;
    public const float CHOP_OFFSET = 0.65f;
    public const float MAX_CHOP_GROUND_DISTANCE = 0.8f;
    public const float MIN_CHOP_GROUND_DISTANCE = 0.15f;

    public override void Equip(Player player, ItemInstance itemInstance)
    {
        base.Equip(player, itemInstance);

        // Create tool mesh
        var parent = player.Animator.GetAttachmentSlot(PlayerAttachmentSlot.Hand);
        toolMesh = GameObject.Instantiate(itemInstance.Data.MeshPrefab, parent);
        GameObject handlePoint = toolMesh.transform.Find("Handle Point").gameObject;
        toolMesh.transform.localPosition = -handlePoint.transform.localPosition;
        toolMesh.transform.localRotation = Quaternion.Inverse(handlePoint.transform.localRotation);

        // Make colliders trigger
        var colliders = toolMesh.GetComponentsInChildren<Collider>();
        foreach (var collider in colliders) collider.isTrigger = true;

        // Create preview
        chopPreview = GameObject.CreatePrimitive(PrimitiveType.Quad);
        chopPreview.transform.localScale = new Vector3(0.3f, 0.15f, 1f);
        Renderer renderer = chopPreview.GetComponent<Renderer>();
        renderer.material = AssetDatabase.GetMaterial("Chop Preview");
        chopPreview.SetActive(false);
    }

    public override void Unequip()
    {
        if (toolMesh != null) GameObject.Destroy(toolMesh);
        if (chopPreview != null) GameObject.Destroy(chopPreview);
    }

    public override void UpdateTool()
    {
        // If we're chopping dont update preview
        if (isChopping)
        {
            if (currentAction.IsRunning) return;
            isChopping = false;
            currentAction = null;
            chopFromPos = null;
        }

        chopPreview.SetActive(false);
        chopFromPos = null;

        // Find the first tree in range
        var hit = FindFirstValidTargetInRange();
        if (!hit.HasValue)
        {
            chopPreview.SetActive(false);
            return;
        }

        // See if we can chop anything valid
        if (!player.IsBlocked(PlayerBlockFlags.Movement | PlayerBlockFlags.Input))
        {
            Vector3 chopFromPosAbove = hit.Value.point + hit.Value.normal * CHOP_OFFSET;
            if (Physics.Raycast(chopFromPosAbove, Vector3.down, out RaycastHit groundHit, MAX_CHOP_GROUND_DISTANCE, LayerMask.GetMask("Ground")))
            {
                if (groundHit.distance > MIN_CHOP_GROUND_DISTANCE)
                {
                    // We can chop so preview it first
                    chopPreview.transform.SetPositionAndRotation(hit.Value.point, Quaternion.LookRotation(-Vector3.up, hit.Value.normal));
                    chopFromPos = groundHit.point;
                    chopPreview.SetActive(true);

                    // Otherwise if clicking then perform the chop action
                    if (player.Input.IsMousePressed)
                    {
                        currentAction = new ChopTreeAction(hit.Value, groundHit.point);
                        player.Actions.StartAction(currentAction);
                        isChopping = true;
                        player.Input.IsMousePressed = false;
                    }
                }
            }
        }
    }

    public override void DebugGizmos()
    {
        if (chopFromPos.HasValue)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(chopFromPos.Value, 0.1f);
        }
    }

    private GameObject toolMesh;
    private GameObject chopPreview;
    private ChopTreeAction currentAction;
    private Vector3? chopFromPos;
    private bool isChopping = false;

    private RaycastHit? FindFirstValidTargetInRange()
    {
        // Find the first raycast hit that is a valid target within the specified distance
        var hits = player.Input.RaycastHits;
        if (hits == null || hits.Length == 0) return null;
        foreach (var hit in hits)
        {
            if (IsValidTarget(hit))
            {
                float dist = Vector3.Distance(player.transform.position, hit.point);
                if (dist <= MAX_PLAYER_TARGET_DISTANCE) return hit;
            }
        }
        return null;
    }

    private bool IsValidTarget(RaycastHit hit)
    {
        return hit.collider.CompareTag("Choppable");
    }
}
