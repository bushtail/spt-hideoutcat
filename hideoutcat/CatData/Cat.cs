using System;
using System.Collections.Generic;
using Comfort.Common;
using EFT;
using EFT.CameraControl;
using EFT.Hideout;
using EFT.Interactive;
using HideoutCat.Extensions;
using HideoutCat.Patches.AreaScreenSubstratePatches;
using HideoutCat.Patches.HideoutPlayerOwnerPatches;
using HideoutCat.Pathfinding;
using HideoutCat.Utils;
using UnityEngine;
using Random = UnityEngine.Random;

namespace HideoutCat.CatData;

public class Cat : InteractableObject
{
    private static readonly int Fidget = Animator.StringToHash("Fidget");
    private static readonly int Defecating = Animator.StringToHash("Defecating");
    private static readonly int Sleeping = Animator.StringToHash("Sleeping");
    private static readonly int LyingSide = Animator.StringToHash("LyingSide");
    private static readonly int LyingBelly = Animator.StringToHash("LyingBelly");
    private static readonly int Sitting = Animator.StringToHash("Sitting");
    private static readonly int Crouching = Animator.StringToHash("Crouching");
    private static readonly int Eating = Animator.StringToHash("Eating");
    private static readonly int SharpeningHorizontal = Animator.StringToHash("SharpeningHorizontal");
    private static readonly int SharpeningVertical = Animator.StringToHash("SharpeningVertical");
    private static readonly int Grooming = Animator.StringToHash("Grooming");
    private static readonly int RunningInCircles = Animator.StringToHash("RunningInCircles");
    private static readonly int Caress = Animator.StringToHash("Caress");
    private static readonly int Meow1 = Animator.StringToHash("Meow");
    private static readonly int Random1 = Animator.StringToHash("Random");
    
    private Animator? _animator;

    private AreaData? _currentTargetArea;

    private CatLookAt? _lookAt;
    private CatEyelids? _eyelids;
    private CatPupils? _pupils;
    private CatAudio? _audio;
    private CatGraphTraverser? _catGraphTraverser;

    private bool _lastLyingPoseBelly;

    private ECatState _currentState;
    private ECatState _prevState;

    private float _fidgetingTime;

    private float _meowCooldown;

    private GamePlayerOwner? _owner;

    private Door? _doorGym;

    private Camera? _playerCam;
    private Camera? _camera;

    private bool Fidgeting => _fidgetingTime > 0f;

    private void Start()
    {
        _camera = Camera.main;
        _animator = GetComponent<Animator>();
        _lookAt = gameObject.GetOrAddComponent<CatLookAt>();
        _eyelids = gameObject.GetOrAddComponent<CatEyelids>();
        _pupils = gameObject.GetOrAddComponent<CatPupils>();
        _catGraphTraverser = gameObject.GetOrAddComponent<CatGraphTraverser>();
        _catGraphTraverser.OnDestinationReached += OnDestinationReached;
        _catGraphTraverser.OnNodeReached += OnNodeReached;
        _audio = gameObject.GetOrAddComponent<CatAudio>();
        var sphereCollider = new GameObject("InteractiveCollider").AddComponent<SphereCollider>();
        sphereCollider.radius = 0.4f;
        sphereCollider.center = new Vector3(0f, 0.15f, 0f);
        sphereCollider.gameObject.layer = 22;
        sphereCollider.transform.SetParent(transform, false);
        _owner = Singleton<GameWorld>.Instance.MainPlayer.GetComponent<GamePlayerOwner>();
        ResetAnimatorParams();
    }

    private void FixedUpdate()
    {
        _meowCooldown -= Time.fixedDeltaTime;

        if (!_animator || !_owner) { return; }

        _animator!.SetFloat(Random1, Random.value);
        HandleState();
        HandlePlayerInteraction();
        if (_prevState != _currentState)
        {
            Plugin.Log!.LogInfo($"New state: {_currentState}");
            _owner!.InteractionsChangedHandler();
        }
        _prevState = _currentState;
    }
    
    private void OnEnable()
    {
        SelectAreaPatch.OnAreaSelected += SetTargetArea;
        SelectAreaPatch.OnAreaLevelUpdated += OnAreaLevelUpdated;
        PrepareWorkoutPatch.OnPlayerPrepareWorkout += OnPlayerPrepareWorkout;
        StopWorkoutPatch.OnPlayerStopWorkout += GoToRandomArea;
    }
    
