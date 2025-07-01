using System;
using UnityEngine;

[Serializable]
public class PlayerAnimator
{
    public void Init(Player player)
    {
        this.player = player;
    }

    private Player player;
}
