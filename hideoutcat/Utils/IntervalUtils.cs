using UnityEngine;

namespace HideoutCat.Utils;

public static class IntervalUtils
{
    private static bool RandomShouldOccur(float avgIntervalSeconds, float deltaTime)
    {
        if (avgIntervalSeconds <= 0f) { return true; }

        var probability = deltaTime / avgIntervalSeconds;
        return Random.value < probability;
    }

    public static bool RandomShouldOccur(float avgIntervalSeconds)
    {
        return RandomShouldOccur(avgIntervalSeconds, Time.fixedDeltaTime);
    }
}