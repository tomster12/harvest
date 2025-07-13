using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine;

public class UIUtil : MonoBehaviour
{
    public static List<RaycastResult> GetEventSystemRaycastResults()
    {
        PointerEventData eventData = new(EventSystem.current);
        eventData.position = Input.mousePosition;
        List<RaycastResult> raycastResults = new();
        EventSystem.current.RaycastAll(eventData, raycastResults);
        return raycastResults;
    }
}
