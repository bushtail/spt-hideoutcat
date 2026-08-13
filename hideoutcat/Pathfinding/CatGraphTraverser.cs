using System;
using System.Collections.Generic;
using EFT.Interactive;
using HideoutCat.Extensions;
using UnityEngine;

namespace HideoutCat.Pathfinding;

public class CatGraphTraverser : MonoBehaviour
{
    private static readonly int Thrust = Animator.StringToHash("Thrust");
    private static readonly int Turn = Animator.StringToHash("Turn");
    private static readonly int JumpingUp = Animator.StringToHash("JumpingUp");
    private static readonly int JumpingDown = Animator.StringToHash("JumpingDown");
    private static readonly int JumpingForward = Animator.StringToHash("JumpingForward");

    private Vector3 _prevPos;
    private Node? _currentNode;
    public List<Node>? currentPath;
    private int _currentPathIndex;

    private Animator? _animator;
    public Door[]? doors;

    private float _currentTurnVelocity;
    private float _currentThrustVelocity;

    private float _prevDistToDest;
    private float _jumpUpEndOffset = -0.5f;

    private Vector3 Velocity { get; set; }
    public float VelocityMagnitude => Velocity.magnitude / Time.deltaTime;
    public float DeltaY { get; private set; }

    private static Graph? PathfindingGraph => Plugin.CatGraph;

    public bool HasDestination => currentPath != null;

    public event Action<Node>? OnDestinationReached;
    public event Action<List<Node>>? OnNodeReached;
    public event Action? OnJumpAirEnd;

    public Door? DoorInTheWay { get; private set; }

    private void Start()
    {
        _animator = GetComponent<Animator>();
        doors = FindObjectsByType<Door>(FindObjectsSortMode.None);
    }

    public void ForgetDestination()
    {
        _currentNode = null;
        currentPath = null;
    }

    public void LayNewPath(Node targetNode)
    {
        _currentNode ??= PathfindingGraph!.GetNodeClosestWaypoint(transform.position);

        currentPath = Graph.FindPathBFS(_currentNode, targetNode);
        _currentPathIndex = 0;

        if (currentPath == null)
        {
            Plugin.Log!.LogError($"No Path Found from {_currentNode} to {targetNode}");
        }
    }

    private void Update()
    {
        if (!_animator) { return; }

        if (currentPath == null || _currentPathIndex >= currentPath.Count)
        {
            TickMovement(0f, 0f);
            return;
        }

        var node = currentPath[_currentPathIndex];
        var isFinalNode = _currentPathIndex == currentPath.Count - 1;

        var stillMoving =
            Vector3.Distance(transform.position, node.position) >= 0.1f ||
            _animator!.GetBool(JumpingUp) ||
            _animator.GetBool(JumpingDown);

        if (stillMoving)
        {
            return;
        }

        _currentNode = node;

        if (isFinalNode)
        {
            var angleDelta = Mathf.DeltaAngle(_currentNode.poseRotation, transform.eulerAngles.y);

            if (_currentNode.poseParameters.Count > 0 && Mathf.Abs(angleDelta) > 10f)
            {
                var turnDir = -Mathf.Sign(angleDelta);
                TickMovement(0f, turnDir);
                return;
            }

            _currentPathIndex++;
            var finalNode = currentPath[^1];
            currentPath = null!;

            Plugin.Log!.LogInfo("Reached final destination!");

            OnDestinationReached!.Invoke(finalNode);
        }
        else
        {
            _currentPathIndex++;
            Plugin.Log!.LogInfo("Set next node to: " + currentPath[_currentPathIndex].name);

            var remaining = new List<Node>();
            for (var i = _currentPathIndex; i < currentPath.Count; i++)
            {
                remaining.Add(currentPath[i]);
            }
            OnNodeReached!.Invoke(remaining);
        }
    }

    private void LateUpdate()
    {
        Velocity = transform.position - _prevPos;
        _prevPos = transform.position;

        if (currentPath == null || currentPath.Count == 0)
        {
            return;
        }

        if (_currentPathIndex < currentPath.Count)
        {
            Locomotion();
        }
        else
        {
            var y = transform.position.y;
            var targetY = Mathf.Lerp(y, currentPath[^1].position.y, Time.deltaTime * 3f);
            transform.SetPositionIndividualAxis(null, targetY);
        }

        DeltaY = transform.position.y - currentPath[Mathf.Min(_currentPathIndex, currentPath.Count - 1)].position.y;
    }

