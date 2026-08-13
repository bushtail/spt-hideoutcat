using System;
using UnityEngine;

namespace HideoutCat.CatData;

public class CatEyelids : MonoBehaviour
{
    private const float AngleClosed = 20f;
    private const float AngleOpen = -42f;

    public ECatEyelidMode Mode { get; private set; }

    private Transform? _boneEyelidL;
    private Transform? _boneEyelidR;

    private float _overrideValue;
    private float _maxValue;
    private float _internalValue;
    private float _releasingTime;

    private void Start()
    {
        _boneEyelidL = transform.Find("RootNode/Arm_Cat/Skeleton/root_bone_01/Spine_base_02/spine_02_03/spine_03_04/spine_04_05/spine_05_06/neck_07/head_08/eyelid.L_014");

        _boneEyelidR = transform.Find("RootNode/Arm_Cat/Skeleton/root_bone_01/Spine_base_02/spine_02_03/spine_03_04/spine_04_05/spine_05_06/neck_07/head_08/eyelid.R_018");

        if (!_boneEyelidL || !_boneEyelidR)
        {
            Plugin.Log!.LogError("Error during init of cat eyelids - unable to find armature bone.");
        }
    }

    private void LateUpdate()
    {
        var angle = _boneEyelidL!.localEulerAngles.x;
        if (angle > 180f)
        {
            angle -= 360f;
        }

        var openness = Mathf.InverseLerp(AngleClosed, AngleOpen, angle);

        switch (Mode)
        {
            case ECatEyelidMode.None:
            {
                if (_releasingTime >= 1f)
                {
                    break;
                }

                _internalValue = Mathf.Lerp(_internalValue, openness, _releasingTime);
                _releasingTime += Time.deltaTime * 2f;
                break;
            }

            case ECatEyelidMode.Override:
            {
                _internalValue = Mathf.Lerp(_internalValue, _overrideValue, Time.deltaTime * 3f);
                break;
            }

            case ECatEyelidMode.Clamp:
            {
                var clamped = Mathf.Clamp(openness, 0f, _maxValue);
                _internalValue = Mathf.Lerp(_internalValue, clamped, Time.deltaTime * 3f);
                break;
            }

            default:
            {
                throw new ArgumentOutOfRangeException();
            }
        }

        var finalAngle = Mathf.Lerp(AngleClosed, AngleOpen, _internalValue);

        _boneEyelidL.localEulerAngles = new Vector3(finalAngle, 0f, 0f);
        _boneEyelidR!.localEulerAngles = new Vector3(finalAngle, 0f, 0f);
    }

    public void SetTarget(float openness)
    {
        Mode = ECatEyelidMode.Override;
        _overrideValue = openness;
    }

    public void SetClamp(float max)
    {
        Mode = ECatEyelidMode.Clamp;
        _maxValue = max;
    }

    public void Release()
    {
        if (Mode == ECatEyelidMode.None) { return; }

        Mode = ECatEyelidMode.None;
        _releasingTime = 0f;
    }
}