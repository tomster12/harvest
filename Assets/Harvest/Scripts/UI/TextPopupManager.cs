using UnityEngine;
using System;

public class TextPopupManager : MonoBehaviour
{
    public static void SpawnTextPopup(Vector3 pos, string text)
    {
        if (!VerifyInstance()) return;

        instance.SpawnPopupObject(pos, text);
    }

    private static bool VerifyInstance()
    {
        if (instance == null)
        {
            Debug.LogError("TextPopupManager not initialized");
            return false;
        }
        return true;
    }

    private static TextPopupManager instance;


    [Header("Prefabs")]
    [SerializeField] private GameObject popupPrefab;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Debug.LogError("TextPopupManager Instance already initialised");
            Destroy(gameObject);
            return;
        }
        instance = this;
    }

    private void SpawnPopupObject(Vector3 pos, string text)
    {
        var go = Instantiate(popupPrefab, pos, Quaternion.identity);
        var popup = go.GetComponent<TextPopupUI>();
        popup.StartAnimation(text, 1.0f, 0.3f);
    }
}