    private void TickMovement(float thrust, float turn)
    {
        if (!_animator) { return; }

        var smoothedThrust = Mathf.SmoothDamp(
            _animator!.GetFloat(Thrust),
            thrust,
            ref _currentThrustVelocity,
            0.3f
        );

        var smoothedTurn = Mathf.SmoothDamp(
            _animator.GetFloat(Turn),
            turn,
            ref _currentTurnVelocity,
            0.2f
        );

        _animator.SetFloat(Thrust, smoothedThrust);
        _animator.SetFloat(Turn, smoothedTurn);
    }

    private void Locomotion()
    {
        if (!_animator) { return; }

        var node = currentPath![_currentPathIndex];
        if (node == null) { throw new NullReferenceException(); }

        var targetPos = node.position;

        if (_animator!.GetBool(JumpingUp))
        {
            HandleJumpingUp();
            return;
        }

        if (_animator.IsInTransition(0))
        {
            var info = _animator.GetAnimatorTransitionInfo(0);

            if (info.IsName("JumpUpAir -> JumpUpEnd") ||
                info.IsName("JumpUpStart -> JumpUpEnd"))
            {
                var t = info.normalizedTime;
                var y = Mathf.Lerp(targetPos.y - _jumpUpEndOffset, targetPos.y, t);
                transform.SetPositionIndividualAxis(null, y);

                transform.position += transform.forward * (Time.deltaTime * (1f - t));
                return;
            }
        }

        if (_animator.GetBool(JumpingDown))
        {
            HandleJumpingDown();
            return;
        }

        if (_animator.GetBool(JumpingForward))
        {
            HandleJumpingForward();
            return;
        }

        var dir = (targetPos - transform.position).normalized;
        dir.y = 0f;

        var angle = Vector3.SignedAngle(transform.forward, dir, Vector3.up);
        var turn = angle.RemapClamped(-40f, 40f, -1f, 1f);
        var dist = Vector3.Distance(transform.position, targetPos);

        var thrust = ComputeThrust(node, angle, dist);

        transform.SetPositionIndividualAxis(
            null,
            Mathf.Lerp(transform.position.y, targetPos.y, Time.deltaTime * 3f)
        );

        if (_currentPathIndex == currentPath.Count - 1 && dist < 0.1f)
            return;

        DoorInTheWay = BlockedPathByDoor();
        if (DoorInTheWay)
        {
            thrust = 0f;
        }

        TickMovement(thrust, turn);
        _prevDistToDest = dist;
    }

    private float ComputeThrust(Node node, float angle, float dist)
    {
        var thrust = 1f;

        if (node.forwardJump && dist > 0.4f && Mathf.Abs(angle) < 7f)
        {
            _animator!.SetBool(JumpingForward, true);
            _animator.Update(0f);
            return 0f;
        }

        if (TargetIsAbove(node, angle))
        {
            _animator!.SetBool(JumpingUp, true);
            _animator.Update(0f);
            return 0f;
        }

        if (TargetIsBelow(node, angle))
        {
            _animator!.SetBool(JumpingDown, true);
            _animator.Update(0f);
            return 0f;
        }

        if (Mathf.Abs(angle) > 20f && dist < 0.5f)
            return 0f;

        if (!(Mathf.Abs(angle) < 30f)) { return thrust; }

        for (var i = _currentPathIndex; i < currentPath!.Count; i++)
        {
            var distance = Vector3.Distance(transform.position, currentPath[i].position);

            switch (distance)
            {
                case <= 3f:
                {
                    thrust = Mathf.Max(thrust, 1f);
                    break;
                }
                case <= 6f:
                {
                    thrust = Mathf.Max(thrust, 1.66f);
                    break;
                }
                case <= 8f:
                {
                    thrust = Mathf.Max(thrust, 2.55f);
                    break;
                }
                default:
                {
                    if (Mathf.Abs(angle) < 5f)
                    {
                        thrust = Mathf.Max(thrust, 3.6f);
                    }

                    break;
                }
            }
        }

        return thrust;
    }

    private bool TargetIsAbove(Node node, float angle)
    {
        return node.position.y > transform.position.y + 0.3f && Mathf.Abs(angle) < 10f;
    }

    private bool TargetIsBelow(Node node, float angle)
    {
        return node.position.y < transform.position.y - 0.3f && Mathf.Abs(angle) < 10f;
    }

