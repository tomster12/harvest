using UnityEngine;

public class DeleteOnAwake : MonoBehaviour
{
    private void Awake()
    {
        DestroyImmediate(gameObject);
    }
}
