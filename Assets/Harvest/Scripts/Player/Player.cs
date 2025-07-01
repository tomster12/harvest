using UnityEngine;

public class Player : MonoBehaviour
{
    public PlayerInput input;
    public new PlayerCamera camera;
    public PlayerInteractor interactor;
    public PlayerMovement movement;
    public PlayerAnimator animator;

    public void OnSpawn()
    {
        input.Init(this);
        camera.Init(this);
        interactor.Init(this);
        movement.Init(this);
        animator.Init(this);
    }

    private void Update()
    {
        input.HandleInput();
        camera.UpdateCamera();
        interactor.UpdateInteractions();
        animator.UpdateAnimation();
    }

    private void FixedUpdate()
    {
        camera.FixedUpdateCamera();
        movement.FixedUpdateMovement();
    }
}
