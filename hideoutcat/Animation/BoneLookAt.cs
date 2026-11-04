using JetBrains.Annotations;
using UnityEngine;

namespace HideoutCat.Animation;

public class BoneLookAt : MonoBehaviour
{
    public Transform? targetLookAt;
    public Transform? bone;

    public bool useAngleLimits;

    public float smoothTime = 0.2f;
    public float resetSmoothTime = 0.5f;
    public float weight = 1f;

    public Vector3 customUpVector = Vector3.zero;
    public Vector3 maxAngleLimits = new(45f, 45f, 45f);
    public Vector3 minAngleLimits = new(-45f, -45f, -45f);
    public Vector3 targetOffset = Vector3.zero;

    private Vector3 _currentAngularVelocity = Vector3.zero;
    private Quaternion _rotationOffset = Quaternion.identity;
    private Quaternion _currentRotation;
    private Quaternion _targetRotation;
    private Quaternion _resetRotation;

    private bool _wasTargetNotNull = true;
    private float _weightTargetNotNull = 1f;

    [UsedImplicitly] private Vector3 _rotationOffsetEuler = Vector3.zero;
    public Vector3 RotationOffsetEuler
    {
        set
        {
            _rotationOffsetEuler = value;
            _rotationOffset = Quaternion.Euler(value);
        }
    }

    private void Start()
    {
        if (!bone)
        {
            bone = transform;
        }

        _currentRotation = bone!.localRotation;
        _resetRotation = bone.localRotation;
    }

    private void LateUpdate()
    {
        if (!bone) { return; }

        _weightTargetNotNull = Mathf.Lerp(
            _weightTargetNotNull,
            targetLookAt ? 1f : 0f,
            Time.deltaTime * 3f
        );

        if (!targetLookAt)
        {
            if (_wasTargetNotNull)
            {
                _resetRotation = bone!.localRotation;
                _wasTargetNotNull = false;
            }

            _targetRotation = _resetRotation;
        }
        else
        {
            _wasTargetNotNull = true;

            var lookPos = targetLookAt!.position + targetOffset;

            var animatedFace = bone!.rotation * Quaternion.Inverse(_rotationOffset);
            var up = customUpVector == Vector3.zero ? animatedFace * Vector3.up : customUpVector;

            var lookDir = lookPos - bone.position;
            if (useAngleLimits)
            {
                lookDir = ClampDirection(animatedFace * Vector3.forward, lookDir);
            }

            var worldRot = Quaternion.LookRotation(lookDir, up) * _rotationOffset;
            var localRot = bone.parent ? Quaternion.Inverse(bone.parent.rotation) * worldRot : worldRot;

            _targetRotation = localRot;
        }

        var st = targetLookAt ? smoothTime : resetSmoothTime;

        _currentRotation = SmoothDampQuaternion(
            _currentRotation,
            _targetRotation,
            ref _currentAngularVelocity,
            st
        );

        bone!.localRotation = Quaternion.Slerp(
            bone.localRotation,
            _currentRotation,
            weight * _weightTargetNotNull
        );
    }

    private Vector3 ClampDirection(Vector3 referenceForward, Vector3 desired)
    {
        YawPitch(referenceForward, out var refYaw, out var refPitch);
        YawPitch(desired, out var yaw, out var pitch);

        var yawDelta = Mathf.Clamp(Mathf.DeltaAngle(refYaw, yaw), minAngleLimits.y, maxAngleLimits.y);
        var pitchDelta = Mathf.Clamp(pitch - refPitch, minAngleLimits.x, maxAngleLimits.x);

        var clampedPitch = Mathf.Clamp(refPitch + pitchDelta, -89f, 89f);
        return Quaternion.Euler(clampedPitch, refYaw + yawDelta, 0f) * Vector3.forward;
    }

    private static void YawPitch(Vector3 direction, out float yaw, out float pitch)
    {
        var n = direction.normalized;
        yaw = Mathf.Atan2(n.x, n.z) * Mathf.Rad2Deg;
        pitch = -Mathf.Asin(Mathf.Clamp(n.y, -1f, 1f)) * Mathf.Rad2Deg;
    }

    private static Quaternion SmoothDampQuaternion(
        Quaternion current,
        Quaternion target,
        ref Vector3 angularVelocity,
        float smoothTime)
    {
        var dot = Quaternion.Dot(current, target);
        var sign = dot > 0f ? 1f : -1f;

        target.x *= sign;
        target.y *= sign;
        target.z *= sign;
        target.w *= sign;

        var resultEuler = new Vector3(
            Mathf.SmoothDampAngle(current.eulerAngles.x, target.eulerAngles.x, ref angularVelocity.x, smoothTime),
            Mathf.SmoothDampAngle(current.eulerAngles.y, target.eulerAngles.y, ref angularVelocity.y, smoothTime),
            Mathf.SmoothDampAngle(current.eulerAngles.z, target.eulerAngles.z, ref angularVelocity.z, smoothTime)
        );

        return Quaternion.Euler(resultEuler);
    }

}