using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Camera))]
public class TreeChopDebug : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera cam;

    [Header("Chop Settings")]
    public float maxDistance = 10f;
    public float strength = 5f;
    public float sharpness = 1f;
    public float accuracy = 1f;
    public float bitWidth = 0.5f;

    [Header("Debug")]
    public bool showDebug = true;
    public Color debugRayColor = Color.red;
    public float hitMarkerSize = 0.05f;

    private Ray hitRay;
    private RaycastHit hit;
    private Transform hitTransform;
    private Vector3? hitPoint;
    private ChoppableTree hitTree;

    private void Update()
    {
        hitRay = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (Physics.Raycast(hitRay, out hit, maxDistance))
        {
            hitTransform = hit.transform;
            hitPoint = hit.point;
            hitTree = hitTransform.GetComponentInParent<ChoppableTree>();

            if (Mouse.current.leftButton.wasPressedThisFrame && hitPoint != null && hitTree != null)
            {
                hitTree.Hit(new TreeHitInput
                {
                    Position = (Vector3)hitPoint,
                    Strength = strength,
                    Sharpness = sharpness,
                    Accuracy = accuracy,
                    BitWidth = bitWidth
                });
            }
        }
        else
        {
            hitTransform = null;
            hitPoint = null;
            hitTree = null;
        }
    }

    private void OnDrawGizmos()
    {
        if (showDebug)
        {
            Gizmos.color = debugRayColor;
            Gizmos.DrawLine(hitRay.origin, hitRay.origin + hitRay.direction * maxDistance);

            if (hitPoint != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawSphere((Vector3)hitPoint, hitMarkerSize);
            }
        }
    }
}
