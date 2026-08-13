using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using EFT;
using UnityEngine;

namespace HideoutCat.Pathfinding;

[SuppressMessage("ReSharper", "UnusedMember.Global")]
public class Graph
{
    public readonly List<Node> nodes;
    private readonly List<Node> _waypointNodes;

    public Graph(List<Node> nodes)
    {
        this.nodes = nodes;
        _waypointNodes = [];

        foreach (var node in nodes)
        {
            if (node.areaType == EAreaType.NotSet)
            {
                _waypointNodes.Add(node);
            }
        }
    }
    
    public void AddNode(Node node)
    {
        nodes.Add(node);
    }
    
    public Node FindNodeByName(string name)
    {
        return nodes.Find(node => node.name == name);
    }
    
    public Node GetNodeClosestAny(Vector3 worldPos)
    {
        Node closest = null!;
        var closestDistSqr = float.MaxValue;

        foreach (var node in nodes)
        {
            var distSqr = (node.position - worldPos).sqrMagnitude;
            if (!(distSqr < closestDistSqr)) { continue; }

            closestDistSqr = distSqr;
            closest = node;
        }

        return closest;
    }

    public Node? GetNodeClosestWaypoint(Vector3 worldPos)
    {
        Node? closest = null;
        var closestDistSqr = float.MaxValue;

        foreach (var t in _waypointNodes)
        {
            var distSqr = (t.position - worldPos).sqrMagnitude;
            if (!(distSqr < closestDistSqr))
            {
                continue;
            }

            closestDistSqr = distSqr;
            closest = t;
        }

        return closest;
    }

    public static List<Node>? FindPathBFS(Node? startNode, Node? endNode)
    {
        if (startNode == null || endNode == null)
        {
            Plugin.Log!.LogError("Start or End node is null!");
            return null;
        }

        var queue = new Queue<Node>();
        var cameFrom = new Dictionary<Node, Node?>();

        queue.Enqueue(startNode);
        cameFrom[startNode] = null;

        while (queue.Count > 0)
        {
            var node = queue.Dequeue();

            if (node == endNode)
            {
                return ReconstructPath(cameFrom!, endNode);
            }

            foreach (var next in node.connectedTo)
            {
                if (cameFrom.ContainsKey(next)) { continue; }

                queue.Enqueue(next);
                cameFrom[next] = node;
            }
        }

        return null;
    }

    private static List<Node> ReconstructPath(Dictionary<Node, Node> cameFrom, Node endNode)
    {
        var path = new List<Node>();

        for (var node = endNode; node != null; node = cameFrom[node])
        {
            path.Add(node);
        }

        path.Reverse();
        return path;
    }

    public List<Node> FindDeadEndNodesByAreaTypeAndLevel(EAreaType areaType, int areaLevel)
    {
        Plugin.Log!.LogDebug($"Requesting dead end node for {areaType} (level {areaLevel})");

        var result = new List<Node>();

        foreach (var node in nodes)
        {
            if (node.poseParameters.Count > 0 && node.areaType == areaType && node.areaLevel == areaLevel)
            {
                result.Add(node);
            }
        }

        return result;
    }
}