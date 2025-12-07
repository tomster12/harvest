using UnityEngine;

[RequireComponent(typeof(Camera))]
public class TreeChopDebug : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera cam;

    [Header("Chop Settings")]
    public float maxDistance = 10f;
    public float hitDepth = 1f;
    public float hitWidth = 1f;
    public float hitHeight = 1f;
    public KeyCode chopKey = KeyCode.Mouse0;

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
        hitRay = cam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(hitRay, out hit, maxDistance))
        {
            hitTransform = hit.transform;
            hitPoint = hit.point;
            hitTree = hitTransform.GetComponentInParent<ChoppableTree>();

            if (Input.GetKeyDown(chopKey) && hitPoint != null && hitTree != null)
            {
                hitTree.Hit((Vector3)hitPoint, hitDepth, hitWidth, hitHeight);
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
