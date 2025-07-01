using System;
using UnityEngine;

[Serializable]
public class PlayerMovement
{
    public Vector3 TargetForward { get; private set; } = Vector3.zero;

    public void Init(Player player)
    {
        this.player = player;
    }

    public void FixedUpdateMovement()
    {
        // Only update the facing direction if there is movement input
        if (player.input.IsInputtingMovement) TargetForward = player.input.InputMovement;

        // Rotate player towards facing direction
        if (TargetForward.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(TargetForward, Vector3.up);
            player.transform.rotation = Quaternion.RotateTowards(player.transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
        }

        if (player.input.IsInputtingMovement)
        {
            // Move in input direction
            float speed = player.input.IsInputtingSprint ? movementSpeed * sprintMultiplier : movementSpeed;
            player.transform.position += speed * Time.fixedDeltaTime * player.input.InputMovement;
        }
    }

    [Header("Movement Config")]
    [SerializeField] private float movementSpeed = 5f;
    [SerializeField] private float sprintMultiplier = 1.2f;
    [SerializeField] private float rotationSpeed = 200f;

    private Player player;
}
