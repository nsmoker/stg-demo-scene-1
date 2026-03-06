using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using Character = STGDemoScene1.Scripts.Characters.Character;

namespace STGDemoScene1.Scripts.AI;

/// A crowd AI Director cycles a group of NPCs through a defined set of tasks with durations and draw probabilities.
[GlobalClass]
[Tool]
public partial class CrowdAiDirector : Resource
{
    public struct CrowdAiCharacterState
    {
        public CrowdAiTask Task;
        public double RemainingDuration;
        public Action OnComplete = () => { };

        public CrowdAiCharacterState() { }
    }

    [Export]
    private Godot.Collections.Array<CrowdAiTask> _possibleTasks;

    public Godot.Collections.Array<CrowdAiTask> PossibleTasks
    {
        get => _possibleTasks;
        set
        {
            foreach (var character in _managedCharacters)
            {
                var state = _states[character.GetInstanceId()];
                _states[character.GetInstanceId()] = new CrowdAiCharacterState
                {
                    Task = state.Task,
                    RemainingDuration = 0.0f,
                    OnComplete = state.OnComplete
                };
            }
            _possibleTasks = value;
        }
    }

    [Export]
    public float WalkSpeed = 20.0f;

    private List<Character> _managedCharacters = [];
    private readonly Dictionary<ulong, CrowdAiCharacterState> _states = [];
    private readonly Dictionary<ulong, bool> _chairMap = [];
    private readonly Random _random = new();

    public List<Character> ManagedCharacters
    {
        get => _managedCharacters;
        set
        {
            _managedCharacters = [.. value];
            _states.Clear();
            foreach (Character c in _managedCharacters)
            {
                CrowdAiTask task = DrawRandomTask();
                var state = new CrowdAiCharacterState
                {
                    Task = task,
                    RemainingDuration = 0.0f
                };
                _states.Add(c.GetInstanceId(), state);
            }
        }
    }

    private CrowdAiTask DrawRandomTask()
    {
        float draw = _random.NextSingle();
        CrowdAiTask returnTask = new();
        foreach (CrowdAiTask task in PossibleTasks)
        {
            draw -= task.Probability;
            if (draw <= 0)
            {
                returnTask = task;
                break;
            }
        }

        return returnTask;
    }

    public CrowdAiTask GetTask(ulong instanceId) => _states[instanceId].Task;

    public void SetState(ulong instanceId, CrowdAiCharacterState state) => _states[instanceId] = state;

    public CrowdAiCharacterState GetState(ulong instanceId) => _states[instanceId];

    public void StartTask(CrowdAiTask task, Character character, StagfootScreen area)
    {
        var state = new CrowdAiCharacterState
        {
            Task = task,
            RemainingDuration = task.Duration
        };
        switch (task.Type)
        {
            case CrowdAiTaskType.WalkToRandomPoint:
                {
                    var targetPoint = character.ToLocal(area.GetRandomTraversablePoint());
                    character.WalkToPoint(targetPoint, onComplete: () => SetState(character.GetInstanceId(), new CrowdAiCharacterState
                    {
                        Task = task,
                        RemainingDuration = 0.0f
                    }), speed: WalkSpeed);
                    break;
                }
            case CrowdAiTaskType.Idle:
                {
                    character.SetIdle();
                    break;
                }
            case CrowdAiTaskType.FindOpenSeat:
                {
                    if (!character.IsSeated())
                    {
                        var openChairs = area.GetUnnamedFurnitureProps().Where(chair => chair is FurnitureProp
                        {
                            Occupied: false
                        } && (!_chairMap.ContainsKey(chair.GetInstanceId()) || !_chairMap[chair.GetInstanceId()]));
                        var staticBody2Ds = openChairs.ToList();
                        if (staticBody2Ds.Count != 0)
                        {
                            var chair = staticBody2Ds.MinBy(x => x.GlobalPosition.DistanceTo(character.GlobalPosition));
                            _chairMap[chair.GetInstanceId()] = true;
                            character.WalkToPoint(chair.GlobalPosition, () => character.SitOn((Prop) chair), speed: WalkSpeed);
                            state.OnComplete = () => _chairMap[chair.GetInstanceId()] = false;
                        }
                    }
                    break;
                }
            case CrowdAiTaskType.TalkToPartner:
                {
                    // Do not interrupt current conversations.
                    var possibleConversationPartners = _managedCharacters.Where(c =>
                    {
                        var t = GetTask(c.GetInstanceId());
                        return t.Type != CrowdAiTaskType.TalkToPartner || _states[c.GetInstanceId()].RemainingDuration <= 0.0f;
                    }).ToList();
                    if (possibleConversationPartners.Count > 0)
                    {
                        int partnerIndex = _random.Next(0, possibleConversationPartners.Count);
                        Character partnerInstance = possibleConversationPartners[partnerIndex];
                        CrowdAiCharacterState partnerState = GetState(partnerInstance.GetInstanceId());
                        // Don't pick ourselves as a conversation partner
                        if (partnerInstance.GetInstanceId().Equals(character.GetInstanceId()))
                        {
                            partnerIndex = (partnerIndex + 1) % possibleConversationPartners.Count;
                            partnerInstance = possibleConversationPartners[partnerIndex];
                            partnerState = GetState(partnerInstance.GetInstanceId());
                        }
                        partnerState.OnComplete?.Invoke();
                        SetState(partnerInstance.GetInstanceId(), state);

                        partnerInstance.WalkToCharacter(character, () =>
                        {
                            partnerInstance.SetTalking();
                            partnerInstance.SetFacing(partnerInstance.ToLocal(character.GlobalPosition));
                        }, WalkSpeed, 12.0f);
                        character.WalkToCharacter(partnerInstance, () =>
                        {
                            character.SetTalking();
                            character.SetFacing(character.ToLocal(partnerInstance.GlobalPosition));
                        }, WalkSpeed, 12.0f);
                    }

                    break;
                }
            case CrowdAiTaskType.FollowCrowdFlow:
                {
                    var randomField = area.FlowFields[task.Tag];
                    var flow = randomField.SampleFlowField(area.ToLocal(character.GlobalPosition));
                    character.WalkToPoint(character.GlobalPosition + flow.Normalized() * WalkSpeed, () =>
                    {
                        var characterState = GetState(character.GetInstanceId());
                        StartTask(new CrowdAiTask
                        {
                            Type = CrowdAiTaskType.FollowCrowdFlow,
                            Duration = characterState.RemainingDuration,
                            Tag = task.Tag
                        }, character, area);
                        if (!area.CheckBounds(character.Position))
                        {
                            character.GlobalPosition = area.ToGlobal(area.GetEdge() + new Vector2(0.0f, -100.0f + _random.NextSingle() * 200.0f));
                        }
                    }, 20.0f);
                    break;
                }
        }

        SetState(character.GetInstanceId(), state);
    }

    public void Process(double delta, StagfootScreen area)
    {
        foreach (Character c in _managedCharacters)
        {
            var state = GetState(c.GetInstanceId());
            state.RemainingDuration -= delta;

            if (state.RemainingDuration <= 0)
            {
                state.Task = DrawRandomTask();
                state.RemainingDuration = state.Task.Duration;
                StartTask(state.Task, c, area);
            }
            else
            {
                SetState(c.GetInstanceId(), state);
            }
        }
    }
}

