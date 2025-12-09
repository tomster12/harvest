using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public enum Axis
{ Global, Local }

public static class AnimationUtil
{
    public static async Task MoveTo(CancellationToken ct, Transform tfm, Vector3 targetPosition, float duration, Axis axis = Axis.Global, Func<float, float> easingFunction = null)
    {
        var startPosition = axis == Axis.Global ? tfm.position : tfm.localPosition;

        float startTime = Time.time;
        while (Time.time < startTime + duration)
        {
            ct.ThrowIfCancellationRequested();

            float t = Mathf.Clamp01((Time.time - startTime) / duration);
            if (easingFunction != null) t = easingFunction(t);
            if (axis == Axis.Global) tfm.position = Vector3.Lerp(startPosition, targetPosition, t);
            else tfm.localPosition = Vector3.Lerp(startPosition, targetPosition, t);

            await Task.Yield();
        }

        if (axis == Axis.Global) tfm.position = targetPosition;
        else tfm.localPosition = targetPosition;
    }

    public static async Task RotateTo(CancellationToken ct, Transform tfm, Quaternion targetRotation, float duration, Axis axis = Axis.Global, Func<float, float> easingFunction = null)
    {
        var startRotation = axis == Axis.Global ? tfm.rotation : tfm.localRotation;

        float startTime = Time.time;
        while (Time.time < startTime + duration)
        {
            ct.ThrowIfCancellationRequested();

            float t = Mathf.Clamp01((Time.time - startTime) / duration);
            if (easingFunction != null) t = easingFunction(t);
            if (axis == Axis.Global) tfm.rotation = Quaternion.Slerp(startRotation, targetRotation, t);
            else tfm.localRotation = Quaternion.Slerp(startRotation, targetRotation, t);

            await Task.Yield();
        }

        if (axis == Axis.Global) tfm.rotation = targetRotation;
        else tfm.localRotation = targetRotation;
    }

    public static async Task MoveAndRotateTo(CancellationToken ct, Transform tfm, Transform target, float duration, Axis axis = Axis.Global, Func<float, float> easingFunction = null)
            => await MoveAndRotateTo(ct, tfm, axis == Axis.Global ? target.position : target.localPosition, axis == Axis.Global ? target.rotation : target.localRotation, duration, axis, easingFunction);

    public static async Task MoveAndRotateTo(CancellationToken ct, Transform tfm, Vector3 targetPosition, Quaternion targetRotation, float duration, Axis axis = Axis.Global, Func<float, float> easingFunction = null)
    {
        Vector3 startPosition = axis == Axis.Global ? tfm.position : tfm.localPosition;
        Quaternion startRotation = axis == Axis.Global ? tfm.rotation : tfm.localRotation;

        float startTime = Time.time;
        while (Time.time < startTime + duration)
        {
            ct.ThrowIfCancellationRequested();

            float t = Mathf.Clamp01((Time.time - startTime) / duration);
            if (easingFunction != null) t = easingFunction(t);
            if (axis == Axis.Global) tfm.SetPositionAndRotation(Vector3.Lerp(startPosition, targetPosition, t), Quaternion.Slerp(startRotation, targetRotation, t));
            else tfm.SetLocalPositionAndRotation(Vector3.Lerp(startPosition, targetPosition, t), Quaternion.Slerp(startRotation, targetRotation, t));

            await Task.Yield();
        }

        if (axis == Axis.Global) tfm.SetPositionAndRotation(targetPosition, targetRotation);
        else tfm.SetLocalPositionAndRotation(targetPosition, targetRotation);
    }
}
