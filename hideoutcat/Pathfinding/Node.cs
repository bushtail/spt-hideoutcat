using System;
using System.Collections.Generic;
using EFT;
using HideoutCat.Animation;
using Newtonsoft.Json;
using UnityEngine;

namespace HideoutCat.Pathfinding;

[Serializable]
public class Node
{
    public string? name;

    public Vector3 position;

    [JsonProperty("connectedTo")]
    public List<string>? connectedToNamesForSerialization;

    [JsonIgnore]
    public List<Node> connectedTo = [];

    public bool forwardJump;

    public EAreaType areaType;

    public int areaLevel;

    public float poseRotation;

    public List<AnimatorParameters> poseParameters = [];

    public override string ToString()
    {
        var poseSuffix = poseParameters.Count > 0 ? "(Pose)" : "";
        return $"{areaType}:{areaLevel}{poseSuffix}-{name}";
    }
}