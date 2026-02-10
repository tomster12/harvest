using System;
using UnityEngine;

[Serializable]
public class PlayerMovement
{
    public Vector3 TargetFacingDir { get; private set; } = Vector3.zero;
    public bool IsMoving { get; private set; }
    public bool HasReachedTarget => !targetPosition.HasValue || (targetPosition.Value - player.transform.position).sqrMagnitude <= targetThreshold;
    public bool IsFacingTarget => Vector3.Angle(TargetFacingDir, player.transform.forward) <= rotationThreshold;

    public void Init(Player player)
    {
        this.player = player;
        TargetFacingDir = player.transform.forward;
        inputDirection = null;
        targetPosition = null;
        IsMoving = false;
    }

    public void MoveInDirection(Vector3 dir)
    {
        // Overwrite and set to moving in a direction
        inputDirection = dir.normalized;
        targetPosition = null;
    }

    public void SetMovementTarget(Vector3 position, float threshold = 0.1f)
    {
        // Overwrite and set to moving towards a position
        inputDirection = null;
        targetPosition = position;
        targetThreshold = threshold;
    }

    public void SetFacingTarget(Vector3 position, float speedMult = 1.0f)
    {
        // Set the target facing direction towards a position
        Vector3 directDir = position - player.transform.position;
        Vector3 flatDir = Vector3.ProjectOnPlane(directDir, Vector3.up);
        rotationSpeedMult = speedMult;
        TargetFacingDir = flatDir.normalized;
    }

    public void FixedUpdateMovement()
    {
        // Reset target position if reached
        if (targetPosition.HasValue && HasReachedTarget) targetPosition = null;

        // Check if moving in direction or towards position
        // Prioritise input but dont clear the target
        Vector3? finalDir = null;
        if (inputDirection.HasValue)
        {
            finalDir = inputDirection.Value;
        }
        else if (targetPosition.HasValue)
        {
            Vector3 directDir = targetPosition.Value - player.transform.position;
            Vector3 flatDir = Vector3.ProjectOnPlane(directDir, Vector3.up);
            finalDir = flatDir.normalized;
        }

        // We are moving in some direction
        if (finalDir.HasValue)
        {
            // Naively move in target direction
            IsMoving = true;
            Vector3 movement = movementSpeed * Time.fixedDeltaTime * finalDir.Value;
            rb.MovePosition(rb.position + movement);

            // Overwrite target rotation with on the movement direction
            Vector3 movementFacingDir = new(finalDir.Value.x, 0f, finalDir.Value.z);
            TargetFacingDir = movementFacingDir;
        }
        else IsMoving = false;

        // Rotate towards the target direction
        Quaternion targetRotation = Quaternion.LookRotation(TargetFacingDir, Vector3.up);
        if (!IsFacingTarget)
        {
            Quaternion newRot = Quaternion.RotateTowards(rb.rotation, targetRotation, rotationSpeedMult * rotationSpeed * Time.fixedDeltaTime);
            rb.MoveRotation(newRot);
        }
        else
        {
            rb.rotation = targetRotation;
            rotationSpeedMult = 1.0f;
        }
    }

    public void LateUpdateMovement()
    {
        inputDirection = null;
    }

    [Header("References")]
    [SerializeField] private Rigidbody rb;

    [Header("Config")]
    [SerializeField] private float movementSpeed = 3.5f;
    [SerializeField] private float rotationSpeed = 450f;
    [SerializeField] private float rotationThreshold = 0.1f;

    private Player player;
    private Vector3? inputDirection;
    private Vector3? targetPosition;
    private float rotationSpeedMult = 1.0f;
    private float targetThreshold = 0.01f;
}