    private void OnDisable()
    {
        SelectAreaPatch.OnAreaSelected -= SetTargetArea;
        SelectAreaPatch.OnAreaLevelUpdated -= OnAreaLevelUpdated;
        PrepareWorkoutPatch.OnPlayerPrepareWorkout -= OnPlayerPrepareWorkout;
        StopWorkoutPatch.OnPlayerStopWorkout -= GoToRandomArea;
    }
    
    private void OnAreaLevelUpdated(AreaData areaData)
    {
        TeleportToClosestWaypoint();
        StartTraversingToArea(areaData);
    }
    
    
    private void OnDestinationReached(Node node)
    {
        if (!_animator || !_lookAt)
            return;

        foreach (var p in node.poseParameters)
        {
            p.Apply(_animator!);
        }

        _lookAt!.SetLookTarget(null);

        var hasSitting = false;
        foreach (var p in node.poseParameters)
        {
            if (p.name != "Sitting") { continue; }

            hasSitting = true;
            break;
        }

        if (hasSitting)
        {
            SetState(ECatState.Sitting, false);
            return;
        }

        var lyingSide = false;
        foreach (var p in node.poseParameters)
        {
            if (p.name != "LyingSide") { continue; }

            lyingSide = true;
            break;
        }

        var lyingBelly = false;
        foreach (var p in node.poseParameters)
        {
            if (p.name != "LyingBelly") { continue; }

            lyingBelly = true;
            break;
        }

        if (lyingSide || lyingBelly)
        {
            _lastLyingPoseBelly = lyingBelly;
            SetState(ECatState.Lying, false);
            return;
        }

        var hasEating = false;
        foreach (var p in node.poseParameters)
        {
            if (p.name != "Eating") { continue; }

            hasEating = true;
            break;
        }

        if (hasEating)
        {
            SetState(ECatState.Eating, false);
            return;
        }

        var hasDefecating = false;
        foreach (var p in node.poseParameters)
        {
            if (p.name != "Defecating") { continue; }

            hasDefecating = true;
            break;
        }

        if (hasDefecating)
        {
            SetState(ECatState.Defecating, false);
            return;
        }

        var hasGrooming = false;
        foreach (var p in node.poseParameters)
        {
            if (p.name != "Grooming") { continue; }

            hasGrooming = true;
            break;
        }

        if (hasGrooming)
        {
            SetState(ECatState.Grooming, false);
            return;
        }

        SetState(ECatState.Idle, false);
    }

    private Transform GetPlayerCam()
    {
        if (!_playerCam)
        {
            _playerCam = _camera;
        }
        return _playerCam!.transform;
    }
    
    public void SetTargetNode(Node node)
    {
        if (!_catGraphTraverser)
        {
            _catGraphTraverser = gameObject.GetOrAddComponent<CatGraphTraverser>();
        }
        
        Plugin.Log!.LogInfo("Set destination node to: " + node.name);
        var catGraphTraverser = _catGraphTraverser;
        if (catGraphTraverser)
        {
            catGraphTraverser!.LayNewPath(node);
        }
    }
    
    private void OnNodeReached(List<Node> nodesLeft)
    {
        if (!_lookAt) { return; }

        if (nodesLeft.Count > 1)
        {
            _lookAt!.LookAt(nodesLeft[Mathf.Min(1, nodesLeft.Count - 1)].position + new Vector3(0f, 0.3f, 0f));
        }
        else
        {
            _lookAt!.SetLookTarget(null);
        }
    }
    
    private void TeleportToClosestWaypoint()
    {
        ResetAnimatorParams();
        var nodeClosestWaypoint = Plugin.CatGraph!.GetNodeClosestWaypoint(transform.position);
        var vector = nodeClosestWaypoint != null ? new Vector3?(nodeClosestWaypoint.position) : null;
        if (vector != null)
        {
            transform.position = vector.Value;
        }
        SetState(ECatState.Idle, true);
    }
    
    private void GoToClosestWaypoint()
    {
        ResetAnimatorParams();
        SetTargetNode(Plugin.CatGraph!.GetNodeClosestWaypoint(transform.position)!);
    }
    
