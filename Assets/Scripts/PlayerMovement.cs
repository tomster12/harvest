using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public bool IsMoving => inputDir.sqrMagnitude > 0.01f;
    public Vector3 InputDir => inputDir.normalized;

    [Header("References")]
    [SerializeField] private PlayerCamera playerCamera = null;

    [Header("Config")]
    [SerializeField] private float movementSpeed = 5f;
    [SerializeField] private float sprintMultiplier = 1.2f;
    [SerializeField] private float rotationSpeed = 200f;

    private Vector3 inputDir = Vector3.zero;
    private Vector3 facingDir = Vector3.zero;

    private bool IsSprinting => Input.GetKey(KeyCode.LeftShift);

    private void Update()
    {
        UpdateInput();
        UpdateVisuals();
    }

    private void UpdateInput()
    {
        // Cast camera transform onto flat plane
        // Receive input from the player and cast on the flat plane
        inputDir = Vector3.zero;
        Vector3 forwardDir = Vector3.ProjectOnPlane(playerCamera.CameraTransform.forward, Vector3.up).normalized;
        inputDir += Input.GetAxisRaw("Horizontal") * playerCamera.CameraTransform.right;
        inputDir += Input.GetAxisRaw("Vertical") * forwardDir;
        inputDir = inputDir.normalized;

        // Only update the last position if we have moved
        if (inputDir.sqrMagnitude != 0) facingDir = inputDir;
    }

    private void UpdateVisuals()
    {
        // Squish character to show sprinting
        float squishAmount = IsSprinting ? 0.9f : 1.0f;
        transform.localScale = new Vector3(1, squishAmount, 1);
    }

    private void FixedUpdate()
    {
        UpdateMovement();
    }

    private void UpdateMovement()
    {
        // Rotate towards input direction
        if (facingDir.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(facingDir, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
        }

        if (!IsMoving) return;

        // Move in input direction
        float speed = IsSprinting ? movementSpeed * sprintMultiplier : movementSpeed;
        transform.position += speed * Time.fixedDeltaTime * inputDir;
    }
}
