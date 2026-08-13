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
            var up = customUpVector == Vector3.zero ? bone!.up : customUpVector;

            var worldRot = Quaternion.LookRotation(lookPos - bone!.position, up);

            var localRot = bone.parent ? Quaternion.Inverse(bone.parent.rotation) * worldRot : worldRot;

            localRot *= _rotationOffset;

            if (useAngleLimits)
            {
                localRot = ClampRotation(localRot);
            }

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

    private Quaternion ClampRotation(Quaternion targetRotation)
    {
        var e = targetRotation.eulerAngles;

        e.x = NormalizeAngle(e.x);
        e.y = NormalizeAngle(e.y);
        e.z = NormalizeAngle(e.z);

        e.x = Mathf.Clamp(e.x, minAngleLimits.x, maxAngleLimits.x);
        e.y = Mathf.Clamp(e.y, minAngleLimits.y, maxAngleLimits.y);
        e.z = Mathf.Clamp(e.z, minAngleLimits.z, maxAngleLimits.z);

        return Quaternion.Euler(e);
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

    private static float NormalizeAngle(float angle)
    {
        while (angle > 180f) angle -= 360f;
        while (angle < -180f) angle += 360f;
        return angle;
    }
}