    private void OnPlayerPrepareWorkout()
    {
        if (!_doorGym)
        {
            _doorGym = null;
            foreach (var d in FindObjectsByType<Door>(FindObjectsSortMode.None))
            {
                if (d.Id != "door_bunker_2_00002") { continue; }

                _doorGym = d;
                break;
            }

            if (!_doorGym)
            {
                Plugin.Log!.LogError("Can't find the gym door! lol");
                return;
            }
        }

        if (_doorGym!.DoorState != EDoorState.Open) { return; }

        var groomingNodesList = new List<Node>();
        foreach (var n in Plugin.CatGraph!.nodes)
        {
            if (n.areaType == EAreaType.Gym && n is { areaLevel: 1, poseParameters.Count: > 0 } && n.poseParameters[0].name == "Grooming")
            {
                groomingNodesList.Add(n);
            }
        }
        var groomingNodes = groomingNodesList.ToArray();

        if (groomingNodes.Length == 0) { return; }

        var node = groomingNodes[Random.Range(0, groomingNodes.Length)];

        _lookAt!.SetLookTarget(null);
        _catGraphTraverser!.ForgetDestination();

        transform.position = node.position;
        transform.eulerAngles = new Vector3(0f, node.poseRotation, 0f);

        SetState(ECatState.Grooming, true);
    }
    
    private void StartFidget()
    {
        var fidgeting = Fidgeting;
        if (fidgeting) { return; }

        _fidgetingTime = 6f;
        _animator!.SetTrigger(Fidget);
        _lookAt!.SetLookTarget(null);
    }
    
    public void Meow()
    {
        if (IsBusy() || Fidgeting) { return; }
        if (_meowCooldown > 0f) { return; }

        _meowCooldown = 2f;
        _animator!.SetTrigger(Meow1);

        _audio!.Meow(DistanceToPlayer() < 5f ? EMeowType.Address : EMeowType.Far);
    }

    public void Pet()
    {
        if (!IsPettable()) { return; }

        _animator!.SetTrigger(Caress);
        _audio!.Purr();
    }
    
    public void WakeUp()
    {
        if (_currentState != ECatState.Sleeping) { return; }

        SetState(ECatState.Lying, true);
        StartFidget();
        _audio!.Meow(EMeowType.Short);
    }
    
    public bool IsSleeping()
    {
        return _currentState == ECatState.Sleeping;
    }
    
    public bool IsPettable()
    {
        var currentState = _currentState;
        return currentState == ECatState.Idle || currentState - ECatState.Sitting <= 1;
    }
    
    private void SetState(ECatState newState, bool applyAnimatorParams)
    {
        if (_currentState == newState) { return; }

        _prevState = _currentState;
        _currentState = newState;
        _owner!.InteractionsChangedHandler();

        if (!applyAnimatorParams) { return; }

        ResetAnimatorParams();

        switch (newState)
        {
            case ECatState.Sitting:
            {
                _animator!.SetBool(Sitting, true);
                break;
            }

            case ECatState.Lying:
            {
                _animator!.SetBool(_lastLyingPoseBelly ? LyingBelly : LyingSide, true);
                break;
            }

            case ECatState.Sleeping:
            { 
                _animator!.SetBool(_lastLyingPoseBelly ? LyingBelly : LyingSide, true);
                _animator.SetBool(Sleeping, true);
                break;
            }

            case ECatState.Eating:
            {
                _animator!.SetBool(Eating, true);
                break;
            }

            case ECatState.Defecating:
            {
                _animator!.SetBool(Defecating, true);
                break;
            }

            case ECatState.WaitingByDoor:
            {
                _animator!.SetBool(Sitting, true);
                _lookAt!.SetLookTarget(_catGraphTraverser!.DoorInTheWay!.transform.parent);
                break;
            }

            case ECatState.Grooming:
            {
                _animator!.SetBool(Grooming, true);
                break;
            }

            case ECatState.Idle:
            case ECatState.Moving:
            case ECatState.Sharpening:
            default:
            {
                throw new ArgumentOutOfRangeException(nameof(newState), newState, null);
            }
        }

        _animator.Update(0f);
    }

