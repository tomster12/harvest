using UnityEngine;

public class DraggableObject : MonoBehaviour
{
    public void OnHoverEnter()
    {
        outline.enabled = true;
    }

    public void OnHoverExit()
    {
        outline.enabled = false;
    }

    public Grab GetGrab(RaycastHit hit)
    {
        return new()
        {
            Offset = transform.InverseTransformPoint(hit.point),
            Reference = transform
        };
    }

    public bool DragTo(Grab grab, Vector3 pos, float forceAmount, float maxDistance)
    {
        var resolved = grab.ResolvePosition();
        var dir = pos - resolved;

        var dirDistance = dir.magnitude;
        var midDistance = maxDistance * 0.5f;
        var finalForceAmount = (dirDistance * dirDistance) / (midDistance * midDistance);

        var force = finalForceAmount * forceAmount * dir.normalized;
        rb.AddForceAtPosition(force, resolved);

        return dirDistance < maxDistance;
    }

    [Header("References")]
    [SerializeField] private Outline outline;
    [SerializeField] private Rigidbody rb;

    private void Awake()
    {
        outline.enabled = false;
    }

    public class Grab
    {
        public Vector3 Offset { get; set; }
        public Transform Reference { get; set; }

        public Vector3 ResolvePosition()
        {
            return Reference.TransformPoint(Offset);
        }
    }
}
