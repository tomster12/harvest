using TMPro;
using UnityEngine;

public class TextPopupUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TextMeshPro text;

    [Header("Config")]
    [SerializeField] private float defaultDuration = 1.0f;
    [SerializeField] private float defaultTargetOffset = 0.3f;

    private float duration;
    private float targetOffset;
    private Vector3 startPos;
    private float currentTime;

    public void StartAnimation(string text, float? duration = null, float? targetOffset = null)
    {
        duration ??= defaultDuration;
        targetOffset ??= defaultTargetOffset;

        this.text.text = text;
        this.duration = duration.Value;
        this.targetOffset = targetOffset.Value;
        currentTime = 0.0f;
        startPos = transform.position;
    }

    private void Update()
    {
        var t = Mathf.Clamp01(currentTime / duration);
        t = Easing.EaseOutQuad(t);
        transform.position = startPos + t * targetOffset * Vector3.up;

        currentTime += Time.deltaTime;
        if (currentTime >= duration)
        {
            Destroy(gameObject);
        }
    }
}
