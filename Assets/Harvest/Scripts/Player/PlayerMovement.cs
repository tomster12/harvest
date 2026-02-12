using System;
using UnityEngine;

[Serializable]
public class PlayerMovement
{
    public Vector3 TargetFacingDir => targetFacingDir;
    public Stat MovementSpeed => movementSpeed;
    public bool IsMoving { get; private set; }
    public bool HasReachedTarget => !setTargetPosition.HasValue || (setTargetPosition.Value - player.transform.position).sqrMagnitude <= targetThreshold;
    public bool IsFacingTarget => Vector3.Angle(TargetFacingDir, player.transform.forward) <= rotationThreshold;

    public void Init(Player player)
    {
        this.player = player;
        targetFacingDir = player.transform.forward;
        inputDirection = null;
        setTargetPosition = null;
        IsMoving = false;
    }

    public void SetMovementTarget(Vector3 position, float threshold = 0.1f)
    {
        // Overwrite and set to moving towards a position
        inputDirection = null;
        setTargetPosition = position;
        targetThreshold = threshold;
    }

    public void SetMovementInput(Vector3 dir)
    {
        // Overwrite and set to moving in a direction
        inputDirection = dir.normalized;
        setTargetPosition = null;
    }

    public void SetFacingTarget(Vector3? position, float speedMult = 1.0f, bool prioritise = false)
    {
        // Set the target facing direction towards a position
        if (!position.HasValue)
        {
            setFacingDir = null;
            setFacingDirPrioritise = false;
        }
        else
        {
            Vector3 directDir = position.Value - player.transform.position;
            Vector3 flatDir = Vector3.ProjectOnPlane(directDir, Vector3.up);
            rotationSpeedMult = speedMult;
            setFacingDir = flatDir.normalized;
            setFacingDirPrioritise = prioritise;
        }
    }

    public void FixedApplyMovement()
    {
        // Reset target position if reached
        if (setTargetPosition.HasValue && HasReachedTarget)
        {
            setTargetPosition = null;
        }

        // Check if moving in direction or towards position, prioritising input movement
        Vector3? finalMovementDir = null;
        if (inputDirection.HasValue)
        {
            finalMovementDir = inputDirection.Value;
        }
        else if (setTargetPosition.HasValue)
        {
            Vector3 directDir = setTargetPosition.Value - player.transform.position;
            Vector3 flatDir = Vector3.ProjectOnPlane(directDir, Vector3.up);
            finalMovementDir = flatDir.normalized;
        }

        // Default to set facing dir
        if (setFacingDir.HasValue) targetFacingDir = setFacingDir.Value;

        if (finalMovementDir.HasValue)
        {
            // Naively move in target movement direction
            IsMoving = true;
            Vector3 movement = movementSpeed.Evaluate() * Time.fixedDeltaTime * finalMovementDir.Value;
            rb.MovePosition(rb.position + movement);

            // Then overwrite final direction if configured
            if (!setFacingDirPrioritise)
            {
                Vector3 movementFacingDir = new(finalMovementDir.Value.x, 0f, finalMovementDir.Value.z);
                targetFacingDir = movementFacingDir;
                setFacingDir = null;
            }
        }
        else IsMoving = false;
        // Naively rotate towards the final facing direction
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

    public void LateClearInputs()
    {
        inputDirection = null;
    }

    [Header("References")]
    [SerializeField] private Rigidbody rb;

    [Header("Config")]
    [SerializeField] private Stat movementSpeed = new(3.5f);
    [SerializeField] private float rotationSpeed = 450f;
    [SerializeField] private float rotationThreshold = 0.1f;

    private Player player;
    private Vector3? inputDirection;
    private Vector3? setTargetPosition;
    private Vector3? setFacingDir;
    private Vector3 targetFacingDir;
    private bool setFacingDirPrioritise = false;
    private float rotationSpeedMult = 1.0f;
    private float targetThreshold = 0.01f;
}
