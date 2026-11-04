using HideoutCat.Animation;
using JetBrains.Annotations;
using UnityEngine;

namespace HideoutCat.CatData;

public class CatLookAt : MonoBehaviour
{
    private const string NeckPath = "RootNode/Arm_Cat/Skeleton/root_bone_01/Spine_base_02/spine_02_03/spine_03_04/spine_04_05/spine_05_06/neck_07";
    private const string HeadPath = NeckPath + "/head_08";

    private const float NeckYawLimit = 45f;
    private const float HeadYawLimit = 45f;

    private Camera? _cameraMain;
    private Transform? _targetLookAtDummy;

    private BoneLookAt? _constraintHead;
    private BoneLookAt? _constraintNeck;

    [UsedImplicitly]
    public bool Tracking =>
        _constraintNeck &&
        _constraintNeck!.targetLookAt;

    private void Start()
    {
        Init();
    }

    private void Init()
    {
        if (_constraintNeck) { return; }

        var neck = transform.Find(NeckPath);
        var head = transform.Find(HeadPath);

        if (!neck || !head)
        {
            Plugin.Log!.LogError("Error init CatLookAt! Can't find armature bone");
            return;
        }

        _constraintNeck = gameObject.AddComponent<BoneLookAt>();
        _constraintNeck.bone = neck;
        _constraintNeck.weight = 0.6f;
        _constraintNeck.RotationOffsetEuler = new Vector3(-15f, 0f, 0f);
        _constraintNeck.customUpVector = Vector3.up;
        _constraintNeck.useAngleLimits = true;
        _constraintNeck.maxAngleLimits = new Vector3(80f, NeckYawLimit, 20f);
        _constraintNeck.minAngleLimits = new Vector3(-50f, -NeckYawLimit, -20f);

        _constraintHead = gameObject.AddComponent<BoneLookAt>();
        _constraintHead.bone = head;
        _constraintHead.weight = 1f;
        _constraintHead.RotationOffsetEuler = new Vector3(-15f, 0f, 0f);
        _constraintHead.customUpVector = Vector3.up;
        _constraintHead.useAngleLimits = true;
        _constraintHead.maxAngleLimits = new Vector3(80f, HeadYawLimit, 20f);
        _constraintHead.minAngleLimits = new Vector3(-40f, -HeadYawLimit, -20f);
    }

    private Transform? PlayerCamera()
    {
        if (!_cameraMain)
        {
            _cameraMain = Camera.main;
        }

        return _cameraMain ? _cameraMain!.transform : null;
    }

    public void LookAt(Vector3 worldPos)
    {
        if (!_targetLookAtDummy)
        {
            _targetLookAtDummy = new GameObject("CatLookTarget").transform;
        }

        _targetLookAtDummy!.position = worldPos;
        SetLookTarget(_targetLookAtDummy);
    }

    public void SetLookTarget(Transform? targetLookAt)
    {
        Init();
        if (!_constraintNeck || !_constraintHead) { return; }

        _constraintNeck!.targetLookAt = targetLookAt;
        _constraintHead!.targetLookAt = targetLookAt;
    }

    public void SetLookAtPlayer()
    {
        var cam = PlayerCamera();
        if (cam)
        {
            SetLookTarget(cam);
        }
    }

    public bool IsLookingAtPlayer()
    {
        var cam = PlayerCamera();
        if (!_constraintNeck || !cam) { return false; }

        return _constraintNeck!.targetLookAt == cam;
    }
}
