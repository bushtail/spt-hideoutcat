using HideoutCat.Animation;
using JetBrains.Annotations;
using UnityEngine;

namespace HideoutCat.CatData;

public class CatLookAt : MonoBehaviour
{
    private Camera? _cameraMain;
    private Transform? _targetLookAtDummy;

    private BoneLookAt? _constraintHead;
    private BoneLookAt? _constraintNeck;

    private Transform? _transformHead;
    private Transform? _transformNeck;

    private BoneLookAt? _boneLookAtNeck;
    private BoneLookAt? _boneLookAtHead;

    [UsedImplicitly]
    public bool Tracking =>
        _constraintNeck &&
        _constraintNeck!.targetLookAt;

    private void Start()
    {
        _boneLookAtHead = gameObject.AddComponent<BoneLookAt>();
        _boneLookAtNeck = gameObject.AddComponent<BoneLookAt>();

        _transformNeck = transform.Find("RootNode/Arm_Cat/Skeleton/root_bone_01/Spine_base_02/spine_02_03/spine_03_04/spine_04_05/spine_05_06/neck_07");
        _transformHead = transform.Find("RootNode/Arm_Cat/Skeleton/root_bone_01/Spine_base_02/spine_02_03/spine_03_04/spine_04_05/spine_05_06/neck_07/head_08");
        
        _cameraMain = Camera.main;
    }

    private void Init()
    {
        if (!_transformHead)
        {
            Plugin.Log!.LogError("Error init CatLookAt! Can't find armature bone");
            return;
        }

        _constraintNeck = _boneLookAtNeck;
        _constraintNeck!.bone = _transformNeck;
        _constraintNeck.weight = 0.6f;
        _constraintNeck.RotationOffsetEuler = new Vector3(-15f, 0f, 0f);
        _constraintNeck.customUpVector = Vector3.up;
        _constraintNeck.useAngleLimits = true;
        _constraintNeck.maxAngleLimits = new Vector3(80f, 20f, 20f);
        _constraintNeck.minAngleLimits = new Vector3(-50f, -20f, -20f);

        _constraintHead = _boneLookAtHead;
        _constraintHead!.bone = _transformHead;
        _constraintHead.weight = 1f;
        _constraintHead.RotationOffsetEuler = new Vector3(-15f, 0f, 0f);
        _constraintHead.customUpVector = Vector3.up;
        _constraintHead.useAngleLimits = true;
        _constraintHead.maxAngleLimits = new Vector3(80f, 20f, 20f);
        _constraintHead.minAngleLimits = new Vector3(-40f, -20f, -20f);
    }

    public void LookAt(Vector3 worldPos)
    {
        if (!_constraintNeck) { Init(); }

        if (!_targetLookAtDummy) { _targetLookAtDummy = new GameObject("CatLookTarget").transform; }

        _targetLookAtDummy!.position = worldPos;

        _constraintNeck!.targetLookAt = _targetLookAtDummy;
        _constraintHead!.targetLookAt = _targetLookAtDummy;
    }

    public void SetLookTarget(Transform? targetLookAt)
    {
        if (!targetLookAt) { return; }

        if (!_constraintNeck) { Init(); }

        _constraintNeck!.targetLookAt = targetLookAt;
        _constraintHead!.targetLookAt = targetLookAt;
    }

    public void SetLookAtPlayer()
    {
        if (_cameraMain)
        {
            SetLookTarget(_cameraMain!.transform);
        }
    }

    public bool IsLookingAtPlayer()
    {
        if (!_constraintNeck || !_cameraMain) { return false; }

        return _constraintNeck!.targetLookAt == _cameraMain!.transform;
    }
}