    private void HandleState()
    {
        if (_catGraphTraverser!.HasDestination)
        {
            ResetAnimatorParams();
            _currentState = ECatState.Moving;
            return;
        }

        if (Fidgeting)
        {
            HandleFidgetingState();
            return;
        }

        switch (_currentState)
        {
            case ECatState.Idle:
            {
                HandleIdleState();
                break;
            }

            case ECatState.Moving:
            {
                HandleMovingState();
                break;
            }

            case ECatState.Sitting:
            {
                HandleSittingState();
                break;
            }

            case ECatState.Lying:
            {
                HandleLyingState();
                break;
            }

            case ECatState.Sleeping:
            {
                HandleSleepingState();
                break;
            }

            case ECatState.Eating:
            {
                HandleEatingState();
                break;
            }

            case ECatState.Defecating:
            {
                HandleDefecatingState();
                break;
            }

            case ECatState.Sharpening:
            {
                HandleSharpeningState();
                break;
            }

            case ECatState.WaitingByDoor:
            {
                HandleWaitingByDoor();
                break;
            }

            case ECatState.Grooming:
            {
                HandleGroomingState();
                break;
            }
            default:
            {
                throw new ArgumentOutOfRangeException();
            }
        }
    }

    private void HandleFidgetingState()
    {
        _fidgetingTime -= Time.fixedDeltaTime;
        if (_fidgetingTime < 2f)
        {
            _animator!.ResetTrigger(Fidget);
        }
    }

    private void HandleMovingState()
    {
        if (_catGraphTraverser!.DoorInTheWay)
        {
            SetState(ECatState.WaitingByDoor, true);
        }
    }

    private void HandleIdleState()
    {
        if (IntervalUtils.RandomShouldOccur(20f))
        {
            StartFidget();
            return;
        }

        if (IntervalUtils.RandomShouldOccur(3f))
        {
            SetState(ECatState.Sitting, true);
            return;
        }

        if (IntervalUtils.RandomShouldOccur(10f))
        {
            GoToRandomArea();
        }
    }

    private void HandleWaitingByDoor()
    {
        if (IntervalUtils.RandomShouldOccur(3f))
        {
            Meow();
            return;
        }

        if (IntervalUtils.RandomShouldOccur(20f))
        {
            GoToRandomArea();
        }
    }

    private void HandleSittingState()
    {
        if (IntervalUtils.RandomShouldOccur(30f))
        {
            StartFidget();
            return;
        }

        if (IntervalUtils.RandomShouldOccur(3f) && !Fidgeting)
        {
            _lookAt!.SetLookAtPlayer();
            return;
        }

        if (IntervalUtils.RandomShouldOccur(30f))
        {
            GoToRandomArea();
        }
    }

    private void HandleLyingState()
    {
        if (IntervalUtils.RandomShouldOccur(60f))
        {
            SetState(ECatState.Sleeping, true);
            _lookAt!.SetLookTarget(null);
            return;
        }

        if (IntervalUtils.RandomShouldOccur(60f))
        {
            GoToRandomArea();
            return;
        }

        if (IntervalUtils.RandomShouldOccur(30f))
        {
            StartFidget();
            return;
        }

        if (IntervalUtils.RandomShouldOccur(5f) && !Fidgeting)
        {
            _lookAt!.SetLookAtPlayer();
        }
    }

    private void HandleSleepingState()
    {
        if (IntervalUtils.RandomShouldOccur(40f))
        {
            WakeUp();
        }
    }

    private void HandleEatingState()
    {
        if (IntervalUtils.RandomShouldOccur(15f))
        {
            GoToClosestWaypoint();
        }
    }
    private void HandleDefecatingState()
    {
        if (IntervalUtils.RandomShouldOccur(15f))
        {
            GoToClosestWaypoint();
        }
    }

    private void HandleGroomingState()
    {
        if (IntervalUtils.RandomShouldOccur(35f))
        {
            GoToClosestWaypoint();
        }
    }

    private void HandleSharpeningState()
    {
        if (IntervalUtils.RandomShouldOccur(15f))
        {
            GoToClosestWaypoint();
        }
    }
    
    private float DistanceToPlayer()
    {
        return Vector3.Distance(transform.position, GetPlayerCam().position);
    }
    
