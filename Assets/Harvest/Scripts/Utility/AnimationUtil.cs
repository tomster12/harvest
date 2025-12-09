using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public enum Axis
{ Global, Local }

public static class AnimationUtil
{
    public static async Task MoveTo(CancellationToken ct, Transform tfm, Vector3 targetPosition, Axis axis, float duration, Func<float, float> easingFunction)
    {
        var startPosition = axis == Axis.Global ? tfm.position : tfm.localPosition;

        float startTime = Time.time;
        while (Time.time < startTime + duration)
        {
            ct.ThrowIfCancellationRequested();

            float t = Mathf.Clamp01((Time.time - startTime) / duration);
            t = easingFunction(t);
            if (axis == Axis.Global) tfm.position = Vector3.Lerp(startPosition, targetPosition, t);
            else tfm.localPosition = Vector3.Lerp(startPosition, targetPosition, t);

            await Task.Yield();
        }

        if (axis == Axis.Global) tfm.position = targetPosition;
        else tfm.localPosition = targetPosition;
    }

    public static async Task RotateTo(CancellationToken ct, Transform tfm, Quaternion targetRotation, Axis axis, float duration, Func<float, float> easingFunction)
    {
        var startRotation = axis == Axis.Global ? tfm.rotation : tfm.localRotation;

        float startTime = Time.time;
        while (Time.time < startTime + duration)
        {
            ct.ThrowIfCancellationRequested();

            float t = Mathf.Clamp01((Time.time - startTime) / duration);
            t = easingFunction(t);
            if (axis == Axis.Global) tfm.rotation = Quaternion.Slerp(startRotation, targetRotation, t);
            else tfm.localRotation = Quaternion.Slerp(startRotation, targetRotation, t);

            await Task.Yield();
        }

        if (axis == Axis.Global) tfm.rotation = targetRotation;
        else tfm.localRotation = targetRotation;
    }

    public static async Task MoveAndRotateTo(CancellationToken ct, Transform tfm, Transform target, Axis axis, float duration, Func<float, float> easingFunction)
            => await MoveAndRotateTo(ct, tfm, axis == Axis.Global ? target.position : target.localPosition, axis == Axis.Global ? target.rotation : target.localRotation, axis, duration, easingFunction);

    public static async Task MoveAndRotateTo(CancellationToken ct, Transform tfm, Vector3 targetPosition, Quaternion targetRotation, Axis axis, float duration, Func<float, float> easingFunction)
    {
        Vector3 startPosition = axis == Axis.Global ? tfm.position : tfm.localPosition;
        Quaternion startRotation = axis == Axis.Global ? tfm.rotation : tfm.localRotation;

        float startTime = Time.time;
        while (Time.time < startTime + duration)
        {
            ct.ThrowIfCancellationRequested();

            float t = Mathf.Clamp01((Time.time - startTime) / duration);
            t = easingFunction(t);
            if (axis == Axis.Global) tfm.SetPositionAndRotation(Vector3.Lerp(startPosition, targetPosition, t), Quaternion.Slerp(startRotation, targetRotation, t));
            else tfm.SetLocalPositionAndRotation(Vector3.Lerp(startPosition, targetPosition, t), Quaternion.Slerp(startRotation, targetRotation, t));

            await Task.Yield();
        }

        if (axis == Axis.Global) tfm.SetPositionAndRotation(targetPosition, targetRotation);
        else tfm.SetLocalPositionAndRotation(targetPosition, targetRotation);
    }
}
