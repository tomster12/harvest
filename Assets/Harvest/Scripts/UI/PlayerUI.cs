using UnityEngine;

public class PlayerUI : MonoBehaviour
{
    public static PlayerUI Instance { get; private set; }

    public static RectTransform GetContainer()
    {
        return (RectTransform)Instance.transform;
    }

    public static T InstantiateElement<T>(GameObject prefab, string name, Transform parent = null)
    {
        if (parent == null) parent = GetContainer();
        GameObject UIObject = Instantiate(prefab, parent);
        UIObject.name = name;
        return UIObject.GetComponent<T>();
    }

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