    private void HandlePlayerInteraction()
    {
        var close = DistanceToPlayer() < 3f;
        var looking = _lookAt!.IsLookingAtPlayer();

        if (looking && close && IntervalUtils.RandomShouldOccur(5f)) { Meow(); }

        if (IntervalUtils.RandomShouldOccur(65f)) { Meow(); }

        if (IsPlayerShiningFlashlightAtFace())
        {
            _eyelids!.SetClamp(0.8f);
            _pupils!.SetDilation(DistanceToPlayer().RemapClamped(0.3f, 2f, 0f, 0.6f));
        }
        else
        {
            if (_eyelids!.Mode > ECatEyelidMode.None)
            {
                _eyelids.Release();
            }

            _pupils!.SetDilation(looking ? 0.6f : 0.4f);
        }

        var blockingMovement = IsPlayerInTheWay() && _currentState == ECatState.Moving && Mathf.Abs(_catGraphTraverser!.DeltaY) < 0.01f;

        if (!blockingMovement) { return; }

        _catGraphTraverser!.ForgetDestination();
        SetState(ECatState.Idle, true);
        _lookAt.SetLookAtPlayer();

        if (IntervalUtils.RandomShouldOccur(4f))
        {
            SetState(ECatState.Sitting, true);
        }
    }
    private static bool IsPlayerShiningFlashlightAtFace()
    {
        return CameraManager.Instance != null && CameraManager.Instance.Flashlight && CameraManager.Instance.Flashlight.IsActive;
    }
    
    private bool IsPlayerInTheWay()
    {
        if (Singleton<GameWorld>.Instance.MainPlayer.PointOfView > 0) { return false; }

        var distance = Vector3.Distance(GetPlayerCam().position, transform.position);

        var dir = (GetPlayerCam().position - transform.position).normalized;
        dir.y = 0f;

        var angle = Vector3.SignedAngle(transform.forward, dir, Vector3.up);

        return distance < 2f && Mathf.Abs(angle) < 30f;
    }
    
    private void ResetAnimatorParams()
    {
        _animator!.SetBool(Defecating, false);
        _animator.SetBool(Sleeping, false);
        _animator.SetBool(LyingSide, false);
        _animator.SetBool(LyingBelly, false);
        _animator.SetBool(Sitting, false);
        _animator.SetBool(Crouching, false);
        _animator.SetBool(Eating, false);
        _animator.SetBool(SharpeningHorizontal, false);
        _animator.SetBool(SharpeningVertical, false);
        _animator.SetBool(Grooming, false);
        _animator.SetBool(RunningInCircles, false);
        _animator.ResetTrigger(Fidget);
        _animator.ResetTrigger(Meow1);
        _animator.ResetTrigger(Caress);
    }
    
    private bool IsBusy()
    {
        var currentState = _currentState;
        return currentState - ECatState.Sleeping <= 2;
    }
    
    private void GoToRandomArea()
    {
        if (IsBusy() || IsPlayerInTheWay()) { return; }

        var shuffledAreas = new List<AreaData>(Singleton<HideoutRepresentation>.Instance.AreaDatas);
        for (var i = shuffledAreas.Count - 1; i > 0; i--)
        {
            var j = Random.Range(0, i + 1);
            (shuffledAreas[i], shuffledAreas[j]) = (shuffledAreas[j], shuffledAreas[i]);
        }

        foreach (var area in shuffledAreas)
        {
            var nodes = Plugin.CatGraph!.FindDeadEndNodesByAreaTypeAndLevel(area.Template.Type, area.CurrentLevel);

            if (nodes.Count <= 0) { continue; }

            SetTargetArea(area);
            break;
        }
    }

    public void SetTargetArea(AreaData area)
    {
        if (area == _currentTargetArea) { return; }

        if (!IsBusy())
        {
            StartTraversingToArea(area);
        }
    }

    private void StartTraversingToArea(AreaData area)
    {
        _currentTargetArea = area;

        var nodes = Plugin.CatGraph!.FindDeadEndNodesByAreaTypeAndLevel(
            area.Template.Type, 
            area.CurrentLevel
        );

        if (nodes.Count == 0)
        {
            Plugin.Log!.LogInfo($"No available nodes for {area.Template.Type} level {area.CurrentLevel}");
            return;
        }

        var node = nodes[Random.Range(0, nodes.Count)];
        SetTargetNode(node);
    }

}