using UnityEngine;

namespace HideoutCat.Extensions;

public static class TransformExtensions
{
    public static void SetPositionIndividualAxis(this Transform transform, float? x = null, float? y = null, float? z = null)
    {
        var pos = transform.position;

        if (x.HasValue)
        {
            pos.x = x.Value;
        }

        if (y.HasValue)
        {
            pos.y = y.Value;
        }

        if (z.HasValue)
        {
            pos.z = z.Value;
        }

        transform.position = pos;
    }
}