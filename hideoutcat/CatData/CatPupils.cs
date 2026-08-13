using UnityEngine;

namespace HideoutCat.CatData;

internal class CatPupils : MonoBehaviour
{
    private static readonly int Dilation = Shader.PropertyToID("_Dilation");

    private Material? _matEye;
    private float _internalValue;
    private float _targetValue;

    private void Start()
    {
        _matEye = GetComponentInChildren<SkinnedMeshRenderer>().materials[1];
        _targetValue = 0.3f;
    }

    private void Update()
    {
        if (!_matEye)
        {
            return;
        }

        _internalValue = Mathf.Lerp(_internalValue, _targetValue, Time.deltaTime * 3f);
        _matEye!.SetFloat(Dilation, _internalValue);
    }

    public void SetDilation(float dilation)
    {
        _targetValue = dilation;
    }
}