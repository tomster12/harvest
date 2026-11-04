using UnityEngine;
using UnityEngine.InputSystem;

public class OrbitCamera : MonoBehaviour
{
    public Transform target;
    public float zoomSpeed = 2f;
    public float orbitSpeed = 50f;
    public float moveSpeed = 2f;
    public Vector2 distanceLimits = new(2f, 15f);

    private float distance;
    private Vector3 offset;
    private float yaw;
    private float pitch;

    private static readonly float SCROLL_PER_NOTCH = 120f;

    private void Start()
    {
        if (!target)
        {
            Debug.LogError("OrbitCamera: No target assigned.");
            enabled = false;
            return;
        }

        Vector3 dir = target.position - transform.position;
        distance = Mathf.Clamp(dir.magnitude, distanceLimits.x, distanceLimits.y);

        Quaternion currentRotation = Quaternion.LookRotation(dir);
        Vector3 eulerAngles = currentRotation.eulerAngles;
        pitch = eulerAngles.x > 180f ? eulerAngles.x - 360f : eulerAngles.x;
        yaw = eulerAngles.y;
    }

    private void Update()
    {
        var keyboard = Keyboard.current;

        // Orbit around target with WASD
        if (keyboard.wKey.isPressed) pitch += orbitSpeed * Time.deltaTime;
        if (keyboard.sKey.isPressed) pitch -= orbitSpeed * Time.deltaTime;
        if (keyboard.aKey.isPressed) yaw += orbitSpeed * Time.deltaTime;
        if (keyboard.dKey.isPressed) yaw -= orbitSpeed * Time.deltaTime;
        pitch = Mathf.Clamp(pitch, -89f, 89f);

        // Move up/down with Q / E
        if (keyboard.qKey.isPressed) target.position += moveSpeed * Time.deltaTime * Vector3.down;
        if (keyboard.eKey.isPressed) target.position += moveSpeed * Time.deltaTime * Vector3.up;

        // Scroll to zoom
        float scroll = Mouse.current.scroll.ReadValue().y / SCROLL_PER_NOTCH * 0.1f;
        distance -= scroll * zoomSpeed;
        distance = Mathf.Clamp(distance, distanceLimits.x, distanceLimits.y);

        // Apply orbit transform
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);
        Vector3 desiredPos = target.position - rotation * Vector3.forward * distance;

        transform.position = desiredPos;
        transform.LookAt(target);
    }
}
