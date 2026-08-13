using JetBrains.Annotations;
using UnityEngine;

namespace HideoutCat.Utils;

public static class GameObjectUtils
{
    [UsedImplicitly]
    public static GameObject? FindGameObjectWithComponentAtPosition<T>(Vector3 position, float tolerance = 0.01f) where T : Component
    {
        foreach (var component in Object.FindObjectsOfType<T>())
        {
            if (component && Vector3.Distance(component.transform.position, position) <= tolerance)
            {
                return component.gameObject;
            }
        }

        Plugin.Log!.LogWarning($"GameObject with {typeof(T)} at position {position} not found");

        return null;
    }
}