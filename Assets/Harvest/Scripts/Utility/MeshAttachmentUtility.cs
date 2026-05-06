using UnityEngine;

public static class MeshAttachmentUtility
{
    public static void AlignTransforms(Transform target, Transform subject, Transform pivot)
    {
        subject.transform.rotation = target.rotation * Quaternion.Inverse(pivot.localRotation);
        subject.transform.position = target.position + (subject.transform.position - pivot.position);
    }

    public static void SetCollidersTrigger(GameObject mesh, bool isTrigger)
    {
        foreach (var col in mesh.GetComponentsInChildren<Collider>())
        {
            col.isTrigger = isTrigger;
        }
    }
}
