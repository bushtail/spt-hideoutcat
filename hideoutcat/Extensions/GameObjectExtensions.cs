using JetBrains.Annotations;
using UnityEngine;

namespace HideoutCat.Extensions;

public static class GameObjectExtensions
{
    [UsedImplicitly]
    public static T GetOrAddComponent<T>(this GameObject gameObject) where T : Component
    {
        return gameObject.TryGetComponent<T>(out var component) ? component : gameObject.AddComponent<T>();
    }
}