using System;
using UnityEngine;

namespace HideoutCat.Animation;

[Serializable]
public class AnimatorParameters
{
    public string? name;
    public AnimatorControllerParameterType type;
    public bool boolValue;
    public float floatValue;
    public int intValue;

    public void Apply(Animator animator)
    {
        Plugin.Log!.LogInfo($"Setting animator parameter {name}");
        switch (type)
        {
            case AnimatorControllerParameterType.Float:
            {
                animator.SetFloat(name, floatValue);
                break;
            }
            case AnimatorControllerParameterType.Int:
            {
                animator.SetInteger(name, intValue);
                break;
            }
            case AnimatorControllerParameterType.Bool:
            {
                animator.SetBool(name, boolValue);
                break;
            }
            case AnimatorControllerParameterType.Trigger:
            default:
            {
                if (type == AnimatorControllerParameterType.Trigger)
                {
                    animator.SetTrigger(name);
                }

                break;
            }
        }
    }
}
