using System;
using UnityEngine;

[Serializable]
public class PlayerAnimator
{
    public void Init(Player player)
    {
        this.player = player;
    }

    public void UpdateAnimation()
    {
        // Squish character to show sprinting
        float squishAmount = player.input.IsInputtingSprint ? 0.9f : 1.0f;
        player.transform.localScale = new Vector3(1, squishAmount, 1);
    }

    private Player player;
}
