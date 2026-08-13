using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using UnityEngine;

namespace HideoutCat.Extensions;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public static class IListExtensions
{
    [UsedImplicitly]
    public static void Shuffle<T>(this IList<T> list)
    {
        var count = list.Count;

        for (var i = 0; i < count - 1; i++)
        {
            var j = Random.Range(i, count);

            (list[j], list[i]) = (list[i], list[j]);
        }
    }
}