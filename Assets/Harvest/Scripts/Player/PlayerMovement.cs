using System;
using Unity.VisualScripting;
using UnityEngine;

[Serializable]
public class PlayerMovement
{
    public Vector3 TargetForward { get; private set; } = Vector3.zero;

    public void Init(Player player)
    {
        this.player = player;
    }

    public void MoveInDirection(Vector3 dir)
    {
        // Overwrite and set to moving in a direction
        inputDirection = dir;
        targetPosition = null;
    }

    public bool MoveTowardsPosition(Vector3 position)
    {
        // Overwrite and set to moving towards a position
        inputDirection = null;
        targetPosition = position;
        return (targetPosition.Value - player.transform.position).sqrMagnitude > 0.01f;
    }

    public void FixedUpdate()
    {
        Vector3 finalDir = Vector3.zero;

        // Either move in direction or towards target position
        if (inputDirection.HasValue) finalDir = inputDirection.Value.normalized;
        else if (targetPosition.HasValue) finalDir = (targetPosition.Value - player.transform.position).normalized;

        // Face towards direction and move towards target position if set
        if (finalDir.sqrMagnitude > 0.01f)
        {
            TargetForward = finalDir;
            Vector3 movement = movementSpeed * Time.fixedDeltaTime * finalDir;
            rb.MovePosition(rb.position + movement);
        }

        // Rotate player towards facing direction
        Vector3 flatForward = new Vector3(TargetForward.x, 0f, TargetForward.z);
        if (flatForward.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(flatForward, Vector3.up);
            Quaternion newRot = Quaternion.RotateTowards(rb.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
            rb.MoveRotation(newRot);
        }

        // Reset target position if reached
        if (targetPosition.HasValue && ReachedTarget()) targetPosition = null;
    }

    public void LateUpdate()
    {
        inputDirection = null;
    }

    public bool ReachedTarget(float threshold = 0.01f)
    {
        return !targetPosition.HasValue || (targetPosition.Value - player.transform.position).sqrMagnitude <= threshold;
    }

    [Header("References")]
    [SerializeField] private Rigidbody rb;

    [Header("Config")]
    [SerializeField] private float movementSpeed = 5f;
    [SerializeField] private float rotationSpeed = 200f;

    private Player player;
    private Vector3? inputDirection;
    private Vector3? targetPosition;
}