    public bool IsMovement()
    {
        return !_animator ? throw new NullReferenceException("IsMovement") : _animator!.GetCurrentAnimatorStateInfo(0).IsName("Movement");
    }

    private Door? BlockedPathByDoor()
    {
        if (doors == null) { throw new NullReferenceException("BlockedPathByDoor"); }

        foreach (var door in doors)
        {
            if (!door || !door.gameObject.activeInHierarchy || door.DoorState == EDoorState.Open) { continue; }

            var dist = Vector3.Distance(door.transform.parent.position, transform.position);

            var dir = (door.transform.parent.position - transform.position).normalized;
            dir.y = 0f;

            var angle = Vector3.SignedAngle(transform.forward, dir, Vector3.up);

            if (dist < 2f && Mathf.Abs(angle) < 90f) { return door; }
        }

        return null;
    }

    private void HandleJumpingForward()
    {
        if (currentPath == null || !_animator) { return; }

        var node = currentPath[_currentPathIndex];
        var targetPos = node.position;

        var info = _animator!.GetCurrentAnimatorStateInfo(0);

        if (info.IsName("JumpForwardStart") || info.IsName("JumpForwardAir"))
        {
            var upSpeed = Time.deltaTime * 10f;
            var forwardSpeed = Time.deltaTime * 2f;

            if (info.IsName("JumpForwardStart"))
            {
                var time = info.normalizedTime.RemapClamped(0.75f, 1f, 0f, 1f);
                upSpeed = Mathf.Lerp(0f, upSpeed, time);
                forwardSpeed = Mathf.Lerp(0f, forwardSpeed, time);
            }

            var yDelta = targetPos.y - transform.position.y;
            upSpeed *= yDelta;

            transform.position += new Vector3(0f, upSpeed, 0f);
            transform.position += transform.forward * forwardSpeed;
        }

        var dist = Vector3.Distance(transform.position, targetPos);

        if (dist < 0.2f || dist > _prevDistToDest)
        {
            transform.SetPositionIndividualAxis(null, targetPos.y);

            _animator.SetBool(JumpingForward, false);
            _animator.Update(0f);

            OnJumpAirEnd?.Invoke();
        }

        _prevDistToDest = dist;
    }

    private void HandleJumpingUp()
    {
        if (currentPath == null || !_animator) { return; }

        var node = currentPath[_currentPathIndex];
        var targetPos = node.position;

        var info = _animator!.GetCurrentAnimatorStateInfo(0);

        var inAir =
            (info.IsName("JumpUpStart") && info.normalizedTime > 0.75f) ||
            info.IsName("JumpUpAir");

        if (!inAir) { return; }

        var upSpeed = Time.deltaTime * 3f;
        var forwardSpeed = Time.deltaTime;

        transform.position += new Vector3(0f, upSpeed, 0f);
        transform.position += transform.forward * forwardSpeed;

        if (!(transform.position.y > targetPos.y - 0.7f)) { return; }

        _animator.SetBool(JumpingUp, false);
        _animator.Update(0f);

        _jumpUpEndOffset = targetPos.y - transform.position.y;
        OnJumpAirEnd?.Invoke();
    }

    private void HandleJumpingDown()
    {
        if (!_animator || currentPath == null) { return; }

        var node = currentPath[_currentPathIndex];
        var targetPos = node.position;

        var info = _animator!.GetCurrentAnimatorStateInfo(0);

        if (info.IsName("JumpDownStart") || info.IsName("JumpDownAir"))
        {
            var downSpeed = Time.deltaTime * 3f;
            var forwardSpeed = Time.deltaTime;

            if (info.IsName("JumpDownStart"))
            {
                var t = info.normalizedTime.RemapClamped(0.75f, 1f, 0f, 1f);
                downSpeed = Mathf.Lerp(0f, downSpeed, t);
                forwardSpeed = Mathf.Lerp(0f, forwardSpeed, t);
            }
            else
            {
                downSpeed += info.normalizedTime * Time.deltaTime * 5f;
            }

            transform.position += new Vector3(0f, -downSpeed, 0f);
            transform.position += transform.forward * forwardSpeed;
        }

        if (!(transform.position.y < targetPos.y + 0.02f)) { return; }

        transform.SetPositionIndividualAxis(null, targetPos.y);
        _animator.SetBool(JumpingDown, false);
        _animator.Update(0f);

        if (_currentPathIndex < currentPath.Count - 2)
        {
            _currentPathIndex++;
        }

        OnJumpAirEnd?.Invoke();
    }
}